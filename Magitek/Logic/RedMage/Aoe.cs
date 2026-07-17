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
            if (!AoeControl.Enabled)
                return false;

            if (!RedMageSettings.Instance.UseAoe)
                return false;

            if (!RedMageSettings.Instance.Moulinet)
                return false;

            if (!Spells.Moulinet.IsKnown())
                return false;

            if (ManaStacks() == 3)
                return false;

            // Continue an in-progress AoE combo off live combo state (matches the melee combo); the
            // enemy-count/mana gates below apply only when starting a fresh combo.
            if (InAoeCombo())
                return await Spells.Moulinet.Cast(Core.Me.CurrentTarget);

            // Starting a fresh AoE combo: honor the Embolden hold, the enemy-count threshold, and the
            // 50/50 mana cost.
            if (Spells.Embolden.IsKnown()
                && Spells.Embolden.Cooldown.TotalSeconds > 0
                && Spells.Embolden.Cooldown.TotalSeconds <= RedMageSettings.Instance.HoldAccelForEmboldenSeconds)
                return false;

            if (Core.Me.EnemiesInCone(8) < RedMageSettings.Instance.AoeEnemies)
                return false;

            //Combo is 50 black and white mana
            if (!Core.Me.HasAura(Auras.MagickedSwordplay) && (WhiteMana < 50 || BlackMana < 50))
                return false;

            return await Spells.Moulinet.Cast(Core.Me.CurrentTarget);
        }
        public static async Task<bool> ContreSixte()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!RedMageSettings.Instance.UseAoe)
                return false;

            if (!RedMageSettings.Instance.UseContreSixte)
                return false;

            if (!Spells.ContreSixte.IsKnown())
                return false;

            if (Spells.ContreSixte.Cooldown != TimeSpan.Zero)
                return false;

            return await Spells.ContreSixte.Cast(Core.Me.CurrentTarget);
        }
        public static async Task<bool> Scatter()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!RedMageSettings.Instance.Scatter)
                return false;

            if (!Spells.Scatter.IsKnown())
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
            if (!Spells.GrandImpact.IsKnown())
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
            if (!AoeControl.Enabled)
                return false;

            if (!RedMageSettings.Instance.UseAoe)
                return false;

            if (!Spells.Impact.IsKnown())
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
            if (!AoeControl.Enabled)
                return false;

            if (!RedMageSettings.Instance.Ver2)
                return false;

            if (!Spells.Verthunder2.IsKnown())
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
            if (!AoeControl.Enabled)
                return false;

            if (!RedMageSettings.Instance.Ver2)
                return false;

            if (!Spells.Veraero2.IsKnown())
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
            return MagicDps.ForceLimitBreak(Spells.Skyshard, Spells.Starstorm, Spells.VermillionScourge, Spells.Jolt);
        }
    }
}
