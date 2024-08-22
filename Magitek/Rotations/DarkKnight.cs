﻿using ff14bot;
using ff14bot.Managers;
using Magitek.Extensions;
using Magitek.Logic;
using Magitek.Logic.DarkKnight;
using Magitek.Logic.Roles;
using Magitek.Models.Account;
using Magitek.Models.DarkKnight;
using Magitek.Utilities;
using System.Threading.Tasks;
using DarkKnightRoutine = Magitek.Utilities.Routines.DarkKnight;

namespace Magitek.Rotations
{
    public static class DarkKnight
    {
        public static async Task<bool> PreCombatBuff()
        {
            await Casting.CheckForSuccessfulCast();

            if (WorldManager.InSanctuary)
                return false;

            return await Buff.Grit();
        }

        public static async Task<bool> Pull()
        {
            if (BotManager.Current.IsAutonomous)
            {
                if (Core.Me.HasTarget)
                {
                    Movement.NavigateToUnitLos(Core.Me.CurrentTarget, Core.Me.CurrentTarget.CombatReach);
                }
            }

            return await Combat();
        }
        public static async Task<bool> Heal()
        {
            if (await GambitLogic.Gambit())
                return true;

            if (await Casting.TrackSpellCast())
                return true;

            await Casting.CheckForSuccessfulCast();

            return false;
        }

        public static async Task<bool> Combat()
        {
            if (BaseSettings.Instance.ActivePvpCombatRoutine)
                return await PvP();

            if (BotManager.Current.IsAutonomous)
            {
                if (Core.Me.HasTarget)
                    Movement.NavigateToUnitLos(Core.Me.CurrentTarget, 2 + Core.Me.CurrentTarget.CombatReach);
            }

            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            if (await CustomOpenerLogic.Opener()) return true;

            //LimitBreak
            if (Defensive.ForceLimitBreak()) return true;

            if (await CommonFightLogic.FightLogic_TankDefensive(DarkKnightSettings.Instance.FightLogicDefensives, DarkKnightRoutine.DefensiveSpells, DarkKnightRoutine.Defensives)) return true;
            if (await CommonFightLogic.FightLogic_PartyShield(DarkKnightSettings.Instance.FightLogicPartyShield, Spells.DarkMissionary, true, aura: Auras.DarkMissionary)) return true;
            if (await CommonFightLogic.FightLogic_Debuff(DarkKnightSettings.Instance.FightLogicReprisal, Spells.Reprisal, true, aura: Auras.Reprisal)) return true;

            //Utility
            if (await Buff.Grit()) return true;
            if (await Tank.Interrupt(DarkKnightSettings.Instance)) return true;

            if (DarkKnightRoutine.GlobalCooldown.CountOGCDs() < 2 && Spells.HardSlash.Cooldown.TotalMilliseconds > 650 + BaseSettings.Instance.UserLatencyOffset)
            {
                //Potion
                if (await Buff.UsePotion()) return true;

                //Defensive Buff
                if (await Defensive.Execute()) return true;
                if (await Defensive.Oblation(true)) return true;
                if (await Defensive.Reprisal()) return true;
                if (await Tank.ArmsLength(DarkKnightSettings.Instance)) return true;

                if (await SingleTarget.CarveAndSpit()) return true;
                if (await Aoe.SaltedEarth()) return true;
                if (await Aoe.AbyssalDrain()) return true;
                if (await Aoe.FloodofDarknessShadow()) return true;
                if (await SingleTarget.EdgeofDarknessShadow()) return true;
                if (await Buff.Delirium()) return true;
                if (await Buff.BloodWeapon()) return true;
                if (await Buff.LivingShadow()) return true;
                if (await SingleTarget.Shadowbringer()) return true;
            }

            //Pull or get back aggro with LightningShot
            if (await SingleTarget.UnmendToPullOrAggro()) return true;
            if (await SingleTarget.UnmendToDps()) return true;

            if (await SingleTarget.Disesteem()) return true;

            if (await Aoe.Quietus()) return true;
            if (await Aoe.StalwartSoul()) return true;
            if (await Aoe.Unleash()) return true;

            if (await SingleTarget.Bloodspiller()) return true;
            if (await SingleTarget.SoulEater()) return true;
            if (await SingleTarget.SyphonStrike()) return true;

            return await SingleTarget.HardSlash();
        }
        public static async Task<bool> PvP()
        {
            if (!BaseSettings.Instance.ActivePvpCombatRoutine)
                return await Combat();

            if (await CommonPvp.CommonTasks(DarkKnightSettings.Instance)) return true;
            
            
            if (await Pvp.SaltAndDarkness()) return true;
            if (await Pvp.EventidePvp()) return true;
            if (await Pvp.BlackestNightPvp()) return true;
            if (await Pvp.SaltedEarthPvp()) return true;

            if (!CommonPvp.GuardCheck(DarkKnightSettings.Instance))
            {
                if (await Pvp.PlungePvp()) return true;
                if (await Pvp.QuietusPvp()) return true;
                if (await Pvp.BloodspillerPvp()) return true;
                if (await Pvp.ShadowbringerPvp()) return true;

            }

            if (await Pvp.SouleaterPvp()) return true;
            if (await Pvp.SyphonStrikePvp()) return true;

            return (await Pvp.HardSlashPvp());
        }

        public static Task<bool> Rest() => Task.FromResult(false);
        public static Task<bool> CombatBuff() => Task.FromResult(false);
    }
}
