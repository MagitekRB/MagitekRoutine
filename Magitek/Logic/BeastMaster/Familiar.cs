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
        /// itself is not on cooldown, which a swap or a Parting Blow would have started). Only standing still, with an
        /// enemy near enough that a pull is coming, and never in the Crucible, where the horns are the duty's.
        /// </summary>
        public static bool AwayReset()
        {
            var settings = BeastMasterSettings.Instance;
            if (!settings.AwayResetBetweenPulls || !settings.SummonFamiliar || !BeastMasterRoutine.FamiliarOut || Core.Me.InCombat)
                return false;

            // The horn that brings it back is a 1 s cast movement interrupts, so a swap on the move leaves you petless.
            if (MovementManager.IsMoving)
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

        /// <summary>
        /// Crucible enmity control. The pieces go for the familiar by default and hit it four to six times harder than
        /// they hit you (first run 2026-09-09), so the beast is the tank without any help. Snarl is the emergency:
        /// a covering beast takes its own hits AND every hit meant for you, and fired at every summon (the piece
        /// targets you until the beast is out) it emptied a beast in thirty seconds (run 2: Drake 100 to 10 %, Buffalo
        /// 100 to 6 %). So Snarl only when you are low and the beast is healthy. Challenge only when you can afford
        /// it: the beast is low and still out, and you are healthy enough to hold the piece. Both are granted by the
        /// duty and never read as known, so they go through the action manager on a castable check alone.
        /// </summary>
        public static bool CrucibleEnmity()
        {
            var settings = BeastMasterSettings.Instance;
            if (!settings.UseSnarlAndChallenge || !BeastMasterRoutine.InCrucible || !Core.Me.InCombat)
                return false;

            var enemy = Core.Me.CurrentTarget as ff14bot.Objects.BattleCharacter;
            var pet = Core.Me.Pet;
            if (enemy == null || pet == null || !pet.IsValid)
                return false;

            // The beast's object can vanish mid-read while it retreats; that is no reason to stop the rotation.
            float petHealth;
            try { petHealth = pet.CurrentHealthPercent; }
            catch { return false; }

            var myHealth = Core.Me.CurrentHealthPercent;

            if (petHealth >= settings.CrucibleSnarlFamiliarHealthPercent && myHealth <= settings.CrucibleSnarlPlayerHealthPercent
                && !Core.Me.HasAura(Auras.Covered) && CastDutyAction(Spells.Snarl, enemy))
            {
                Logger.WriteInfo("[Beastmaster] Snarl: " + pet.EnglishName + " at " + petHealth.ToString("0") + " % covers you at " + myHealth.ToString("0") + " %.");
                return true;
            }

            // A retreating or dead beast cannot be spared, and a beast at the swap threshold is leaving anyway.
            if (petHealth > 0 && petHealth <= settings.CrucibleChallengeFamiliarHealthPercent && !BeastMasterRoutine.FamiliarRetreating
                && myHealth >= settings.CrucibleChallengePlayerHealthPercent && !Core.Me.BeingTargeted()
                && CastDutyAction(Spells.Challenge, enemy))
            {
                Logger.WriteInfo("[Beastmaster] Challenge: " + pet.EnglishName + " at " + petHealth.ToString("0") + " %, you at " + myHealth.ToString("0") + " %: the piece turns to you.");
                return true;
            }

            return false;
        }

        // Duty actions bypass the routine's known-spell gate: castable now is the only test the client offers.
        private static bool CastDutyAction(ff14bot.Objects.SpellData spell, ff14bot.Objects.GameObject target)
        {
            if (!ActionManager.CanCast(spell, target))
                return false;

            if (!ActionManager.DoAction(spell, target))
                return false;

            Logger.WriteInfo("[Magitek] Cast: " + spell.Name);
            return true;
        }

        // One reaction per cast: the piece and the cast id are remembered so a five-second cast is answered once.
        private static uint _reactedTarget;
        private static uint _reactedSpell;
        private static System.DateTime _reactedAt = System.DateTime.MinValue;
        private static readonly System.Collections.Generic.HashSet<string> PhysicalTypes = new System.Collections.Generic.HashSet<string> { "Slashing", "Piercing", "Blunt" };
        private static readonly System.Collections.Generic.HashSet<string> MagicTypes = new System.Collections.Generic.HashSet<string> { "Fire", "Ice", "Wind", "Earth", "Lightning", "Water", "Unaspected" };

        /// <summary>
        /// Crucible cast reactions from the piece library. The board says, per signature move, who it targets, its
        /// damage type, whether it can be interrupted and what it applies; the routine reads the piece's casting id
        /// against that and answers with what it holds: Soul Crush on a move the board marks interruptible; the
        /// matching skin, if that kin is borrowed, before a physical or magic hit aimed at you; and the beast's own
        /// mitigating Tempered Release before a heavy hit aimed at it. Snarl is deliberately not here: a signature
        /// aimed at you costs about 8 % of your HP and the covered beast 26 % of its own (run 5, 2026-09-11), so the
        /// enmity rule alone decides Snarl, by your actual HP. Nothing here targets: it acts on the current target only.
        /// </summary>
        public static async Task<bool> CrucibleCastReaction()
        {
            var settings = BeastMasterSettings.Instance;
            if (!settings.UseCrucibleCastReactions || !BeastMasterRoutine.InCrucible || !Core.Me.InCombat)
                return false;

            var enemy = Core.Me.CurrentTarget as ff14bot.Objects.BattleCharacter;
            if (enemy == null || !enemy.IsValid || !enemy.IsCasting)
                return false;

            var piece = BeastMasterRoutine.CurrentPiece;
            if (piece == null)
                return false;

            var spellId = enemy.CastingSpellId;
            var move = piece.Actions.FirstOrDefault(a => a.Id == spellId);
            if (move == null || move.Basic)
                return false;

            if (_reactedTarget == enemy.ObjectId && _reactedSpell == spellId && (System.DateTime.Now - _reactedAt).TotalSeconds < 15)
                return false;

            var targeted = Core.Me.BeingTargeted();
            var onMe = move.Target == "Player" || move.Shape == "Universal" || (move.Target == "Highest Enmity" && targeted);
            var onBeast = move.Target == "Highest Enmity" && !targeted;
            var physical = PhysicalTypes.Contains(move.DamageType ?? "");
            var magic = MagicTypes.Contains(move.DamageType ?? "");
            string did = null;

            if (move.Interruptible == true && settings.UseSoulCrush && BeastMasterRoutine.SoulKinship && await Spells.SoulCrush.Cast(enemy))
                did = "Soul Crush on " + move.Name + " (the board marks it interruptible)";
            else if (onMe && physical && settings.UseBeastskin && BeastMasterRoutine.BeastKinship && await Spells.Beastskin.Cast(Core.Me))
                did = "Beastskin before " + move.Name + " (physical, aimed at you)";
            else if (onMe && magic && settings.UseScaleskin && BeastMasterRoutine.ScaleKinship && await Spells.Scaleskin.Cast(Core.Me))
                did = "Scaleskin before " + move.Name + " (magic, aimed at you)";
            else if (onMe && physical && settings.UseVileskin && BeastMasterRoutine.VileKinship && await Spells.Vileskin.Cast(Core.Me))
                did = "Vileskin before " + move.Name + " (physical, aimed at you)";
            else if (onBeast && piece.Strength >= 3 && Core.Me.HasAura(Auras.OneWithNature) && BeastMasterRoutine.Familiar?.TemperedRelease?.Kind == AbilityKind.Mitigation)
            {
                BeastMasterRoutine.MitigationWantedAt = System.DateTime.Now;
                did = "the beast's mitigating Tempered Release before " + move.Name + " (aimed at the beast)";
            }

            if (did == null)
                return false;

            _reactedTarget = enemy.ObjectId;
            _reactedSpell = spellId;
            _reactedAt = System.DateTime.Now;
            Logger.WriteInfo("[Beastmaster] Crucible: " + did + ".");
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

            // A chain swap puts another beast in front of the piece; in the Crucible the beast out stays out.
            if (BeastMasterRoutine.InCrucible)
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
            // damage or a sleep waiting for company keeps One with Nature for its moment. Borrow takes it when the
            // controlled ability is off, unknown, or ruled out (a knockback in a party), or, with PreferBorrow, when
            // no Kinship is up: one Borrow lasts 90 s and pauses while a familiar is out, so Tempered Release gets
            // every summon after that until it lapses. Without this Tempered Release won every summon and Borrow
            // never fired (0 casts in every log, 2026-09-09).
            //
            // Borrow is an order with nothing to aim: cast on self. Tempered Release is aimed the way the beast's
            // ability is: one centred on the familiar (Ultrasonics, the party buffs and mitigations) takes the order
            // on self, one aimed at an enemy (Necrotic Nectar, Petribreath and every cone, line or single-target hit)
            // only goes through with the order on that enemy; on self the client drops it without a word
            // (0 of 95 attempts, 2026-09-08). The catalogue's range tells the two apart.
            if (borrow && (timing == Timing.Never || (BeastMasterSettings.Instance.PreferBorrow && !BeastMasterRoutine.AnyKinship)))
                return await Spells.Borrow.Cast(Core.Me);

            if (timing == Timing.Now)
                return await Spells.TemperedRelease.Cast(TemperedReleaseOrderTarget());

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
        // A finisher waits for the piece to drop, but not past the last ten seconds of a physical vulnerability on it.
        private const int FinisherVulnerabilityWindowMs = 10000;
        // The time-to-die estimate reads zero until the tracker has a few seconds on the target; under this many
        // seconds left, a finisher is a beast sent home for nothing.
        private const int FinisherEstimateWarmupSeconds = 4;
        private const int FinisherWasteSeconds = 3;

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
                    if (ability.Has("Dispel") && !BeastMasterRoutine.HasStrippableBuff(target) && Combat.CombatTime.Elapsed.TotalSeconds < settings.TemperedReleaseDispelWaitSeconds)
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
                        || BeastMasterRoutine.MitigationWanted
                        ? Timing.Now : Timing.Later;

                case AbilityKind.CrowdControl:
                    return BeastMasterRoutine.EnemiesNearFamiliar(settings.TemperedReleaseSleepRadius) >= settings.TemperedReleaseSleepMinEnemies ? Timing.Now : Timing.Later;

                case AbilityKind.Finisher:
                    // A finisher on a target that is dying anyway sends the beast home for nothing: Final Sting went
                    // out on a Cavalier Piece at 408 of 46,602 HP (2026-09-16). One with Nature keeps for the next one.
                    // Read straight off the tracker: the shared check counts a fresh target's zero estimate as dying
                    // and skips bosses, and a piece is one or the other for most of a node.
                    if (target.TimeInCombat() >= FinisherEstimateWarmupSeconds && target.CombatTimeLeft() > 0 && target.CombatTimeLeft() < FinisherWasteSeconds)
                        return Timing.Later;

                    var lowEnough = target.CurrentHealthPercent <= settings.TemperedReleaseFinisherHealthPercent;
                    if (BeastMasterRoutine.InCrucible)
                    {
                        // In a node the other horns are on their 90 s cooldowns from the swaps that brought this beast
                        // (the Vilekin team, 2026-09-12: Damselfly, Mantis, then the Wespe), so waiting for one held
                        // Final Sting for the whole fight. It goes at the threshold, or before a physical vulnerability the
                        // team put on the piece (Eerie Soundwave, +10 % for 30 s) runs out. The only finisher in the
                        // bestiary is piercing, so the window is read for every finisher until a magical one exists.
                        var windowClosing = target.HasAura(Auras.PhysicalVulnerabilityUp) && !target.HasAura(Auras.PhysicalVulnerabilityUp, false, FinisherVulnerabilityWindowMs);
                        return lowEnough || windowClosing ? Timing.Now : Timing.Later;
                    }

                    if (!lowEnough)
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
                    // This holds whatever the familiar bar reads. The one window it takes is the one after Rally,
                    // with our bar at 250: its link opens the window the 250 axe turns into Universality.
                    if (!BeastMasterRoutine.TrickContinuesChain && !BeastMasterRoutine.TrickTakesWindow)
                        return false;
                }
                else if (BeastMasterRoutine.PetTpFull && BeastMasterRoutine.Tp < LoneTrickOurTpBelow)
                {
                    // Nothing lit, the familiar bar is full and our TP is far from a pair: every further
                    // auto-attack is lost, so the Trick goes out on its own. With our TP nearer, the same Trick
                    // inside a pair is worth far more than the few auto-attacks the wait loses.
                }
                else if (BeastMasterRoutine.NaturalPreferred && !BeastMasterRoutine.HoldPairForRally
                         && BeastMasterRoutine.HasTpFor(BeastMasterRoutine.AxeFor(Affinity.Previous(affinity))))
                {
                    // Two yellow diamonds and no blue, or the yellow ones full: our axe opens this pair so the
                    // Trick finishes it and the stack lands on blue. Only while that axe can go now, or the Trick
                    // would wait on our TP for nothing.
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
        // A Crucible swap has to buy something (user, 2026-09-12: a swap two seconds after a summon, into a beast no
        // healthier, is a surprise mid-fight). The next beast must be above the swap line and clearly healthier than
        // the one out, a beast just summoned gets a few seconds, and a beast with no replacement ready stays; only a
        // critical beast (half the swap line) leaves regardless, since a dead beast is gone for the run.
        private const float SwapGainPercent = 10f;
        private const double SwapGraceSeconds = 8;
        private static uint _swapHeldFor;
        // The horn has a one-second cast, so a step or an animation lock refuses it for a pulse; a ready horn is retried
        // this long before a critical beast leaves by Parting Blow instead.
        private const double HornRetrySeconds = 2;
        private static System.DateTime _hornRefusedSince = System.DateTime.MinValue;

        public static async Task<bool> PartingBlow()
        {
            if (!BeastMasterSettings.Instance.UsePartingBlow || !BeastMasterRoutine.FamiliarOut || !Spells.PartingBlow.IsKnown())
                return false;

            // The beast is owed the next link of the chain, or Rally is about to open the window it takes: the exit
            // waits the few seconds that costs rather than send the beast home out of its own Universality chain.
            if (BeastMasterRoutine.FamiliarLinkPending || BeastMasterRoutine.RallyImminent)
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

            // In the Crucible the three beasts in the slots are the node's share of a finite roster and their HP
            // carries from node to node (first run 2026-09-09: the field cycle put all three in front of the piece
            // every node and one died). The familiar out stays out; it leaves only when its health says so and a
            // horn can bring another. No spacing exit, no time-to-death exit.
            if (BeastMasterRoutine.InCrucible)
            {
                // A beast already on its way out is not read again: its object vanishes mid-retreat and the health
                // read threw out of the rotation (Ice Golem, run 3, 2026-09-10).
                if (BeastMasterRoutine.FamiliarRetreating)
                    return false;

                var pet = Core.Me.Pet;
                if (pet == null || !pet.IsValid)
                    return false;

                float petHealth;
                try { petHealth = pet.CurrentHealthPercent; }
                catch { return false; }

                if (petHealth > BeastMasterSettings.Instance.CrucibleSwapHealthPercent)
                {
                    _hornRefusedSince = System.DateTime.MinValue;
                    return false;
                }

                // A beast that is covering you stays, unless it is about to be lost anyway: the horn takes the Snarl
                // with it. On the Third Board (2026-09-16) the covering Damselfly was swapped out at 26 % with the
                // player at 19 %, the cover ended with it, and the player was dead two hits later.
                if (Core.Me.HasAura(Auras.Covered) && petHealth > BeastMasterSettings.Instance.CrucibleSwapHealthPercent / 2)
                    return false;

                // A horn blown over the beast swaps it in a second with its HP intact. Parting Blow is the fallback:
                // the beast keeps taking hits while it performs the blow and retreats, and at 38 % that was fatal
                // (Behemoth, run 2, 2026-09-09), which is why the threshold sits where it does.
                var horn = BeastMasterRoutine.AnotherReadyHorn;
                var next = BeastMasterRoutine.NextHealth(horn);
                var nextName = horn == null ? "nothing" : BeastMasterRoutine.SlotPet(BeastMasterRoutine.HornSlot(horn)).ToString();
                var critical = petHealth <= BeastMasterSettings.Instance.CrucibleSwapHealthPercent / 2;

                if (!critical)
                {
                    if (horn == null)
                        return false;

                    if (next <= BeastMasterSettings.Instance.CrucibleSwapHealthPercent || next < petHealth + SwapGainPercent)
                    {
                        if (_swapHeldFor != pet.ObjectId)
                        {
                            _swapHeldFor = pet.ObjectId;
                            Logger.WriteInfo("[Beastmaster] Crucible: " + pet.EnglishName + " at " + petHealth.ToString("0") + " % stays; the next beast, " + nextName + ", was last seen at " + next.ToString("0") + " %.");
                        }
                        return false;
                    }

                    if (BeastMasterRoutine.FamiliarOutSeconds < SwapGraceSeconds)
                        return false;
                }

                if (horn != null && await horn.Cast(Core.Me))
                {
                    // A beast on its way out after a Parting Blow the routine did not cast reads 0 HP (2026-09-11: every
                    // "at 0 %" swap followed a hand-cast Parting Blow); the horn then brings the next beast, not a swap.
                    Logger.WriteInfo(petHealth <= 0
                        ? "[Beastmaster] Crucible: " + pet.EnglishName + " is already leaving; the horn brings " + nextName + " (last seen at " + next.ToString("0") + " %)."
                        : "[Beastmaster] Crucible: " + pet.EnglishName + " at " + petHealth.ToString("0") + " % is swapped out by the horn for " + nextName + " (last seen at " + next.ToString("0") + " %).");
                    BeastMasterRoutine.NoteHornCast(horn);
                    BeastMasterRoutine.NotePartingBlow();
                    _hornRefusedSince = System.DateTime.MinValue;
                    return true;
                }

                if (horn != null)
                {
                    // A ready horn the client refused is retried, not given up on. On the Third Board (2026-09-16) the
                    // covering Mantis at 22 % left by Parting Blow, cover and all, in the pulse the Wespe's horn was
                    // refused, and the Wespe arrived six seconds later instead of at once with the Mantis's HP kept;
                    // the covering Damselfly at 25 % went the same way four minutes later with the player at 15 %.
                    // A beast above the critical line never leaves by the blow: the horn is the only swap for it.
                    if (_hornRefusedSince == System.DateTime.MinValue)
                        _hornRefusedSince = System.DateTime.Now;
                    if (!critical || (System.DateTime.Now - _hornRefusedSince).TotalSeconds < HornRetrySeconds)
                        return false;
                }

                // No horn ready, or none the client would take: only a critical beast retreats into an empty slot, since
                // staying would lose it.
                if (petHealth <= 0 || !await Spells.PartingBlow.Cast(Core.Me.CurrentTarget))
                    return false;

                _hornRefusedSince = System.DateTime.MinValue;
                Logger.WriteInfo("[Beastmaster] Crucible: " + pet.EnglishName + " at " + petHealth.ToString("0") + " % leaves by Parting Blow before it dies; "
                    + (horn == null ? "no horn is ready." : "the horn was refused for " + HornRetrySeconds.ToString("0") + " s."));
                BeastMasterRoutine.NotePartingBlow();
                return true;
            }

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
            if (!outOfMelee && !BeastMasterRoutine.HasStrippableBuff(target))
                return false;

            return await Spells.QuellingWave.Cast(target);
        }

        /// <summary>
        /// Rally spends the Mastered Instinct our combos built (40 TP, +70 per stack). It goes out at the cap, or
        /// with two stacks when an axe is waiting on TP.
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
                // Two yellow with a blue banked: Rally (40 + 140) plus what the bar holds reaches 250 as well, and the
                // blue pays the Trick that takes the window as the next link before the 250 axe (the chain measured
                // at 4x the axe on 2026-09-12: pair, both Rallies, Trick, then the opposite 250 axe).
                var withBlue = stacks == 2 && BeastMasterRoutine.NaturalInstinct >= 1
                    && BeastMasterRoutine.Tp + 40 + 70 * stacks >= BeastMasterRoutine.TpCap;
                if (stacks < 3 && !withBlue)
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

            return await Spells.Rally.Cast(Core.Me);
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

            // The window after Rally: the familiar takes the next link with its Trick only if it can pay, and the blue
            // diamond is what pays it. Cheer goes at once there.
            if (BeastMasterRoutine.TrickTakesWindow && !BeastMasterRoutine.HasTpFor(Spells.Trick) && !BeastMasterRoutine.FamiliarRetreating)
                return await Spells.RallyingCheer.Cast(Core.Me);

            if (BeastMasterRoutine.KeepBlueForRally)
                return false;

            if (!BeastMasterRoutine.PairWaitingOnFamiliar && (!BeastMasterRoutine.TrickSettled || BeastMasterRoutine.FamiliarRetreating))
                return false;

            return await Spells.RallyingCheer.Cast(Core.Me);
        }
    }
}
