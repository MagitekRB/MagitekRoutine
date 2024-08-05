﻿using ff14bot;
using ff14bot.Objects;
using Magitek.Models.Roles;
using Magitek.Extensions;
using Magitek.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magitek.Models.Account;

namespace Magitek.Logic.Roles
{
    internal class CommonFightLogic
    {
        public static async Task<bool> FightLogic_TankDefensive(bool useDefensive, SpellData[] defensiveSpells, uint[] defensiveAuras, int castTimeRemainingMs = 0)
        {
            if (!useDefensive)
                return false;

            if (!FightLogic.ZoneHasFightLogic() || !FightLogic.EnemyHasAnyTankbusterLogic())
                return false;

            if (FightLogic.EnemyIsCastingTankBuster() != null
                || FightLogic.EnemyIsCastingSharedTankBuster() != null)
            {
                if (Core.Me.HasAnyAura(defensiveAuras))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                foreach (var defensiveSpell in defensiveSpells)
                {
                    if (defensiveSpell.IsKnownAndReady())
                    {
                        if (BaseSettings.Instance.DebugFightLogic)
                            Logger.WriteInfo($"[TankDefensive Response] Cast {defensiveSpell.Name}");
                        return await FightLogic.DoAndBuffer(defensiveSpell.Cast(Core.Me));
                    }
                }
            }
            return false;
        }

        public static async Task<bool> FightLogic_SelfShield(bool useShield, SpellData spell, bool selfAuraCheck = false, uint aura = 0, int castTimeRemainingMs = 0)
        {
            if (!useShield)
                return false;

            if (!spell.IsKnownAndReady())
                return false;

            if (!FightLogic.ZoneHasFightLogic() || !FightLogic.EnemyHasAnyAoeLogic())
                return false;

            if (FightLogic.EnemyIsCastingAoe() || FightLogic.EnemyIsCastingBigAoe())
            {
                if (selfAuraCheck && Core.Me.HasAura(aura))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[SelfShield Response] Cast {spell.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(Core.Me));
            }
            return false;
        }

        public static async Task<bool> FightLogic_PartyShield(bool useShield, SpellData spell, bool selfAuraCheck = false, uint[] auras = null, uint aura = 0, int castTimeRemainingMs = 0)
        {
            if (!useShield)
                return false;

            if (!spell.IsKnownAndReady())
                return false;

            if (!FightLogic.ZoneHasFightLogic() || !FightLogic.EnemyHasAnyAoeLogic())
                return false;

            if (FightLogic.EnemyIsCastingAoe() || FightLogic.EnemyIsCastingBigAoe())
            {
                if (selfAuraCheck && auras != null && Core.Me.HasAnyAura(auras))
                    return false;

                if (selfAuraCheck && aura != 0 && Core.Me.HasAura(aura))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[PartyShield Response] Cast {spell.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(Core.Me));
            }
            return false;
        }

        public static async Task<bool> FightLogic_Debuff(bool useDebuff, SpellData spell, bool targetAuraCheck = false, uint aura = 0, int castTimeRemainingMs = 0)
        {
            if (!useDebuff)
                return false;

            if (!spell.IsKnownAndReady())
                return false;

            if (!FightLogic.ZoneHasFightLogic())
                return false;

            if (FightLogic.EnemyIsCastingAoe() 
                || FightLogic.EnemyIsCastingBigAoe() 
                || FightLogic.EnemyIsCastingTankBuster() != null 
                || FightLogic.EnemyIsCastingSharedTankBuster() != null)
            {
                if (targetAuraCheck && Core.Me.CurrentTarget.HasAura(aura))
                    return false;

                if (!FightLogic.HodlCastTimeRemaining(castTimeRemainingMs, BaseSettings.Instance.FightLogicResponseDelay))
                    return false;

                if (BaseSettings.Instance.DebugFightLogic)
                    Logger.WriteInfo($"[Debuff Response] Cast {spell.Name} on {Core.Me.CurrentTarget.Name}");

                return await FightLogic.DoAndBuffer(spell.Cast(Core.Me.CurrentTarget));
            }

            return false;
        }
    }
}
