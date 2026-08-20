using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.BlackMage;
using Magitek.Logic.Roles;
using Magitek.Models.BlackMage;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.BlackMage;
using BlackMageRoutine = Magitek.Utilities.Routines.BlackMage;

namespace Magitek.Rotations
{
    public static class BlackMage
    {
        public static Task<bool> Rest()
        {
            var needRest = Core.Me.CurrentHealthPercent < BlackMageSettings.Instance.RestHealthPercent;
            return Task.FromResult(needRest);
        }

        public static async Task<bool> PreCombatBuff()
        {
            if (!BlackMageSettings.Instance.UsePreCombatTranspose)
                return false;

            if (await Buff.PreCombatUmbralSoul()) return true;
            if (await Buff.PreCombatTranspose()) return true;

            return false;
        }

        public static async Task<bool> Pull()
        {
            return await Combat();
        }

        public static async Task<bool> Heal()
        {
            return false;
        }

        public static async Task<bool> CombatBuff()
        {
            if (global::Magitek.Utilities.Combat.IsBoss() && await MagicDps.UsePotion(BlackMageSettings.Instance)) return true;
            return false;
        }

        // =========================================================================================
        // COMBAT METHOD - Unified On-The-Fly Dynamic Routine
        // =========================================================================================
        public static async Task<bool> Combat()
        {
            // Ensure we have a valid, attackable target before attempting any combat logic
            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            // 1. Universal / Survival: Evaluated every tick regardless of stance
            // Execute Limit Break if forced by user settings
            if (Aoe.ForceLimitBreak()) return true;

            // Automatically pop defensive cooldowns (Manaward, Addle, Surecast) based on incoming boss mechanics
            if (await CommonFightLogic.FightLogic_SelfShield(BlackMageSettings.Instance.FightLogicManaward, Spells.Manaward, castTimeRemainingMs: 19000)) return true;
            if (await MagicDps.FightLogic_Addle(BlackMageSettings.Instance)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(BlackMageSettings.Instance.FightLogicKnockback, Spells.Surecast, true, aura: Auras.Surecast)) return true;

            // 2. Dynamic Target & Stance Evaluation: Determine AoE vs Single-Target and current elemental stance
            bool isAoe = AoeControl.Enabled && BlackMageSettings.Instance.UseAoe && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies;
            bool inAstralFire = AstralStacks > 0;
            bool inUmbralIce = UmbralStacks > 0;
            bool isNeutral = !inAstralFire && !inUmbralIce;

            // 3. Movement Safety: Check if we have instant cast buffs active to allow casting on the run
            bool hasInstantCastBuff = Core.Me.HasAura(Auras.Triplecast) || Core.Me.HasAura(Auras.Swiftcast);

            // If moving without instant-cast buffs, use natural instant spells to maintain uptime
            if (MovementManager.IsMoving && !hasInstantCastBuff)
            {
                if (isAoe)
                {
                    // Instant AoE fillers
                    if (await Aoe.Thunder4()) return true;
                    if (await Aoe.Foul()) return true;
                }
                else
                {
                    // Instant Single-Target fillers
                    if (await SingleTarget.Xenoglossy()) return true;
                    if (await SingleTarget.Paradox()) return true;
                    if (await SingleTarget.Thunder3()) return true;

                    // Consume Firestarter proc if we have one
                    if (Core.Me.HasAura(Auras.FireStarter) && await SingleTarget.Fire3()) return true;
                }

                // If no instant spells are available, try to pop a movement buff
                // If no instant spells are available, try to pop a movement buff
                if (BlackMageSettings.Instance.TripleCast && Spells.Triplecast.IsKnownAndReady()) return await Spells.Triplecast.Cast(Core.Me);
                if (Spells.Swiftcast.IsKnownAndReady()) return await Spells.Swiftcast.Cast(Core.Me);

                // Safely yield the tick so the bot's navigation system can move the character
                return false;
            }

            // 4. Off-Global Cooldowns: Use buffs and oGCDs independently of our current elemental stance
            if (await Buff.Amplifier()) return true;
            if (await Buff.Triplecast()) return true;
            if (await Buff.Swiftcast()) return true;
            if (await Buff.LeyLines()) return true;
            if (await Buff.Retrace()) return true;
            if (await Buff.ManaFont()) return true;

            // =========================================================
            // 5. ASTRAL FIRE PHASE: The primary damage phase. Burns MP to deal heavy damage.
            // =========================================================
            if (inAstralFire)
            {
                if (isAoe)
                {
                    // AoE Recovery: Force Flare to reach Astral Fire III if we somehow dropped stacks or transposed
                    if (AstralStacks < 3 && Core.Me.CurrentMana >= 800)
                    {
                        if (await Aoe.Flare()) return true;
                        if (Spells.Flare.IsKnownAndReady()) return await Spells.Flare.Cast(Core.Me.CurrentTarget);
                    }

                    // Determine the minimum MP required to continue the AoE Main Phase (Flare takes 800, Fire II takes 3000)
                    int minAoeMp = Spells.Flare.IsKnown() ? 800 : 3000;

                    // AoE Main Phase: Spam Flare or Fire II as long as we have the MP to support it
                    if (Core.Me.CurrentMana >= minAoeMp)
                    {
                        // Use Flare if known and we meet the MP/Heart requirements
                        if (await Aoe.Flare()) return true;
                        if (Spells.Flare.IsKnownAndReady() && (Core.Me.CurrentMana >= 800 || UmbralHearts > 0))
                            return await Spells.Flare.Cast(Core.Me.CurrentTarget);

                        // Fallback to Fire 2 / High Fire 2 if Flare isn't known
                        if (await Aoe.Fire2()) return true;
                        if (Spells.Fire2.IsKnownAndReady()) return await Spells.Fire2.Cast(Core.Me.CurrentTarget);

                        return false;
                    }
                    // AoE Finisher Phase: MP is depleted. Execute finishers, weave, and swap to ice.
                    else
                    {
                        // 1. Flare Star: Dump 6 Astral Soul stacks if we have them
                        if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                        {
                            if (await Aoe.FlareStar()) return true;
                            if (Spells.FlareStar.IsKnownAndReady()) return await Spells.FlareStar.Cast(Core.Me.CurrentTarget);
                            return true; // HOLD TICK
                        }

                        // FIXED: Now properly calling Ether helper so the setting actually works!
                        if (await Aoe.UseAoeEther()) return true;

                        // 2. Weaves: Maintain Thunder DoT and use Foul during the instant cast window
                        if (await Aoe.Thunder4()) return true;
                        if (await Aoe.Foul()) return true;

                        // FIXED: Now properly calling Transpose helper to swap to Umbral Ice!
                        if (await Aoe.AoeTranspose()) return true;
                        return true; // HOLD TICK
                    }
                }
                else
                {
                    // Single-Target Recovery: Cast Fire I or Fire III to reach Astral Fire III if we dropped Enochian or Transposed
                    if (AstralStacks < 3)
                    {
                        if (await SingleTarget.Fire3()) return true;
                        if (Spells.Fire3.IsKnownAndReady()) return await Spells.Fire3.Cast(Core.Me.CurrentTarget);
                        if (Spells.Fire.IsKnownAndReady()) return await Spells.Fire.Cast(Core.Me.CurrentTarget);
                    }

                    // Cast Paradox whenever available to refresh the Astral Fire timer
                    if (await SingleTarget.Paradox()) return true;

                    // Calculate MP threshold: 2400 to fit Despair + Fire IV, or 1600 for just Fire IV if Despair isn't known
                    int minStMp = Spells.Despair.IsKnown() ? 2400 : 1600;

                    // Single-Target Main Phase: Maintain DoTs and spam Fire IV while MP allows
                    if (Core.Me.CurrentMana >= minStMp)
                    {
                        // Maintain Thunder and prevent Polyglot overcap during Main Phase
                        if (await SingleTarget.Thunder3()) return true;
                        if (await SingleTarget.Xenoglossy()) return true;

                        // Primary nuke
                        if (await SingleTarget.Fire4()) return true;
                        if (Spells.Fire4.IsKnownAndReady()) return await Spells.Fire4.Cast(Core.Me.CurrentTarget);

                        // Consume Firestarter procs if generated
                        if (await SingleTarget.Fire3()) return true;

                        // Fallback to basic Fire if Fire IV isn't known yet
                        if (await SingleTarget.Fire()) return true;
                        if (Spells.Fire.IsKnownAndReady()) return await Spells.Fire.Cast(Core.Me.CurrentTarget);

                        return false;
                    }
                    // Single-Target Finisher Phase: MP is too low for Fire IV. Dump remaining MP and transition.
                    else
                    {
                        // 1. Despair: Consumes all remaining MP (Minimum 800 MP required)
                        if (BlackMageSettings.Instance.Despair && Spells.Despair.IsKnown() && Core.Me.CurrentMana >= 800)
                        {
                            if (await SingleTarget.Despair()) return true;
                            if (Spells.Despair.IsKnownAndReady()) return await Spells.Despair.Cast(Core.Me.CurrentTarget);
                        }

                        // 2. Flare Star: Cast for 0 MP, Consumes 6 Astral Soul Stacks
                        if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                        {
                            if (await Aoe.FlareStar()) return true;
                            if (Spells.FlareStar.IsKnownAndReady()) return await Spells.FlareStar.Cast(Core.Me.CurrentTarget);
                            return true; // HOLD TICK FOR FLARE STAR
                        }

                        // 3. Blizzard III Transition: Costs 0 MP under Astral Fire III to swap to Umbral Ice
                        if (await SingleTarget.Blizzard3()) return true;
                        if (Spells.Blizzard3.IsKnownAndReady()) return await Spells.Blizzard3.Cast(Core.Me.CurrentTarget);

                        // Low-level fallback if Blizzard III isn't known
                        if (!Spells.Blizzard3.IsKnown() && Spells.Transpose.IsKnownAndReady())
                            return await Spells.Transpose.Cast(Core.Me);

                        return true; // HOLD TICK FOR TRANSITION
                    }
                }
            }
            // =========================================================
            // 6. UMBRAL ICE PHASE: The recovery phase. Restores MP and secures Umbral Hearts.
            // =========================================================
            else if (inUmbralIce)
            {
                if (isAoe)
                {
                    // Secure Umbral Hearts using Freeze (grants 3 hearts). Hold the tick to account for travel time.
                    if (Spells.Freeze.IsKnown() && UmbralHearts < 3 && (!MovementManager.IsMoving || hasInstantCastBuff))
                    {
                        if (Core.Me.CurrentTarget != null)
                        {
                            if (await Aoe.Freeze()) return true;
                            if (Spells.Freeze.IsKnownAndReady()) return await Spells.Freeze.Cast(Core.Me.CurrentTarget);
                        }
                        return true; // HOLD TICK: Wait for Freeze travel time! Never fall through.
                    }

                    // Once Hearts are secured (or MP is full at low levels), weave instant spells and Transpose back to Fire
                    if (UmbralHearts == 3 || Core.Me.CurrentMana >= 10000 || Core.Me.CurrentMana == Core.Me.MaxMana)
                    {
                        if (await Aoe.Thunder4()) return true;
                        if (await Aoe.Foul()) return true;

                        if (Spells.Transpose.IsKnownAndReady()) return await Spells.Transpose.Cast(Core.Me);
                        return true; // HOLD TICK FOR TRANSITION
                    }

                    // Low-Level AoE Filler: Use Umbral Soul or Blizzard II until MP is full (< Lv 58)
                    if (!Spells.Freeze.IsKnown())
                    {
                        if (await Buff.UmbralSoul()) return true;
                        if (await Aoe.Blizzard2()) return true;
                        if (Spells.Blizzard2.IsKnownAndReady()) return await Spells.Blizzard2.Cast(Core.Me.CurrentTarget);
                    }

                    return true; // Hold tick to prevent dropping combat
                }
                else
                {
                    // Single-Target Recovery: Ensure we reach Umbral Ice III for maximum MP regen speed
                    if (UmbralStacks < 3)
                    {
                        if (await SingleTarget.Blizzard3()) return true;
                        if (Spells.Blizzard3.IsKnownAndReady()) return await Spells.Blizzard3.Cast(Core.Me.CurrentTarget);
                        return true; // HOLD TICK FOR B3
                    }

                    // Secure Umbral Hearts using Blizzard 4 (grants 3 hearts and instantly restores 10k MP on hit)
                    if (Spells.Blizzard4.IsKnown() && UmbralHearts < 3 && (!MovementManager.IsMoving || hasInstantCastBuff))
                    {
                        if (await SingleTarget.Blizzard4()) return true;
                        if (Spells.Blizzard4.IsKnownAndReady()) return await Spells.Blizzard4.Cast(Core.Me.CurrentTarget);
                        return true; // HOLD TICK: Wait for B4 travel time! Never fall through.
                    }

                    // Main Ice Spells & Procs: Refresh Thunder DoT, dump Xenoglossy, and cast Paradox
                    if (await SingleTarget.Thunder3()) return true;
                    if (await SingleTarget.Xenoglossy()) return true;
                    if (await SingleTarget.Paradox()) return true;

                    // Transition to Fire: Dawntrail removed passive MP ticks. Use full Hearts or full MP as the trigger.
                    if (UmbralHearts == 3 || Core.Me.CurrentMana >= 10000 || Core.Me.CurrentMana == Core.Me.MaxMana)
                    {
                        if (await SingleTarget.Fire3()) return true;
                        if (Spells.Fire3.IsKnownAndReady()) return await Spells.Fire3.Cast(Core.Me.CurrentTarget);
                        return true; // HOLD TICK FOR TRANSITION
                    }

                    // Low-Level Filler: Spam Blizzard I until MP passively ticks back to full (< Lv 58)
                    if (!Spells.Blizzard4.IsKnown())
                    {
                        if (await SingleTarget.Blizzard()) return true;
                        if (Spells.Blizzard.IsKnownAndReady()) return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);
                    }

                    return true; // Hold tick to prevent dropping combat
                }
            }
            // =========================================================
            // 7. NEUTRAL PHASE: Opening of combat or recovering from dropping Enochian entirely
            // =========================================================
            else if (isNeutral)
            {
                if (isAoe)
                {
                    // Start with Blizzard II / High Blizzard II if we have zero stacks
                    if (AstralStacks == 0 && UmbralStacks == 0)
                    {
                        if (await Aoe.Blizzard2()) return true;
                        if (Spells.Blizzard2.IsKnownAndReady()) return await Spells.Blizzard2.Cast(Core.Me.CurrentTarget);
                    }

                    if (Spells.Transpose.IsKnownAndReady() && await Spells.Transpose.Cast(Core.Me)) return true;
                    if (await Aoe.Freeze()) return true;
                    if (Spells.Freeze.IsKnownAndReady()) return await Spells.Freeze.Cast(Core.Me.CurrentTarget);
                    // AoE Neutral Recovery: Apply Thunder and cast Freeze to enter Umbral Ice
                    if (await Aoe.Thunder4()) return true;
                    if (Spells.Transpose.IsKnownAndReady() && await Spells.Transpose.Cast(Core.Me)) return true;
                    if (await Aoe.Freeze()) return true;
                    if (Spells.Freeze.IsKnownAndReady()) return await Spells.Freeze.Cast(Core.Me.CurrentTarget);
                }
                else
                {
                    // Single-Target Neutral Recovery: Cast Blizzard 3 or Fire 3 to immediately enter a stance
                    if (await SingleTarget.Blizzard3()) return true;
                    if (Spells.Blizzard3.IsKnownAndReady()) return await Spells.Blizzard3.Cast(Core.Me.CurrentTarget);

                    if (await SingleTarget.Fire3()) return true;
                    if (await SingleTarget.Blizzard()) return true;
                    if (await SingleTarget.Fire()) return true;
                }
                return false;
            }

            return false;
        }

        public static async Task<bool> PvP()
        {
            BlackMageRoutine.RefreshVars();

            if (await CommonPvp.CommonTasks(BlackMageSettings.Instance)) return true;

            // Limit Break. The Soul Resonance follow-up (Flare/Frost Star) is held behind GuardCheck because its
            // consumed-buff window outlasts Guard — better to wait Guard out than waste it into 99% mitigation.
            if (CommonPvp.ShouldUseBurst() && !CommonPvp.GuardCheck(BlackMageSettings.Instance))
            {
                if (await Pvp.SoulResonancePvp()) return true;
            }

            // Elemental Weave
            if (CommonPvp.ShouldUseBurst())
            {
                if (await Pvp.ElementalWeave()) return true;
            }

            if (CommonPvp.ShouldUseBurst() && !CommonPvp.GuardCheck(BlackMageSettings.Instance))
            {
                // Utility actions
                if (await Pvp.Lethargy()) return true;

                // Main rotation
                if (await Pvp.Paradox()) return true;
                if (await Pvp.Xenoglossy()) return true;
            }

            // AoE and basic attacks (ungated)
            if (await Pvp.Burst()) return true;
            if (await Pvp.Fire()) return true;
            if (await Pvp.Blizzard()) return true;
            return false;
        }
    }
}