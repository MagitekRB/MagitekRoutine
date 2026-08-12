using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.BlackMage;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.BlackMage;
using BlackMageRoutine = Magitek.Utilities.Routines.BlackMage;

namespace Magitek.Logic.BlackMage
{
    internal static class Aoe
    {
        public static async Task<bool> Foul()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!PolyglotStatus)
                return false;

            if (!Spells.Foul.IsKnown())
                return false;

            if (AstralSoulStacks == 6 && Spells.FlareStar.IsKnown())
                return false;

            if (MovementManager.IsMoving)
            {
                if (Core.Me.ClassLevel >= 80 && (Core.Me.HasAura(Auras.Swiftcast) || Core.Me.HasAura(Auras.Triplecast)))
                    return false;

                if (Core.Me.ClassLevel < 80 && !Core.Me.HasAura(Auras.Swiftcast) && !Core.Me.HasAura(Auras.Triplecast))
                    return false;
            }

            if (BlackMageRoutine.WillOvercapPolyglot())
                return await Spells.Foul.Cast(Core.Me.CurrentTarget);

            return await Spells.Foul.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Flare()
        {
            if (!AoeControl.Enabled)
                return false;

            if (UmbralStacks > 0)
                return false;

            if (AstralSoulStacks == 6)
                return false;

            if (!Spells.Flare.IsKnown())
                return false;

            if (Core.Me.ClassLevel == 100)
            {
                if (AstralStacks > 0)
                    return await Spells.Flare.Cast(Core.Me.CurrentTarget);
            }

            if (Casting.LastSpellWas(Spells.ManaFont))
                return await Spells.Flare.Cast(Core.Me.CurrentTarget);

            return await Spells.Flare.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> FlareStar()
        {
            if (!Spells.FlareStar.IsKnown())
                return false;

            if (!Spells.FlareStar.IsKnownAndReadyAndCastableAtTarget())
                return false;

            if (Casting.LastSpellWas(Spells.Fire3) || Casting.LastSpellWas(Spells.Blizzard3))
                return false;

            return await Spells.FlareStar.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Freeze()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!Spells.Freeze.IsKnown())
                return false;

            if (Casting.LastSpellWas(Spells.Freeze))
                return false;

            if (AstralSoulStacks == 6)
                return false;

            if (UmbralStacks != 3)
                return false;

            if (UmbralHearts == 3)
                return false;

            return await Spells.Freeze.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Thunder4()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!Spells.Thunder2.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.ThunderAoe)
                return false;

            if (!Core.Me.HasAura(Auras.Thunderhead))
                return false;

            if (AstralSoulStacks == 6)
                return false;

            if (Casting.LastSpellWas(Spells.Triplecast))
                return false;

            if (Core.Me.HasAura(Auras.Triplecast))
                return false;

            if (Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                return false;

            if (BlackMageSettings.Instance.UseTTDForThunderAoe && Combat.CurrentTargetCombatTimeLeft <= BlackMageSettings.Instance.ThunderAoeTTDSeconds && !Core.Me.CurrentTarget.IsBoss())
                return false;

            if (!Spells.Thunder4.IsKnown())
                return await Spells.Thunder2.Cast(Core.Me.CurrentTarget);

            if (!Spells.HighThunderII.IsKnown())
                return await Spells.Thunder4.Cast(Core.Me.CurrentTarget);

            return await Spells.HighThunderII.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fire2()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!Spells.Fire2.IsKnown())
                return false;

            if (AstralStacks < 3 && UmbralStacks == 0)
                return await Spells.Fire2.Cast(Core.Me.CurrentTarget);

            if (AstralSoulStacks == 6)
                return false;

            if (UmbralStacks == 3 && UmbralHearts != 3)
                return false;

            if (Core.Me.ClassLevel >= 58)
            {
                if (AstralStacks == 3 || UmbralStacks < 3)
                    return false;
            }

            return await Spells.Fire2.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Blizzard2()
        {
            if (!AoeControl.Enabled)
                return false;

            if (!Spells.Blizzard2.IsKnown())
                return false;

            if (AstralSoulStacks == 6)
                return false;

            if (Casting.LastSpellWas(Spells.Blizzard2)
                || Casting.LastSpellWas(Spells.HighBlizzardII)
                || Casting.LastSpellWas(Spells.ManaFont))
                return false;

            if (AstralStacks < 3 || UmbralStacks == 3)
                return false;

            if (Core.Me.CurrentMana >= 1600)
                return false;

            if (Spells.ManaFont.IsKnownAndReady())
                return false;

            return await Spells.Blizzard2.Cast(Core.Me.CurrentTarget);
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

        public static bool ForceLimitBreak()
        {
            return MagicDps.ForceLimitBreak(Spells.Skyshard, Spells.Starstorm, Spells.Meteor, Spells.Blizzard);
        }
    }
}