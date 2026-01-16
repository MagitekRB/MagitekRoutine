using ff14bot;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using System.Collections.Generic;


namespace Magitek.Utilities.Routines
{
    internal static class Machinist
    {
        public static WeaveWindow GlobalCooldown = new WeaveWindow(ClassJobType.Machinist, Spells.SplitShot, new List<SpellData>() { Spells.Flamethrower });

        public static SpellData HeatedSplitShot => Spells.HeatedSplitShot.IsKnown()
                                                    ? Spells.HeatedSplitShot
                                                    : Spells.SplitShot;
        public static SpellData HeatedSlugShot => Spells.HeatedSlugShot.IsKnown()
                                                    ? Spells.HeatedSlugShot
                                                    : Spells.SlugShot;

        public static SpellData HeatedCleanShot => Spells.HeatedCleanShot.IsKnown()
                                                    ? Spells.HeatedCleanShot
                                                    : Spells.CleanShot;

        public static SpellData HotAirAnchor => Spells.AirAnchor.IsKnown()
                                                    ? Spells.AirAnchor
                                                    : Spells.HotShot;

        public static SpellData RookQueenPet => Spells.AutomationQueen.IsKnown()
                                                    ? Spells.AutomationQueen
                                                    : Spells.RookAutoturret;

        public static SpellData RookQueenOverdrive => Spells.QueenOverdrive.IsKnown()
                                                    ? Spells.QueenOverdrive
                                                    : Spells.RookOverdrive;

        public static SpellData Scattergun => Spells.Scattergun.IsKnown()
                                                    ? Spells.Scattergun
                                                    : Spells.SpreadShot;

        public static bool CanContinueComboAfter(SpellData LastSpellExecuted)
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (ActionManager.LastSpell.Id != LastSpellExecuted.Id)
                return false;

            return true;
        }

        public static bool CheckCurrentDamageIncrease(int neededDmgIncrease)
        {
            double dmgIncrease = 1;

            //From PLD
            //From DRK
            //From GNB
            //From WAR
            //From WHM
            //From BLM
            //From SAM
            //From MCH
            //From BRD
            if (Core.Me.HasAura(Auras.RadiantFinale))
                dmgIncrease *= 1.06;
            if (Core.Me.HasAura(Auras.BattleVoice))
                dmgIncrease *= 1.01;
            if (Core.Me.HasAura(Auras.TheWanderersMinuet))
                dmgIncrease *= 1.02;
            if (Core.Me.HasAura(Auras.MagesBallad))
                dmgIncrease *= 1.01;
            if (Core.Me.HasAura(Auras.ArmysPaeon))
                dmgIncrease *= 1.01;

            //From DNC
            if (Core.Me.HasAura(Auras.Devilment))
                dmgIncrease *= 1.01;
            if (Core.Me.HasAura(Auras.TechnicalFinish))
                dmgIncrease *= 1.06;
            if (Core.Me.HasAura(Auras.StandardFinish))
                dmgIncrease *= 1.06;

            //From RDM
            if (Core.Me.HasAura(Auras.Embolden))
                dmgIncrease *= 1.05;

            //From SMN
            if (Core.Me.HasAura(Auras.SearingLight))
                dmgIncrease *= 1.03;

            //From MNK
            if (Core.Me.HasAura(Auras.Brotherhood))
                dmgIncrease *= 1.05;

            //From NIN
            if (Core.Me.CurrentTarget.HasAura(Auras.VulnerabilityUp))
                dmgIncrease *= 1.05;

            //From DRG
            if (Core.Me.HasAura(Auras.BattleLitany))
                dmgIncrease *= 1.01;

            //From RPR
            if (Core.Me.HasAura(Auras.ArcaneCircle))
                dmgIncrease *= 1.03;

            //From SCH
            if (Core.Me.CurrentTarget.HasAura(Auras.ChainStratagem))
                dmgIncrease *= 1.01;

            //From SGE

            //From AST
            if (Core.Me.HasAura(Auras.Divination))
                dmgIncrease *= 1.06;
            if (Core.Me.HasAnyDpsCardAura())
                dmgIncrease *= 1.06;

            Logger.WriteInfo($@"Damage Increase: {dmgIncrease}");
            return dmgIncrease >= (1 + (double)neededDmgIncrease / 100);
        }
    }
}
