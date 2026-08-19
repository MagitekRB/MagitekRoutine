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
            if (Aoe.ForceLimitBreak()) return true;
            
            if (await CommonFightLogic.FightLogic_SelfShield(BlackMageSettings.Instance.FightLogicManaward, Spells.Manaward, castTimeRemainingMs: 19000)) return true;
            if (await MagicDps.FightLogic_Addle(BlackMageSettings.Instance)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(BlackMageSettings.Instance.FightLogicKnockback, Spells.Surecast, true, aura: Auras.Surecast)) return true;

            // 2. Dynamic Target & Stance Evaluation
            bool isAoe = BlackMageSettings.Instance.UseAoe && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies;
            bool inAstralFire = AstralStacks > 0;
            bool inUmbralIce = UmbralStacks > 0;
            bool isNeutral = !inAstralFire && !inUmbralIce;
            
            // 3. Movement Safety: Check if we have instant cast buffs active to allow casting on the run
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

                if (await Buff.Triplecast()) return true;
                if (await Buff.Swiftcast()) return true;
                
                return false; 
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
                    // AoE Recovery: Force Flare to reach Astral Fire III via helper logic
                    if (AstralStacks < 3 && Core.Me.CurrentMana >= 800)
                    {
                        if (await Aoe.Flare()) return true;
                    }

                    int minAoeMp = Spells.Flare.IsKnown() ? 800 : 3000;

                    // AoE Main Phase
                    if (Core.Me.CurrentMana >= minAoeMp)
                    {
                        if (await Aoe.Flare()) return true;
                        if (await Aoe.Fire2()) return true;
                        
                        return false; 
                    }
                    // AoE Finisher Phase
                    else 
                    {
                        // 1. Flare Star
                        if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                        {
                            if (await Aoe.FlareStar()) return true;
                            return true; 
                        }

                        // Use Ether helper
                        if (await Aoe.UseAoeEther()) return true;

                        // 2. Weaves
                        if (await Aoe.Thunder4()) return true;
                        if (await Aoe.Foul()) return true;

                        // 3. Transpose helper
                        if (await Aoe.AoeTranspose()) return true;
                        return true; 
                    }
                }
                else
                {
                    // Single-Target Recovery
                    if (AstralStacks < 3)
                    {
                        if (await SingleTarget.Fire3()) return true;
                        if (await SingleTarget.Fire()) return true;
                    }

                    if (await SingleTarget.Paradox()) return true;

                    int minStMp = Spells.Despair.IsKnown() ? 2400 : 1600;

                    // Single-Target Main Phase
                    if (Core.Me.CurrentMana >= minStMp)
                    {
                        if (await SingleTarget.Thunder3()) return true;
                        if (await SingleTarget.Xenoglossy()) return true;
                        if (await SingleTarget.Fire4()) return true;
                        if (await SingleTarget.Fire3()) return true; 
                        if (await SingleTarget.Fire()) return true;
                        
                        return false; 
                    }
                    // Single-Target Finisher Phase
                    else 
                    {
                        // 1. Despair (Calls helper which properly respects user settings)
                        if (Spells.Despair.IsKnown() && Core.Me.CurrentMana >= 800)
                        {
                            if (await SingleTarget.Despair()) return true;
                            return true; 
                        }

                        // 2. Flare Star
                        if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                        {
                            if (await Aoe.FlareStar()) return true;
                            return true; 
                        }

                        // 3. Blizzard III Transition
                        if (await SingleTarget.Blizzard3()) return true;
                        
                        if (!Spells.Blizzard3.IsKnown() && await Buff.Transpose()) 
                            return true;

                        return true; 
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
                        }
                        return true; 
                    }

                    if (UmbralHearts == 3 || Core.Me.CurrentMana >= 10000 || Core.Me.CurrentMana == Core.Me.MaxMana)
                    {
                        if (await Aoe.Thunder4()) return true;
                        if (await Aoe.Foul()) return true;

                        if (await Aoe.AoeTranspose()) return true;
                        return true; 
                    }

                    if (!Spells.Freeze.IsKnown())
                    {
                        if (await Buff.UmbralSoul()) return true;
                        if (await Aoe.Blizzard2()) return true;
                    }
                    
                    return true; 
                }
                else
                {
                    if (UmbralStacks < 3)
                    {
                        if (await SingleTarget.Blizzard3()) return true;
                        return true; 
                    }

                    if (Spells.Blizzard4.IsKnown() && UmbralHearts < 3 && (!MovementManager.IsMoving || hasInstantCastBuff))
                    {
                        if (await SingleTarget.Blizzard4()) return true;
                        return true; 
                    }
                    
                    if (await SingleTarget.Thunder3()) return true;
                    if (await SingleTarget.Xenoglossy()) return true;
                    if (await SingleTarget.Paradox()) return true;

                    if (UmbralHearts == 3 || Core.Me.CurrentMana >= 10000 || Core.Me.CurrentMana == Core.Me.MaxMana)
                    {
                        if (await SingleTarget.Fire3()) return true;
                        return true; 
                    }

                    if (!Spells.Blizzard4.IsKnown())
                    {
                        if (await SingleTarget.Blizzard()) return true;
                    }
                    
                    return true; 
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
                    if (await Buff.Transpose()) return true;
                    if (await Aoe.Freeze()) return true;
                }
                else
                {
                    if (await SingleTarget.Blizzard3()) return true;
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

            if (CommonPvp.ShouldUseBurst() && !CommonPvp.GuardCheck(BlackMageSettings.Instance))
            {
                if (await Pvp.SoulResonancePvp()) return true;
            }

            if (CommonPvp.ShouldUseBurst())
            {
                if (await Pvp.ElementalWeave()) return true;
            }

            if (CommonPvp.ShouldUseBurst() && !CommonPvp.GuardCheck(BlackMageSettings.Instance))
            {
                if (await Pvp.Lethargy()) return true;
                if (await Pvp.Paradox()) return true;
                if (await Pvp.Xenoglossy()) return true;
            }

            if (await Pvp.Burst()) return true;
            if (await Pvp.Fire()) return true;
            if (await Pvp.Blizzard()) return true;
            return false;
        }
    }
}