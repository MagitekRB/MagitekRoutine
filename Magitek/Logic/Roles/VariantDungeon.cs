using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.VariantDungeon;
using Magitek.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Roles
{
    internal static class VDSpells
    {
        // Dawntrail variant dungeon spell IDs (The Merchant's Tale and later)
        public static readonly SpellData VariantCure = DataManager.GetSpellData(46939);
        public static readonly SpellData VariantUltimatum = DataManager.GetSpellData(29730);
        public static readonly SpellData VariantRaise = DataManager.GetSpellData(29731);
        public static readonly SpellData VariantSpiritDart = DataManager.GetSpellData(46940);
        public static readonly SpellData VariantRampart = DataManager.GetSpellData(46941);
        public static readonly SpellData VariantEagleEyeShot = DataManager.GetSpellData(46942);

        // Endwalker variant dungeon spell IDs. Cure, Spirit Dart and Rampart have three rows each in the Action sheet:
        // 297xx (Sil'dihn, logged), 338xx (Aloalo Island, logged 2026-09-13: the routine cast none of the three there
        // while Ultimatum and Raise, shared by every dungeon, were fine) and 469xx (Dawntrail). Mount Rokkon is on one
        // of the two Endwalker sets. The effect statuses (3358-3360) are the same in every generation.
        public static readonly SpellData VariantCureOld = DataManager.GetSpellData(29729);
        public static readonly SpellData VariantSpiritDartOld = DataManager.GetSpellData(29732);
        public static readonly SpellData VariantRampartOld = DataManager.GetSpellData(29733);
        public static readonly SpellData VariantCureAloalo = DataManager.GetSpellData(33862);
        public static readonly SpellData VariantSpiritDartAloalo = DataManager.GetSpellData(33863);
        public static readonly SpellData VariantRampartAloalo = DataManager.GetSpellData(33864);

        // The Criterion tiers have their own raise, Variant Raise II (1.3 s cast, 25 s recast), the only raise there
        // (Another Sil'dihn Subterrane, ACT 2026-06-12).
        public static readonly SpellData VariantRaiseCriterion = DataManager.GetSpellData(29734);
    }

    internal static class VDAuras
    {
        public const int
            VariantUltimatum = 3358,
            VariantSpiritDart = 3359,
            VariantRampart = 3360,
            Rehabilitation = 3367;
    }

    internal static class VariantDungeon
    {
        private static bool IsVariantSpellReady(SpellData spell, GameObject target = null)
        {
            if (spell == null)
                return false;

            return ActionManager.CanCast(spell, target ?? Core.Me);
        }

        /// <summary>The first of the candidates the client will cast on the target now (self when null), or null.</summary>
        private static SpellData GetCastableVariant(GameObject target, params SpellData[] candidates)
        {
            var t = target ?? Core.Me;
            return candidates.FirstOrDefault(s => s != null && ActionManager.CanCast(s, t));
        }

        private static readonly HashSet<ushort> VariantDungeonZoneIds = new()
        {
            1069,  // The Sil'dihn Subterrane (Variant)
            1075,  // The Sil'dihn Subterrane (Criterion)
            1076,  // The Sil'dihn Subterrane (Criterion Savage)
            1137,  // Mount Rokkon (Variant)
            1155,  // Mount Rokkon (Criterion)
            1156,  // Mount Rokkon (Criterion Savage)
            1176,  // Aloalo Island (Variant)
            1179,  // Aloalo Island (Criterion)
            1180,  // Aloalo Island (Criterion Savage)
            1315,  // The Merchant's Tale (Variant)
            1316,  // The Merchant's Tale (Advanced)
            1317,  // The Merchant's Tale (Criterion)
        };

        public static bool IsInVariantDungeon()
        {
            return VariantDungeonZoneIds.Contains(WorldManager.ZoneId);
        }

        public static async Task<bool> Execute()
        {
            if (!VariantDungeonSettings.Instance.Enable)
                return false;

            if (!IsInVariantDungeon())
                return false;

            if (await VariantRaise()) return true;
            if (await VariantCure()) return true;
            if (await VariantRampart()) return true;
            if (await VariantUltimatum()) return true;
            if (await VariantSpiritDart()) return true;
            if (await VariantEagleEyeShot()) return true;

            return false;
        }

        private static async Task<bool> VariantCure()
        {
            if (!VariantDungeonSettings.Instance.UseVariantCure)
                return false;

            if (Core.Me.CurrentHealthPercent <= VariantDungeonSettings.Instance.VariantCureHealthPercent)
            {
                var spell = GetCastableVariant(null, VDSpells.VariantCure, VDSpells.VariantCureOld, VDSpells.VariantCureAloalo);
                if (spell != null)
                    return await spell.Cast(Core.Me);
            }

            if (VariantDungeonSettings.Instance.VariantCureOnAllies && Globals.InParty)
            {
                var allyTarget = Group.CastableAlliesWithin30
                    .Where(a => a.CurrentHealthPercent <= VariantDungeonSettings.Instance.VariantCureAllyHealthPercent
                                && a.IsAlive
                                && a.InLineOfSight())
                    .OrderBy(a => a.CurrentHealthPercent)
                    .FirstOrDefault();

                if (allyTarget != null)
                {
                    var spell = GetCastableVariant(allyTarget, VDSpells.VariantCure, VDSpells.VariantCureOld, VDSpells.VariantCureAloalo);
                    if (spell != null)
                        return await spell.Cast(allyTarget);
                }
            }

            return false;
        }

        private static async Task<bool> VariantUltimatum()
        {
            if (!VariantDungeonSettings.Instance.UseVariantUltimatum)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!IsVariantSpellReady(VDSpells.VariantUltimatum))
                return false;

            var enemyNotTargetingUs = Combat.Enemies.FirstOrDefault(r => r.WithinSpellRange(5) && r.TargetGameObject != Core.Me);

            if (enemyNotTargetingUs == null)
                return false;

            return await VDSpells.VariantUltimatum.Cast(Core.Me);
        }

        private static async Task<bool> VariantRaise()
        {
            if (!VariantDungeonSettings.Instance.UseVariantRaise)
                return false;

            if (!Globals.InParty)
                return false;

            // Raw 3D distance is deliberate here — see the resurrection range exception in
            // AGENTS.md. A corpse has no usable CombatReach, and WithinSpellRange is built on
            // Distance2D so it would ignore height. Leave this as Distance.
            var deadTarget = Group.DeadAllies
                .Where(u => u.CurrentHealth == 0
                            && !u.HasAura(Auras.Raise)
                            && u.Distance(Core.Me) <= 30
                            && u.IsVisible
                            && u.InLineOfSight()
                            && u.IsTargetable)
                .OrderByDescending(r => r.GetResurrectionWeight())
                .FirstOrDefault();

            if (deadTarget == null)
                return false;

            var raise = GetCastableVariant(deadTarget, VDSpells.VariantRaise, VDSpells.VariantRaiseCriterion);
            if (raise == null)
                return false;

            // Variant Raise II is a 1.3 s cast: it goes as it is, with Swiftcast kept for the job and the slowcast
            // setting (about the 8 s raise) not in its way.
            if (raise == VDSpells.VariantRaiseCriterion)
                return await raise.Cast(deadTarget);

            if (VariantDungeonSettings.Instance.UseSwiftcastForVariantRaise
                && Spells.Swiftcast.IsKnownAndReady()
                && Core.Me.InCombat)
            {
                if (await Spells.Swiftcast.Cast(Core.Me))
                {
                    if (await raise.CastAura(deadTarget, Auras.Raise))
                        return true;
                }
            }

            if (!Core.Me.InCombat || VariantDungeonSettings.Instance.SlowcastVariantRaise)
                return await raise.Cast(deadTarget);

            return false;
        }

        private static async Task<bool> VariantSpiritDart()
        {
            if (!VariantDungeonSettings.Instance.UseVariantSpiritDart)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            if (Core.Me.CurrentTarget.HasAura(VDAuras.VariantSpiritDart, true, 3000))
                return false;

            var spell = GetCastableVariant(Core.Me.CurrentTarget, VDSpells.VariantSpiritDart, VDSpells.VariantSpiritDartOld, VDSpells.VariantSpiritDartAloalo);
            if (spell == null)
                return false;

            return await spell.CastAura(Core.Me.CurrentTarget, (uint)VDAuras.VariantSpiritDart, true, 3000);
        }

        private static async Task<bool> VariantRampart()
        {
            if (!VariantDungeonSettings.Instance.UseVariantRampart)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (Core.Me.HasAura(VDAuras.VariantRampart))
                return false;

            if (Core.Me.CurrentHealthPercent > VariantDungeonSettings.Instance.VariantRampartHealthPercent)
                return false;

            var spell = GetCastableVariant(null, VDSpells.VariantRampart, VDSpells.VariantRampartOld, VDSpells.VariantRampartAloalo);
            if (spell == null)
                return false;

            return await spell.Cast(Core.Me);
        }

        private static async Task<bool> VariantEagleEyeShot()
        {
            if (!VariantDungeonSettings.Instance.UseVariantEagleEyeShot)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (Core.Me.CurrentTarget == null)
                return false;

            if (!IsVariantSpellReady(VDSpells.VariantEagleEyeShot, Core.Me.CurrentTarget))
                return false;

            return await VDSpells.VariantEagleEyeShot.Cast(Core.Me.CurrentTarget);
        }
    }
}
