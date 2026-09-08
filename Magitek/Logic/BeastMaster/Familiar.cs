using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.BeastMaster;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using BeastMasterRoutine = Magitek.Utilities.Routines.BeastMaster;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.BeastMaster
{
    /// <summary>
    /// Everything that goes through the familiar: the horns, the two things One with Nature can buy, the kinship
    /// action Beast Mode turns into, Trick, Parting Blow, and the two Rallies.
    /// </summary>
    internal static class Familiar
    {
        public static async Task<bool> Summon()
        {
            if (!BeastMasterSettings.Instance.SummonFamiliar || BeastMasterRoutine.FamiliarOut)
                return false;

            // A horn just blown is a familiar on its way (1 s cast); do not stack a second order on it.
            if (BeastMasterRoutine.Battlehorns.Any(h => Casting.LastSpellWas(h, 5000)))
                return false;

            var horn = BeastMasterRoutine.ReadyBattlehorn();
            if (horn == null)
                return false;

            if (!await horn.Cast(Core.Me))
                return false;

            BeastMasterRoutine.LastHorn = horn;
            return true;
        }

        /// <summary>
        /// One with Nature is spent by either of these, once per summon. Borrow when preferred (its kinship outlives
        /// the familiar), otherwise the familiar's own controlled ability.
        /// </summary>
        public static async Task<bool> SpendOneWithNature()
        {
            if (!Core.Me.HasAura(Auras.OneWithNature) || !BeastMasterRoutine.FamiliarOut || !Core.Me.InCombat)
                return false;

            var borrow = BeastMasterSettings.Instance.UseBorrow && Spells.Borrow.IsKnown();
            var release = BeastMasterSettings.Instance.UseTemperedRelease && Spells.TemperedRelease.IsKnown();

            // Both are orders to the familiar (range 0): cast on self, the familiar resolves what its ability hits.
            if (borrow && (BeastMasterSettings.Instance.PreferBorrow || !release))
                return await Spells.Borrow.Cast(Core.Me);

            if (release && await Spells.TemperedRelease.Cast(Core.Me))
                return true;

            if (borrow)
                return await Spells.Borrow.Cast(Core.Me);

            return false;
        }

        /// <summary>
        /// Beast Mode after Borrow: the kinship action the summoned familiar's class allows. Each has its moment;
        /// the defensive ones are off by default and read the health threshold.
        /// </summary>
        public static async Task<bool> KinshipAction()
        {
            if (!Spells.BeastMode.IsKnown())
                return false;

            var settings = BeastMasterSettings.Instance;
            var target = Core.Me.CurrentTarget;
            var enemy = target as ff14bot.Objects.BattleCharacter;

            if (BeastMasterRoutine.SoulKinship && settings.UseSoulCrush && enemy != null
                && enemy.IsCasting && enemy.SpellCastInfo != null && enemy.SpellCastInfo.Interruptible)
                return await Spells.SoulCrush.Cast(target);

            if (BeastMasterRoutine.AshKinship && settings.UseScouringAsh && Core.Me.HasAnyDispellableAura())
                return await Spells.ScouringAsh.Cast(Core.Me);

            if (BeastMasterRoutine.SeedKinship && settings.UseSeedsower && !target.HasAura(Auras.SeedsSown, true))
                return await Spells.Seedsower.Cast(Core.Me);

            var lowHealth = Core.Me.CurrentHealthPercent <= settings.DefensiveKinshipHealthPercent;

            if (BeastMasterRoutine.BeastKinship && settings.UseBeastskin && lowHealth)
                return await Spells.Beastskin.Cast(Core.Me);

            if (BeastMasterRoutine.ScaleKinship && settings.UseScaleskin && lowHealth)
                return await Spells.Scaleskin.Cast(Core.Me);

            if (BeastMasterRoutine.VileKinship && settings.UseVileskin && lowHealth)
                return await Spells.Vileskin.Cast(Core.Me);

            return false;
        }

        /// <summary>
        /// Trick spends the familiar's TP on its instinctual skill (the client refuses it below 100). Best right
        /// after an axe whose affinity precedes the familiar's, but never held: the familiar's TP is worth nothing
        /// banked.
        /// </summary>
        public static async Task<bool> Trick()
        {
            if (!BeastMasterSettings.Instance.UseTrick || !BeastMasterRoutine.FamiliarOut)
                return false;

            if (!BeastMasterRoutine.HasTpFor(Spells.Trick))
                return false;

            return await Spells.Trick.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Parting Blow: 1,000 (1,500 under Lingering Vantage) and the familiar goes home, which starts the horn's
        /// 90 s cooldown. So: under Vantage, and only when another horn can follow at once (unless the user says
        /// otherwise), so the fight never runs without a familiar.
        /// </summary>
        public static async Task<bool> PartingBlow()
        {
            if (!BeastMasterSettings.Instance.UsePartingBlow || !BeastMasterRoutine.FamiliarOut || !Spells.PartingBlow.IsKnown())
                return false;

            if (BeastMasterSettings.Instance.PartingBlowOnlyWithVantage && !Core.Me.HasAura(Auras.LingeringVantage))
                return false;

            // The horn that summoned this familiar still reads castable while it is out (its 90 s starts at the
            // retreat), so "another horn is ready" has to look at the other horns' own cooldowns.
            var another = BeastMasterRoutine.Battlehorns.Any(h => h != BeastMasterRoutine.LastHorn && h.IsKnown() && h.Cooldown == System.TimeSpan.Zero);
            if (!another)
                return false;

            return await Spells.PartingBlow.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Quelling Wave is a spell on the GCD (Wave Kinship): 350 water at range, +10 TP, and +50 TP when it strips
        /// a buff. Worth a GCD when the target carries a dispellable buff or you are out of melee; otherwise the combo
        /// is better.
        /// </summary>
        public static async Task<bool> QuellingWave()
        {
            if (!BeastMasterSettings.Instance.UseQuellingWave || !BeastMasterRoutine.WaveKinship || !Spells.BeastMode.IsKnown())
                return false;

            var target = Core.Me.CurrentTarget;
            var outOfMelee = target.Distance(Core.Me) > 5 + target.CombatReach;
            if (!outOfMelee && !target.HasDispellableBuff())
                return false;

            return await Spells.QuellingWave.Cast(target);
        }

        public static async Task<bool> Rally()
        {
            if (!BeastMasterSettings.Instance.UseRally || !Core.Me.InCombat)
                return false;

            return await Spells.Rally.Cast(Core.Me);
        }

        public static async Task<bool> RallyingCheer()
        {
            if (!BeastMasterSettings.Instance.UseRallyingCheer || !Core.Me.InCombat || !BeastMasterRoutine.FamiliarOut)
                return false;

            return await Spells.RallyingCheer.Cast(Core.Me);
        }
    }
}
