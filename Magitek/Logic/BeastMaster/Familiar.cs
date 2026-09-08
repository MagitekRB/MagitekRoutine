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

            var horn = BeastMasterRoutine.ReadyBattlehorn();
            if (horn == null)
                return false;

            return await horn.Cast(Core.Me);
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

            if (borrow && (BeastMasterSettings.Instance.PreferBorrow || !release))
                return await Spells.Borrow.Cast(Core.Me);

            if (release)
                return await Spells.TemperedRelease.Cast(Core.Me.CurrentTarget);

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

            if (BeastMasterRoutine.WaveKinship && settings.UseQuellingWave)
                return await Spells.QuellingWave.Cast(target);

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

            var current = BeastMasterRoutine.Battlehorns.FirstOrDefault(h => h.IsKnown() && !ActionManager.CanCast(h.Id, Core.Me));
            var another = BeastMasterRoutine.Battlehorns.Any(h => h != current && h.IsKnown() && ActionManager.CanCast(h.Id, Core.Me));
            if (!another)
                return false;

            return await Spells.PartingBlow.Cast(Core.Me.CurrentTarget);
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
