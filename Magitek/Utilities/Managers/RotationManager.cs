using ff14bot;
using ff14bot.Enums;
using Magitek.Logic;
using Magitek.Models.Account;
using Magitek.Utilities.CombatMessages;
using PropertyChanged;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using ff14bot.Managers;
using Magitek.Extensions;

namespace Magitek.Utilities.Managers
{
    [AddINotifyPropertyChangedInterface]
    internal static class RotationManager
    {
        public static IRotation Rotation => new RotationComposites();
        public static ClassJobType CurrentRotation => Core.Me.CurrentJob;
    }

    public interface IRotation
    {
        Task<bool> Rest();
        Task<bool> PreCombatBuff();
        Task<bool> Pull();
        Task<bool> Heal();
        Task<bool> CombatBuff();
        Task<bool> Combat();
        Task<bool> PvP();
    }

    public abstract class Rotation : IRotation
    {
        public abstract Task<bool> Rest();
        public abstract Task<bool> PreCombatBuff();
        public abstract Task<bool> Pull();
        public abstract Task<bool> Heal();
        public abstract Task<bool> CombatBuff();
        public abstract Task<bool> Combat();
        public abstract Task<bool> PvP();
    }

    [AddINotifyPropertyChangedInterface]
    internal class RotationComposites : Rotation
    {
        private static readonly Dictionary<ClassJobType, string> RotationClassMap = new()
        {
            { ClassJobType.Gladiator, "Paladin" },
            { ClassJobType.Paladin, "Paladin" },
            { ClassJobType.Pugilist, "Monk" },
            { ClassJobType.Monk, "Monk" },
            { ClassJobType.Marauder, "Warrior" },
            { ClassJobType.Warrior, "Warrior" },
            { ClassJobType.Lancer, "Dragoon" },
            { ClassJobType.Dragoon, "Dragoon" },
            { ClassJobType.Archer, "Bard" },
            { ClassJobType.Bard, "Bard" },
            { ClassJobType.Conjurer, "WhiteMage" },
            { ClassJobType.WhiteMage, "WhiteMage" },
            { ClassJobType.Thaumaturge, "BlackMage" },
            { ClassJobType.BlackMage, "BlackMage" },
            { ClassJobType.Arcanist, "Summoner" },
            { ClassJobType.Summoner, "Summoner" },
            { ClassJobType.Scholar, "Scholar" },
            { ClassJobType.Rogue, "Ninja" },
            { ClassJobType.Ninja, "Ninja" },
            { ClassJobType.Machinist, "Machinist" },
            { ClassJobType.DarkKnight, "DarkKnight" },
            { ClassJobType.Astrologian, "Astrologian" },
            { ClassJobType.Samurai, "Samurai" },
            { ClassJobType.BlueMage, "BlueMage" },
            { ClassJobType.RedMage, "RedMage" },
            { ClassJobType.Gunbreaker, "Gunbreaker" },
            { ClassJobType.Dancer, "Dancer" },
            { ClassJobType.Reaper, "Reaper" },
            { ClassJobType.Sage, "Sage" },
            { ClassJobType.Viper, "Viper" },
            { ClassJobType.Pictomancer, "Pictomancer" }
        };

        // Cache the reflected methods so reflection only has to happen once.
        private static readonly Dictionary<(ClassJobType, string), Func<Task<bool>>> MethodCache = new();
        private async Task<bool> ExecuteRotationMethod(ClassJobType jobType, string methodName)
        {
            var cacheKey = (jobType, methodName);

            if (!MethodCache.TryGetValue(cacheKey, out var cachedMethod))
            {
                if (RotationClassMap.TryGetValue(jobType, out var className))
                {
                    var rotationType = Type.GetType($"Magitek.Rotations.{className}");
                    if (rotationType != null)
                    {
                        var method = rotationType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public);
                        if (method != null)
                        {
                            cachedMethod = (Func<Task<bool>>)Delegate.CreateDelegate(typeof(Func<Task<bool>>), method);
                            MethodCache[cacheKey] = cachedMethod; // Cache the method
                        }
                    }
                }
            }

            return cachedMethod != null ? await cachedMethod() : false;
        }

        private static Action GetGroupExtensionForJob(ClassJobType jobType)
        {
            return jobType switch
            {
                ClassJobType.WhiteMage => Routines.WhiteMage.GroupExtension,
                ClassJobType.Scholar => Routines.Scholar.GroupExtension,
                ClassJobType.Astrologian => Routines.Astrologian.GroupExtension,
                ClassJobType.Sage => Routines.Sage.GroupExtension,
                _ => null
            };
        }

        public override async Task<bool> Rest()
        {
            if (!BaseSettings.Instance.ActiveCombatRoutine)
                return false;

            if (BaseSettings.Instance.ActivePvpCombatRoutine)
                return await PvP();

            await Chocobo.HandleChocobo();

            return await ExecuteRotationMethod(RotationManager.CurrentRotation, "Rest");            
        }

        public override async Task<bool> PreCombatBuff()
        {
            if (!BaseSettings.Instance.ActiveCombatRoutine)
                return false;

            if (BaseSettings.Instance.ActivePvpCombatRoutine)
                return await PvP();

            await Chocobo.HandleChocobo();

            Group.UpdateAllies(GetGroupExtensionForJob(RotationManager.CurrentRotation));
            Globals.HealTarget = Group.CastableAlliesWithin30.FirstOrDefault();

            if (Core.Me.IsMounted)
                return false;

            if (Core.Me.IsCasting)
                return true;

            if (await Casting.TrackSpellCast())
                return true;

            await Casting.CheckForSuccessfulCast();
            SpellQueueLogic.SpellQueue.Clear();

            if (WorldManager.InSanctuary)
                return false;

            if (Globals.OnPvpMap)
                return false;

            if (DutyManager.InInstance && !Globals.InActiveDuty)
                return false;

            return await ExecuteRotationMethod(RotationManager.CurrentRotation, "PreCombatBuff");
        }

        public override async Task<bool> Pull()
        {
            if (!BaseSettings.Instance.ActiveCombatRoutine)
                return false;

            if (BaseSettings.Instance.ActivePvpCombatRoutine)
                return await PvP();

            if (BotManager.Current.IsAutonomous)
            {
                if (Core.Me.HasTarget)
                    Movement.NavigateToUnitLos(Core.Me.CurrentTarget, (Core.Me.IsRanged() ? 20 : 0) + Core.Me.CurrentTarget.CombatReach);
            }

            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            if (await Casting.TrackSpellCast())
                return true;

            await Casting.CheckForSuccessfulCast();

            if (await GambitLogic.Gambit()) 
                return true;
            if (await CustomOpenerLogic.Opener()) 
                return true;

            return await ExecuteRotationMethod(RotationManager.CurrentRotation, "Pull");
        }

        public override async Task<bool> Heal()
        {
            if (!BaseSettings.Instance.ActiveCombatRoutine)
                return false;

            if (BaseSettings.Instance.ActivePvpCombatRoutine)
                return await PvP();

            if (Core.Me.IsMounted)
                return true;

            if (WorldManager.InSanctuary)
                return false;

            await Chocobo.HandleChocobo();

            Group.UpdateAllies(GetGroupExtensionForJob(RotationManager.CurrentRotation));
            Globals.HealTarget = Group.CastableAlliesWithin30.FirstOrDefault();

            if (await Casting.TrackSpellCast()) return true;
            await Casting.CheckForSuccessfulCast();
            Casting.DoHealthChecks = false;

            if (await GambitLogic.Gambit()) 
                return true;
            // Heal is pulsed even when not in combat.
            // which allows openers to be checked when not in combat.
            if (await CustomOpenerLogic.Opener()) 
                return true;

            return await ExecuteRotationMethod(RotationManager.CurrentRotation, "Heal");
        }

        public override async Task<bool> CombatBuff()
        {
            if (!BaseSettings.Instance.ActiveCombatRoutine)
                return false;

            if (BaseSettings.Instance.ActivePvpCombatRoutine)
                return await PvP();

            return await ExecuteRotationMethod(RotationManager.CurrentRotation, "CombatBuff");
        }

        public override async Task<bool> Combat()
        {
            if (!BaseSettings.Instance.ActiveCombatRoutine)
                return false;

            if (BaseSettings.Instance.ActivePvpCombatRoutine)
                return await PvP();

            Group.UpdateAllies(GetGroupExtensionForJob(RotationManager.CurrentRotation));
            Globals.HealTarget = Group.CastableAlliesWithin30.FirstOrDefault();
            await Chocobo.HandleChocobo();

            if (BotManager.Current.IsAutonomous)
            {
                if (Core.Me.HasTarget)
                    Movement.NavigateToUnitLos(Core.Me.CurrentTarget, (Core.Me.IsRanged() ? 20 : 2) + Core.Me.CurrentTarget.CombatReach);
            }

            if (Core.Me.CurrentTarget.HasAnyAura(Auras.Invincibility))
                return false;

            if (Core.Me.IsCasting)
                return true;

            if (await Casting.TrackSpellCast())
                return true;
            await Casting.CheckForSuccessfulCast();

            if (!SpellQueueLogic.SpellQueue.Any())
                SpellQueueLogic.InSpellQueue = false;

            if (SpellQueueLogic.SpellQueue.Any())
                if (await SpellQueueLogic.SpellQueueMethod())
                    return true;

            if (await GambitLogic.Gambit()) 
                return true;
            if (await CustomOpenerLogic.Opener())
                return true;

            return await ExecuteRotationMethod(RotationManager.CurrentRotation, "Combat");
        }

        public override async Task<bool> PvP()
        {
            if (!BaseSettings.Instance.ActiveCombatRoutine)
                return false;

            if (!BaseSettings.Instance.ActivePvpCombatRoutine)
                return await Combat();

            Group.UpdateAllies(GetGroupExtensionForJob(RotationManager.CurrentRotation));
            Globals.HealTarget = Group.CastableAlliesWithin30.FirstOrDefault();

            return await ExecuteRotationMethod(RotationManager.CurrentRotation, "PvP");
        }
    }
}
