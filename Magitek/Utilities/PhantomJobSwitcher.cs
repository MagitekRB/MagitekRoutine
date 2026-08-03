using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ff14bot;
using ff14bot.Managers;
using Buddy.Coroutines;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Models.OccultCrescent;
using Magitek.Utilities.Agents;
using Magitek.Logic.Roles;

namespace Magitek.Utilities
{
    public static class PhantomJobSwitcher
    {
        /// <summary>
        /// Throttling to prevent rapid repeated attempts when spells fail
        /// </summary>
        private static DateTime _lastAttemptTime = DateTime.MinValue;
        private static readonly TimeSpan AttemptCooldown = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Back-off latch so a character that genuinely lacks Inquiring Mind (Freelancer &lt; 15) isn't
        /// repeatedly flipped to Freelancer: set after a failed cast, cleared after a successful one.
        /// </summary>
        private static DateTime _inquiringMindRetryAfter = DateTime.MinValue;
        private static readonly TimeSpan InquiringMindRetryCooldown = TimeSpan.FromMinutes(5);
        /// <summary>
        /// Buffs Inquiring Mind has demonstrably failed to grant. It cannot buff a phantom job the character
        /// has not levelled, and that does not change until they level it — so without this the routine
        /// re-swaps to Freelancer and re-casts at every crystal chasing a buff it can never get.
        /// Self-healing: an entry is dropped the moment that buff is observed present.
        /// </summary>
        private static readonly HashSet<PhantomJobId> _inquiringMindCannotGrant = new HashSet<PhantomJobId>();

        private static int _inquiringMindFailures;
        private const int InquiringMindFailuresBeforeBackoff = 3;

        /// <summary>
        /// Maps phantom jobs to their corresponding knowledge crystal buff auras
        /// </summary>
        private static readonly Dictionary<PhantomJobId, KnowledgeCrystalBuff> KnowledgeCrystalBuffs = new()
        {
            // Bard (ID=6) -> Romeo's Ballad -> Romeo's Ballad aura
            {
                PhantomJobId.Bard,
                new KnowledgeCrystalBuff
                {
                    AuraId = OCAuras.RomeosBallad, // 4244
                    BuffName = "Romeo's Ballad",
                    JobName = "Bard",
                    RequiredJobLevel = 2
                }
            },
            // Knight (ID=1) -> Pray -> Enduring Fortitude aura
            {
                PhantomJobId.Knight,
                new KnowledgeCrystalBuff
                {
                    AuraId = OCAuras.EnduringFortitude, // 4233
                    BuffName = "Enduring Fortitude",
                    JobName = "Knight",
                    RequiredJobLevel = 2
                }
            },
            // Monk (ID=3) -> Counterstance -> Fleetfooted aura
            {
                PhantomJobId.Monk,
                new KnowledgeCrystalBuff
                {
                    AuraId = OCAuras.Fleetfooted, // 4239
                    BuffName = "Fleetfooted",
                    JobName = "Monk",
                    RequiredJobLevel = 3
                }
            },
            // Dancer (ID=15) -> Quickstep -> Quicker Step aura
            {
                PhantomJobId.Dancer,
                new KnowledgeCrystalBuff
                {
                    AuraId = OCAuras.QuickerStep, // 4799
                    BuffName = "Quicker Step",
                    JobName = "Dancer",
                    RequiredJobLevel = 2
                }
            }
        };

        /// <summary>
        /// Automatically switch phantom jobs and cast knowledge crystal buffs when near a crystal.
        /// If Freelancer is level 15 and Inquiring Mind is preferred, casts all buffs in one action.
        /// Otherwise falls back to individual job switching, skipping jobs below required level.
        /// Restores the original phantom job after completing all buffs.
        /// </summary>
        /// <returns>True if any action was taken</returns>
        public static async Task<bool> AutoSwitchForKnowledgeCrystalBuffs()
        {
            // Check if automatic switching is enabled
            if (!OccultCrescentSettings.Instance.EnableAutomaticPhantomJobSwitching)
                return false;

            // Must be in Occult Crescent
            if (!Core.Me.OnOccultCrescent())
                return false;

            // Must be out of combat
            if (Core.Me.InCombat)
                return false;

            // Throttle attempts to prevent rapid ping-ponging when spells fail
            var now = DateTime.Now;
            if (now - _lastAttemptTime < AttemptCooldown)
                return false;

            bool preferInquiring = OccultCrescentSettings.Instance.PreferInquiringMind;

            // All four crystal buffs we're currently missing. Inquiring Mind (Freelancer) grants them
            // together, so it is driven by the FULL missing set, decoupled from the per-job AutoSwitch
            // settings — those only gate the individual-swap fallback. This lets a player run Inquiring
            // Mind alone (all four individual swaps turned off) and still get buffed.
            var missingBuffs = new List<(PhantomJobId jobId, KnowledgeCrystalBuff buffInfo)>();
            var individualCandidates = new List<(PhantomJobId jobId, KnowledgeCrystalBuff buffInfo)>();

            foreach (var kvp in KnowledgeCrystalBuffs)
            {
                if (!NeedsBuff(kvp.Value))
                {
                    _inquiringMindCannotGrant.Remove(kvp.Key);   // we have it, so it is obtainable after all
                    continue;
                }

                missingBuffs.Add((kvp.Key, kvp.Value));

                bool jobSwitchEnabled = kvp.Key switch
                {
                    PhantomJobId.Knight => OccultCrescentSettings.Instance.AutoSwitchToKnightForEnduringFortitude,
                    PhantomJobId.Bard => OccultCrescentSettings.Instance.AutoSwitchToBardForRomeosBallad,
                    PhantomJobId.Monk => OccultCrescentSettings.Instance.AutoSwitchToMonkForFleetfooted,
                    PhantomJobId.Dancer => OccultCrescentSettings.Instance.AutoSwitchToDancerForQuickerStep,
                    _ => false
                };

                if (jobSwitchEnabled)
                    individualCandidates.Add((kvp.Key, kvp.Value));
            }

            // Nothing missing -> done. With Inquiring Mind off we can only act on the enabled individual
            // jobs, so bail if none are enabled.
            if (missingBuffs.Count == 0)
                return false;
            if (!preferInquiring && individualCandidates.Count == 0)
                return false;

            // Only now check if we're near a knowledge crystal (more expensive check)
            if (!OccultCrescent.IsNearKnowledgeCrystal())
                return false;

            // Update attempt timestamp now that we're actually going to try
            _lastAttemptTime = now;

            // Get the current phantom job before switching
            var originalPhantomJobId = GetCurrentPhantomJobId();

            // Track successful buffs and if any action was taken
            var successfulBuffs = new List<string>();
            bool anyActionTaken = false;

            // Fast path: Inquiring Mind (Freelancer) applies all four buffs at once. We do NOT hard-gate
            // on the phantom-job level memory read — it has proven unreliable (it can report a wrong
            // level even for a level-15 Freelancer that clearly has Inquiring Mind), which would silently
            // force the slow 4-job swap. Instead we attempt it and let the cast be the authority; a
            // genuine "can't cast" latches the fast path off briefly (see _inquiringMindRetryAfter).
            // Worth the swap only if Inquiring Mind can still grant at least one of the missing buffs.
            bool inquiringMindCanHelp = missingBuffs.Any(x => !_inquiringMindCannotGrant.Contains(x.jobId));

            if (preferInquiring && inquiringMindCanHelp && now >= _inquiringMindRetryAfter)
            {
                // A lapsed back-off earns the fast path its three strikes back. The counter is a static that
                // otherwise only clears on a successful cast, so once it reached the threshold it stayed
                // there for the life of the process and the very next transient failure re-armed another
                // five minutes — making the grace period something you get once per session instead of once
                // per back-off, which is the whole reason the counter was added.
                if (_inquiringMindRetryAfter != DateTime.MinValue)
                {
                    _inquiringMindRetryAfter = DateTime.MinValue;
                    _inquiringMindFailures = 0;
                }

                Logger.WriteInfo("[PhantomJobSwitcher] Trying Inquiring Mind (Freelancer) for all buffs");

                bool onFreelancer = true;
                if (GetCurrentPhantomJobId() != PhantomJobId.Freelancer)
                {
                    if (await SwitchToPhantomJob(PhantomJobId.Freelancer))
                    {
                        anyActionTaken = true;

                        // Wait for Inquiring Mind to actually become usable rather than sleeping a fixed
                        // interval and hoping. A phantom job swap is a server round-trip, not just an
                        // animation lock, so any constant is a race: 500ms lost it every time, and the
                        // animation lock (~770ms) still lost it perhaps a third of the time — the same
                        // character at Freelancer 19 would buff at one crystal and fail at the next.
                        // Waiting on the action's own castability is the precondition we actually need,
                        // and it also sidesteps GetCurrentPhantomJobId() reporting Freelancer for a job it
                        // simply does not recognise.
                        if (!await Coroutine.Wait(3000, () => OCSpells.InquiringMind.CanCast()))
                        {
                            // Counts toward the same back-off. A readiness timeout and a failed cast mean
                            // the same thing — Inquiring Mind did not happen — and a character that simply
                            // lacks it would otherwise swap to Freelancer and wait three seconds at every
                            // crystal forever, which is precisely what the back-off exists to stop.
                            RecordInquiringMindFailure(now, "never became castable");
                            onFreelancer = false;
                        }
                    }
                    else
                    {
                        Logger.WriteInfo("[PhantomJobSwitcher] Failed to switch to Freelancer, falling back to individual buffs");
                        onFreelancer = false;
                    }
                }

                if (onFreelancer)
                {
                    if (await OCSpells.InquiringMind.Cast(Core.Me))
                    {
                        Logger.WriteInfo("[PhantomJobSwitcher] Successfully cast Inquiring Mind");
                        await Casting.CheckForSuccessfulCast();
                        anyActionTaken = true;
                        _inquiringMindRetryAfter = DateTime.MinValue; // it works -> clear any back-off
                        _inquiringMindFailures = 0;

                        // Wait for the buffs to actually register before deciding on any individual
                        // fallback. Inquiring Mind's auras take a beat to land; checking NeedsBuff too
                        // early reports them still missing and we re-swap through all four jobs to
                        // re-apply what we already have. Returns as soon as every buff lands (all jobs
                        // levelled), or times out only for a buff it genuinely can't grant (unlevelled).
                        await Coroutine.Wait(3000, () => !missingBuffs.Exists(x => NeedsBuff(x.buffInfo)));

                        foreach (var (_, buffInfo) in missingBuffs)
                        {
                            if (!NeedsBuff(buffInfo))
                                successfulBuffs.Add(buffInfo.BuffName);
                        }

                        // Everything we could still be missing is now handled -> done.
                        if (!missingBuffs.Exists(x => NeedsBuff(x.buffInfo)))
                        {
                            await RestoreOriginalJob(originalPhantomJobId, anyActionTaken);
                            Logger.WriteInfo($"[PhantomJobSwitcher] Inquiring Mind applied all buffs: {string.Join(", ", successfulBuffs)}");
                            return true;
                        }

                        // Inquiring Mind can't grant buffs for jobs the player hasn't levelled. Remember
                        // which, so the next crystal does not repeat the swap for them, then let the enabled
                        // individual fallback cover whatever is still missing.
                        foreach (var (jobId, buffInfo) in missingBuffs)
                        {
                            if (NeedsBuff(buffInfo))
                                _inquiringMindCannotGrant.Add(jobId);
                        }

                        individualCandidates.RemoveAll(x => !NeedsBuff(x.buffInfo));
                        Logger.WriteInfo($"[PhantomJobSwitcher] Inquiring Mind applied some buffs; {individualCandidates.Count} to try individually");
                    }
                    else
                    {
                        RecordInquiringMindFailure(now, "didn't fire");
                    }
                }
            }

            // Individual buff path: switch to each enabled job that still needs its buff.
            foreach (var (neededJobId, neededBuffInfo) in individualCandidates)
            {
                // Check job level before switching to avoid wasted attempts
                byte jobLevel = OccultCrescentMemory.GetSupportJobLevel(neededJobId);
                if (OccultCrescentMemory.IsAvailable && jobLevel < neededBuffInfo.RequiredJobLevel)
                {
                    Logger.WriteInfo($"[PhantomJobSwitcher] Skipping {neededBuffInfo.JobName} (level {jobLevel}, need {neededBuffInfo.RequiredJobLevel})");
                    continue;
                }

                // Check if we're already in the needed phantom job
                var currentJob = GetCurrentPhantomJobId();
                bool alreadyInCorrectJob = currentJob == neededJobId;

                if (alreadyInCorrectJob)
                {
                    Logger.WriteInfo($"[PhantomJobSwitcher] Already in {neededBuffInfo.JobName}, casting {neededBuffInfo.BuffName}");
                }
                else
                {
                    Logger.WriteInfo($"[PhantomJobSwitcher] Switching to {neededBuffInfo.JobName} for {neededBuffInfo.BuffName}");

                    if (!await SwitchToPhantomJob(neededJobId))
                    {
                        Logger.WriteInfo($"[PhantomJobSwitcher] Failed to switch to {neededBuffInfo.JobName}, trying next job");
                        continue;
                    }

                    Logger.WriteInfo($"[PhantomJobSwitcher] Successfully switched to {neededBuffInfo.JobName}");
                    anyActionTaken = true;
                    await Coroutine.Wait(500, () => false);
                }

                if (await CastKnowledgeCrystalBuff(neededJobId))
                {
                    Logger.WriteInfo($"[PhantomJobSwitcher] Successfully cast {neededBuffInfo.BuffName}");
                    successfulBuffs.Add(neededBuffInfo.BuffName);
                    anyActionTaken = true;

                    await Casting.CheckForSuccessfulCast();
                    await Coroutine.Wait(500, () => false);
                }
                else
                {
                    Logger.WriteInfo($"[PhantomJobSwitcher] Failed to cast {neededBuffInfo.BuffName}, continuing");
                }
            }

            await RestoreOriginalJob(originalPhantomJobId, anyActionTaken);

            // Log summary
            if (successfulBuffs.Count > 0)
            {
                Logger.WriteInfo($"[PhantomJobSwitcher] Completed automatic buffing. Successfully cast: {string.Join(", ", successfulBuffs)}");
            }
            else if (anyActionTaken)
            {
                Logger.WriteInfo("[PhantomJobSwitcher] Attempted automatic buffing but no buffs were successfully cast");
            }

            return anyActionTaken;
        }

        /// <summary>
        /// Restore the original phantom job after buffing
        /// </summary>
        private static async Task RestoreOriginalJob(PhantomJobId originalPhantomJobId, bool anyActionTaken)
        {
            if (anyActionTaken &&
                OccultCrescentSettings.Instance.RestoreOriginalPhantomJobAfterAutoBuff &&
                GetCurrentPhantomJobId() != originalPhantomJobId)
            {
                Logger.WriteInfo($"[PhantomJobSwitcher] Restoring to original phantom job: {GetPhantomJobName(originalPhantomJobId)}");
                await Coroutine.Wait(500, () => false);

                if (await SwitchToPhantomJob(originalPhantomJobId))
                {
                    Logger.WriteInfo($"[PhantomJobSwitcher] Successfully restored to {GetPhantomJobName(originalPhantomJobId)}");
                }
                else
                {
                    Logger.WriteWarning($"[PhantomJobSwitcher] Failed to restore to {GetPhantomJobName(originalPhantomJobId)}");
                }
            }
        }

        /// <summary>
        /// Check if we need a specific buff based on its remaining time
        /// </summary>
        /// <param name="buffInfo">The buff information to check</param>
        /// <returns>True if the buff is needed</returns>
        private static bool NeedsBuff(KnowledgeCrystalBuff buffInfo)
        {
            var refreshMinutes = OccultCrescentSettings.Instance.PartyBuffRefreshMinutes;
            var msLeft = (int)(refreshMinutes * 60 * 1000);

            return !Core.Me.HasAura(buffInfo.AuraId, msLeft: msLeft);
        }

        /// <summary>
        /// Switch to the specified phantom job using memory injection result for immediate feedback
        /// </summary>
        /// <param name="jobId">The phantom job ID to switch to</param>
        /// <returns>True if the switch was successful and aura was applied</returns>
        private static async Task<bool> SwitchToPhantomJob(PhantomJobId jobId)
        {
            try
            {
                if (!AgentMKDSupportJobList.IsAvailable)
                {
                    Logger.WriteWarning("[PhantomJobSwitcher] Unable to automatically change phantom jobs");
                    return false;
                }

                // Call the memory injection and check immediate result (0x1 = success)
                long memoryCallResult = AgentMKDSupportJobList.SwitchToPhantomJob((byte)jobId);
                bool memoryCallSuccess = memoryCallResult == 0x1;

                if (!memoryCallSuccess)
                {
                    // Memory call failed immediately - job likely not unlocked or some other issue
                    Logger.WriteInfo($"[PhantomJobSwitcher] Memory call failed for phantom job {jobId} (likely not unlocked) result ({memoryCallResult})");
                }

                // Memory call succeeded (0x1), do a quick verification that aura actually applied
                return await VerifyPhantomJobAura(jobId);
            }
            catch (Exception ex)
            {
                Logger.WriteInfo($"[PhantomJobSwitcher] Error switching to phantom job {jobId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Quick verification that the phantom job aura was applied after successful memory call
        /// Since memory call succeeded (0x1), this should be nearly instant
        /// </summary>
        /// <param name="jobId">The phantom job ID we switched to</param>
        /// <returns>True if the aura was applied within short timeout, false otherwise</returns>
        private static async Task<bool> VerifyPhantomJobAura(PhantomJobId jobId)
        {
            const int timeoutMs = 5500; // 5.5 seconds
            const int checkIntervalMs = 50; // Check every 50ms for quick response
            int elapsedMs = 0;

            // Check immediately first (often succeeds on first check)
            var currentJobId = GetCurrentPhantomJobId();
            if (currentJobId == jobId)
            {
                return true;
            }

            // If not immediate, do a few quick checks
            while (elapsedMs < timeoutMs)
            {
                await Coroutine.Wait(checkIntervalMs, () => false);
                elapsedMs += checkIntervalMs;

                currentJobId = GetCurrentPhantomJobId();
                if (currentJobId == jobId)
                {
                    return true;
                }
            }

            Logger.WriteWarning($"[PhantomJobSwitcher] Aura verification timeout for {GetPhantomJobName(jobId)} after {timeoutMs}ms");
            return false;
        }

        /// <summary>
        /// Cast the knowledge crystal buff for the specified phantom job
        /// Reuses existing OccultCrescent spell casting logic
        /// </summary>
        /// <param name="jobId">The phantom job ID</param>
        /// <returns>True if the spell was cast successfully</returns>
        private static async Task<bool> CastKnowledgeCrystalBuff(PhantomJobId jobId)
        {
            try
            {
                return jobId switch
                {
                    PhantomJobId.Bard => await OCSpells.RomeosBallad.CastAura(Core.Me, OCAuras.RomeosBallad),
                    PhantomJobId.Knight => await OCSpells.Pray.CastAura(Core.Me, OCAuras.EnduringFortitude),
                    PhantomJobId.Monk => await OCSpells.Counterstance.CastAura(Core.Me, OCAuras.Fleetfooted),
                    PhantomJobId.Dancer => await OCSpells.Quickstep.CastAura(Core.Me, OCAuras.QuickerStep),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                Logger.WriteInfo($"[PhantomJobSwitcher] Error casting buff for {jobId}: {ex.Message}");
                return false;
            }
        }



        /// <summary>
        /// Get the current phantom job ID by reusing OccultCrescent logic
        /// </summary>
        /// <returns>The current phantom job ID, or None if no phantom job is active</returns>
        /// <summary>
        /// One attempt at Inquiring Mind did not produce buffs. Neither a failed cast nor a readiness
        /// timeout proves the character lacks the action — both also happen for passing reasons — so back
        /// off only once it has failed repeatedly, which a genuine absence does and a blip does not.
        /// </summary>
        private static void RecordInquiringMindFailure(DateTime now, string reason)
        {
            _inquiringMindFailures++;

            if (_inquiringMindFailures >= InquiringMindFailuresBeforeBackoff)
            {
                _inquiringMindRetryAfter = now.Add(InquiringMindRetryCooldown);
                Logger.WriteInfo($"[PhantomJobSwitcher] Inquiring Mind unavailable ({reason}); backing off, falling back to individual buffs");
            }
            else
            {
                Logger.WriteInfo($"[PhantomJobSwitcher] Inquiring Mind {reason} (attempt {_inquiringMindFailures}); will retry");
            }
        }

        private static PhantomJobId GetCurrentPhantomJobId()
        {
            // Reuse existing OccultCrescent logic instead of duplicating
            var currentJob = OccultCrescent.GetCurrentPhantomJob();
            return ConvertToPhantomJobId(currentJob);
        }

        /// <summary>
        /// Convert OccultCrescent.PhantomJob enum to PhantomJobId enum
        /// </summary>
        /// <param name="phantomJob">The OccultCrescent phantom job</param>
        /// <returns>The corresponding PhantomJobId</returns>
        private static PhantomJobId ConvertToPhantomJobId(OccultCrescent.PhantomJob phantomJob)
        {
            return phantomJob switch
            {
                OccultCrescent.PhantomJob.None => PhantomJobId.Freelancer, // Default to Freelancer
                OccultCrescent.PhantomJob.Knight => PhantomJobId.Knight,
                OccultCrescent.PhantomJob.Berserker => PhantomJobId.Berserker,
                OccultCrescent.PhantomJob.Monk => PhantomJobId.Monk,
                OccultCrescent.PhantomJob.Ranger => PhantomJobId.Ranger,
                OccultCrescent.PhantomJob.Samurai => PhantomJobId.Samurai,
                OccultCrescent.PhantomJob.Bard => PhantomJobId.Bard,
                OccultCrescent.PhantomJob.Geomancer => PhantomJobId.Geomancer,
                OccultCrescent.PhantomJob.TimeMage => PhantomJobId.TimeMage,
                OccultCrescent.PhantomJob.Cannoneer => PhantomJobId.Cannoneer,
                OccultCrescent.PhantomJob.Chemist => PhantomJobId.Chemist,
                OccultCrescent.PhantomJob.Oracle => PhantomJobId.Oracle,
                OccultCrescent.PhantomJob.PhantomThief => PhantomJobId.Thief,
                OccultCrescent.PhantomJob.Dancer => PhantomJobId.Dancer,
                OccultCrescent.PhantomJob.MysticKnight => PhantomJobId.MysticKnight,
                OccultCrescent.PhantomJob.Gladiator => PhantomJobId.Gladiator,
                OccultCrescent.PhantomJob.Ninja => PhantomJobId.Ninja,
                OccultCrescent.PhantomJob.WhiteMage => PhantomJobId.WhiteMage,
                OccultCrescent.PhantomJob.BlackMage => PhantomJobId.BlackMage,
                OccultCrescent.PhantomJob.Dragoon => PhantomJobId.Dragoon,
                OccultCrescent.PhantomJob.Summoner => PhantomJobId.Summoner,
                OccultCrescent.PhantomJob.BlueMage => PhantomJobId.BlueMage,
                OccultCrescent.PhantomJob.RedMage => PhantomJobId.RedMage,
                OccultCrescent.PhantomJob.Necromancer => PhantomJobId.Necromancer,
                _ => PhantomJobId.Freelancer // Default to Freelancer
            };
        }

        /// <summary>
        /// Get the display name for a phantom job ID
        /// </summary>
        /// <param name="jobId">The phantom job ID</param>
        /// <returns>The display name of the phantom job</returns>
        private static string GetPhantomJobName(PhantomJobId jobId)
        {
            return jobId switch
            {
                PhantomJobId.Freelancer => "Freelancer",
                PhantomJobId.Knight => "Knight",
                PhantomJobId.Berserker => "Berserker",
                PhantomJobId.Monk => "Monk",
                PhantomJobId.Ranger => "Ranger",
                PhantomJobId.Samurai => "Samurai",
                PhantomJobId.Bard => "Bard",
                PhantomJobId.Geomancer => "Geomancer",
                PhantomJobId.TimeMage => "Time Mage",
                PhantomJobId.Cannoneer => "Cannoneer",
                PhantomJobId.Chemist => "Chemist",
                PhantomJobId.Oracle => "Oracle",
                PhantomJobId.Thief => "Phantom Thief",
                PhantomJobId.Dancer => "Dancer",
                PhantomJobId.MysticKnight => "Mystic Knight",
                PhantomJobId.Gladiator => "Gladiator",
                _ => jobId.ToString()
            };
        }

        /// <summary>
        /// Information about a knowledge crystal buff
        /// </summary>
        private class KnowledgeCrystalBuff
        {
            public uint AuraId { get; set; }
            public string BuffName { get; set; }
            public string JobName { get; set; }
            public byte RequiredJobLevel { get; set; }
        }
    }
}