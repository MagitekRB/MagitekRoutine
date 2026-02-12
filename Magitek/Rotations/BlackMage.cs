using ff14bot;
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
            //Try to keep stacks outside combat
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
        public static Task<bool> CombatBuff()
        {
            return Task.FromResult(false);
        }
        public static async Task<bool> Combat()
        {
            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            //LimitBreak
            if (Aoe.ForceLimitBreak()) return true;

            if (await CommonFightLogic.FightLogic_SelfShield(BlackMageSettings.Instance.FightLogicManaward, Spells.Manaward, castTimeRemainingMs: 19000)) return true;
            if (await MagicDps.FightLogic_Addle(BlackMageSettings.Instance)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(BlackMageSettings.Instance.FightLogicKnockback, Spells.Surecast, true, aura: Auras.Surecast)) return true;

            if (await Aoe.FlareStar()) return true;

            if (await Buff.Amplifier()) return true;
            if (await Buff.Triplecast()) return true;
            if (await Buff.LeyLines()) return true;
            if (await Buff.Retrace()) return true;
            if (await Buff.ManaFont()) return true;
            if (await Buff.UmbralSoul()) return true;

            if (await SingleTarget.Thunder3()) return true;
            if (await SingleTarget.Paradox()) return true;
            if (await SingleTarget.Despair()) return true;

            //AoE Section
            if (BlackMageSettings.Instance.UseAoe && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
            {
                //Either
                if (await Aoe.Thunder4()) return true;
                if (await Aoe.Foul()) return true;

                if (await Aoe.Blizzard2()) return true;
                if (await Aoe.Fire2()) return true;

                if (await Aoe.Flare()) return true;
                if (await Aoe.Freeze()) return true;
            }

            if (await SingleTarget.Xenoglossy()) return true;
            if (await SingleTarget.Blizzard4()) return true;
            if (await SingleTarget.Fire4()) return true;

            if (await SingleTarget.Blizzard3()) return true;
            if (await SingleTarget.Fire3()) return true;

            // Low-level mana management: Transpose between Astral Fire and Umbral Ice
            if (!Spells.Blizzard3.IsKnown() || !Spells.Fire3.IsKnown())
            {
                // FFXIV MP costs (patch 7.x): Fire base cost 800, doubled to 1600 in Astral Fire.
                // Spells.Fire.Cost is NOT reliably dynamic for AF stance; hardcode known game values.
                // If SQEX changes Fire's MP cost in a future patch, update these constants.
                // AF→UI: Transpose when MP too low for Fire (1600 in AF)
                if (AstralStacks > 0 && Core.Me.CurrentMana < 1600 && Spells.Transpose.IsKnownAndReady())
                {
                    if (await Buff.Transpose()) return true;
                }
                // UI→AF: Transpose when MP is full to start Fire phase
                if (UmbralStacks > 0 && Core.Me.CurrentMana == Core.Me.MaxMana && Spells.Transpose.IsKnownAndReady())
                {
                    if (await Buff.Transpose()) return true;
                }
            }

            if (await SingleTarget.Fire()) return true;
            if (await SingleTarget.Blizzard()) return true;

            return false;
        }

        public static async Task<bool> PvP()
        {
            BlackMageRoutine.RefreshVars();

            if (await CommonPvp.CommonTasks(BlackMageSettings.Instance)) return true;

            // Limit Break
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
