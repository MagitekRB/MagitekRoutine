using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.BeastMaster;
using System.Collections.Generic;
using System.Linq;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Utilities.Routines
{
    internal static class BeastMaster
    {
        // RebornBuddy 1.0.911 names the job BeastMaster (43); the reference assemblies Magitek compiles against
        // predate it, so the value is used directly.
        public const ClassJobType Job = (ClassJobType)43;

        public static WeaveWindow GlobalCooldown = new WeaveWindow(Job, Spells.SmashAxe, new List<SpellData>());

        // Cached each pulse
        public static int EnemiesIn5Yards;
        public static BeastMasterFamiliar Familiar;

        // The horn that summoned the current familiar: it reads castable while the familiar is out, so Parting Blow
        // has to look past it to the other horns.
        public static SpellData LastHorn;
        private static string _unmatchedFamiliar;

        // The pet's name when the last horn was blown: once it changes, the newcomer is what that horn summons.
        private static string _petNameAtHornCast;

        // A horn the swap logic wants blown next (after Parting Blow sent the current familiar home).
        public static SpellData WantedHorn;
        private static System.DateTime _wantedHornSince = System.DateTime.MinValue;
        private const int WantedHornMs = 10000;

        // A horn blown by anyone, the player included, shows as that horn on its 2 s recast. The familiar arrives
        // about a second after the cast, and in that gap no familiar is out: the auto-summon must not answer the
        // gap with a second horn (it did, 2026-09-08, and wasted the player's own choice).
        private static System.DateTime _hornSeenRecasting = System.DateTime.MinValue;
        private const int HornArrivalGraceMs = 5000;

        public static bool HornJustBlown =>
            Battlehorns.Any(h => Casting.LastSpellWas(h, HornArrivalGraceMs))
            || (System.DateTime.Now - _hornSeenRecasting).TotalMilliseconds < HornArrivalGraceMs;

        public static void RefreshVars()
        {
            if (Battlehorns.Any(h => h.IsKnown() && h.Cooldown > System.TimeSpan.Zero))
                _hornSeenRecasting = System.DateTime.Now;

            // The chat listener that fills the bestiary: armed here as well as at bot start, since a hot-reload
            // re-initialises the routine without the start hook.
            BeastMasterBestiary.Start();

            EnemiesIn5Yards = Combat.Enemies.Count(e => e.Distance(Core.Me) <= 5 + e.CombatReach);
            Familiar = FamiliarOut ? FamiliarByName(Core.Me.Pet?.EnglishName) : null;
            TrackWaveringHeart();
            LearnHornFamiliar();
            TrackCaptureHold();

            if (FamiliarOut && Familiar == null && _unmatchedFamiliar != Core.Me.Pet.EnglishName)
            {
                _unmatchedFamiliar = Core.Me.Pet.EnglishName;
                Logger.WriteInfo($"[Beastmaster] Familiar \"{_unmatchedFamiliar}\" is not in the bestiary; the compass will follow your own Hearts only.");
            }
        }

        /// <summary>Enemies within a radius of the summoned familiar (its abilities go out from where it stands).</summary>
        public static int EnemiesNearFamiliar(float yards)
        {
            var pet = Core.Me.Pet;
            if (pet == null)
                return 0;
            return Combat.Enemies.Count(e => e.Distance(pet) <= yards + e.CombatReach);
        }

        /// <summary>A familiar is summoned. RebornBuddy exposes it as the player's pet.</summary>
        public static bool FamiliarOut => Core.Me.Pet != null && Core.Me.Pet.IsValid;

        public static BeastMasterFamiliar FamiliarByName(string name) =>
            string.IsNullOrEmpty(name) ? null
                : XivDataHelper.BeastMasterFamiliars.FirstOrDefault(f => string.Equals(f.Name, name, System.StringComparison.OrdinalIgnoreCase));

        /// <summary>The affinity the summoned familiar's Trick carries, or null.</summary>
        public static string FamiliarAffinity => Familiar?.Trick?.Affinity;

        /// <summary>
        /// The Heart on the player right now: the affinity of the last instinctual skill (mine or the familiar's),
        /// which the next one has to follow clockwise for an intentional combo. Null when nothing is lit.
        /// </summary>
        public static string CurrentHeart
        {
            get
            {
                if (Core.Me.HasAura(Auras.VolantHeart)) return Affinity.Volant;
                if (Core.Me.HasAura(Auras.RampantHeart)) return Affinity.Rampant;
                if (Core.Me.HasAura(Auras.DurantHeart)) return Affinity.Durant;
                if (Core.Me.HasAura(Auras.EldritchHeart)) return Affinity.Eldritch;
                return null;
            }
        }

        // A Heart takes a moment to appear after our own axe; until it does, the axe just cast says what the Heart
        // will be, so the follow-up is chosen right instead of restarting the chain.
        private const int HeartLagMs = 1500;

        // Trick is an order: the familiar acts on its own time. An axe thrown 0.6 s after the order, before its
        // skill had landed, earned Wavering Heart every time; so did one thrown the instant the familiar's Heart
        // appeared (1.2 s after the order, 2026-09-08 17:15). The Heart shows when the familiar starts its skill,
        // not when it lands. So after a Trick the axe waits for the Heart and for this much time since the order;
        // the axe and any Wavering Heart log their distance from the order so the safe gap gets measured.
        private const int TrickLandingMs = 4000;
        private const int TrickSettleMs = 2500;

        public static System.DateTime LastTrickAt = System.DateTime.MinValue;
        public static double MsSinceTrick => (System.DateTime.Now - LastTrickAt).TotalMilliseconds;

        /// <summary>A Trick was ordered and the familiar has not been given its time yet: nothing of ours should go out.</summary>
        public static bool TrickPending
        {
            get
            {
                if (!Casting.LastSpellWas(Spells.Trick, TrickLandingMs))
                    return false;
                return CurrentHeart == null || MsSinceTrick < TrickSettleMs;
            }
        }

        /// <summary>The Heart lit now, or the one about to be lit by an axe just cast.</summary>
        public static string EffectiveHeart
        {
            get
            {
                var heart = CurrentHeart;
                if (heart != null)
                    return heart;

                foreach (var affinity in Affinity.Clockwise)
                {
                    var axe = AxeFor(affinity);
                    if (axe != null && Casting.LastSpellWas(axe, HeartLagMs))
                        return affinity;
                }

                return null;
            }
        }

        /// <summary>The familiar's Trick would continue the chain from the Heart lit now.</summary>
        public static bool TrickContinuesChain =>
            EffectiveHeart != null && FamiliarAffinity != null && FamiliarAffinity == Affinity.Next(EffectiveHeart);

        /// <summary>
        /// Wavering Heart: the client says combos with the familiar are off for a while. Its cause is not documented,
        /// so its first appearance in a fight is logged with what was cast just before it.
        /// </summary>
        public static bool WaveringHeart => Core.Me.HasAura(Auras.WaveringHeart);
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
            Logger.WriteInfo($"[Beastmaster] Wavering Heart after {Casting.LastSpell?.LocalizedName ?? "nothing"} (familiar {(FamiliarOut ? "out" : "away")}, heart {CurrentHeart ?? "none"}, {MsSinceTrick:0} ms after the last Trick order).");
        }

        /// <summary>
        /// The player's instinctual axe of a given affinity. At level 50 the bar swaps them for the 250 TP forms;
        /// Masked() follows the swap, so the caller always casts what the bar shows.
        /// </summary>
        public static SpellData AxeFor(string affinity)
        {
            switch (affinity)
            {
                case Affinity.Volant: return Spells.GaleAxe.Masked();
                case Affinity.Rampant: return Spells.AvalancheAxe.Masked();
                case Affinity.Durant: return Spells.MistralAxe.Masked();
                case Affinity.Eldritch: return Spells.SpinningAxe.Masked();
                default: return null;
            }
        }

        public static SpellData[] Axes => new[] { Spells.GaleAxe.Masked(), Spells.AvalancheAxe.Masked(), Spells.MistralAxe.Masked(), Spells.SpinningAxe.Masked() };

        /// <summary>
        /// The bot does not expose the TP gauges yet, so readiness is read the way the client enforces it: an
        /// instinctual skill is castable only from 100 TP (250 for the level 50 forms).
        /// </summary>
        public static bool HasTpFor(SpellData skill) =>
            skill != null && skill.IsKnown() && Core.Me.HasTarget && ActionManager.CanCast(skill.Id, Core.Me.CurrentTarget);

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

        private static readonly uint[] HeartAuras = { Auras.VolantHeart, Auras.RampantHeart, Auras.DurantHeart, Auras.EldritchHeart };

        /// <summary>Milliseconds left on the lit Heart, 0 when none.</summary>
        public static double HeartMsLeft
        {
            get
            {
                var aura = Core.Me.CharacterAuras.FirstOrDefault(a => HeartAuras.Contains(a.Id));
                return aura?.TimespanLeft.TotalMilliseconds ?? 0;
            }
        }

        /// <summary>Horn slot 1-3, or 0 for anything else.</summary>
        public static int HornSlot(SpellData horn) => horn == null ? 0 : System.Array.IndexOf(Battlehorns, horn) + 1;

        /// <summary>A horn was blown: remember it and the pet on the field at that moment.</summary>
        public static void NoteHornCast(SpellData horn)
        {
            LastHorn = horn;
            _petNameAtHornCast = Core.Me.Pet?.EnglishName ?? string.Empty;
            if (horn == WantedHorn)
                WantedHorn = null;
        }

        public static void WantHorn(SpellData horn)
        {
            WantedHorn = horn;
            _wantedHornSince = System.DateTime.Now;
        }

        /// <summary>
        /// Which beast each horn summons is not in any data sheet; it is learned when a familiar shows up after a horn
        /// and kept in the settings, so the swap logic knows what the other horns would bring.
        /// </summary>
        private static void LearnHornFamiliar()
        {
            if (!FamiliarOut)
                return;

            var name = Core.Me.Pet.EnglishName;
            if (string.IsNullOrEmpty(name))
                return;

            // With a familiar out, the client refuses the horn that would summon the same beast and accepts the
            // others (which swap). So the one known horn that reads not castable is the horn this familiar came
            // from: the mapping is read without blowing anything, including for a familiar summoned by hand.
            var horn = CurrentFamiliarHorn;
            if (horn == null)
                return;

            var slot = HornSlot(horn);
            var settings = BeastMasterSettings.Instance;
            var known = settings.BattlehornFamiliars;
            if (known != null && known.TryGetValue(slot, out var recorded) && recorded == name)
                return;

            var copy = known == null ? new Dictionary<int, string>() : new Dictionary<int, string>(known);
            copy[slot] = name;
            settings.BattlehornFamiliars = copy;
            Logger.WriteInfo("[Beastmaster] Battlehorn " + slot + " summons " + name + " (" + (FamiliarByName(name)?.Trick?.Affinity ?? "affinity unknown") + " Trick).");
        }

        /// <summary>
        /// The horn the familiar out came from: the only known horn the client will not cast while it is out
        /// (the others swap). Null when no familiar is out, when a horn was blown in the last moments (its recast
        /// would also read not castable), or when the read is ambiguous.
        /// </summary>
        public static SpellData CurrentFamiliarHorn
        {
            get
            {
                if (!FamiliarOut || Battlehorns.Any(h => Casting.LastSpellWas(h, 3000)))
                    return null;

                SpellData found = null;
                foreach (var horn in Battlehorns)
                {
                    if (!horn.IsKnown() || ActionManager.CanCast(horn.Id, Core.Me))
                        continue;
                    if (found != null)
                        return null;
                    found = horn;
                }
                return found;
            }
        }

        /// <summary>The Trick affinity of the beast a horn is known to summon, or null.</summary>
        public static string HornAffinity(SpellData horn)
        {
            var known = BeastMasterSettings.Instance.BattlehornFamiliars;
            if (known == null || !known.TryGetValue(HornSlot(horn), out var name))
                return null;
            return FamiliarByName(name)?.Trick?.Affinity;
        }

        /// <summary>Another horn, off cooldown, whose beast carries this affinity.</summary>
        public static SpellData SwapHornFor(string affinity)
        {
            if (affinity == null)
                return null;

            var current = CurrentFamiliarHorn ?? LastHorn;
            return Battlehorns.FirstOrDefault(h => h != current && h.IsKnown() && h.Cooldown == System.TimeSpan.Zero && HornAffinity(h) == affinity);
        }

        /// <summary>A horn that can be blown now: the one the swap asked for, else the preferred one first.</summary>
        public static SpellData ReadyBattlehorn()
        {
            if (WantedHorn != null && (System.DateTime.Now - _wantedHornSince).TotalMilliseconds > WantedHornMs)
                WantedHorn = null;
            if (WantedHorn != null && WantedHorn.IsKnown() && ActionManager.CanCast(WantedHorn.Id, Core.Me))
                return WantedHorn;

            var preferred = System.Math.Max(1, System.Math.Min(3, BeastMasterSettings.Instance.PreferredBattlehorn)) - 1;
            for (var i = 0; i < 3; i++)
            {
                var horn = Battlehorns[(preferred + i) % 3];
                if (horn.IsKnown() && ActionManager.CanCast(horn.Id, Core.Me))
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
        private static System.DateTime _sleepDisengageUntil = System.DateTime.MinValue;
        private static bool _familiarHeeled;

        public static void BeginSleepDisengage()
        {
            _sleepDisengageUntil = System.DateTime.Now.AddMilliseconds(SleepDisengageMs);
            Logger.WriteInfo("[Beastmaster] Sleep out: disengaging from sleeping targets for up to 30 s.");
        }

        public static bool SleepDisengageActive => BeastMasterSettings.Instance.DisengageAfterSleep && System.DateTime.Now < _sleepDisengageUntil;

        public static bool IsAsleep(GameObject unit) => unit != null && unit.HasAnyAura(SleepStatuses);

        public static bool AnyEnemyAsleepNearby => Combat.Enemies.Any(e => IsAsleep(e) && e.Distance(Core.Me) <= 20 + e.CombatReach);

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
        /// A beast Capture should go on: the game said it can be captured (with odds the setting accepts), it is not
        /// befriended, not above our level, and not marked yet. Health is not part of it; Capture itself waits for
        /// the threshold, the hold below waits with it.
        /// </summary>
        public static bool CaptureWanted(BattleCharacter target)
        {
            var settings = BeastMasterSettings.Instance;
            if (!settings.UseCapture || !Spells.Capture.IsKnown() || target == null || !target.IsNpc)
                return false;

            if (target.HasAura(Auras.InterestCaptured) || target.ClassLevel > Core.Me.ClassLevel)
                return false;

            var verdict = BeastMasterBestiary.Verdict(target.NpcId);
            if (verdict == null || verdict < BeastMasterBestiary.LowestOdds)
                return false;

            return verdict - BeastMasterBestiary.LowestOdds + 1 >= settings.CaptureMinimumOdds;
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

            if (!HoldingForCapture)
            {
                _holdLoggedFor = 0;
                return;
            }

            if (_holdLoggedFor == target.ObjectId)
                return;

            _holdLoggedFor = target.ObjectId;
            Logger.WriteInfo("[Beastmaster] Holding weaponskills on " + target.EnglishName + " until the mark is on it (auto-attacks only).");
        }

        public static void NoteEngaged(GameObject target)
        {
            if (target != null)
                EngagedTargetId = target.ObjectId;
        }
    }
}
