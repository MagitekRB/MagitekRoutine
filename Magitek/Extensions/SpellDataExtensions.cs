using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Models.Account;
using Magitek.Utilities;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using Debug = Magitek.ViewModels.Debug;

namespace Magitek.Extensions
{
    internal static class SpellDataExtensions
    {
        #region PvpCombo
        public static async Task<bool> CastPvpCombo(this SpellData spell, uint spellPvpCombo, GameObject target, [CallerMemberName] string caller = null, [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = null)
        {
            if (BaseSettings.Instance.DebugCastingCallerMemberName)
            {
                Logger.WriteInfo($@"[Cast Pvp Combo] [{sourceLineNumber}] {caller}");

                if (BaseSettings.Instance.DebugCastingCallerMemberNameIncludePath)
                {
                    Logger.WriteInfo($@"[Path] {sourceFilePath}");
                }
            }

            if (!GameSettingsManager.FaceTargetOnAction && BaseSettings.Instance.AssumeFaceTargetOnAction)
                GameSettingsManager.FaceTargetOnAction = true;

            if (BotManager.Current.IsAutonomous && !GameSettingsManager.FaceTargetOnAction && !RoutineManager.IsAnyDisallowed(CapabilityFlags.Facing) && !MovementManager.IsMoving)
                Core.Me.Face(target);

            return await DoPvPCombo(spell, spellPvpCombo, target);
        }

        private static async Task<bool> DoPvPCombo(SpellData spell, uint pvpComboId, GameObject target)
        {
            if (target == null)
                return false;

            if (target.Distance() > spell.Range)
                return false;
            
            if (ActionManager.GetPvPComboCurrentActionId(pvpComboId) != spell.Id)
                return false;

            if (!ActionManager.DoPvPCombo(pvpComboId, target))
                return false;

            Logger.WriteCastExecuted($@"Cast PVP Combo: [{pvpComboId}] {spell}");

            if (spell.AdjustedCastTime != TimeSpan.Zero)
            {
                if (!await Coroutine.Wait(3000, () => Core.Me.IsCasting))
                    return false;
            }

            Casting.CastingSpell = spell;
            Casting.SpellCastTime = spell.AdjustedCastTime;
            Casting.CastingHeal = false;
            Casting.SpellTarget = target;
            Casting.CastingTime.Restart();

            if (!BaseSettings.Instance.DebugPlayerCasting)
                return true;

            Debug.Instance.CastingSpell = spell;
            Debug.Instance.CastingHeal = false;
            Debug.Instance.SpellTarget = target;

            return true;
        }
        #endregion

        public static async Task<bool> Cast(this SpellData spell, GameObject target, [CallerMemberName] string caller = null, [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = null)
        {
            if (BaseSettings.Instance.DebugCastingCallerMemberName)
            {
                Logger.WriteInfo($@"[Cast] [{sourceLineNumber}] {caller}");

                if (BaseSettings.Instance.DebugCastingCallerMemberNameIncludePath)
                {
                    Logger.WriteInfo($@"[Path] {sourceFilePath}");
                }

            }

            if (!GameSettingsManager.FaceTargetOnAction && BaseSettings.Instance.AssumeFaceTargetOnAction)
                GameSettingsManager.FaceTargetOnAction = true;

            if (BotManager.Current.IsAutonomous && !GameSettingsManager.FaceTargetOnAction && !RoutineManager.IsAnyDisallowed(CapabilityFlags.Facing) && !MovementManager.IsMoving)
                Core.Me.Face(target);

            return await DoAction(spell, target);
        }

        public static async Task<bool> CastAura(this SpellData spell, GameObject target, uint aura, bool useRefreshTime = false, int refreshTime = 0, bool needAura = true, GameObject auraTarget = null, [CallerMemberName] string caller = null, [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = null)
        {
            if (BaseSettings.Instance.DebugCastingCallerMemberName)
            {
                Logger.WriteInfo($@"[CastAura] [{sourceLineNumber}] {caller}");

                if (BaseSettings.Instance.DebugCastingCallerMemberNameIncludePath)
                {
                    Logger.WriteInfo($@"[Path] {sourceFilePath}");
                }
            }

            if (!GameSettingsManager.FaceTargetOnAction && BaseSettings.Instance.AssumeFaceTargetOnAction)
                GameSettingsManager.FaceTargetOnAction = true;

            if (BotManager.Current.IsAutonomous && !GameSettingsManager.FaceTargetOnAction && !RoutineManager.IsAnyDisallowed(CapabilityFlags.Facing) && !MovementManager.IsMoving)
                Core.Me.Face(target);

            return await DoAction(spell, target, aura, needAura, useRefreshTime, refreshTime, auraTarget: auraTarget);
        }

        public static async Task<bool> Heal(this SpellData spell, GameObject target, bool healthChecks = true, [CallerMemberName] string caller = null, [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = null)
        {
            if (BaseSettings.Instance.DebugCastingCallerMemberName)
            {
                Logger.WriteInfo($@"[Heal] [{sourceLineNumber}] {caller}");

                if (BaseSettings.Instance.DebugCastingCallerMemberNameIncludePath)
                {
                    Logger.WriteInfo($@"[Path] {sourceFilePath}");
                }
            }

            return await DoActionHeal(spell, target, healthChecks);
        }

        public static async Task<bool> HealAura(this SpellData spell, GameObject target, uint aura, bool healthChecks = true, bool needAura = true, bool useRefreshTime = false, int refreshTime = 0, GameObject auraTarget = null, [CallerMemberName] string caller = null, [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = null)
        {
            if (BaseSettings.Instance.DebugCastingCallerMemberName)
            {
                Logger.WriteInfo($@"[HealAura] [{sourceLineNumber}] {caller}");

                if (BaseSettings.Instance.DebugCastingCallerMemberNameIncludePath)
                {
                    Logger.WriteInfo($@"[Path] {sourceFilePath}");
                }
            }

            return await DoActionHeal(spell, target, healthChecks, aura, needAura, useRefreshTime, refreshTime, auraTarget: auraTarget);
        }

        private static bool Check(SpellData spell, GameObject target)
        {
            if (target == null)
                return false;

            if (!ActionManager.HasSpell(spell.Id) && !Core.Me.OnPvpMap())
                return false;

            if (!BaseSettings.Instance.UseCastOrQueue)
            {
                if (!ActionManager.CanCast(spell, target) && !Core.Me.OnPvpMap())
                    return false;
            }
            else
            {
                if (!ActionManager.CanCastOrQueue(spell, target) && !Core.Me.OnPvpMap())
                    return false;
            }

            if (Core.Me.OnPvpMap())
            {
                // these spells have cast times, but can be cast while moving
                if (spell == Spells.BlastChargePvp || spell == Spells.PowerfulShotPvp)
                    return true;
            }

            if (BotManager.Current.IsAutonomous)
            {
                // If we're using an autonomous bot base and we're running out of avoidance then don't cast
                if (AvoidanceManager.IsRunningOutOfAvoid &&
                    !(Core.Me.HasAura(Auras.Swiftcast) || spell.AdjustedCastTime <= TimeSpan.Zero))
                {
                    return false;
                }

                // If Facing is dis-allowed and the action would cause a new facing check then don't cast
                if (GameSettingsManager.FaceTargetOnAction
                    && RoutineManager.IsAnyDisallowed(CapabilityFlags.Facing)
                    && target != Core.Me)
                    return false;

                // Autonomous mode should still perform moving or instant
                // spell checks. This reverts a previous issue where bots may
                // potentially get stuck on a wall due to the avoidance not 
                // stopping after it successfully avoids
                // See.
                // https://github.com/Exmortem/MagitekRoutine/pull/396
            }

            return Core.Me.HasAura(Auras.Swiftcast) || !MovementManager.IsMoving || spell.AdjustedCastTime <= TimeSpan.Zero;
        }

        public static bool CanCast(this SpellData spell, GameObject target)
        {
            if (!BaseSettings.Instance.UseCastOrQueue)
            {
                return ActionManager.CanCast(spell, target);
            }
            else
            {
                return ActionManager.CanCastOrQueue(spell, target);
            }
        }


        public static bool CanCast(this SpellData spell)
        {
            if (!BaseSettings.Instance.UseCastOrQueue)
            {
                return ActionManager.CanCast(spell, Core.Me);
            }
            else
            {
                return ActionManager.CanCastOrQueue(spell, Core.Me);
            }
        }

        public static SpellData Masked(this SpellData spell)
        {
            return ActionManager.GetMaskedAction(spell.Id);
        }

        private static async Task<bool> DoAction(SpellData spell, GameObject target, uint aura = 0, bool needAura = false, bool useRefreshTime = false, int refreshTime = 0, bool canCastCheck = true, GameObject auraTarget = null)
        {
            if (!Check(spell, target))
                return false;

            if (spell.GroundTarget)
            {
                if (!ActionManager.DoActionLocation(spell.Id, target.Location))
                    return false;
            }
            else
            {
                if (!ActionManager.DoAction(spell, target))
                    return false;
            }

            Logger.WriteCastExecuted($@"Cast: {spell}");

            if (spell.AdjustedCastTime != TimeSpan.Zero)
            {
                if (!await Coroutine.Wait(3000, () => Core.Me.IsCasting))
                {
                    return false;
                }
            }

            Casting.CastingSpell = spell;
            Casting.SpellCastTime = spell.AdjustedCastTime;
            Casting.CastingHeal = false;
            Casting.SpellTarget = target;
            Casting.NeedAura = needAura;
            Casting.Aura = aura;
            Casting.AuraTarget = auraTarget;
            Casting.UseRefreshTime = useRefreshTime;
            Casting.RefreshTime = refreshTime;
            Casting.CastingTime.Restart();

            if (!BaseSettings.Instance.DebugPlayerCasting)
                return true;

            Debug.Instance.CastingSpell = spell;
            Debug.Instance.CastingHeal = false;
            Debug.Instance.SpellTarget = target;
            Debug.Instance.NeedAura = needAura;
            Debug.Instance.Aura = aura;
            Debug.Instance.UseRefreshTime = useRefreshTime;
            Debug.Instance.RefreshTime = refreshTime;

            return true;
        }

        private static async Task<bool> DoActionHeal(SpellData spell, GameObject target, bool healthChecks = true, uint aura = 0, bool needAura = false, bool useRefreshTime = false, int refreshTime = 0, GameObject auraTarget = null)
        {
            if (!Check(spell, target))
                return false;

            if (!ActionManager.DoAction(spell, target))
                return false;

            Logger.WriteCastExecuted($@"Cast: {spell}");

            if (spell.AdjustedCastTime != TimeSpan.Zero)
            {
                if (!await Coroutine.Wait(3000, () => Core.Me.IsCasting))
                {
                    return false;
                }
            }

            Casting.CastingHeal = true;
            Casting.CastingSpell = spell;
            Casting.SpellCastTime = spell.AdjustedCastTime;
            Casting.SpellTarget = target;
            Casting.NeedAura = needAura;
            Casting.Aura = aura;
            Casting.AuraTarget = auraTarget;
            Casting.UseRefreshTime = useRefreshTime;
            Casting.DoHealthChecks = healthChecks;
            Casting.RefreshTime = refreshTime;
            Casting.CastingTime.Restart();

            if (!BaseSettings.Instance.DebugPlayerCasting)
                return true;

            Debug.Instance.CastingHeal = true;
            Debug.Instance.CastingSpell = spell;
            Debug.Instance.SpellTarget = target;
            Debug.Instance.NeedAura = needAura;
            Debug.Instance.Aura = aura;
            Debug.Instance.UseRefreshTime = useRefreshTime;
            Debug.Instance.DoHealthChecks = healthChecks;
            Debug.Instance.RefreshTime = refreshTime;

            return true;
        }

        public static bool IsKnown(this SpellData spell)
        {
            return ActionManager.HasSpell(spell.Id);
        }

        public static double CooldownToNextCharge(this SpellData spell)
        {
            if (spell.MaxCharges > 1)
            {
                if (spell.Charges == spell.MaxCharges)
                    return 0;
                var remainingCooldownTime = spell.Cooldown.TotalMilliseconds - (spell.AdjustedCooldown.TotalMilliseconds * (spell.MaxCharges - (uint)Math.Floor(spell.Charges) - 1));
                if (remainingCooldownTime < 0)
                    return 0;
                return remainingCooldownTime;
            }

            return Math.Max(spell.Cooldown.TotalMilliseconds, 0);
        }

        public static bool IsReady(this SpellData spell, int remainingTimeInMs = 0)
        {
            if (spell.MaxCharges > 1)
            {
                if (spell.Charges >= 1)
                    return true;

                var remainingCooldownTime = spell.CooldownToNextCharge();

                if (!BaseSettings.Instance.UseCastOrQueue)
                {
                    return remainingCooldownTime <= remainingTimeInMs;
                }
                else
                {
                    return remainingCooldownTime <= 500;
                }
            }

            if (!BaseSettings.Instance.UseCastOrQueue)
            {
                return spell.Cooldown.TotalMilliseconds <= remainingTimeInMs;
            }
            else
            {
                return spell.Cooldown.TotalMilliseconds <= 500;
            }
        }

        public static bool IsKnownAndReady(this SpellData spell, int remainingTimeInMs = 0)
        {
            return spell.IsKnown() && spell.IsReady(remainingTimeInMs);
        }

        public static bool IsKnownAndReadyAndCastable(this SpellData spell, int remainingTimeInMs = 0)
        {
            return spell.IsKnown() && spell.IsReady(remainingTimeInMs) && spell.CanCast(Core.Me.CurrentTarget);
        }

        public static bool IsKnownAndReadyAndCastableAtTarget(this SpellData spell, int remainingTimeInMs = 0)
        {
            return spell.IsKnown() && spell.IsReady(remainingTimeInMs) && spell.CanCast(Core.Me.CurrentTarget);
        }

        public static bool IsKnownAndReadyAndCastable(this SpellData spell, GameObject target, int remainingTimeInMs = 0)
        {
            return spell.IsKnown() && spell.IsReady(remainingTimeInMs) && spell.CanCast(target);
        }

        public static string IconUrl(this SpellData spell)
        {
            var icon = (decimal)spell.Icon;
            var folder = (Math.Floor(icon / 1000) * 1000).ToString(CultureInfo.InvariantCulture).Trim().PadLeft(6, '0');
            var image = spell.Icon.ToString(CultureInfo.InvariantCulture).Trim().PadLeft(6, '0');
            return $@"https://secure.xivdb.com/img/game/{folder}/{image}.png";
        }

        public static bool HasCastRecently(this SpellData LastSpellExecuted)
        {
            return Casting.SpellCastHistory.Any(s => s.Spell == LastSpellExecuted);
        }

        public static bool CanContinueComboAfter(this SpellData LastSpellExecuted)
        {
            if (ActionManager.ComboTimeLeft <= 0)
                return false;

            if (ActionManager.LastSpell.Id != LastSpellExecuted.Id)
                return false;

            return true;
        }
    }
}
