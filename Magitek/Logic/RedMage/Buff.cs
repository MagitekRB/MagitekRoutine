using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.RedMage;
using Magitek.Toggles;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using static Magitek.Logic.RedMage.Utility;


namespace Magitek.Logic.RedMage
{
    internal class Buff
    {
        public static async Task<bool> Acceleration()
        {
            if (!RedMageSettings.Instance.Acceleration)
                return false;

            if (!Spells.Acceleration.IsKnown())
                return false;

            if (Spells.Acceleration.Cooldown != TimeSpan.Zero
                && Spells.Acceleration.Charges == 0)
                return false;

            if (Core.Me.HasAura(Auras.Swiftcast)
                || Core.Me.HasAura(Auras.Dualcast)
                || Core.Me.HasAura(Auras.Acceleration))
                return false;

            if (!MovementManager.IsMoving && Spells.Acceleration.Charges <= RedMageSettings.Instance.SaveAccelChargesForMovement)
                return false;

            if (Core.Me.HasAura(Auras.MagickedSwordplay))
                return false;

            if (InComboEnder())
                return false;

            if (InAoeCombo())
                return false;

            if (InCombo())
                return false;

            return await Spells.Acceleration.Cast(Core.Me.CurrentTarget);
        }
        public static async Task<bool> Embolden()
        {
            if (!RedMageSettings.Instance.Embolden)
                return false;

            if (!Spells.Embolden.IsKnown())
                return false;

            if (Spells.Embolden.Cooldown != TimeSpan.Zero)
                return false;

            if (InAoeCombo())
                return false;

            if (InCombo())
                return false;

            return await Spells.Embolden.Cast(Core.Me.CurrentTarget);
        }
        public static async Task<bool> Manafication()
        {
            if (!RedMageSettings.Instance.Manafication)
                return false;

            if (!Spells.Manafication.IsKnown())
                return false;

            if (Spells.Manafication.Cooldown != TimeSpan.Zero)
                return false;

            //Manafication cancels any combo

            if (InAoeCombo())
                return false;

            if (InCombo())
                return false;

            if (InComboEnder())
                return false;

            if (Spells.Embolden.Cooldown.TotalMilliseconds <= 13000) //trying a little more leeway
                return false;

            if (Spells.Manafication.IsKnownAndReady())
                return await Spells.Manafication.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> MagickBarrier()
        {
            if (!RedMageSettings.Instance.MagickBarrier)
                return false;

            if (!Spells.MagickBarrier.IsKnown())
                return false;

            if (Spells.MagickBarrier.Cooldown != TimeSpan.Zero)
                return false;

            if (RedMageSettings.Instance.ForceMagickBarrier)
            {
                if (await Spells.MagickBarrier.Cast(Core.Me))
                {
                    RedMageSettings.Instance.ForceMagickBarrier = false;
                    TogglesManager.ResetToggles();
                    return true;
                }
                return false;
            }

            if (InAoeCombo())
                return false;

            if (InCombo())
                return false;

            return false;
        }
        public static async Task<bool> LucidDreaming()
        {
            if (!RedMageSettings.Instance.LucidDreaming)
                return false;

            if (!Spells.LucidDreaming.IsKnown())
                return false;

            if (Core.Me.CurrentManaPercent > RedMageSettings.Instance.LucidDreamingManaPercent)
                return false;

            if (Spells.LucidDreaming.Cooldown != TimeSpan.Zero)
                return false;

            if (InAoeCombo())
                return false;

            if (InCombo())
                return false;

            return await Spells.LucidDreaming.Cast(Core.Me.CurrentTarget);
        }
        public static async Task<bool> Swiftcast()
        {
            if (!Spells.Swiftcast.IsKnown())
                return false;

            if (Spells.Swiftcast.Cooldown != TimeSpan.Zero)
                return false;

            if (InAoeCombo())
                return false;

            if (InCombo())
                return false;

            if (Core.Me.HasAura(Auras.Dualcast)
                 || Core.Me.HasAura(Auras.Acceleration))
                return false;

            if (!RedMageSettings.Instance.SwiftcastScatter)
                return false;

            if (!RedMageSettings.Instance.SwiftcastVerthunderVeraero)
                return false;

            return await Spells.Swiftcast.Cast(Core.Me.CurrentTarget);
        }

    }
}
