using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.BlackMage;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.BlackMage;
using Auras = Magitek.Utilities.Auras;
using BlackMageRoutine = Magitek.Utilities.Routines.BlackMage;

namespace Magitek.Logic.BlackMage
{
    internal static class SingleTarget
    {
        public static async Task<bool> Xenoglossy()
        {
            if (!Spells.Xenoglossy.IsKnown())
            {
                if (Spells.Foul.IsKnown() && PolyglotStatus)
                    return await Aoe.Foul();
                return false;
            }

            if (!BlackMageSettings.Instance.Xenoglossy)
                return false;

            if (AstralStacks == 0 && UmbralStacks == 0)
                return false;

            if (AstralSoulStacks == 6)
                return false;

            if (MovementManager.IsMoving)
            {
                if (!Core.Me.HasAura(Auras.Swiftcast) && !Core.Me.HasAura(Auras.Triplecast))
                    return await Spells.Xenoglossy.Cast(Core.Me.CurrentTarget);
            }

            if (BlackMageRoutine.WillOvercapPolyglot())
                return await Spells.Xenoglossy.Cast(Core.Me.CurrentTarget);

            if (PolyglotCount <= BlackMageSettings.Instance.SaveXenoglossyCharges)
                return false;

            return await Spells.Xenoglossy.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Despair()
        {
            if (!Spells.Despair.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.Despair)
                return false;

            // Skip single-target Despair during AoE encounters
            if (AoeControl.Enabled 
                && BlackMageSettings.Instance.UseAoe 
                && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
                return false;

            if (Casting.LastSpellWas(Spells.Despair))
                return false;

            if (AstralSoulStacks == 6)
                return false;

            if (UmbralStacks > 0)
                return false;

            if (!Spells.Despair.IsKnownAndReadyAndCastable())
                return false;

            if (Core.Me.CurrentMana < 800)
                return false;

            int fire4Cost = UmbralHearts > 0 ? 800 : 1600;
            if (Core.Me.CurrentMana >= fire4Cost + 800)
                return false;

            return await Spells.Despair.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fire()
        {
            if (!Spells.Fire.IsKnown())
                return false;

            if (Spells.Fire4.IsKnown())
                return false;

            if (AstralSoulStacks == 6)
                return false;

            if (UmbralStacks > 0)
                return false;

            if (AstralStacks > 0 && Core.Me.CurrentMana < 1600)
                return false;

            if (AstralStacks == 0 && Core.Me.CurrentMana < 800)
                return false;

            return await Spells.Fire.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fire4()
        {
            if (!Spells.Fire4.IsKnown())
                return false;

            if (AstralStacks != 3)
                return false;

            return await Spells.Fire4.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Fire3()
        {
            if (!Spells.Fire3.IsKnown())
                return false;

            if (AstralStacks < 3 && UmbralStacks == 0)
                return await Spells.Fire3.Cast(Core.Me.CurrentTarget);

            if (Casting.LastSpellWas(Spells.Fire3))
                return false;

            if (Core.Me.CurrentMana < 2000 && Core.Me.HasAura(Auras.FireStarter))
                return false;

            if (Core.Me.HasAura(Auras.FireStarter) && Core.Me.CurrentMana < 8400)
                return false;

            if (AstralSoulStacks == 6)
                return false;

            if (AstralStacks == 3 || UmbralStacks < 3)
                return false;

            if (UmbralStacks > 0 && Core.Me.CurrentMana != Core.Me.MaxMana)
                return false;

            return await Spells.Fire3.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Thunder3()
        {
            if (!Spells.Thunder.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.ThunderSingle)
                return false;

            // Dawntrail check: Thunder requires the Thunderhead buff
            if (!Core.Me.HasAura(Auras.Thunderhead))
                return false;

            if (AoeControl.Enabled
                && BlackMageSettings.Instance.UseAoe
                && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
                return false;

            if (Casting.LastSpellWas(Spells.Triplecast))
                return false;

            if (Core.Me.HasAura(Auras.Triplecast))
                return false;

            if (Casting.LastSpellWas(Spells.Thunder)
                || Casting.LastSpellWas(Spells.Thunder3)
                || Casting.LastSpellWas(Spells.HighThunder))
                return false;

            if (Casting.LastSpellWas(Spells.Thunder)
                || Casting.LastSpellWas(Spells.Thunder2)
                || Casting.LastSpellWas(Spells.Thunder3)
                || Casting.LastSpellWas(Spells.Thunder4)
                || Casting.LastSpellWas(Spells.HighThunder)
                || Casting.LastSpellWas(Spells.HighThunderII))
                return false;

            if (Core.Me.CurrentTarget.HasAnyAura(ThunderAuras, true, BlackMageSettings.Instance.ThunderRefreshSecondsLeft * 1000 + 500))
                return false;

            if (BlackMageSettings.Instance.UseTTDForThunderSingle && Combat.CurrentTargetCombatTimeLeft <= BlackMageSettings.Instance.ThunderSingleTTDSeconds && !Core.Me.CurrentTarget.IsBoss())
                return false;

            if (!Spells.Thunder3.IsKnown())
                return await Spells.Thunder.Cast(Core.Me.CurrentTarget);

            if (!Spells.HighThunder.IsKnown())
                return await Spells.Thunder3.Cast(Core.Me.CurrentTarget);

            return await Spells.HighThunder.Cast(Core.Me.CurrentTarget);
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

        public static async Task<bool> Blizzard4()
        {
            if (!Spells.Blizzard4.IsKnown())
                return false;

            if (UmbralStacks != 3)
                return false;

            // Do not skip Blizzard4 at max mana; check Umbral Hearts instead
            if (UmbralHearts == 3)
                return false;

            if (Casting.LastSpellWas(Spells.Blizzard4))
                return false;

            if (Casting.LastSpellWas(Spells.Transpose))
                return false;

            return await Spells.Blizzard4.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Blizzard3()
        {
            if (!Spells.Blizzard3.IsKnown())
                return false;

            if (Casting.LastSpellWas(Spells.Blizzard3) || Casting.LastSpellWas(Spells.ManaFont))
                return false;

            if (AstralSoulStacks == 6)
                return false;

            if (AstralStacks < 3 || UmbralStacks == 3)
                return false;

            if (AoeControl.Enabled
                && BlackMageSettings.Instance.UseAoe
                && Core.Me.CurrentTarget.EnemiesNearby(10).Count() >= BlackMageSettings.Instance.AoeEnemies)
                return false;

            if (Core.Me.CurrentMana >= 1600)
                return false;

            if (Spells.ManaFont.IsKnownAndReady())
                return false;

            return await Spells.Blizzard3.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Blizzard()
        {
            if (!Spells.Blizzard3.IsKnown())
            {
                if (Casting.LastSpellWas(Spells.Transpose) && AstralStacks > 0)
                    return false;

                if (AstralStacks > 0 && Core.Me.CurrentMana < 1600)
                    return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);

                if (UmbralStacks > 0 && Core.Me.CurrentMana < Core.Me.MaxMana)
                    return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);

                if (AstralStacks == 0 && UmbralStacks == 0 && Core.Me.CurrentMana < 1600)
                    return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);

                return false;
            }

            if (Casting.LastSpellWas(Spells.Blizzard4))
                return false;

            return await Spells.Blizzard.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Paradox()
        {
            if (!Spells.Paradox.IsKnown())
                return false;

            if (!BlackMageSettings.Instance.Paradox)
                return false;

            if (Casting.LastSpellWas(Spells.Fire3))
                return false;

            if (Casting.LastSpellWas(Spells.Blizzard3))
                return false;

            // Dawntrail check: Paradox is strictly available in Astral Fire
            if (AstralStacks < 3)
                return false;

            if (Spells.ManaFont.IsKnownAndReady())
                return false;

            if (Spells.Fire4.IsKnownAndReadyAndCastableAtTarget() && Spells.ManaFont.Cooldown.TotalMilliseconds >= 70000)
                return false;

            return await Spells.Paradox.Cast(Core.Me.CurrentTarget);
        }
    }
}