using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Dragoon;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using DragoonRoutine = Magitek.Utilities.Routines.Dragoon;

namespace Magitek.Logic.Dragoon
{
    internal static class Buff
    {
        public static async Task<bool> LanceCharge() //Damage +10%
        {
            if (!DragoonSettings.Instance.UseBuffs)
                return false;

            if (!DragoonSettings.Instance.UseLanceCharge)
                return false;

            if (Core.Me.HasAura(Auras.LanceCharge))
                return false;

            //Exec LanceCharge after Disembowel or RaidenThrust if only 1 ennemy
            if (Combat.Enemies.Count(x => x.Distance(Core.Me) <= 10 + x.CombatReach) == 1)
            {
                if (ActionManager.LastSpell.Id != Spells.Disembowel.Id && ActionManager.LastSpell.Id != Spells.RaidenThrust.Id)
                    return false;
            }

            return await Spells.LanceCharge.Cast(Core.Me);
        }

        public static async Task<bool> BattleLitany() // Crit +10%
        {
            if (!DragoonSettings.Instance.UseBuffs)
                return false;

            if (!DragoonSettings.Instance.UseBattleLitany)
                return false;

            if (!Core.Me.HasAura(Auras.LanceCharge))
                return false;

            return await Spells.BattleLitany.Cast(Core.Me);
        }

        public static async Task<bool> LifeSurge() // Crit +10%
        {
            if (!DragoonSettings.Instance.UseBuffs)
                return false;

            if (!DragoonSettings.Instance.UseLifeSurge)
                return false;

            if (!Core.Me.HasAura(Auras.LanceCharge))
                return false;

            if (Casting.LastSpell == Spells.LifeSurge)
                return false;

            //Life surge only for HeavensThrust / FangAndClaw for SingleTarget or DraconianFury / CoerthanTorment for AOE 
            if (!Spells.FullThrust.IsKnown()
                ||
                (Spells.FullThrust.IsKnown()
                    &&
                    (
                        ActionManager.LastSpell == Spells.VorpalThrust
                        || ActionManager.LastSpell == DragoonRoutine.Disembowel
                        || ActionManager.LastSpell == DragoonRoutine.HeavensThrust
                        || ActionManager.LastSpell == DragoonRoutine.ChaoticSpring
                        || ActionManager.LastSpell == Spells.WheelingThrust
                        || ActionManager.LastSpell == Spells.FangAndClaw
                    )
                )
                ||
                (Spells.CoerthanTorment.IsKnown()
                    &&
                    (
                        ActionManager.LastSpell == Spells.SonicThrust
                    )
                )
                ||
                (!Spells.CoerthanTorment.IsKnown()
                    &&
                    (
                        ActionManager.LastSpell == Spells.DoomSpike
                    )
                )
                ||
                Core.Me.HasAura(Auras.DraconianFire, true)
            )
                return await Spells.LifeSurge.Cast(Core.Me);

            return false;
        }

        public static async Task<bool> UsePotion()
        {
            if (Spells.BattleLitany.IsKnown() && !Spells.BattleLitany.IsReady(5000))
                return false;

            return await PhysicalDps.UsePotion(DragoonSettings.Instance);
        }

    }

}
