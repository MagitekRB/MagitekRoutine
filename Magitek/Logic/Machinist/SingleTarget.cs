using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Models.Machinist;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using MachinistRoutine = Magitek.Utilities.Routines.Machinist;

namespace Magitek.Logic.Machinist
{
    internal static class SingleTarget
    {
        public static async Task<bool> HeatedSplitShot()
        {
            //One to disable them all
            if (!MachinistSettings.Instance.UseSplitShotCombo)
                return false;

            if (MachinistSettings.Instance.UseDrill && Spells.Drill.IsKnownAndReady(200))
                return false;

            if (MachinistSettings.Instance.UseHotAirAnchor && Spells.AirAnchor.IsKnownAndReady(200) && ActionResourceManager.Machinist.Battery <= 80)
                return false;

            if (MachinistSettings.Instance.UseChainSaw && Spells.ChainSaw.IsKnownAndReady(200) && ActionResourceManager.Machinist.Battery <= 80)
                return false;

            if (Core.Me.HasAura(Auras.Overheated) && Spells.HeatBlast.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.Reassembled) && Spells.Drill.IsKnown())
                return false;

            return await MachinistRoutine.HeatedSplitShot.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HeatedSlugShot()
        {
            if (!MachinistRoutine.CanContinueComboAfter(Spells.SplitShot))
                return false;

            if (MachinistSettings.Instance.UseDrill && Spells.Drill.IsKnownAndReady(200))
                return false;

            if (MachinistSettings.Instance.UseHotAirAnchor && Spells.AirAnchor.IsKnownAndReady(200))
                return false;

            if (MachinistSettings.Instance.UseChainSaw && Spells.ChainSaw.IsKnownAndReady(200))
                return false;

            if (Core.Me.HasAura(Auras.Overheated) && Spells.HeatBlast.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.Reassembled) && Spells.Drill.IsKnown())
                return false;

            return await MachinistRoutine.HeatedSlugShot.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HeatedCleanShot()
        {
            if (!MachinistRoutine.CanContinueComboAfter(Spells.SlugShot))
                return false;

            if (MachinistSettings.Instance.UseDrill && Spells.Drill.IsKnownAndReady(200))
                return false;

            if (MachinistSettings.Instance.UseHotAirAnchor && Spells.AirAnchor.IsKnownAndReady(200))
                return false;

            if (MachinistSettings.Instance.UseChainSaw && Spells.ChainSaw.IsKnownAndReady(200))
                return false;

            if (Core.Me.HasAura(Auras.Overheated) && Spells.HeatBlast.IsKnown())
                return false;

            if (Core.Me.HasAura(Auras.Reassembled) && Spells.Drill.IsKnown())
                return false;

            return await MachinistRoutine.HeatedCleanShot.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Drill()
        {
            if (!MachinistSettings.Instance.UseDrill)
                return false;

            if (!Spells.Drill.IsKnownAndReady())
                return false;

            if (Core.Me.HasAura(Auras.Overheated) && !MachinistSettings.Instance.DoubleHyperchargedWildfire)
                return false;

            if (Core.Me.HasAura(Auras.WildfireBuff) && Core.Me.HasAura(Auras.Overheated))
                return false;

            /*
            if (MachinistSettings.Instance.UseReassembleOnDrill && !Core.Me.HasAura(Auras.Reassembled) && Core.Me.ClassLevel >= 10)
            {
               if ((Core.Me.ClassLevel > 83 && Spells.Reassemble.Charges >= 1 && Spells.Reassemble.IsKnown())
                   || (Core.Me.ClassLevel < 84 && Spells.Reassemble.IsKnownAndReady()) )
               {
                    SpellQueueLogic.SpellQueue.Clear();
                    SpellQueueLogic.Timeout.Start();
                    SpellQueueLogic.CancelSpellQueue = () => SpellQueueLogic.Timeout.ElapsedMilliseconds > 3000;
                    SpellQueueLogic.SpellQueue.Enqueue(new QueueSpell { Spell = Spells.Reassemble, TargetSelf = true });
                    SpellQueueLogic.SpellQueue.Enqueue(new QueueSpell { Spell = Spells.Drill });
                    return true;
                }
            }
            */

            return await Spells.Drill.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HotAirAnchor()
        {
            if (!MachinistSettings.Instance.UseHotAirAnchor)
                return false;

            if (!Spells.AirAnchor.IsKnownAndReady() && !Spells.HotShot.IsKnownAndReady())
                return false;

            if (Core.Me.HasAura(Auras.Overheated) && !MachinistSettings.Instance.DoubleHyperchargedWildfire)
                return false;

            if (Core.Me.HasAura(Auras.WildfireBuff) && Core.Me.HasAura(Auras.Overheated))
                return false;

            if (ActionResourceManager.Machinist.Battery >= 100)
                return false;

            /*
            if (MachinistSettings.Instance.UseReassembleOnAA && !Core.Me.HasAura(Auras.Reassembled) && Core.Me.ClassLevel >= 10)
            {
                if ((Core.Me.ClassLevel > 83 && Spells.Reassemble.Charges >= 1 && Spells.Reassemble.IsKnown())
                    || (Core.Me.ClassLevel < 84 && Spells.Reassemble.IsKnownAndReady()))
                {
                    SpellQueueLogic.SpellQueue.Clear();
                    SpellQueueLogic.Timeout.Start();
                    SpellQueueLogic.CancelSpellQueue = () => SpellQueueLogic.Timeout.ElapsedMilliseconds > 3000;
                    SpellQueueLogic.SpellQueue.Enqueue(new QueueSpell { Spell = Spells.Reassemble, TargetSelf = true });
                    SpellQueueLogic.SpellQueue.Enqueue(new QueueSpell { Spell = MachinistRoutine.HotAirAnchor });
                    return true;
                }
            }
            */

            return await MachinistRoutine.HotAirAnchor.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> HeatBlast()
        {
            if (ActionResourceManager.Machinist.OverheatRemaining == TimeSpan.Zero)
                return false;

            if (MachinistSettings.Instance.DoubleHyperchargedWildfire
                && Spells.FullMetalField.IsKnown()
                && Spells.Wildfire.IsReady())
                return false;

            return await Spells.HeatBlast.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> GaussRound()
        {
            if (!MachinistSettings.Instance.UseGaussRound)
                return false;

            var spell = Spells.GaussRound.Masked();

            if (Casting.LastSpell == Spells.Wildfire || Casting.LastSpell == Spells.Hypercharge || Casting.LastSpell == spell)
                return false;

            if (Spells.Wildfire.IsKnownAndReady() && Spells.Hypercharge.IsKnownAndReady() && spell.Charges < 1.5f)
                return false;

            if (Spells.Ricochet.IsKnown())
            {
                if (spell.Charges < 1.5f && Spells.Wildfire.IsKnownAndReady(2000))
                    return false;

                // Do not run Gauss if an hypercharge is almost ready and not enough charges available for Rico and Gauss
                if (ActionResourceManager.Machinist.Heat > 40 || Spells.Hypercharge.IsKnownAndReady())
                {
                    if (spell.Charges < 1.5f && Spells.Ricochet.Masked().Charges < 0.5f)
                        return false;
                }
            }

            if (MachinistSettings.Instance.UseRicochet && Spells.Ricochet.Masked().Charges > spell.Charges)
                return false;

            if (MachinistSettings.Instance.DoubleHyperchargedWildfire
                && Combat.IsBoss()
                && Core.Me.HasAura(Auras.WildfireBuff, true)
                && !Core.Me.HasAura(Auras.Overheated)
                && ActionResourceManager.Machinist.Heat > 50)
                return false;

            return await spell.Cast(Core.Me.CurrentTarget);
        }
    }
}
