using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.BeastMaster;
using System.Collections.Generic;
using System.Linq;
using Gauge = ff14bot.Managers.ActionResourceManager.BeastMaster;

namespace Magitek.Utilities.Routines
{
    internal static class BeastMaster
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.BeastMaster, Spells.SmashAxe, new List<SpellData>());

        // Cached each pulse
        public static BeastMasterFamiliar Familiar;
        public static SpellData[] Axes = new SpellData[4];
        private static string _unmatchedFamiliar;
        private static int _loggedFamiliarId;
        private static uint _petObjectId;

        /// <summary>When the familiar out now arrived (a new pet object was first seen).</summary>
        public static System.DateTime FamiliarSince = System.DateTime.MinValue;
        public static double FamiliarOutSeconds => FamiliarOut ? (System.DateTime.Now - FamiliarSince).TotalSeconds : 0;

        // Parting Blow sends the familiar home, but its pet object lingers through the retreat: a Trick ordered in
        // that moment is accepted by the client, acted on by nobody, and costs the whole familiar TP (144 lost on
        // the dummy, 2026-09-09). The beast counts as retreating until a different pet object is out.
        private static uint _petAtPartingBlow;
        private static System.DateTime _partingBlowAt = System.DateTime.MinValue;
        private const int RetreatMaxMs = 10000;

        public static void NotePartingBlow()
        {
            _petAtPartingBlow = Core.Me.Pet?.ObjectId ?? 0;
            _partingBlowAt = System.DateTime.Now;
        }

        public static bool FamiliarRetreating =>
            FamiliarOut && Core.Me.Pet.ObjectId == _petAtPartingBlow
            && (System.DateTime.Now - _partingBlowAt).TotalMilliseconds < RetreatMaxMs;

        /// <summary>Both gauges cap at 250 (measured 2026-09-09); an axe spends the whole bar, at full potency from here.</summary>
        public const int TpCap = 250;
        public static bool TpFull => Gauge.TP >= TpCap;
        public static int Tp => (int)Gauge.TP;
        public static int PetTp => (int)Gauge.PetTP;

        /// <summary>
        /// Our half of a pair is paid (a 100 TP axe is affordable) and the familiar cannot pay its Trick yet. Not
        /// in the moment after a Trick order, before its Heart shows: the familiar TP reads zero there and a Cheer
        /// in that gap refilled a bar that had just been spent (dummy, 2026-09-09).
        /// </summary>
        /// <summary>The last Trick order has had all the time the familiar needs to act on it.</summary>
        public static bool TrickSettled => TrickActed || MsSinceTrick > TrickLandingMs + 2000;

        public static bool PairWaitingOnFamiliar =>
            FamiliarOut && !FamiliarRetreating && CurrentHeart == null && !WaveringHeart && TrickSettled
            && !HasTpFor(Spells.Trick) && Axes.Any(a => a != null && a.Cost <= 100 && HasTpFor(a));

        public static bool PetTpFull => Gauge.PetTP >= TpCap;

        // A horn the swap logic wants blown next (after Parting Blow sent the current familiar home).
        public static SpellData WantedHorn;
        private static System.DateTime _wantedHornSince = System.DateTime.MinValue;
        private const int WantedHornMs = 10000;

        // A horn blown by anyone, the player included, shows as that horn on its 2 s recast. The familiar arrives
        // about a second after the cast, and in that gap no familiar is out: the auto-summon must not answer the
        // gap with a second horn (it did, 2026-09-08, and wasted the player's own choice).
        private static System.DateTime _hornSeenRecasting = System.DateTime.MinValue;
        private const int HornArrivalGraceMs = 5000;

        // The post-cast recast is 2 s; the cooldown a retreat starts is 90 s. Only the short one means a horn was
        // just blown; the long one would otherwise keep the grace alive for a minute and a half.
        private static readonly System.TimeSpan HornRecast = System.TimeSpan.FromSeconds(3);

        public static bool HornJustBlown =>
            Battlehorns.Any(h => Casting.LastSpellWas(h, HornArrivalGraceMs))
            || (System.DateTime.Now - _hornSeenRecasting).TotalMilliseconds < HornArrivalGraceMs;

        // The bestiary unlock state is empty on a fresh client until it is fetched (RB 1.0.916, probed 2026-09-12: no
        // beast unlocked and an empty list until PetManager.EnsureBeastmasterPetUnlockStateAsync had run, all fifty
        // and true six seconds later). The fetch is started once from the pulse and never awaited on it (that would
        // hang the client); the reads that depend on it wait for the flag, and after three failed attempts they fall
        // back to the cache so a broken fetch cannot switch Capture off for good.
        private static System.Threading.Tasks.Task<bool> _unlockStateTask;
        private static System.DateTime _unlockStateAskedAt = System.DateTime.MinValue;
        private static int _unlockStateAttempts;
        private static bool _unlockStateLogged;
        private const int UnlockStateMaxAttempts = 3;

        /// <summary>The bestiary unlock state has been fetched, so the unlock reads mean what they say.</summary>
        public static bool UnlockStateReady => _unlockStateTask != null && _unlockStateTask.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && _unlockStateTask.Result;

        /// <summary>The fetch failed three times; the unlock reads are trusted as they are.</summary>
        public static bool UnlockStateGaveUp => !UnlockStateReady && _unlockStateAttempts >= UnlockStateMaxAttempts;

        private static void EnsureUnlockState()
        {
            if (_unlockStateTask != null && !_unlockStateTask.IsCompleted)
                return;

            if (UnlockStateReady)
            {
                if (!_unlockStateLogged)
                {
                    _unlockStateLogged = true;
                    Logger.WriteInfo("[Beastmaster] Bestiary: " + PetManager.UnlockedBeastmasterPets.Count(p => p != BeastmasterPet.None) + " beasts unlocked.");
                }
                return;
            }

            if (_unlockStateAttempts >= UnlockStateMaxAttempts || (System.DateTime.Now - _unlockStateAskedAt).TotalSeconds < 10)
            {
                if (_unlockStateAttempts >= UnlockStateMaxAttempts && !_unlockStateLogged)
                {
                    _unlockStateLogged = true;
                    Logger.WriteInfo("[Beastmaster] Bestiary: the unlock state could not be fetched; the cached reads are trusted as they are.");
                }
                return;
            }

            _unlockStateAskedAt = System.DateTime.Now;
            _unlockStateAttempts++;
            try { _unlockStateTask = PetManager.EnsureBeastmasterPetUnlockStateAsync(); }
            catch (System.Exception e)
            {
                _unlockStateTask = null;
                Logger.WriteInfo("[Beastmaster] Bestiary: the unlock state fetch threw " + e.GetType().Name + " (attempt " + _unlockStateAttempts + ").");
            }
        }

        public static void RefreshVars()
        {
            EnsureUnlockState();

            if (Battlehorns.Any(h => h.IsKnown() && h.Cooldown > System.TimeSpan.Zero && h.Cooldown <= HornRecast))
                _hornSeenRecasting = System.DateTime.Now;

            // At level 50 the bar swaps the axes for their 250 TP forms; one masked read per axe per pulse.
            Axes[0] = Spells.GaleAxe.Masked();
            Axes[1] = Spells.AvalancheAxe.Masked();
            Axes[2] = Spells.MistralAxe.Masked();
            Axes[3] = Spells.SpinningAxe.Masked();

            Familiar = FamiliarOut ? CurrentFamiliar() : null;
            TrackTrickActed();
            TrackWaveringHeart();
            TrackCaptureHold();
            TrackCruciblePiece();

            // The compass state a pair consumes says who finished it; kept from the pulse before Wavering Heart,
            // together with the colour of the beast that was out then (a swap or a Parting Blow can replace it
            // before the bot sees Wavering Heart).
            var compass = Gauge.InnerCompass;
            if (compass != Gauge.InnerCompassState.Wavering && compass != Gauge.InnerCompassState.None)
            {
                _compassBeforeWavering = compass;
                _familiarBeforeWavering = FamiliarAffinity;
            }

            if (!FamiliarOut)
            {
                _loggedFamiliarId = 0;
                _petObjectId = 0;
                return;
            }

            if (Core.Me.Pet.ObjectId != _petObjectId)
            {
                _petObjectId = Core.Me.Pet.ObjectId;
                FamiliarSince = System.DateTime.Now;
            }

            if (Familiar == null && _unmatchedFamiliar != Core.Me.Pet.EnglishName)
            {
                _unmatchedFamiliar = Core.Me.Pet.EnglishName;
                Logger.WriteInfo($"[Beastmaster] Familiar \"{_unmatchedFamiliar}\" is not in the bestiary; the compass will follow your own Hearts only.");
            }

            if (Familiar != null && _loggedFamiliarId != Familiar.Id)
            {
                _loggedFamiliarId = Familiar.Id;
                Logger.WriteInfo($"[Beastmaster] Familiar out: {Familiar.Name} ({Familiar.Trick?.Affinity ?? "no"} Trick); gauge horn {Gauge.ActiveBattlehorn}, slots {string.Join(", ", PetManager.BeastmasterPetSlots)}.");
            }
        }

        /// <summary>Enemies within a radius of the summoned familiar (its abilities go out from where it stands).</summary>
        public static int EnemiesNearFamiliar(float yards)
        {
            var pet = Core.Me.Pet;
            return pet == null ? 0 : pet.EnemiesNearby(yards).Count();
        }

        /// <summary>A familiar is summoned. RebornBuddy exposes it as the player's pet.</summary>
        public static bool FamiliarOut => Core.Me.Pet != null && Core.Me.Pet.IsValid;

        public static BeastMasterFamiliar FamiliarFor(BeastmasterPet pet) =>
            pet == BeastmasterPet.None ? null : XivDataHelper.BeastMasterFamiliars.FirstOrDefault(f => f.Id == (int)pet);

        public static BeastMasterFamiliar FamiliarByName(string name) =>
            string.IsNullOrEmpty(name) ? null
                : XivDataHelper.BeastMasterFamiliars.FirstOrDefault(f => string.Equals(f.Name, name, System.StringComparison.OrdinalIgnoreCase));

        // The pet's name is the catalogue name; the gauge's active horn slot is the fallback for a renamed pet.
        private static BeastMasterFamiliar CurrentFamiliar() =>
            FamiliarByName(Core.Me.Pet?.EnglishName) ?? FamiliarFor(SlotPet(Gauge.ActiveBattlehorn));

        /// <summary>The affinity the summoned familiar's Trick carries, or null.</summary>
        public static string FamiliarAffinity => Familiar?.Trick?.Affinity;

        /// <summary>
        /// The Heart lit right now: the affinity of the last instinctual skill (mine or the familiar's), which the
        /// next one has to follow clockwise for an intentional combo. Null when nothing is lit.
        /// </summary>
        public static string CurrentHeart => HeartOf(Gauge.InnerCompass);

        private static string HeartOf(Gauge.InnerCompassState state)
        {
            switch (state)
            {
                case Gauge.InnerCompassState.Volant: return Affinity.Volant;
                case Gauge.InnerCompassState.Rampant: return Affinity.Rampant;
                case Gauge.InnerCompassState.Durant: return Affinity.Durant;
                case Gauge.InnerCompassState.Eldritch: return Affinity.Eldritch;
                default: return null;
            }
        }

        private static Gauge.InnerCompassState _compassBeforeWavering = Gauge.InnerCompassState.None;

        private static string _familiarBeforeWavering;

        // A Heart takes a moment to appear after our own axe; until it does, the axe just cast says what the Heart
        // will be, so the follow-up is chosen right instead of restarting the chain.
        private const int HeartLagMs = 1500;

        // Trick is an order: the familiar acts on its own time (its hit lands about 0.2 s after the order, its Heart
        // shows on us about 0.8 s after, measured on ACT 2026-09-08). An axe thrown before that Heart continues
        // nothing; one thrown the moment it shows completes the pair. So after a Trick the axe waits for the Heart,
        // up to this long.
        // Right after a summon the familiar is busy with its Tempered Release ability and the Trick queues behind
        // it: 3.3 to 3.8 s from order to action, Heart a second later (dummy, 2026-09-09). Four seconds expired in
        // the instant before the Heart showed, and a second Trick and a Rallying Cheer went into that instant.
        private const int TrickLandingMs = 6500;

        public static System.DateTime LastTrickAt = System.DateTime.MinValue;
        public static double MsSinceTrick => (System.DateTime.Now - LastTrickAt).TotalMilliseconds;

        /// <summary>
        /// A Trick was ordered and the familiar's Heart has not shown yet: no instinctual skill of ours should go
        /// out. Keyed on the order's own time, not on "the last spell was Trick": a Smash Axe in between made the
        /// routine forget the order and open a new chain with the wrong affinity (six wrong-order pairs, 2026-09-08).
        /// </summary>
        public static bool TrickPending => !TrickActed && CurrentHeart == null && MsSinceTrick < TrickLandingMs;

        // The order has visibly been acted on: the Heart of the familiar showed (Trick first) or the pair it
        // finished is resolving (axe first). Seen within the landing window, it ends the wait at once; the
        // window itself is only the fallback for a Trick the bot never sees land.
        private static System.DateTime _trickActedFor = System.DateTime.MinValue;
        public static bool TrickActed => LastTrickAt != System.DateTime.MinValue && _trickActedFor == LastTrickAt;

        private static void TrackTrickActed()
        {
            if (_trickActedFor == LastTrickAt || MsSinceTrick > TrickLandingMs + 2000)
                return;
            var heart = CurrentHeart;
            if (WaveringHeart || (heart != null && heart == FamiliarAffinity))
                _trickActedFor = LastTrickAt;
        }

        // How a combo resolves (ACT, 76 intentional combos on 2026-09-08): the finishing skill consumes the Heart and
        // the compass reads Wavering; the second half of the combo damage lands 2.1 s later, Wavering clears, and a
        // Sunstrider or Moonstalker window (7 s) opens for the next link. Nothing chains with the familiar while
        // Wavering is up, so instinctual actions wait it out; in the window that follows, the chain continues
        // clockwise from the affinity of the skill that finished the pair, not from a Heart (there is none).
        public static string LastInstinctAffinity;
        private static System.DateTime _lastInstinctAt = System.DateTime.MinValue;
        private const int InstinctMemoryMs = 10000;

        public static bool ChainWindowOpen =>
            Gauge.InnerCompass == Gauge.InnerCompassState.Sunstrider || Gauge.InnerCompass == Gauge.InnerCompassState.Moonstalker;

        public static void NoteInstinct(string affinity)
        {
            if (affinity == null)
                return;
            LastInstinctAffinity = affinity;
            _lastInstinctAt = System.DateTime.Now;
        }

        // Mastered Instinct (Wild Heart III, level 28) and Natural Instinct (Wild Heart IV, level 40) are read off
        // the gauge (RebornBuddy 1.0.915).
        public static int MasteredInstinct => Gauge.MasteredInstinct;
        public static int NaturalInstinct => Gauge.NaturalInstinct;
        private const int InstinctStacksMax = 3;

        /// <summary>
        /// The yellow diamonds are full: a pair our axe finishes pays a stack that overflows, even the pair Rally
        /// fires on, since the stack lands at the finishing hit and Rally spends 0.9 s later. That pair is better
        /// finished by the Trick, paying a Natural stack for Rallying Cheer. Three pairs and the Universality pay
        /// four Mastered per 90 s Rally, so one was lost every cycle (dummy, 2026-09-09, twice over).
        /// </summary>
        public static bool NaturalPreferred => MasteredInstinct >= InstinctStacksMax && NaturalInstinct < InstinctStacksMax;

        /// <summary>
        /// Rally is a few seconds from ready with the yellow diamonds full: the next pair is worth holding so its
        /// window is the one Rally spends into (finishers came every 105 s instead of 90 on the dummy). Held only
        /// while our TP is short of the cap, where the axes turn into their 250 forms and cannot pair.
        /// </summary>
        public static bool HoldPairForRally =>
            BeastMasterSettings.Instance.UseRally && MasteredInstinct >= InstinctStacksMax
            && Spells.Rally.IsKnown() && Spells.Rally.Cooldown > System.TimeSpan.Zero
            && Spells.Rally.Cooldown.TotalSeconds <= RallyHoldSeconds && Gauge.TP < TpCap - 50;
        private const int RallyHoldSeconds = 12;

        /// <summary>The familiar is here, not leaving, and holds the TP for its Trick.</summary>
        public static bool FamiliarCanAnswer => FamiliarAffinity != null && !FamiliarRetreating && HasTpFor(Spells.Trick);

        /// <summary>
        /// The affinity the next link has to follow: the Heart lit now, the one about to be lit by an axe just
        /// cast, or, inside a Sunstrider/Moonstalker window, the skill that finished the last pair.
        /// </summary>
        public static string EffectiveHeart
        {
            get
            {
                var heart = CurrentHeart;
                if (heart != null)
                    return heart;

                foreach (var axe in Axes)
                {
                    if (axe != null && Casting.LastSpellWas(axe, HeartLagMs) && System.Array.IndexOf(Affinity.Clockwise, AxeAffinity(axe)) >= 0)
                        return AxeAffinity(axe);
                }

                if (ChainWindowOpen && LastInstinctAffinity != null && (System.DateTime.Now - _lastInstinctAt).TotalMilliseconds < InstinctMemoryMs)
                    return LastInstinctAffinity;

                return null;
            }
        }

        /// <summary>The familiar's Trick would continue the chain from the Heart lit now.</summary>
        public static bool TrickContinuesChain =>
            EffectiveHeart != null && FamiliarAffinity != null && FamiliarAffinity == Affinity.Next(EffectiveHeart);

        /// <summary>
        /// Wavering: a pair just completed and is resolving (2.1 s to the second hit), during which nothing chains
        /// with the familiar. Its appearance is the combo signal, and says who finished it.
        /// </summary>
        public static bool WaveringHeart => Gauge.InnerCompass == Gauge.InnerCompassState.Wavering;
        private static bool _waveringLogged;

        private static void TrackWaveringHeart()
        {
            if (!WaveringHeart)
            {
                _waveringLogged = false;
                return;
            }

            if (_waveringLogged)
                return;

            _waveringLogged = true;

            // The state the pair consumed says who finished it. A Heart of the familiar's own colour was lit by its
            // Trick, so our axe followed it; a Heart of any other colour was ours, so the Trick followed. Inside a
            // Sunstrider or Moonstalker window only our 250 TP forms act. "The last spell was an axe" is not the
            // test: a Smash Axe in the 1.7 s before the bot sees Wavering Heart credited the familiar with a pair
            // Spinning Axe finished (dummy, 2026-09-09).
            var before = _compassBeforeWavering;
            var heart = HeartOf(before);
            var window = before == Gauge.InnerCompassState.Sunstrider || before == Gauge.InnerCompassState.Moonstalker;

            // Inside a window either our 250 TP axe or the Trick of the familiar can act, and the Trick did on
            // 2026-09-09 (it consumed a Sunstrider window as chain link 2, logged as Universality): ours only if
            // our 250 axe was noted moments ago.
            var ours = window
                ? (LastInstinctAffinity == Affinity.Sunstrider || LastInstinctAffinity == Affinity.Moonstalker)
                    && (System.DateTime.Now - _lastInstinctAt).TotalMilliseconds < 3000
                : heart != null && heart == _familiarBeforeWavering;

            string finisher = null;
            if (ours)
            {
                if (heart != null)
                {
                    finisher = Affinity.Next(heart);
                    NoteInstinct(finisher);
                }
            }
            else
            {
                NoteInstinct(FamiliarAffinity);
            }

            var by = ours ? (window ? "Universality" : AxeFor(finisher)?.LocalizedName ?? "our axe") : "the familiar";
            Logger.WriteInfo($"[Beastmaster] Combo completed by {by} (chain {Gauge.ComboCounter}; instinct {MasteredInstinct} mastered / {NaturalInstinct} natural).");
        }

        /// <summary>
        /// The affinity an axe carries in the form the bar shows now: its compass point, or at level 50 with 250 TP
        /// Sunstrider (Brutal Rage, Risen Fall) or Moonstalker (Hawkish Talons, Calamity).
        /// </summary>
        public static string AxeAffinity(SpellData axe)
        {
            if (axe == null)
                return null;
            if (axe.Id == Spells.BrutalRage.Id || axe.Id == Spells.RisenFall.Id)
                return Affinity.Sunstrider;
            if (axe.Id == Spells.HawkishTalons.Id || axe.Id == Spells.Calamity.Id)
                return Affinity.Moonstalker;
            var i = System.Array.IndexOf(Axes, axe);
            return i < 0 ? null : Affinity.Clockwise[i];
        }

        /// <summary>The player's axe carrying a given affinity in its current form, or null (a compass point is not on the bar once the axes have turned).</summary>
        public static SpellData AxeFor(string affinity) => affinity == null ? null : Axes.FirstOrDefault(a => AxeAffinity(a) == affinity);

        /// <summary>
        /// Level 50: the 250 TP axes make no intentional combo; a Sunstrider axe under Moonstalker, or a Moonstalker
        /// axe under Sunstrider, is the infinitive combo Universality. Null outside those windows or below 250 TP.
        /// </summary>
        public static SpellData UniversalityAxe()
        {
            string wanted;
            switch (Gauge.InnerCompass)
            {
                case Gauge.InnerCompassState.Sunstrider: wanted = Affinity.Moonstalker; break;
                case Gauge.InnerCompassState.Moonstalker: wanted = Affinity.Sunstrider; break;
                default: return null;
            }
            // Risen Fall and Calamity stay put; Brutal Rage and Hawkish Talons rush to the target. The standing ones first.
            return Axes.Where(a => AxeAffinity(a) == wanted && HasTpFor(a))
                .OrderBy(a => a.Id == Spells.BrutalRage.Id || a.Id == Spells.HawkishTalons.Id ? 1 : 0)
                .FirstOrDefault();
        }

        // Cost types on the Action sheet: 111 is the player's TP, 112 the familiar's.
        private const int FamiliarTpCostType = 112;

        /// <summary>The skill is known and the gauge it draws on holds its cost (100, or 250 for the level 50 forms).</summary>
        public static bool HasTpFor(SpellData skill)
        {
            if (skill == null || !skill.IsKnown())
                return false;
            var tp = (int)skill.CostType == FamiliarTpCostType ? Gauge.PetTP : Gauge.TP;
            return tp >= skill.Cost;
        }

        public static bool KinshipOf(uint timed, uint permanent) => Core.Me.HasAura(timed) || Core.Me.HasAura(permanent);

        public static bool BeastKinship => KinshipOf(Auras.BeastKinship, Auras.BeastKinshipSummoned);
        public static bool VileKinship => KinshipOf(Auras.VileKinship, Auras.VileKinshipSummoned);
        public static bool CloudKinship => KinshipOf(Auras.CloudKinship, Auras.CloudKinshipSummoned);
        public static bool SeedKinship => KinshipOf(Auras.SeedKinship, Auras.SeedKinshipSummoned);
        public static bool WaveKinship => KinshipOf(Auras.WaveKinship, Auras.WaveKinshipSummoned);
        public static bool ScaleKinship => KinshipOf(Auras.ScaleKinship, Auras.ScaleKinshipSummoned);
        public static bool SoulKinship => KinshipOf(Auras.SoulKinship, Auras.SoulKinshipSummoned);
        public static bool AshKinship => KinshipOf(Auras.AshKinship, Auras.AshKinshipSummoned);
        public static bool AnyKinship => BeastKinship || VileKinship || CloudKinship || SeedKinship || WaveKinship || ScaleKinship || SoulKinship || AshKinship;

        public static readonly SpellData[] Battlehorns = { Spells.FirstBattlehorn, Spells.SecondBattlehorn, Spells.ThirdBattlehorn };

        // The Crucible of the Unbroken (7.56): the duty owns the battlehorns. It empties the slots between nodes
        // and fills them from its own roster, and summoning on the board is blocked. Writing pacts into those
        // slots and blowing horns on the board crashed two players on launch day (2026-09-09). Territories f1x1
        // to f1x5: the three Boards and the two Master Boards.
        private static readonly HashSet<uint> CrucibleZones = new HashSet<uint> { 1339, 1340, 1341, 1342, 1343 };
        public static bool InCrucible => CrucibleZones.Contains(WorldManager.ZoneId);
        private static bool _crucibleLogged;

        /// <summary>Inside the Crucible the routine leaves the horns and the slots to the duty; said once per visit.</summary>
        public static bool LeaveHornsToTheDuty()
        {
            if (!InCrucible)
            {
                _crucibleLogged = false;
                return false;
            }

            if (!_crucibleLogged)
            {
                _crucibleLogged = true;
                Logger.WriteInfo("[Beastmaster] Crucible of the Unbroken: the duty assigns the battlehorns, so the slots are left alone and a familiar is summoned only in a fight.");
            }

            return true;
        }

        /// <summary>Milliseconds left on the lit Heart, 0 when none.</summary>
        public static double HeartMsLeft => CurrentHeart == null ? 0 : Gauge.InnerCompassTimer.TotalMilliseconds;

        /// <summary>Horn slot 1-3, or 0 for anything else.</summary>
        public static int HornSlot(SpellData horn) => horn == null ? 0 : System.Array.IndexOf(Battlehorns, horn) + 1;

        /// <summary>The beast assigned to a horn slot (1-3) in the Master's Bestiary, or None.</summary>
        public static BeastmasterPet SlotPet(int slot)
        {
            var slots = PetManager.BeastmasterPetSlots;
            return slot >= 1 && slot <= slots.Length ? slots[slot - 1] : BeastmasterPet.None;
        }

        /// <summary>The horn whose slot holds the familiar out, or null.</summary>
        public static SpellData ActiveHorn =>
            Familiar == null ? null : Battlehorns.FirstOrDefault(h => (int)SlotPet(HornSlot(h)) == Familiar.Id);

        public static void NoteHornCast(SpellData horn)
        {
            if (horn == WantedHorn)
                WantedHorn = null;
        }

        public static void WantHorn(SpellData horn)
        {
            WantedHorn = horn;
            _wantedHornSince = System.DateTime.Now;
        }

        /// <summary>The Trick affinity of the beast in a horn's slot, or null.</summary>
        public static string HornAffinity(SpellData horn) => FamiliarFor(SlotPet(HornSlot(horn)))?.Trick?.Affinity;

        private static bool HornReady(SpellData horn) =>
            horn.IsKnownAndReady() && SlotPet(HornSlot(horn)) != BeastmasterPet.None;

        /// <summary>A horn other than the familiar's own, off cooldown, with a beast in its slot.</summary>
        public static bool AnotherHornReady => Battlehorns.Any(h => h != ActiveHorn && HornReady(h));

        /// <summary>That horn, or null.</summary>
        public static SpellData AnotherReadyHorn => Battlehorns.FirstOrDefault(h => h != ActiveHorn && HornReady(h));
        // Enemy buffs a dispel removes although the status sheet does not flag them dispellable: the Piscodemon
        // Piece's Damage Up (1225, from its Clear Mind, 15 s) went at the Quelling Wave that hit it seven seconds in
        // (Crucible, 2026-09-10 21:58). The sheet flag stays the first test; this list is the second.
        private static readonly HashSet<uint> StrippableBuffs = new HashSet<uint> { 1225 };

        /// <summary>A buff on the target worth a dispel: flagged by the sheet, or on the list above.</summary>
        public static bool HasStrippableBuff(GameObject target)
        {
            var character = target as Character;
            if (character == null || !character.IsValid)
                return false;

            return character.HasDispellableBuff() || character.CharacterAuras.Any(a => StrippableBuffs.Contains(a.Id));
        }

        /// <summary>Another horn, off cooldown, whose beast carries this affinity.</summary>
        public static SpellData SwapHornFor(string affinity) =>
            affinity == null ? null : Battlehorns.FirstOrDefault(h => h != ActiveHorn && HornReady(h) && HornAffinity(h) == affinity);

        /// <summary>A horn that can be blown now: the one the swap asked for, else the preferred one first.</summary>
        public static SpellData ReadyBattlehorn()
        {
            if (WantedHorn != null && (System.DateTime.Now - _wantedHornSince).TotalMilliseconds > WantedHornMs)
                WantedHorn = null;
            if (WantedHorn != null && WantedHorn != ActiveHorn && WantedHorn.IsKnownAndReady())
                return WantedHorn;

            var preferred = System.Math.Max(1, System.Math.Min(3, BeastMasterSettings.Instance.PreferredBattlehorn)) - 1;
            for (var i = 0; i < 3; i++)
            {
                var horn = Battlehorns[(preferred + i) % 3];
                if (horn != ActiveHorn && HornReady(horn))
                    return horn;
            }
            return null;
        }

        public static bool CheckTTDIsEnemyDyingSoon() => Common.CheckTTDIsEnemyDyingSoon(BeastMasterSettings.Instance);

        /// <summary>
        /// The bestiary entry a wild beast would fill, or None when it is not a capturable beast. The game flags the
        /// capturable mobs on their BNpcBase row and the species follows from the model skeleton
        /// (Resources/BeastMasterCapturableBases.json, keyed by BNpcBase id): a Black Eft is a salamander, an Anole a
        /// raptor, a Lemur nothing.
        /// </summary>
        public static BeastmasterPet PetFor(BattleCharacter target)
        {
            if (target == null || !target.IsNpc)
                return BeastmasterPet.None;
            return XivDataHelper.BeastMasterCapturableBases.TryGetValue(target.BaseId, out var pet) ? (BeastmasterPet)(byte)pet : BeastmasterPet.None;
        }

        /// <summary>
        /// A beast Capture should go on: a capturable beast the bestiary lacks, not above our level, not marked yet.
        /// Health is not part of it; Capture itself waits for the threshold, the hold below waits with it.
        /// </summary>
        public static bool CaptureWanted(BattleCharacter target)
        {
            if (!BeastMasterSettings.Instance.UseCapture || !Spells.Capture.IsKnown())
                return false;

            return PetFor(target) != BeastmasterPet.None && CaptureSkipReason(target) == null;
        }

        /// <summary>Why a capturable beast is not for capturing right now, or null when it is.</summary>
        public static string CaptureSkipReason(BattleCharacter target)
        {
            var pet = PetFor(target);
            if (pet == BeastmasterPet.None)
                return null;
            if (!UnlockStateReady && !UnlockStateGaveUp)
                return "the bestiary is still loading";
            if (PetManager.IsBeastmasterPetUnlocked(pet))
                return pet + " is already in the bestiary";
            if (target.HasAura(Auras.InterestCaptured))
                return "already marked";
            if (target.ClassLevel > Core.Me.ClassLevel)
                return "level " + target.ClassLevel + " against your " + Core.Me.ClassLevel + ", Capture would be ineffective";
            return null;
        }

        // Weaponskills and familiar orders wait while a capturable beast is unmarked: an axe at 55% is what kills it
        // before the mark. Auto-attacks (ours and the familiar's) bring it to the threshold, and Capture is next.
        public static bool HoldingForCapture;

        // A wanted beast at or above our level: auto-attacks alone never bring it to the threshold (a Morbol killed the
        // character three times), so the basic combo takes it there while the familiar's burst stays holstered.
        public static bool BasicComboForCapture;

        // The target the first Smash Axe went out on: that one hit is allowed, since auto-attack starts on it.
        public static uint EngagedTargetId;
        private static uint _holdLoggedFor;

        /// <summary>The Crucible piece this target is, by its BNpcName, or null outside the Crucible or for anything not in the library.</summary>
        public static CruciblePiece CruciblePieceFor(GameObject target)
        {
            if (target == null || !InCrucible)
                return null;

            return XivDataHelper.BeastMasterCruciblePieces.TryGetValue(target.NpcId, out var piece) ? piece : null;
        }

        /// <summary>The library entry for the current target, or null.</summary>
        public static CruciblePiece CurrentPiece => CruciblePieceFor(Core.Me.CurrentTarget);

        private static uint _pieceLoggedFor;

        // Once per piece targeted: what the library knows about it, so the log shows the plan the rules will build on.
        private static void TrackCruciblePiece()
        {
            var target = Core.Me.CurrentTarget;
            if (target == null || !InCrucible || _pieceLoggedFor == target.ObjectId)
                return;

            _pieceLoggedFor = target.ObjectId;
            var piece = CruciblePieceFor(target);
            if (piece == null)
            {
                Logger.WriteInfo("[Beastmaster] Crucible: " + target.EnglishName + " (NpcId " + target.NpcId + ") is not in the piece library.");
                return;
            }

            Logger.WriteInfo("[Beastmaster] Crucible piece: " + piece.Name + (piece.IsBoss ? " (boss)" : "") + (piece.HasWeakness ? ", weak to " + piece.Weakness : ", no weakness")
                + " (Str " + piece.Strength + ", Int " + piece.Intelligence + ", PhysRes " + piece.PhysicalResistance + ", MagRes " + piece.MagicResistance + ", Con " + piece.Constitution + ")"
                + (piece.Actions.Count > 0 ? "; " + string.Join(", ", piece.Actions.Select(a => a.Name)) : "") + ".");
        }

        private static void TrackCaptureHold()
        {
            var target = Core.Me.CurrentTarget as BattleCharacter;
            var wanted = CaptureWanted(target);
            // Twelve species are only in the bestiary as a duty boss (Karlabos in Sastasha, the Zu in Pharos Sirius),
            // so Capture goes out on a boss; the hold does not, since a boss dies by the party's damage, not ours.
            var boss = wanted && target.IsBoss();
            var belowUs = wanted && target.ClassLevel < Core.Me.ClassLevel;
            var holdWanted = BeastMasterSettings.Instance.HoldForCapture && Core.Me.InCombat && wanted && !boss;
            HoldingForCapture = holdWanted && belowUs;
            BasicComboForCapture = holdWanted && !belowUs;

            if (target == null || _holdLoggedFor == target.ObjectId)
                return;
            _holdLoggedFor = target.ObjectId;

            if (HoldingForCapture)
            {
                Logger.WriteInfo("[Beastmaster] Holding weaponskills on " + target.EnglishName + " until the mark is on it (auto-attacks only).");
                return;
            }
            if (BasicComboForCapture)
            {
                Logger.WriteInfo("[Beastmaster] Basic combo only on " + target.EnglishName + " until the mark is on it (level " + target.ClassLevel + " against your " + Core.Me.ClassLevel + ").");
                return;
            }

            if (boss)
            {
                Logger.WriteInfo("[Beastmaster] " + target.EnglishName + " is a boss: Capture goes out, but weaponskills are not held for it.");
                return;
            }

            var reason = BeastMasterSettings.Instance.UseCapture ? CaptureSkipReason(target) : null;
            if (reason != null && reason != "already marked")
                Logger.WriteInfo("[Beastmaster] Not capturing " + target.EnglishName + ": " + reason + ".");
        }

        /// <summary>
        /// A pact with nothing to blow it: the first empty slot whose horn you know gets an unassigned beast. Slots
        /// already holding a beast are never touched. Assigning a slot sends the familiar out home (seen 2026-09-08),
        /// so this runs only when none is out, right before a horn is blown.
        /// </summary>
        public static void AssignPactsToEmptyHorns()
        {
            if (!BeastMasterSettings.Instance.AssignPactsToEmptyHorns || FamiliarOut || LeaveHornsToTheDuty())
                return;

            if (!UnlockStateReady && !UnlockStateGaveUp)
                return;

            var slots = PetManager.BeastmasterPetSlots;
            var unassigned = PetManager.UnlockedBeastmasterPets.Where(p => p != BeastmasterPet.None && !slots.Contains(p)).ToList();
            if (unassigned.Count == 0)
                return;

            for (var i = 0; i < slots.Length && i < Battlehorns.Length; i++)
            {
                if (slots[i] != BeastmasterPet.None || !Battlehorns[i].IsKnown())
                    continue;

                var pet = unassigned[0];
                var ok = PetManager.SetBeastmasterPetSlot(i, pet);
                Logger.WriteInfo("[Beastmaster] Battlehorn " + (i + 1) + " " + (ok ? "assigned" : "could not be assigned") + " " + pet + ".");
                return;
            }
        }

        public static void NoteEngaged(GameObject target)
        {
            if (target != null)
                EngagedTargetId = target.ObjectId;
        }
    }
}
