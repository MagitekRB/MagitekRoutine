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

            // On the Crucible board a horn does nothing but upset the duty; inside a fight the horns are its own.
            if (BeastMasterRoutine.LeaveHornsToTheDuty() && !Core.Me.InCombat)
                return false;

            // A horn just blown, by the routine or by hand, is a familiar on its way: no second order on top of it.
            if (BeastMasterRoutine.HornJustBlown)
                return false;

            // No familiar out: the moment to fill an empty horn slot without sending one home.
            BeastMasterRoutine.AssignPactsToEmptyHorns();

            var horn = BeastMasterRoutine.ReadyBattlehorn();
            if (horn == null)
                return false;

            if (!await horn.Cast(Core.Me))
                return false;

            BeastMasterRoutine.NoteHornCast(horn);
            return true;
        }

        // Away is instant and the familiar takes a moment to leave; the next order waits for that.
        private static System.DateTime _awayAt = System.DateTime.MinValue;
        private const int AwayRetryMs = 5000;

        /// <summary>
        /// Out of combat, a familiar whose One with Nature is spent goes Away and the horn brings it back with a fresh
        /// one, so every pull opens with Tempered Release (Icy Veins, 2026-09-09: the cooldowns reset while the horn
        /// itself is not on cooldown, which a swap or a Parting Blow would have started). Only with an enemy near
        /// enough that a pull is coming, and never in the Crucible, where the horns are the duty's.
        /// </summary>
        public static bool AwayReset()
        {
            var settings = BeastMasterSettings.Instance;
            if (!settings.AwayResetBetweenPulls || !settings.SummonFamiliar || !BeastMasterRoutine.FamiliarOut || Core.Me.InCombat)
                return false;

            if (Core.Me.HasAura(Auras.OneWithNature) || BeastMasterRoutine.LeaveHornsToTheDuty())
                return false;

            if ((System.DateTime.Now - _awayAt).TotalMilliseconds < AwayRetryMs)
                return false;

            var horn = BeastMasterRoutine.ActiveHorn;
            if (horn == null || horn.Cooldown != System.TimeSpan.Zero)
                return false;

            if (!Core.Me.EnemiesNearbyOoc(settings.AwayResetRange).Any())
                return false;

            if (!PetManager.DoAction("Away", Core.Me))
                return false;

            _awayAt = System.DateTime.Now;
            Logger.WriteInfo("[Beastmaster] Away: One with Nature is spent and a pull is near; the horn brings the familiar back with a fresh one.");
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

            // The client accepts any horn but the one whose beast is already out, and SwapHornFor never returns that one.
            if (horn.IsKnownAndReady())
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

            BeastMasterRoutine.NotePartingBlow();
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
            var release = BeastMasterSettings.Instance.UseTemperedRelease && Spells.TemperedRelease.IsKnown();
            var timing = release ? TemperedReleaseTiming() : Timing.Never;

            // One use per summon, so "not now" and "not at all" are different answers: a mitigation waiting for
            // damage or a sleep waiting for company keeps One with Nature for its moment, and Borrow only takes it
            // when the controlled ability is off, unknown, or ruled out (a knockback in a party), or when Borrow is
            // preferred outright.
            //
            // Borrow is an order with nothing to aim: cast on self. Tempered Release is aimed the way the beast's
            // ability is: one centred on the familiar (Ultrasonics, the party buffs and mitigations) takes the order
            // on self, one aimed at an enemy (Necrotic Nectar, Petribreath and every cone, line or single-target hit)
            // only goes through with the order on that enemy; on self the client drops it without a word
            // (0 of 95 attempts, 2026-09-08). The catalogue's range tells the two apart.
            if (borrow && (BeastMasterSettings.Instance.PreferBorrow || timing == Timing.Never))
                return await Spells.Borrow.Cast(Core.Me);

            if (timing == Timing.Now)
            {
                if (!await Spells.TemperedRelease.Cast(TemperedReleaseOrderTarget()))
                    return false;

                var ability = BeastMasterRoutine.Familiar?.TemperedRelease;
                if (ability != null && ability.Has("Sleep"))
                    BeastMasterRoutine.BeginSleepDisengage();
                return true;
            }

            return false;
        }

        private enum Timing { Now, Later, Never }

        /// <summary>Who the Tempered Release order is placed on: the enemy for an aimed ability, ourselves otherwise.</summary>
        private static ff14bot.Objects.GameObject TemperedReleaseOrderTarget()
        {
            var ability = BeastMasterRoutine.Familiar?.TemperedRelease;
            if (ability != null && ability.Range > 0 && Core.Me.HasTarget)
                return Core.Me.CurrentTarget;
            return Core.Me;
        }

        /// <summary>
        /// When the summoned familiar's controlled ability is worth the one One with Nature a summon grants: now,
        /// later in this summon, or never. Its class decides: damage as soon as the target will live to feel it,
        /// party buffs likewise, the familiar's own buff at once, mitigation when we are hurt or a catalogued AoE is
        /// coming, sleep only with company to put down, and Final Sting (the familiar retreats) as a kill shot with
        /// another horn ready. Knockbacks and pull-ins are never in a party unless asked for. A beast the bestiary
        /// does not classify is used at once, as before.
        /// </summary>
        private static Timing TemperedReleaseTiming()
        {
            var ability = BeastMasterRoutine.Familiar?.TemperedRelease;
            if (ability == null || string.IsNullOrEmpty(ability.Kind))
                return Timing.Now;

            var settings = BeastMasterSettings.Instance;
            var target = Core.Me.CurrentTarget;

            if (Globals.InParty && !settings.TemperedReleaseKnockbacksInParty && (ability.Has("Knockback") || ability.Has("DrawIn")))
                return Timing.Never;

            switch (ability.Kind)
            {
                case AbilityKind.Damage:
                    // A dispel is worth more with something to strip: give the fight a few seconds to show one.
                    if (ability.Has("Dispel") && !target.HasDispellableBuff() && Combat.CombatTime.Elapsed.TotalSeconds < settings.TemperedReleaseDispelWaitSeconds)
                        return Timing.Later;
                    return BeastMasterRoutine.CheckTTDIsEnemyDyingSoon() ? Timing.Later : Timing.Now;

                case AbilityKind.PartyBuff:
                    if (ability.Has("SelfDamage") && Core.Me.CurrentHealthPercent < settings.TemperedReleaseSelfDamageHealthPercent)
                        return Timing.Later;
                    return BeastMasterRoutine.CheckTTDIsEnemyDyingSoon() ? Timing.Later : Timing.Now;

                case AbilityKind.FamiliarBuff:
                    return Timing.Now;

                case AbilityKind.Mitigation:
                    return Core.Me.CurrentHealthPercent <= settings.TemperedReleaseMitigationHealthPercent
                        || FightLogic.EnemyIsCastingAoe() || FightLogic.EnemyIsCastingBigAoe()
                        ? Timing.Now : Timing.Later;

                case AbilityKind.CrowdControl:
                    return BeastMasterRoutine.EnemiesNearFamiliar(settings.TemperedReleaseSleepRadius) >= settings.TemperedReleaseSleepMinEnemies ? Timing.Now : Timing.Later;

                case AbilityKind.Finisher:
                    if (target.CurrentHealthPercent > settings.TemperedReleaseFinisherHealthPercent)
                        return Timing.Later;
                    return BeastMasterRoutine.AnotherHornReady
                        ? Timing.Now : Timing.Later;

                default:
                    return Timing.Now;
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
            if (!BeastMasterSettings.Instance.UseTrick || !BeastMasterRoutine.FamiliarOut || BeastMasterRoutine.FamiliarRetreating)
                return false;

            if (!BeastMasterRoutine.HasTpFor(Spells.Trick))
                return false;

            // A pair still resolving: nothing chains with the familiar until it clears (about 2 s). And a Trick
            // already ordered whose Heart has not shown yet is not to be doubled: the second one went into the
            // Wavering Heart of the first (dummy, 2026-09-09).
            if (BeastMasterRoutine.WaveringHeart || BeastMasterRoutine.TrickPending)
                return false;

            var affinity = BeastMasterRoutine.FamiliarAffinity;
            if (affinity != null)
            {
                var heart = BeastMasterRoutine.EffectiveHeart;
                if (heart != null)
                {
                    // Another Heart is lit, or a Sunstrider or Moonstalker window is open: a Trick that does not
                    // continue the chain restarts it, and inside a window it consumes the window as a link (a
                    // lone Trick at a full bar did, and cost three Universalities in one dummy run, 2026-09-09).
                    // This holds whatever the familiar bar reads.
                    if (!BeastMasterRoutine.TrickContinuesChain)
                        return false;
                }
                else if (BeastMasterRoutine.PetTpFull && BeastMasterRoutine.Tp < LoneTrickOurTpBelow)
                {
                    // Nothing lit, the familiar bar is full and our TP is far from a pair: every further
                    // auto-attack is lost, so the Trick goes out on its own. With our TP nearer, the same Trick
                    // inside a pair is worth far more than the few auto-attacks the wait loses.
                }
                else if (BeastMasterRoutine.NaturalPreferred)
                {
                    // The yellow diamonds are full: our axe opens this pair so the Trick finishes it and the stack
                    // lands on blue instead of overflowing.
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

            // Vantage is worth waiting for only while it can still come, that is while One with Nature is unspent.
            // Once it is spent and Vantage has run out, waiting keeps this beast out for the rest of the fight: on
            // the dummy (2026-09-09) the third beast's Vantage expired ten seconds before the first horn returned
            // and the cycle never resumed. An unbuffed Parting Blow still brings the next beast and its Tempered
            // Release.
            if (BeastMasterSettings.Instance.PartingBlowOnlyWithVantage && !Core.Me.HasAura(Auras.LingeringVantage)
                && Core.Me.HasAura(Auras.OneWithNature))
                return false;

            // The horn that summoned this familiar still reads castable while it is out (its 90 s starts at the
            // retreat), so "another horn is ready" has to look at the other horns' own cooldowns.
            var another = BeastMasterRoutine.AnotherHornReady;
            if (!another)
                return false;

            // Three horns on a 90 s recast that starts at the retreat allow one summon per 45 s on average however
            // fast the exits come. Exiting sooner only bunches them: two beasts out ten seconds each, then one
            // stranded for eighty with its Vantage long gone (dummy, 2026-09-09). Holding each beast for the
            // interval keeps every exit under Vantage. A target that will die inside the interval is the
            // exception, and so is a Vantage about to run out.
            var hold = BeastMasterSettings.Instance.PartingBlowSpacingSeconds;
            var left = hold - BeastMasterRoutine.FamiliarOutSeconds;
            string early = null;
            if (hold > 0 && left > 0)
            {
                // A Vantage whose timer the bot has not read yet shows 0 ms left; that is not an ending Vantage.
                // At the pull, with every horn ready, the check ran inside the first second after Tempered Release
                // and sent two beasts home at once (dummy, 2026-09-09).
                var vantage = Core.Me.CharacterAuras.FirstOrDefault(a => a.Id == Auras.LingeringVantage);
                var vantageMsLeft = vantage?.TimespanLeft.TotalMilliseconds ?? 0;
                var vantageEnding = vantageMsLeft > 0 && vantageMsLeft < 3000;
                var ttd = Core.Me.CurrentTarget?.CombatTimeLeft() ?? 0;
                var diesFirst = ttd > 0 && ttd < left;
                if (!vantageEnding && !diesFirst)
                    return false;

                early = $"[Beastmaster] Parting Blow {left:0} s before the spacing interval ends: {(diesFirst ? $"target dead in {ttd} s" : $"Vantage has {vantageMsLeft:0} ms left")}.";
            }

            // The reason is logged once the cast has gone out. The check passes on every pulse while the beast is
            // still leaving, and it wrote the line fifteen times a second on a pack in the field (2026-09-09).
            if (!await Spells.PartingBlow.Cast(Core.Me.CurrentTarget))
                return false;

            if (early != null)
                Logger.WriteInfo(early);

            BeastMasterRoutine.NotePartingBlow();
            return true;
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
            var outOfMelee = !target.WithinSpellRange(5);
            if (!outOfMelee && !target.HasDispellableBuff())
                return false;

            return await Spells.QuellingWave.Cast(target);
        }

        /// <summary>
        /// Rally spends the Mastered Instinct our combos built (40 TP, +70 per stack). Without the gauge the stacks
        /// are estimated from the combos we finished; it goes out at the cap, or with two stacks when an axe is
        /// waiting on TP.
        /// </summary>
        public static async Task<bool> Rally()
        {
            if (!BeastMasterSettings.Instance.UseRally || !Core.Me.InCombat)
                return false;

            var stacks = BeastMasterRoutine.MasteredInstinct;
            if (Core.Me.ClassLevel >= 50)
            {
                // Level 50: three stacks fill the bar from nothing (40 + 3 x 70 = 250) and turn the axes into their
                // 250 TP forms. Spent while a pair is resolving or its Sunstrider or Moonstalker window is open, the
                // opposite form completes Universality (dummy, 2026-09-09: Rally 0.8 s after the finishing axe,
                // Calamity under Sunstrider, "Infinitive Combo: Universality", chain counter 2). Spent anywhere else
                // the bar goes into a lone 250 axe whose window nothing can answer.
                if (stacks < 3)
                    return false;
                if (!BeastMasterRoutine.WaveringHeart && !BeastMasterRoutine.ChainWindowOpen)
                    return false;
                // The bar is already full: the finisher needs no Rally.
                if (BeastMasterRoutine.Axes.Any(a => a != null && a.Cost >= 250 && BeastMasterRoutine.HasTpFor(a)))
                    return false;
            }
            else
            {
                var axeWaiting = !BeastMasterRoutine.Axes.Any(a => BeastMasterRoutine.HasTpFor(a));
                if (stacks < 3 && !(stacks >= 2 && axeWaiting))
                    return false;
            }

            if (!await Spells.Rally.Cast(Core.Me))
                return false;

            BeastMasterRoutine.SpentMastered();
            return true;
        }

        private const int CheerOverflowAllowed = 40;
        private const int LoneTrickOurTpBelow = 40;

        public static async Task<bool> RallyingCheer()
        {
            if (!BeastMasterSettings.Instance.UseRallyingCheer || !Core.Me.InCombat || !BeastMasterRoutine.FamiliarOut)
                return false;

            // Natural Instinct from the pairs the familiar finished (30 familiar TP, +70 per stack). One stack is a
            // whole Trick, and its moment is when our half of a pair is paid and the familiar's is not: spent then
            // it is an extra pair, spent at the cap next to Rally it sat near 200 pet TP for thirty seconds while
            // our TP rebuilt (dummy, 2026-09-09). Never into an overflowing bar.
            var natural = BeastMasterRoutine.NaturalInstinct;
            if (natural < 1)
                return false;

            // A Trick scales with the familiar TP it spends (about 880 at 100-149, 1,500 at 200-249, 1,800 at 250
            // on the dummy, 2026-09-09), so every point Cheer adds is damage as long as the bar does not overflow:
            // the moment is right after a Trick has landed, into an empty bar, with one stack or three. The
            // pending gap after a Trick order is skipped, since that Trick has already taken its TP. A pair
            // waiting on the familiar takes it whatever the bar reads, within the same headroom.
            var gain = 30 + 70 * natural;
            if (BeastMasterRoutine.PetTp + gain > BeastMasterRoutine.TpCap + CheerOverflowAllowed)
                return false;

            if (!BeastMasterRoutine.PairWaitingOnFamiliar && (!BeastMasterRoutine.TrickSettled || BeastMasterRoutine.FamiliarRetreating))
                return false;

            if (!await Spells.RallyingCheer.Cast(Core.Me))

                return false;


            BeastMasterRoutine.SpentNatural();

            return true;
        }
    }
}
