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

        public static void RefreshVars()
        {
            // The chat listener that fills the bestiary: armed here as well as at bot start, since a hot-reload
            // re-initialises the routine without the start hook.
            BeastMasterBestiary.Start();

            EnemiesIn5Yards = Combat.Enemies.Count(e => e.Distance(Core.Me) <= 5 + e.CombatReach);
            Familiar = FamiliarOut ? FamiliarByName(Core.Me.Pet?.EnglishName) : null;
            TrackWaveringHeart();

            if (FamiliarOut && Familiar == null && _unmatchedFamiliar != Core.Me.Pet.EnglishName)
            {
                _unmatchedFamiliar = Core.Me.Pet.EnglishName;
                Logger.WriteInfo($"[Beastmaster] Familiar \"{_unmatchedFamiliar}\" is not in the bestiary; the compass will follow your own Hearts only.");
            }
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

        // A Heart takes a moment to appear after the skill that lights it; until it does, the skill just cast says
        // what the Heart will be, so the follow-up is chosen right instead of restarting the chain.
        private const int HeartLagMs = 1500;

        /// <summary>The Heart lit now, or the one about to be lit by a Trick or axe just cast.</summary>
        public static string EffectiveHeart
        {
            get
            {
                var heart = CurrentHeart;
                if (heart != null)
                    return heart;

                if (Casting.LastSpellWas(Spells.Trick, HeartLagMs))
                    return FamiliarAffinity;

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
            Logger.WriteInfo($"[Beastmaster] Wavering Heart after {Casting.LastSpell?.LocalizedName ?? "nothing"} (familiar {(FamiliarOut ? "out" : "away")}, heart {CurrentHeart ?? "none"}).");
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

        /// <summary>A horn that can be blown now, the preferred one first.</summary>
        public static SpellData ReadyBattlehorn()
        {
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
    }
}
