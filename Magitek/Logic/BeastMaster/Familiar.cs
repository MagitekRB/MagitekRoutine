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

            BeastMasterRoutine.NoteHornCast(horn);
            return true;
        }

        // A horn blown over the familiar out swaps it (the client accepts any horn but the one whose beast is already
        // out; seen out of combat 2026-09-08, one-second cast, cancelled by moving). The Parting Blow route (retreat,
        // then the horn) is the fallback when the client refuses, and takes longer; the Heart lasts seven seconds.
        private const int DirectSwapMs = 3500;
        private const int RetreatSwapMs = 5500;

        /// <summary>
        /// The Heart lit now cannot be continued by the familiar out, but another horn's beast carries the affinity
        /// that would: swap. A familiar's TP has to be there for the Trick that follows, so the swap is only taken
        /// when Trick reads castable. If the horn can be blown over the current familiar it is; otherwise Parting Blow
        /// sends the familiar home and Summon blows the wanted horn next.
        /// </summary>
        public static async Task<bool> SwapForChain()
        {
            var settings = BeastMasterSettings.Instance;
            if (!settings.UseBattlehornSwaps || !settings.UseTrick || !Core.Me.InCombat || !BeastMasterRoutine.FamiliarOut)
                return false;

            if (BeastMasterRoutine.WaveringHeart || BeastMasterRoutine.TrickPending)
                return false;

            var heart = BeastMasterRoutine.CurrentHeart;
            if (heart == null)
                return false;

            var wanted = Affinity.Next(heart);
            if (BeastMasterRoutine.FamiliarAffinity == wanted)
                return false;

            if (!BeastMasterRoutine.HasTpFor(Spells.Trick))
                return false;

            var horn = BeastMasterRoutine.SwapHornFor(wanted);
            if (horn == null)
                return false;

            var msLeft = BeastMasterRoutine.HeartMsLeft;
            var slot = BeastMasterRoutine.HornSlot(horn);

            if (ActionManager.CanCast(horn.Id, Core.Me))
            {
                if (msLeft < DirectSwapMs)
                    return false;

                if (!await horn.Cast(Core.Me))
                    return false;

                BeastMasterRoutine.NoteHornCast(horn);
                Logger.WriteInfo("[Beastmaster] Battlehorn " + slot + " blown over the familiar for a " + wanted + " Trick (" + msLeft.ToString("0") + " ms left on the Heart).");
                return true;
            }

            if (!Spells.PartingBlow.IsKnown() || Spells.PartingBlow.Cooldown != System.TimeSpan.Zero || msLeft < RetreatSwapMs)
                return false;

            if (!await Spells.PartingBlow.Cast(Core.Me.CurrentTarget))
                return false;

            BeastMasterRoutine.WantHorn(horn);
            Logger.WriteInfo("[Beastmaster] Parting Blow, then battlehorn " + slot + " for a " + wanted + " Trick (" + msLeft.ToString("0") + " ms left on the Heart).");
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
            var release = BeastMasterSettings.Instance.UseTemperedRelease && Spells.TemperedRelease.IsKnown() && TemperedReleaseWanted();

            // Borrow is an order with nothing to aim: cast on self. Tempered Release is aimed the way the beast's
            // ability is: one centred on the familiar (Ultrasonics, the party buffs and mitigations) takes the order
            // on self, one aimed at an enemy (Necrotic Nectar, Petribreath and every cone, line or single-target hit)
            // only goes through with the order on that enemy; on self the client drops it without a word
            // (0 of 95 attempts, 2026-09-08). The catalogue's range tells the two apart.
            if (borrow && (BeastMasterSettings.Instance.PreferBorrow || !release))
                return await Spells.Borrow.Cast(Core.Me);

            if (release && await Spells.TemperedRelease.Cast(TemperedReleaseOrderTarget()))
                return true;

            if (borrow)
                return await Spells.Borrow.Cast(Core.Me);

            return false;
        }

        /// <summary>Who the Tempered Release order is placed on: the enemy for an aimed ability, ourselves otherwise.</summary>
        private static ff14bot.Objects.GameObject TemperedReleaseOrderTarget()
        {
            var ability = BeastMasterRoutine.Familiar?.TemperedRelease;
            if (ability != null && ability.Range > 0 && Core.Me.HasTarget)
                return Core.Me.CurrentTarget;
            return Core.Me;
        }

        /// <summary>
        /// Whether this is the moment for the summoned familiar's controlled ability. The horns are the only source
        /// of One with Nature, so it is one use per summon and its class decides when that use is worth taking:
        /// damage as soon as the target will live to feel it, party buffs likewise, the familiar's own buff at once,
        /// mitigation when we are hurt or a catalogued AoE is coming, sleep only with company to put down, and Final
        /// Sting (the familiar retreats) as a kill shot with another horn ready. Knockbacks and pull-ins stay off in
        /// a party unless asked for. A beast the bestiary does not classify is used as before.
        /// </summary>
        private static bool TemperedReleaseWanted()
        {
            var ability = BeastMasterRoutine.Familiar?.TemperedRelease;
            if (ability == null || string.IsNullOrEmpty(ability.Kind))
                return true;

            var settings = BeastMasterSettings.Instance;
            var target = Core.Me.CurrentTarget;

            if (Globals.InParty && !settings.TemperedReleaseKnockbacksInParty && (ability.Has("Knockback") || ability.Has("DrawIn")))
                return false;

            switch (ability.Kind)
            {
                case AbilityKind.Damage:
                    // A dispel is worth more with something to strip: give the fight a few seconds to show one.
                    if (ability.Has("Dispel") && !target.HasDispellableBuff() && Combat.CombatTime.Elapsed.TotalSeconds < 8)
                        return false;
                    return !BeastMasterRoutine.CheckTTDIsEnemyDyingSoon();

                case AbilityKind.PartyBuff:
                    if (ability.Has("SelfDamage") && Core.Me.CurrentHealthPercent < 60)
                        return false;
                    return !BeastMasterRoutine.CheckTTDIsEnemyDyingSoon();

                case AbilityKind.FamiliarBuff:
                    return true;

                case AbilityKind.Mitigation:
                    return Core.Me.CurrentHealthPercent <= settings.TemperedReleaseMitigationHealthPercent
                        || FightLogic.EnemyIsCastingAoe() || FightLogic.EnemyIsCastingBigAoe();

                case AbilityKind.CrowdControl:
                    return BeastMasterRoutine.EnemiesNearFamiliar(8) >= settings.TemperedReleaseSleepMinEnemies;

                case AbilityKind.Finisher:
                    if (target.CurrentHealthPercent > settings.TemperedReleaseFinisherHealthPercent)
                        return false;
                    return BeastMasterRoutine.Battlehorns.Any(h => h != BeastMasterRoutine.LastHorn && h.IsKnown() && h.Cooldown == System.TimeSpan.Zero);

                default:
                    return true;
            }
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
        /// Trick spends the familiar's TP on its instinctual skill (the client refuses it below 100), and it is one
        /// half of a pair: it goes out when it continues the Heart we lit, or opens a chain we can answer at once
        /// with the next axe. Held otherwise, since the familiar's TP keeps growing its damage up to the cap. Under
        /// Wavering Heart no pair is possible and it goes out for the damage; a familiar the bestiary does not know
        /// has no affinity to plan around and is used as it comes.
        /// </summary>
        public static async Task<bool> Trick()
        {
            if (!BeastMasterSettings.Instance.UseTrick || !BeastMasterRoutine.FamiliarOut)
                return false;

            if (!BeastMasterRoutine.HasTpFor(Spells.Trick))
                return false;

            var affinity = BeastMasterRoutine.FamiliarAffinity;
            if (affinity != null && !BeastMasterRoutine.WaveringHeart)
            {
                var heart = BeastMasterRoutine.EffectiveHeart;
                if (heart != null)
                {
                    // Another Heart is lit: Trick would restart the chain instead of continuing it.
                    if (!BeastMasterRoutine.TrickContinuesChain)
                        return false;
                }
                else if (!BeastMasterRoutine.HasTpFor(BeastMasterRoutine.AxeFor(Affinity.Next(affinity))))
                {
                    // Nothing lit and no axe ready to answer: wait for our TP, or for our axe to open.
                    return false;
                }
            }

            if (!await Spells.Trick.Cast(Core.Me.CurrentTarget))
                return false;

            BeastMasterRoutine.LastTrickAt = System.DateTime.Now;
            return true;
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
