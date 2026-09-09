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

        public static void RefreshVars()
        {
            if (Battlehorns.Any(h => h.IsKnown() && h.Cooldown > System.TimeSpan.Zero && h.Cooldown <= HornRecast))
                _hornSeenRecasting = System.DateTime.Now;

            // At level 50 the bar swaps the axes for their 250 TP forms; one masked read per axe per pulse.
            Axes[0] = Spells.GaleAxe.Masked();
            Axes[1] = Spells.AvalancheAxe.Masked();
            Axes[2] = Spells.MistralAxe.Masked();
            Axes[3] = Spells.SpinningAxe.Masked();

            Familiar = FamiliarOut ? CurrentFamiliar() : null;
            if (FamiliarAffinity != null)
                _lastFamiliarAffinity = FamiliarAffinity;
            TrackWaveringHeart();
            TrackCaptureHold();

            // The compass state a pair consumes says who finished it; kept from the pulse before Wavering Heart.
            var compass = Gauge.InnerCompass;
            if (compass != Gauge.InnerCompassState.Wavering && compass != Gauge.InnerCompassState.None)
                _compassBeforeWavering = compass;

            if (!FamiliarOut)
            {
                _loggedFamiliarId = 0;
                return;
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

        // Remembered past the retreat: a Parting Blow can land between the finishing hit and the bot seeing
        // Wavering Heart, and the credit still needs the colour of the beast that was out.
        private static string _lastFamiliarAffinity;

        // A Heart takes a moment to appear after our own axe; until it does, the axe just cast says what the Heart
        // will be, so the follow-up is chosen right instead of restarting the chain.
        private const int HeartLagMs = 1500;

        // Trick is an order: the familiar acts on its own time (its hit lands about 0.2 s after the order, its Heart
        // shows on us about 0.8 s after, measured on ACT 2026-09-08). An axe thrown before that Heart continues
        // nothing; one thrown the moment it shows completes the pair. So after a Trick the axe waits for the Heart,
        // up to this long.
        private const int TrickLandingMs = 4000;

        public static System.DateTime LastTrickAt = System.DateTime.MinValue;
        public static double MsSinceTrick => (System.DateTime.Now - LastTrickAt).TotalMilliseconds;

        /// <summary>
        /// A Trick was ordered and the familiar's Heart has not shown yet: no instinctual skill of ours should go
        /// out. Keyed on the order's own time, not on "the last spell was Trick": a Smash Axe in between made the
        /// routine forget the order and open a new chain with the wrong affinity (six wrong-order pairs, 2026-09-08).
        /// </summary>
        public static bool TrickPending => CurrentHeart == null && MsSinceTrick < TrickLandingMs;

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

        // The gauge has no stack counts for Rally and Rallying Cheer, so they are still estimated: a combo we finish
        // adds a Mastered Instinct (Wild Heart III, level 28), one the familiar finishes adds a Natural Instinct
        // (Wild Heart IV, level 40), three of each at most.
        public static int MasteredInstinct;
        public static int NaturalInstinct;
        private const int InstinctStacksMax = 3;

        public static void SpentMastered() => MasteredInstinct = 0;
        public static void SpentNatural() => NaturalInstinct = 0;

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
                : heart != null && heart == _lastFamiliarAffinity;

            string finisher = null;
            if (ours)
            {
                // Universality is counted as ours; whether it pays a stack is not measured yet.
                MasteredInstinct = System.Math.Min(InstinctStacksMax, MasteredInstinct + 1);
                if (heart != null)
                {
                    finisher = Affinity.Next(heart);
                    NoteInstinct(finisher);
                }
            }
            else
            {
                NaturalInstinct = System.Math.Min(InstinctStacksMax, NaturalInstinct + 1);
                NoteInstinct(FamiliarAffinity);
            }

            var by = ours ? (window ? "Universality" : AxeFor(finisher)?.LocalizedName ?? "our axe") : "the familiar";
            Logger.WriteInfo($"[Beastmaster] Combo completed by {by} (chain {Gauge.ComboCounter}; instinct {MasteredInstinct} mastered / {NaturalInstinct} natural, estimated).");
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

        public static readonly SpellData[] Battlehorns = { Spells.FirstBattlehorn, Spells.SecondBattlehorn, Spells.ThirdBattlehorn };

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

        // Every Sleep status in the 7.56 data (the Lullaby one is not told apart from the others).
        public static readonly uint[] SleepStatuses = { 3, 926, 1348, 1363, 1510, 1596, 1947, 2983, 3466, 4894 };

        // After a sleep from the familiar (Lullaby, 30 s): drop sleeping targets so nothing wakes them, call the
        // familiar to heel, and fight only what is awake and on us, with the 1-2-3 alone (the axes and Trick are
        // cones and lines that would wake the rest).
        private const int SleepDisengageMs = 30000;
        // The familiar acts after the order: the Sleep is not on anyone for the first moments, and that must not
        // read as the sleep being over.
        private const int SleepLandingMs = 4000;
        private static System.DateTime _sleepDisengageUntil = System.DateTime.MinValue;
        private static System.DateTime _sleepDisengageSince = System.DateTime.MinValue;
        private static bool _familiarHeeled;

        public static void BeginSleepDisengage()
        {
            _sleepDisengageSince = System.DateTime.Now;
            _sleepDisengageUntil = System.DateTime.Now.AddMilliseconds(SleepDisengageMs);
            Logger.WriteInfo("[Beastmaster] Sleep out: disengaging from sleeping targets for up to 30 s.");
        }

        public static bool SleepDisengageActive => BeastMasterSettings.Instance.DisengageAfterSleep && System.DateTime.Now < _sleepDisengageUntil;

        public static bool IsAsleep(GameObject unit) => unit != null && unit.HasAnyAura(SleepStatuses);

        public static bool AnyEnemyAsleepNearby => Core.Me.EnemiesNearby(20).Any(IsAsleep);

        /// <summary>The sleep was ordered so recently that it may not have landed yet.</summary>
        public static bool SleepStillLanding => (System.DateTime.Now - _sleepDisengageSince).TotalMilliseconds < SleepLandingMs;

        /// <summary>The familiar follows and stops attacking; the pet bar's Heel. Once per disengage.</summary>
        public static void HeelFamiliar()
        {
            if (_familiarHeeled || !FamiliarOut)
                return;

            _familiarHeeled = true;
            var names = new List<string>();
            var heel = false;
            foreach (var pair in PetManager.CurrentActions)
            {
                var action = pair.Value;
                if (action == null)
                    continue;
                names.Add(action.LocalizedName);
                if (action.LocalizedName == "Heel")
                    heel = true;
            }

            var list = names.Count == 0 ? "none" : string.Join(", ", names);
            if (heel && PetManager.DoAction("Heel", Core.Me))
                Logger.WriteInfo("[Beastmaster] Familiar called to heel (pet actions: " + list + ").");
            else
                Logger.WriteInfo("[Beastmaster] Could not call the familiar to heel (pet actions: " + list + ").");
        }

        public static void EndSleepDisengage()
        {
            if (_sleepDisengageUntil == System.DateTime.MinValue)
                return;

            _sleepDisengageUntil = System.DateTime.MinValue;
            _familiarHeeled = false;
            Logger.WriteInfo("[Beastmaster] Disengage over: back to the rotation.");
        }

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
            if (PetManager.IsBeastmasterPetUnlocked(pet))
                return pet + " is already in the bestiary";
            if (target.HasAura(Auras.InterestCaptured))
                return "already marked";
            if (target.ClassLevel > Core.Me.ClassLevel)
                return "level " + target.ClassLevel + " against your " + Core.Me.ClassLevel + ", Capture would be ineffective";
            // Bosses share skeletons with ordinary beasts; holding weaponskills on one for a refused Capture is not worth it.
            if (target.IsBoss())
                return "a boss";
            return null;
        }

        // Weaponskills and familiar orders wait while a capturable beast is unmarked: an axe at 55% is what kills it
        // before the mark. Auto-attacks (ours and the familiar's) bring it to the threshold, and Capture is next.
        public static bool HoldingForCapture;

        // The target the first Smash Axe went out on: that one hit is allowed, since auto-attack starts on it.
        public static uint EngagedTargetId;
        private static uint _holdLoggedFor;

        private static void TrackCaptureHold()
        {
            var target = Core.Me.CurrentTarget as BattleCharacter;
            HoldingForCapture = BeastMasterSettings.Instance.HoldForCapture && Core.Me.InCombat && CaptureWanted(target);

            if (target == null || _holdLoggedFor == target.ObjectId)
                return;
            _holdLoggedFor = target.ObjectId;

            if (HoldingForCapture)
            {
                Logger.WriteInfo("[Beastmaster] Holding weaponskills on " + target.EnglishName + " until the mark is on it (auto-attacks only).");
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
            if (!BeastMasterSettings.Instance.AssignPactsToEmptyHorns || FamiliarOut)
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
