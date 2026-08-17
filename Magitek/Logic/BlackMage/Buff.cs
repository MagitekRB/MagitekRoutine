using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.BlackMage;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.BlackMage;

namespace Magitek.Logic.BlackMage
{
    internal static class Buff
    {
        public static async Task<bool> Triplecast()
        {
            if (!Spells.Triplecast.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.TripleCast)
                return false;

            if (AstralSoulStacks == 6)
                return false;

            if (Spells.Triplecast.Cooldown != TimeSpan.Zero && Spells.Triplecast.Charges == 0)
                return false;

            if (Core.Me.HasAura(Auras.Triplecast) || Core.Me.HasAura(Auras.Swiftcast))
                return false;

            // Restrict Triplecast entirely to Astral Fire
            if (AstralStacks == 0)
                return false;

            if (Core.Me.CurrentMana < 4800 && !MovementManager.IsMoving)
                return false;

            if ((MovementManager.IsMoving && AstralStacks == 3))
                return await Spells.Triplecast.Cast(Core.Me);

            if (BlackMageSettings.Instance.TripleCastWhileMoving && Spells.Triplecast.Charges <= 1)
                return false;

            return await Spells.Triplecast.Cast(Core.Me);
        }

        public static async Task<bool> Swiftcast()
        {
            if (!Spells.Swiftcast.IsKnownAndReady())
                return false;

            if (Core.Me.HasAura(Auras.Triplecast) || Core.Me.HasAura(Auras.Swiftcast))
                return false;

            // Restrict Swiftcast strictly to Astral Fire
            if (AstralStacks == 0)
                return false;

            // Never pop Swiftcast if out of MP; next action will be Blizzard III (Ice transition)
            if (Core.Me.CurrentMana < 1600 && !MovementManager.IsMoving)
                return false;

            // Use Swiftcast for Movement
            if (MovementManager.IsMoving)
                return await Spells.Swiftcast.Cast(Core.Me);

            // Use Swiftcast for high-cost spells at the end of the Fire phase (like Despair)
            if (Core.Me.CurrentMana > 800 && Core.Me.CurrentMana <= 2400)
                return await Spells.Swiftcast.Cast(Core.Me);

            return false;
        }

        public static async Task<bool> LeyLines()
        {
            if (!Spells.LeyLines.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.LeyLines)
                return false;

            if (BlackMageSettings.Instance.LeyLinesBossOnly && !Core.Me.CurrentTarget.IsBoss())
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.LeyLines))
                return false;

            if (!Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                return false;

            if (AstralStacks != 3 || UmbralStacks > 0)
                return false;

            if (Core.Me.HasAura(Auras.CircleOfPower))
                return false;

            if (Casting.LastSpellWas(Spells.Fire3) && (UmbralHearts == 3 || Core.Me.HasAura(Auras.Triplecast)))
                return await Spells.LeyLines.Cast(Core.Me);

            if (Casting.LastSpellWas(Spells.HighFireII) && (UmbralHearts == 3 || Core.Me.HasAura(Auras.Triplecast)))
                return await Spells.LeyLines.Cast(Core.Me);

            return await Spells.LeyLines.Cast(Core.Me);
        }

        private static readonly uint[] ThunderAuras =
        {
            Auras.Thunder,
            Auras.Thunder2,
            Auras.Thunder3,
            Auras.Thunder4,
            Auras.HighThunder,
            Auras.HighThunder2
        };

        public static async Task<bool> Retrace()
        {
            if (!Spells.Retrace.IsKnown())
                return false;

            if (!Spells.Retrace.IsKnownAndReady())
                return false;

            if (Spells.Retrace.Cooldown != TimeSpan.Zero)
                return false;

            if (!Core.Me.HasAura(Auras.LeyLines))
                return false;

            if (Core.Me.HasAura(Auras.CircleOfPower))
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (!Core.Me.InCombat)
                return false;

            return await Spells.Retrace.Cast(Core.Me);
        }

        public static async Task<bool> UmbralSoul()
        {
            if (!Spells.UmbralSoul.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.UmbralSoul)
                return false;

            if (Spells.UmbralSoul.Cooldown != TimeSpan.Zero)
                return false;

            // Allow Umbral Soul in combat IF moving to recover mana on the run
            // if (Core.Me.InCombat && Core.Me.HasTarget)
            if (Core.Me.InCombat && Core.Me.HasTarget && !MovementManager.IsMoving)
                return false;

            if (UmbralStacks == 0)
                return false;

            if (UmbralStacks < 3 || UmbralHearts < 3)
                return await Spells.UmbralSoul.Cast(Core.Me);

            return false;
        }

        public static async Task<bool> PreCombatUmbralSoul()
        {
            if (!Spells.UmbralSoul.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.UmbralSoul)
                return false;

            if (!Spells.UmbralSoul.IsKnownAndReady())
                return false;

            if (Core.Me.InCombat)
                return false;

            if (UmbralStacks == 0)
                return false;

            if (UmbralStacks < 3 || UmbralHearts < 3 || Core.Me.CurrentMana < Core.Me.MaxMana)
                return await Spells.UmbralSoul.Cast(Core.Me);

            return false;
        }

        public static async Task<bool> ManaFont()
        {
            if (!Spells.ManaFont.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.ManaFont)
                return false;

            // Dawntrail check: Manafont requires Astral Fire
            if (AstralStacks == 0)
                return false;

            if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                return false;

            if (Core.Me.CurrentMana >= 800)
                return false;

            return await Spells.ManaFont.Cast(Core.Me);
        }

        public static async Task<bool> Transpose()
        {
            if (!Spells.Transpose.IsKnown())
                return false;

            return await Spells.Transpose.Cast(Core.Me);
        }

        public static async Task<bool> PreCombatTranspose()
        {
            if (!Spells.Transpose.IsKnown())
                return false;

            if (!Spells.Transpose.IsKnownAndReady())
                return false;

            if (Core.Me.InCombat)
                return false;

            if (AstralStacks == 0)
                return false;

            if (Core.Me.CurrentMana >= Core.Me.MaxMana)
                return false;

            return await Spells.Transpose.Cast(Core.Me);
        }

        public static async Task<bool> Amplifier()
        {
            if (!Spells.Amplifier.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.Amplifier)
                return false;

            if (!Core.Me.InCombat)
                return false;

            int maxPolyglot = Core.Me.ClassLevel >= 98 ? 3 : (Core.Me.ClassLevel >= 80 ? 2 : 1);
            if (ActionResourceManager.BlackMage.PolyglotCount >= maxPolyglot)
                return false;

            return await Spells.Amplifier.Cast(Core.Me);
        }
    }
}