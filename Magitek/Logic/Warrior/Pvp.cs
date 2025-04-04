using ff14bot;
using Magitek.Extensions;
using Magitek.Models.Warrior;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;

namespace Magitek.Logic.Warrior
{
    internal static class Pvp
    {
        public static async Task<bool> HeavySwingPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.HeavySwingPvp.CanCast())
                return false;

            return await Spells.HeavySwingPvp.CastPvpCombo(Spells.StormPathPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> MaimPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.MaimPvp.CanCast())
                return false;

            return await Spells.MaimPvp.CastPvpCombo(Spells.StormPathPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> StormPathPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.StormPathPvp.CanCast())
                return false;

            return await Spells.StormPathPvp.CastPvpCombo(Spells.StormPathPvpCombo, Core.Me.CurrentTarget);
        }

        public static async Task<bool> FellCleavePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.FellCleavePvp.CanCast())
                return false;

            if (!WarriorSettings.Instance.Pvp_FellCleave)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 5)
                return false;

            return await Spells.FellCleavePvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BlotaPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.BlotaPvp.CanCast())
                return false;

            if (!WarriorSettings.Instance.Pvp_Blota)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.BlotaPvp.Range))
                return false;

            if (Spells.BlotaPvp.Masked() != Spells.BlotaPvp)
                return false;

            return await Spells.BlotaPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> OrogenyPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.OrogenyPvp.CanCast())
                return false;

            if (!WarriorSettings.Instance.Pvp_Orogeny)
                return false;

            if (Core.Me.CurrentHealthPercent < WarriorSettings.Instance.Pvp_OrogenyHealthPercent)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.OrogenyPvp.Radius)) < 1)
                return false;

            return await Spells.OrogenyPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BloodwhettingPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.BloodwhettingPvp.CanCast())
                return false;

            if (!WarriorSettings.Instance.Pvp_Bloodwhetting)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.ChaoticCyclonePvp.Radius)) < 1)
                return false;

            return await Spells.BloodwhettingPvp.Cast(Core.Me);
        }

        public static async Task<bool> ChaoticCyclonePvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.ChaoticCyclonePvp.CanCast())
                return false;

            if (!WarriorSettings.Instance.Pvp_ChaoticCyclone)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.ChaoticCyclonePvp.Radius)) < 1)
                return false;

            return await Spells.ChaoticCyclonePvp.Cast(Core.Me);
        }

        public static async Task<bool> PrimalRendPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.PrimalRendPvp.CanCast())
                return false;

            if (!WarriorSettings.Instance.Pvp_PrimalRend)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.PrimalRendPvp.Range))
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (WarriorSettings.Instance.Pvp_SafePrimalRendNOnslaught && Core.Me.CurrentTarget.Distance(Core.Me) > 5)
                return false;

            return await Spells.PrimalRendPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PrimalRuinationPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.PrimalRuinationPvp.CanCast())
                return false;

            if (!WarriorSettings.Instance.Pvp_PrimalRuination)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.PrimalRuinationPvp.Radius)) < 1)
                return false;

            return await Spells.PrimalRuinationPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> InnerChaosPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.InnerChaosPvp.CanCast())
                return false;

            if (!WarriorSettings.Instance.Pvp_InnerChaos)
                return false;

            if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.InnerChaosPvp.Range))
                return false;

            return await Spells.InnerChaosPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> OnslaughtPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.OnslaughtPvp.CanCast())
                return false;

            if (!WarriorSettings.Instance.Pvp_Onslaught)
                return false;

            if (Core.Me.CurrentTarget.Distance(Core.Me) > 20)
                return false;

            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            if (Core.Me.CurrentHealthPercent < WarriorSettings.Instance.Pvp_OnslaughtHealthPercent)
                return false;

            if (WarriorSettings.Instance.Pvp_SafePrimalRendNOnslaught && Core.Me.CurrentTarget.Distance(Core.Me) > 5)
                return false;

            return await Spells.OnslaughtPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PrimalScreamPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.PrimalScreamPvp.CanCast())
                return false;

            if (!WarriorSettings.Instance.Pvp_PrimalScream)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.PrimalScreamPvp.Radius)) < 1)
                return false;

            return await Spells.PrimalScreamPvp.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> PrimalWrathPvp()
        {
            if (Core.Me.HasAura(Auras.PvpGuard))
                return false;

            if (!Spells.PrimalWrathPvp.CanCast())
                return false;

            if (!WarriorSettings.Instance.Pvp_PrimalWrath)
                return false;

            if (Combat.Enemies.Count(x => x.WithinSpellRange(Spells.PrimalWrathPvp.Radius)) < 1)
                return false;

            return await Spells.PrimalWrathPvp.Cast(Core.Me.CurrentTarget);
        }
    }
}