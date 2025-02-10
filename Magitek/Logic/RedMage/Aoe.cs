using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.RedMage;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.RedMage;
using static Magitek.Logic.RedMage.Utility;

namespace Magitek.Logic.RedMage
{
    internal class Aoe
    {
        public static async Task<bool> Moulinet()
        {
            if (!RedMageSettings.Instance.UseAoe)
                return false;

            if (!RedMageSettings.Instance.Moulinet)
                return false;

            if (Core.Me.ClassLevel < Spells.Moulinet.LevelAcquired)
                return false;

            if (ManaStacks() == 3)
                return false;

            if (Casting.LastSpell == Spells.Moulinet
                || Casting.LastSpell == Spells.EnchantedMoulinet
                || Casting.LastSpell == Spells.EnchantedMoulinetDeux)
                return await Spells.Moulinet.Cast(Core.Me.CurrentTarget);

            if (!InAoeCombo())
            {
                if (Core.Me.ClassLevel >= Spells.Embolden.LevelAcquired
                    && Spells.Embolden.Cooldown.TotalSeconds <= 13)
                    return false;

                if (Core.Me.EnemiesInCone(8) < RedMageSettings.Instance.AoeEnemies)
                    return false;

                //Combo is now 50 black and white mana not 60
                if (!Core.Me.HasAura(Auras.MagickedSwordplay) && (WhiteMana < 50 || BlackMana < 50))
                    return false;
            }

            //Updated to 50 white or black mana as this is now a legit combo
            if (!Core.Me.HasAura(Auras.MagickedSwordplay) && (WhiteMana < 50 || BlackMana < 50))
                return false;

            return await Spells.Moulinet.Cast(Core.Me.CurrentTarget);
        }
        public static async Task<bool> ContreSixte()
        {
            if (!RedMageSettings.Instance.UseAoe)
                return false;

            if (!RedMageSettings.Instance.UseContreSixte)
                return false;

            if (Core.Me.ClassLevel < Spells.ContreSixte.LevelAcquired)
                return false;

            if (Spells.ContreSixte.Cooldown != TimeSpan.Zero)
                return false;

            return await Spells.ContreSixte.Cast(Core.Me.CurrentTarget);
        }
        public static async Task<bool> Scatter()
        {
            if (!RedMageSettings.Instance.Scatter)
                return false;

            if (Core.Me.ClassLevel < Spells.Scatter.LevelAcquired)
                return false;

            if (!Core.Me.HasAura(Auras.Dualcast)
                && !Core.Me.HasAura(Auras.Swiftcast))
                return false;

            if (Core.Me.HasAura(Auras.Swiftcast)
                && !RedMageSettings.Instance.SwiftcastScatter)
                return false;

            if (InAoeCombo())
                return false;

            if (InCombo())
                return false;

            return await Spells.Scatter.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> GrandImpact()
        {
            if (Core.Me.ClassLevel < Spells.GrandImpact.LevelAcquired)
                return false;

            if (!Core.Me.HasAura(Auras.GrandImpactReady))
                return false;

            if (InAoeCombo())
                return false;

            if (InCombo())
                return false;

            return await Spells.GrandImpact.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Impact()
        {
            if (!RedMageSettings.Instance.UseAoe)
                return false;

            if (Core.Me.ClassLevel < Spells.Impact.LevelAcquired)
                return false;

            if (InAoeCombo())
                return false;

            if (InCombo())
                return false;

            if (Core.Me.HasAura(Auras.Dualcast))
                return await Spells.Impact.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.Swiftcast)
                && RedMageSettings.Instance.SwiftcastScatter)
                return await Spells.Impact.Cast(Core.Me.CurrentTarget);

            if (Core.Me.HasAura(Auras.Acceleration))
                return await Spells.Impact.Cast(Core.Me.CurrentTarget);

            return false;
        }
        public static async Task<bool> Verthunder2()
        {
            if (!RedMageSettings.Instance.Ver2)
                return false;

            if (Core.Me.ClassLevel < Spells.Verthunder2.LevelAcquired)
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (InAoeCombo())
                return false;

            if (InCombo())
                return false;

            if (Core.Me.HasAura(Auras.Dualcast)
                || Core.Me.HasAura(Auras.Swiftcast)
                || Core.Me.HasAura(Auras.Acceleration))
                return false;

            if (BlackMana > WhiteMana)
                return false;

            return await Spells.Verthunder2.Cast(Core.Me.CurrentTarget);
        }
        public static async Task<bool> Veraero2()
        {
            if (!RedMageSettings.Instance.Ver2)
                return false;

            if (Core.Me.ClassLevel < Spells.Veraero2.LevelAcquired)
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (InAoeCombo())
                return false;

            if (InCombo())
                return false;

            if (Core.Me.HasAura(Auras.Dualcast)
                || Core.Me.HasAura(Auras.Swiftcast)
                || Core.Me.HasAura(Auras.Acceleration))
                return false;

            if (WhiteMana >= BlackMana)
                return false;

            return await Spells.Veraero2.Cast(Core.Me.CurrentTarget);
        }

        /**********************************************************************************************
        *                              Limit Break
        * ********************************************************************************************/
        public static bool ForceLimitBreak()
        {
            return MagicDps.ForceLimitBreak(Spells.Skyshard, Spells.Starstorm, Spells.Meteor, Spells.Blizzard);
        }
    }
}
