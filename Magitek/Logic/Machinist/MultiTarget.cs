using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Machinist;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using MachinistRoutine = Magitek.Utilities.Routines.Machinist;

namespace Magitek.Logic.Machinist
{
    internal static class MultiTarget
    {
        public static async Task<bool> Scattergun()
        {
            if (!MachinistSettings.Instance.UseScattergun)
                return false;

            if (!MachinistSettings.Instance.UseAoe)
                return false;

            if (Casting.LastSpell == Spells.Hypercharge)
                return false;

            if (Core.Me.EnemiesInCone(12) < 3)
                return false;

            return await MachinistRoutine.Scattergun.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> BioBlaster()
        {
            if (!MachinistSettings.Instance.UseBioBlaster)
                return false;

            if (!MachinistSettings.Instance.UseAoe)
                return false;

            if (Core.Me.EnemiesInCone(12) < MachinistSettings.Instance.BioBlasterEnemyCount)
                return false;

            if (Core.Me.CurrentTarget.HasAura(Auras.Bioblaster, true))
                return false;

            return await Spells.Bioblaster.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> AutoCrossbow()
        {
            if (!MachinistSettings.Instance.UseAutoCrossbow)
                return false;

            if (!MachinistSettings.Instance.UseAoe)
                return false;

            if (ActionResourceManager.Machinist.OverheatRemaining == TimeSpan.Zero)
                return false;

            if (Core.Me.EnemiesInCone(12) < 3)
                return false;

            return await Spells.AutoCrossbow.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Flamethrower()
        {
            if (!MachinistSettings.Instance.UseFlamethrower)
                return false;

            if (!MachinistSettings.Instance.UseAoe)
                return false;

            if (ActionResourceManager.Machinist.Heat >= 50)
                return false;

            if (Spells.Wildfire.IsKnownAndReady(11000))
                return false;

            if (Spells.Reassemble.IsKnownAndReady(11000))
                return false;

            if (MovementManager.IsMoving)
                return false;

            if (Core.Me.HasAura(Auras.WildfireBuff, true) || Casting.SpellCastHistory.Any(x => x.Spell == Spells.Wildfire))
                return false;

            if (Core.Me.HasAura(Auras.Overheated))
                return false;

            if (Core.Me.EnemiesInCone(12) < MachinistSettings.Instance.FlamethrowerEnemyCount)
                return false;

            if (Spells.FlameThrower.CanCast())
                Core.Me.CurrentTarget.Face();

            return await Spells.Flamethrower.CastAura(Core.Me, Auras.Flamethrower);
        }

        public static async Task<bool> Ricochet()
        {
            if (!MachinistSettings.Instance.UseRicochet)
                return false;

            var spell = Spells.Ricochet.Masked();

            if (Casting.LastSpell == Spells.Wildfire || Casting.LastSpell == Spells.Hypercharge || Casting.LastSpell == spell)
                return false;

            if (Spells.Wildfire.IsKnownAndReady() && Spells.Hypercharge.IsKnownAndReady() && spell.Charges < 1.5f)
                return false;

            if (Core.Me.ClassLevel >= Spells.Wildfire.LevelAcquired)
            {
                if (spell.Charges < 1.5f && Spells.Wildfire.IsKnownAndReady(2000))
                    return false;

                // Do not run Rico if an hypercharge is almost ready and not enough charges available for Rico and Gauss
                if (ActionResourceManager.Machinist.Heat > 40 || Spells.Hypercharge.IsKnownAndReady())
                {
                    if (spell.Charges < 1.5f && Spells.GaussRound.Masked().Charges < 0.5f)
                        return false;
                }
            }

            if (MachinistSettings.Instance.UseGaussRound && Spells.GaussRound.Masked().Charges > spell.Charges)
                return false;

            if (MachinistSettings.Instance.DoubleHyperchargedWildfire
                && Combat.IsBoss()
                && Core.Me.HasAura(Auras.WildfireBuff, true)
                && !Core.Me.HasAura(Auras.Overheated)
                && ActionResourceManager.Machinist.Heat > 50)
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ChainSaw()
        {
            if (!MachinistSettings.Instance.UseChainSaw)
                return false;

            if (!Spells.ChainSaw.IsKnownAndReady())
                return false;

            if (Core.Me.HasAura(Auras.Overheated) && !MachinistSettings.Instance.DoubleHyperchargedWildfire)
                return false;

            if (Core.Me.HasAura(Auras.WildfireBuff) && Core.Me.HasAura(Auras.Overheated))
                return false;

            if (ActionResourceManager.Machinist.Battery >= 100)
                return false;

            /*
            if (MachinistSettings.Instance.UseReassembleOnChainSaw && Spells.Reassemble.Charges >= 1 && Spells.Reassemble.IsKnown() && !Core.Me.HasAura(Auras.Reassembled))
            {
                SpellQueueLogic.SpellQueue.Clear();
                SpellQueueLogic.Timeout.Start();
                SpellQueueLogic.CancelSpellQueue = () => SpellQueueLogic.Timeout.ElapsedMilliseconds > 3000;
                SpellQueueLogic.SpellQueue.Enqueue(new QueueSpell { Spell = Spells.Reassemble, TargetSelf = true });
                SpellQueueLogic.SpellQueue.Enqueue(new QueueSpell { Spell = Spells.ChainSaw });
                return true;
            }
            */

            return await Spells.ChainSaw.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Excavator()
        {
            if (!MachinistSettings.Instance.UseExcavator)
                return false;

            if (!Spells.Excavator.IsKnownAndReady())
                return false;

            if (Core.Me.HasAura(Auras.Overheated) && !MachinistSettings.Instance.DoubleHyperchargedWildfire)
                return false;

            if (Core.Me.HasAura(Auras.WildfireBuff) && Core.Me.HasAura(Auras.Overheated))
                return false;

            if (ActionResourceManager.Machinist.Battery >= 100 && Core.Me.HasAura(Auras.ExcavatorReady, true, 7500))
                return false;

            return await Spells.Excavator.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> FullMetalField()
        {
            if (!MachinistSettings.Instance.UseFullMetalField)
                return false;

            if (!Spells.FullMetalField.IsKnownAndReady())
                return false;

            if (!Core.Me.HasAura(Auras.FullMetalMachinist))
                return false;

            if (!Core.Me.HasAura(Auras.Overheated) && MachinistSettings.Instance.DoubleHyperchargedWildfire && Combat.IsBoss())
                return false;

            if (Core.Me.HasAura(Auras.Overheated) && !MachinistSettings.Instance.DoubleHyperchargedWildfire)
                return false;

            if (Core.Me.HasAura(Auras.WildfireBuff) && Core.Me.HasAura(Auras.Overheated))
                return false;

            return await Spells.FullMetalField.Cast(Core.Me.CurrentTarget);
        }

        /**********************************************************************************************
        *                              Limit Break
        * ********************************************************************************************/
        public static bool ForceLimitBreak()
        {
            return PhysicalDps.ForceLimitBreak(Spells.BigShot, Spells.Desperado, Spells.SatelliteBeam, Spells.SplitShot);
        }
    }
}
