using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ff14bot;
using ff14bot.Managers;
using Buddy.Coroutines;
using Magitek.Extensions;
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
                    JobName = "Bard"
                }
            },
            // Knight (ID=1) -> Pray -> Enduring Fortitude aura
            {
                PhantomJobId.Knight,
                new KnowledgeCrystalBuff
                {
                    AuraId = OCAuras.EnduringFortitude, // 4233
                    BuffName = "Enduring Fortitude",
                    JobName = "Knight"
                }
            },
            // Monk (ID=3) -> Counterstance -> Fleetfooted aura
            {
                PhantomJobId.Monk,
                new KnowledgeCrystalBuff
                {
                    AuraId = OCAuras.Fleetfooted, // 4239
                    BuffName = "Fleetfooted",
                    JobName = "Monk"
                }
            }
        };

        /// <summary>
        /// Automatically switch phantom jobs and cast knowledge crystal buffs when near a crystal
        /// Restores the original phantom job after completing all buffs
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

            // Build list of needed buffs (cheap aura checks)
            var neededBuffs = new List<(PhantomJobId jobId, KnowledgeCrystalBuff buffInfo)>();

            foreach (var kvp in KnowledgeCrystalBuffs)
            {
                var phantomJobId = kvp.Key;
                var buffInfo = kvp.Value;

                // Check if this specific job switching is enabled
                bool jobSwitchEnabled = phantomJobId switch
                {
                    PhantomJobId.Knight => OccultCrescentSettings.Instance.AutoSwitchToKnightForEnduringFortitude,
                    PhantomJobId.Bard => OccultCrescentSettings.Instance.AutoSwitchToBardForRomeosBallad,
                    PhantomJobId.Monk => OccultCrescentSettings.Instance.AutoSwitchToMonkForFleetfooted,
                    _ => false
                };

                // Skip if this job switching is disabled
                if (!jobSwitchEnabled)
                    continue;

                // Check if we need this buff
                if (NeedsBuff(buffInfo))
                {
                    neededBuffs.Add((phantomJobId, buffInfo));
                }
            }

            // If we don't need any buffs, no point checking crystal location
            if (neededBuffs.Count == 0)
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

            // Try to cast all needed buffs
            foreach (var (neededJobId, neededBuffInfo) in neededBuffs)
            {
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

                    // Try to switch to the needed job
                    if (!await SwitchToPhantomJob(neededJobId))
                    {
                        Logger.WriteInfo($"[PhantomJobSwitcher] Failed to switch to {neededBuffInfo.JobName} (likely not unlocked), trying next job");
                        continue; // Try next job
                    }

                    Logger.WriteInfo($"[PhantomJobSwitcher] Successfully switched to {neededBuffInfo.JobName}");
                    anyActionTaken = true;
                    await Coroutine.Wait(500, () => false); // Brief delay to ensure buffs are fully applied
                }

                // Try to cast the buff - if it fails, that's okay (user has job but not required level)
                if (await CastKnowledgeCrystalBuff(neededJobId))
                {
                    Logger.WriteInfo($"[PhantomJobSwitcher] Successfully cast {neededBuffInfo.BuffName}");
                    successfulBuffs.Add(neededBuffInfo.BuffName);
                    anyActionTaken = true;

                    // Wait for the spell to actually finish casting before switching jobs
                    await Casting.CheckForSuccessfulCast();
                    await Coroutine.Wait(500, () => false); // Brief delay to ensure buffs are fully applied
                }
                else
                {
                    Logger.WriteInfo($"[PhantomJobSwitcher] Failed to cast {neededBuffInfo.BuffName} (user may not have required level), continuing");
                    // Continue to next buff - this is okay, user has job but not required level
                }
            }

            // Restore original phantom job only once at the end if we took any action
            if (anyActionTaken &&
                OccultCrescentSettings.Instance.RestoreOriginalPhantomJobAfterAutoBuff &&
                GetCurrentPhantomJobId() != originalPhantomJobId)
            {
                Logger.WriteInfo($"[PhantomJobSwitcher] Restoring to original phantom job: {GetPhantomJobName(originalPhantomJobId)}");
                await Coroutine.Wait(500, () => false); // Brief delay to ensure buffs are fully applied

                if (await SwitchToPhantomJob(originalPhantomJobId))
                {
                    Logger.WriteInfo($"[PhantomJobSwitcher] Successfully restored to {GetPhantomJobName(originalPhantomJobId)}");
                }
                else
                {
                    Logger.WriteWarning($"[PhantomJobSwitcher] Failed to restore to {GetPhantomJobName(originalPhantomJobId)}");
                }
            }

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
        }
    }
}