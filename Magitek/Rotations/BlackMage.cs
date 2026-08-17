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
            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            // 1. Universal / Survival
            if (Aoe.ForceLimitBreak()) return true;
            if (await CommonFightLogic.FightLogic_SelfShield(BlackMageSettings.Instance.FightLogicManaward, Spells.Manaward, castTimeRemainingMs: 19000)) return true;
            if (await MagicDps.FightLogic_Addle(BlackMageSettings.Instance)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(BlackMageSettings.Instance.FightLogicKnockback, Spells.Surecast, true, aura: Auras.Surecast)) return true;

            // 2. Dynamic Target & Stance Evaluation
            bool isAoe = BlackMageSettings.Instance.UseAoe && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies;
            bool inAstralFire = AstralStacks > 0;
            bool inUmbralIce = UmbralStacks > 0;
            bool isNeutral = !inAstralFire && !inUmbralIce;
            
            // 3. Movement Safety (Respects Swiftcast/Triplecast)
            bool hasInstantCastBuff = Core.Me.HasAura(Auras.Triplecast) || Core.Me.HasAura(Auras.Swiftcast);

            if (MovementManager.IsMoving && !hasInstantCastBuff)
            {
                if (isAoe)
                {
                    if (await Aoe.Thunder4()) return true;
                    if (await Aoe.Foul()) return true;
                }
                else
                {
                    if (await SingleTarget.Xenoglossy()) return true;
                    if (await SingleTarget.Paradox()) return true;
                    if (await SingleTarget.Thunder3()) return true;
                    if (Core.Me.HasAura(Auras.FireStarter) && await SingleTarget.Fire3()) return true;
                }

                if (Spells.Triplecast.IsKnownAndReady()) return await Spells.Triplecast.Cast(Core.Me);
                if (Spells.Swiftcast.IsKnownAndReady()) return await Spells.Swiftcast.Cast(Core.Me);
                
                return false; // Safely yield tick so bot can navigate
            }

            // 4. Off-Global Cooldowns
            if (await Buff.Amplifier()) return true;
            if (await Buff.Triplecast()) return true;
            if (await Buff.Swiftcast()) return true; 
            if (await Buff.LeyLines()) return true;
            if (await Buff.Retrace()) return true;
            if (await Buff.ManaFont()) return true;

            // =========================================================
            // 5. ASTRAL FIRE PHASE
            // =========================================================
            if (inAstralFire)
            {
                if (isAoe)
                {
                    // Transpose Recovery: Force Flare
                    if (AstralStacks < 3 && Core.Me.CurrentMana >= 800)
                    {
                        if (await Aoe.Flare()) return true;
                        if (Spells.Flare.IsKnownAndReady()) return await Spells.Flare.Cast(Core.Me.CurrentTarget);
                    }

                    int minAoeMp = Spells.Flare.IsKnown() ? 800 : 3000;

                    // AoE Astral Fire loop
                    if (Core.Me.CurrentMana < minAoeMp)
                    {
                        if (await Aoe.Thunder4()) return true;
                        if (await Aoe.Foul()) return true;

                        if (await Aoe.AoeTranspose()) return true;
                        return true; // Intentionally holding tick waiting for Transpose CD
                    }
                    // AoE Finisher Phase
                    else 
                    {
                        // 1. Flare Star
                        if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                        {
                            if (await Aoe.FlareStar()) return true;
                            if (Spells.FlareStar.IsKnownAndReady()) return await Spells.FlareStar.Cast(Core.Me.CurrentTarget);
                            return true; // HOLD TICK
                        }

                        // NEW: Pop an ether to buy another Flare before transposing!
                        if (await Aoe.UseAoeEther()) return true;

                        // 2. Weaves
                        if (await Aoe.Thunder4()) return true;
                        if (await Aoe.Foul()) return true;

                        // 3. Transpose
                        if (await Aoe.AoeTranspose()) return true;
                        return true; // HOLD TICK
                    }
                }
                else
                {
                    // Single-Target Recovery: Must reach AF3
                    if (AstralStacks < 3)
                    {
                        if (await SingleTarget.Fire3()) return true;
                        if (Spells.Fire3.IsKnownAndReady()) return await Spells.Fire3.Cast(Core.Me.CurrentTarget);
                        if (Spells.Fire.IsKnownAndReady()) return await Spells.Fire.Cast(Core.Me.CurrentTarget);
                    }

                    if (await SingleTarget.Paradox()) return true;

                    // Calculate threshold: 2400 to fit Despair + Fire4, or 1600 if Despair isn't known
                    int minStMp = Spells.Despair.IsKnown() ? 2400 : 1600;

                    // Single-Target Main Phase
                    if (Core.Me.CurrentMana >= minStMp)
                    {
                        // Maintain Thunder and prevent Polyglot overcap during Main Phase
                        if (await SingleTarget.Thunder3()) return true;
                        if (await SingleTarget.Xenoglossy()) return true;

                        if (await SingleTarget.Fire4()) return true;
                        if (Spells.Fire4.IsKnownAndReady()) return await Spells.Fire4.Cast(Core.Me.CurrentTarget);

                        if (await SingleTarget.Fire3()) return true; // Firestarter

                        if (await SingleTarget.Fire()) return true;
                        if (Spells.Fire.IsKnownAndReady()) return await Spells.Fire.Cast(Core.Me.CurrentTarget);
                        
                        return false; 
                    }
                    // Single-Target Finisher Phase
                    else 
                    {
                        // 1. Despair (Minimum 800 MP required)
                        if (Spells.Despair.IsKnown() && Core.Me.CurrentMana >= 800)
                        {
                            if (await SingleTarget.Despair()) return true;
                            if (Spells.Despair.IsKnownAndReady()) return await Spells.Despair.Cast(Core.Me.CurrentTarget);
                            return true; // HOLD TICK FOR DESPAIR
                        }

                        // 2. Flare Star (0 MP, Consumes 6 Stacks)
                        if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                        {
                            if (await Aoe.FlareStar()) return true;
                            if (Spells.FlareStar.IsKnownAndReady()) return await Spells.FlareStar.Cast(Core.Me.CurrentTarget);
                            return true; // HOLD TICK FOR FLARE STAR
                        }

                        // 3. Blizzard III Transition (0 MP under AF3)
                        if (await SingleTarget.Blizzard3()) return true;
                        if (Spells.Blizzard3.IsKnownAndReady()) return await Spells.Blizzard3.Cast(Core.Me.CurrentTarget);
                        
                        if (!Spells.Blizzard3.IsKnown() && Spells.Transpose.IsKnownAndReady()) 
                            return await Spells.Transpose.Cast(Core.Me);

                        return true; // HOLD TICK FOR TRANSITION
                    }
                }
            }
            // =========================================================
            // 6. UMBRAL ICE PHASE
            // =========================================================
            else if (inUmbralIce)
            {
                if (isAoe)
                {
                    // Secure Umbral Hearts
                    if (Spells.Freeze.IsKnown() && UmbralHearts < 3 && (!MovementManager.IsMoving || hasInstantCastBuff))
                    {
                        if (Core.Me.CurrentTarget != null)
                        {
                            if (await Aoe.Freeze()) return true;
                            if (Spells.Freeze.IsKnownAndReady()) return await Spells.Freeze.Cast(Core.Me.CurrentTarget);
                        }
                        return true; // HOLD TICK: Wait for Freeze travel time! Never fall through.
                    }

                    // Once Hearts == 3 (or full MP for low levels): Weave Thunder/Foul, then Transpose back to Fire
                    if (UmbralHearts == 3 || Core.Me.CurrentMana >= 10000 || Core.Me.CurrentMana == Core.Me.MaxMana)
                    {
                        if (await Aoe.Thunder4()) return true;
                        if (await Aoe.Foul()) return true;

                        if (await Aoe.AoeTranspose()) return true;
                        return true; // HOLD TICK FOR TRANSITION
                    }

                    // Low-Level Filler (< Lv 58)
                    if (!Spells.Freeze.IsKnown())
                    {
                        if (await Buff.UmbralSoul()) return true;
                        if (await Aoe.Blizzard2()) return true;
                        if (Spells.Blizzard2.IsKnownAndReady()) return await Spells.Blizzard2.Cast(Core.Me.CurrentTarget);
                    }
                    
                    return true; // Hold to prevent dropping combat
                }
                else
                {
                    // Single-Target UI Recovery: Ensure UI3
                    if (UmbralStacks < 3)
                    {
                        if (await SingleTarget.Blizzard3()) return true;
                        if (Spells.Blizzard3.IsKnownAndReady()) return await Spells.Blizzard3.Cast(Core.Me.CurrentTarget);
                        return true; // HOLD TICK FOR B3
                    }

                    // Secure Hearts (B4 grants 3 hearts and instantly restores 10k MP on hit)
                    if (Spells.Blizzard4.IsKnown() && UmbralHearts < 3 && (!MovementManager.IsMoving || hasInstantCastBuff))
                    {
                        if (await SingleTarget.Blizzard4()) return true;
                        if (Spells.Blizzard4.IsKnownAndReady()) return await Spells.Blizzard4.Cast(Core.Me.CurrentTarget);
                        return true; // HOLD TICK: Wait for B4 travel time! Never fall through.
                    }
                    
                    // Main Ice Spells & Procs
                    if (await SingleTarget.Thunder3()) return true;
                    if (await SingleTarget.Xenoglossy()) return true;
                    if (await SingleTarget.Paradox()) return true;

                    // Transition to Fire
                    // Since Dawntrail removed passive MP ticks, we use Hearts as our "ready" flag instead of raw MP
                    if (UmbralHearts == 3 || Core.Me.CurrentMana >= 10000 || Core.Me.CurrentMana == Core.Me.MaxMana)
                    {
                        if (await SingleTarget.Fire3()) return true;
                        if (Spells.Fire3.IsKnownAndReady()) return await Spells.Fire3.Cast(Core.Me.CurrentTarget);
                        return true; // HOLD TICK FOR TRANSITION
                    }

                    // Low-Level Filler (< Lv 58)
                    if (!Spells.Blizzard4.IsKnown())
                    {
                        if (await SingleTarget.Blizzard()) return true;
                        if (Spells.Blizzard.IsKnownAndReady()) return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);
                    }
                    
                    return true; // Hold to prevent dropping combat
                }
            }
            // =========================================================
            // 7. NEUTRAL PHASE
            // =========================================================
            else if (isNeutral)
            {
                if (isAoe)
                {
                    if (await Aoe.Thunder4()) return true;
                    if (Spells.Transpose.IsKnownAndReady() && await Spells.Transpose.Cast(Core.Me)) return true;
                    if (await Aoe.Freeze()) return true;
                    if (Spells.Freeze.IsKnownAndReady()) return await Spells.Freeze.Cast(Core.Me.CurrentTarget);
                }
                else
                {
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