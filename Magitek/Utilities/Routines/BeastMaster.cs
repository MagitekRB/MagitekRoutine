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

        public static void RefreshVars()
        {
            EnemiesIn5Yards = Combat.Enemies.Count(e => e.Distance(Core.Me) <= 5 + e.CombatReach);
            Familiar = FamiliarOut ? FamiliarByName(Core.Me.Pet?.Name) : null;
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

        /// <summary>The player's instinctual axe of a given affinity (the level 50 forms replace them on the bar).</summary>
        public static SpellData AxeFor(string affinity)
        {
            switch (affinity)
            {
                case Affinity.Volant: return Spells.GaleAxe;
                case Affinity.Rampant: return Spells.AvalancheAxe;
                case Affinity.Durant: return Spells.MistralAxe;
                case Affinity.Eldritch: return Spells.SpinningAxe;
                default: return null;
            }
        }

        public static readonly SpellData[] Axes = { Spells.GaleAxe, Spells.AvalancheAxe, Spells.MistralAxe, Spells.SpinningAxe };

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
