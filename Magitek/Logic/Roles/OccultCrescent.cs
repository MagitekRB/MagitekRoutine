using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models;
using Magitek.Models.Roles;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using System.Collections.Generic;
using System;

namespace Magitek.Logic.Roles
{
    internal static class OCAuras
    {
        public const int
            OffensiveAria = 4247,
            RomeosBallad = 4244;
    }

    internal static class OCSpells
    {
        // Bard Spells
        public static readonly SpellData OffensiveAria = DataManager.GetSpellData(41608);
        public static readonly SpellData RomeosBallad = DataManager.GetSpellData(41609);

        // Knight Spells
        public static readonly SpellData PhantomGuard = DataManager.GetSpellData(41588);

        // Monk Spells
        public static readonly SpellData PhantomKick = DataManager.GetSpellData(41595);
    }

    internal class OccultCrescent
    {
        private static readonly uint KnowledgeCrystal = 2007457;

        private static readonly Dictionary<uint, PhantomJob> PhantomJobAuras = new()
        {
            // { auraId, PhantomJob.JobName }
            { 4363, PhantomJob.Bard },
            { 4358, PhantomJob.Knight },
            { 4360, PhantomJob.Monk }
        };

        public enum PhantomJob
        {
            None,
            Bard,
            Knight,
            Monk
        }

        /// <summary>
        /// Main entry point for Occult Crescent Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        public static async Task<bool> Execute()
        {
            // Check if we're in Occult Crescent content
            if (!Core.Me.OnOccultCrescent())
                return false;

            // Get the current phantom job
            var phantomJob = GetCurrentPhantomJob();
            if (phantomJob == PhantomJob.None)
                return false;

            // Execute phantom job specific logic
            return phantomJob switch
            {
                PhantomJob.Bard => await ExecuteBardPhantomJob(),
                PhantomJob.Knight => await ExecuteKnightPhantomJob(),
                PhantomJob.Monk => await ExecuteMonkPhantomJob(),
                _ => false
            };
        }

        /// <summary>
        /// Determine the current phantom job based on player auras
        /// </summary>
        /// <returns>The current phantom job, or None if no phantom job is active</returns>
        private static PhantomJob GetCurrentPhantomJob()
        {
            foreach (var kvp in PhantomJobAuras)
            {
                if (Core.Me.HasAura(kvp.Key))
                    return kvp.Value;
            }
            return PhantomJob.None;
        }

        /// <summary>
        /// Execute Bard Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteBardPhantomJob()
        {
            // Offensive Aria - damage buff that lasts 70 seconds, only cast in combat
            if (await OffensiveAria())
                return true;

            // Romeo's Ballad - interrupt ability
            if (await RomeosBallad())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Knight Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteKnightPhantomJob()
        {
            // Phantom Guard - defensive cooldown like Rampart
            if (await PhantomGuard())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Monk Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteMonkPhantomJob()
        {
            // Phantom Kick - leap attack with stacking damage buff
            if (await PhantomKick())
                return true;

            return false;
        }

        /// <summary>
        /// Cast Offensive Aria - a damage buff that lasts 70 seconds
        /// Only cast when in combat and buff is not already active
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OffensiveAria()
        {
            // Must be in combat to use this ability
            if (!Core.Me.InCombat)
                return false;

            if (Core.Me.HasAura(OCAuras.OffensiveAria, msLeft: 500))
                return false;

            if (!OCSpells.OffensiveAria.CanCast())
                return false;

            return await OCSpells.OffensiveAria.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Romeo's Ballad - interrupt ability
        /// Out of combat: cast if NpcId 2007457 is in range
        /// In combat: cast only if a monster is casting (to interrupt)
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> RomeosBallad()
        {
            if (!OCSpells.RomeosBallad.CanCast())
                return false;

            if (!Core.Me.InCombat && !Core.Me.HasTarget)
            {
                if (Core.Me.HasAura(OCAuras.RomeosBallad, msLeft: 1500000))
                    return false;

                var targetNpc = GameObjectManager.GetObjectByNPCId(KnowledgeCrystal);

                if (targetNpc != null && targetNpc.IsValid && targetNpc.Distance(Core.Me) <= 5)
                    return await OCSpells.RomeosBallad.Cast(Core.Me);
            }
            // Temporarily disable casting in combat until i can figure out how to determine if a monster will resist or not.
            // else
            // {
            //     // In combat: only cast if a monster is casting (to interrupt)
            //     var castingEnemy = Combat.Enemies.FirstOrDefault(enemy =>
            //         enemy.IsCasting &&
            //         enemy.ValidAttackUnit() &&
            //         enemy.InLineOfSight() &&
            //         enemy.WithinSpellRange(OCSpells.RomeosBallad.Radius));

            //     if (castingEnemy != null)
            //         return await OCSpells.RomeosBallad.Cast(castingEnemy);
            // }

            return false;
        }

        /// <summary>
        /// Cast Phantom Guard - defensive cooldown that reduces damage by 60% for 10s
        /// Works like Rampart for tanks
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> PhantomGuard()
        {
            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.PhantomGuard.CanCast())
                return false;

            // Cast when health is below 75% (like Rampart)
            if (Core.Me.CurrentHealthPercent > 75)
                return false;

            return await OCSpells.PhantomGuard.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Phantom Kick - leap attack that grants stacking damage buff
        /// 100 potency AoE, grants up to 3 stacks for increased damage
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> PhantomKick()
        {
            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.PhantomKick.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is in range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.PhantomKick.Range))
                return false;

            return await OCSpells.PhantomKick.Cast(Core.Me.CurrentTarget);
        }
    }
}