using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models;
using Magitek.Models.OccultCrescent;
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
            RomeosBallad = 4244,
            Pray = 4232,
            EnduringFortitude = 4233,
            HerosRime = 0; // TODO: get real aura value 4248;
    }

    internal static class OCSpells
    {
        // Bard Spells
        public static readonly SpellData OffensiveAria = DataManager.GetSpellData(41608);
        public static readonly SpellData RomeosBallad = DataManager.GetSpellData(41609);
        public static readonly SpellData MightyMarch = DataManager.GetSpellData(41607);
        public static readonly SpellData HerosRime = DataManager.GetSpellData(41610);

        // Knight Spells
        public static readonly SpellData PhantomGuard = DataManager.GetSpellData(41588);
        public static readonly SpellData Pray = DataManager.GetSpellData(41589);
        public static readonly SpellData OccultHeal = DataManager.GetSpellData(41590);
        public static readonly SpellData Pledge = DataManager.GetSpellData(41591);

        // Monk Spells
        public static readonly SpellData PhantomKick = DataManager.GetSpellData(41595);
        public static readonly SpellData OccultCounter = DataManager.GetSpellData(41596);
        public static readonly SpellData Counterstance = DataManager.GetSpellData(41597);
        public static readonly SpellData OccultChakra = DataManager.GetSpellData(41598);

        // Berserker Spells
        public static readonly SpellData Rage = DataManager.GetSpellData(41592);
        public static readonly SpellData DeadlyBlow = DataManager.GetSpellData(41594);

        // Chemist Spells
        public static readonly SpellData OccultPotion = DataManager.GetSpellData(41631);
        public static readonly SpellData OccultEther = DataManager.GetSpellData(41633);
        public static readonly SpellData Revive = DataManager.GetSpellData(41634);
        public static readonly SpellData OccultElixir = DataManager.GetSpellData(41635);

        // Cannoneer Spells
        public static readonly SpellData PhantomFire = DataManager.GetSpellData(41626);
        public static readonly SpellData HolyCannon = DataManager.GetSpellData(41627);
        public static readonly SpellData DarkCannon = DataManager.GetSpellData(41628);
        public static readonly SpellData ShockCannon = DataManager.GetSpellData(41629);
        public static readonly SpellData SilverCannon = DataManager.GetSpellData(41630);
    }

    internal class OccultCrescent
    {
        private static readonly uint KnowledgeCrystal = 2007457;

        private static readonly Dictionary<uint, PhantomJob> PhantomJobAuras = new()
        {
            // { auraId, PhantomJob.JobName }
            { 4363, PhantomJob.Bard },
            { 4358, PhantomJob.Knight },
            { 4360, PhantomJob.Monk },
            { 4359, PhantomJob.Berserker },
            { 4367, PhantomJob.Chemist },
            { 4366, PhantomJob.Cannoneer }
        };

        public enum PhantomJob
        {
            None,
            Bard,
            Knight,
            Monk,
            Berserker,
            Chemist,
            Cannoneer
        }

        /// <summary>
        /// Main entry point for Occult Crescent Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        public static async Task<bool> Execute()
        {
            // Check if OC is enabled
            if (!OccultCrescentSettings.Instance.Enable)
                return false;

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
                PhantomJob.Berserker => await ExecuteBerserkerPhantomJob(),
                PhantomJob.Chemist => await ExecuteChemistPhantomJob(),
                PhantomJob.Cannoneer => await ExecuteCannoneerPhantomJob(),
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
            // Hero's Rime - party damage/healing buff, priority over Aria (can't stack)
            if (await HerosRime())
                return true;

            // Offensive Aria - damage buff that lasts 70 seconds, only cast in combat
            if (await OffensiveAria())
                return true;

            // Mighty March - party regen, high cooldown utility
            if (await MightyMarch())
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

            // Pray - regen effect, buff near knowledge crystal
            if (await Pray())
                return true;

            // Occult Heal - heal spell for party members
            if (await OccultHeal())
                return true;

            // Pledge - invulnerability stacks for party members
            if (await Pledge())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Monk Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteMonkPhantomJob()
        {
            // OccultCounter - attack after parry, highest priority
            if (await OccultCounter())
                return true;

            // Counterstance - parry rate buff / movement speed near crystal
            if (await Counterstance())
                return true;

            // OccultChakra - healing ability
            if (await OccultChakra())
                return true;

            // Phantom Kick - leap attack with stacking damage buff
            if (await PhantomKick())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Berserker Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteBerserkerPhantomJob()
        {
            // Deadly Blow - high damage attack based on missing HP, 30s cooldown
            if (await DeadlyBlow()) return true;

            // Rage - auto attack current target with high damage
            if (await Rage())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Chemist Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteChemistPhantomJob()
        {
            // Revive - resurrect dead party members first
            if (await Revive())
                return true;

            // OccultElixir - party-wide HP/MP restoration (most expensive)
            if (await OccultElixir())
                return true;

            // OccultPotion - HP restoration (expensive)
            if (await OccultPotion())
                return true;

            // OccultEther - MP restoration
            if (await OccultEther())
                return true;

            return false;
        }

        /// <summary>
        /// Execute Cannoneer Phantom Job actions
        /// </summary>
        /// <returns>True if an action was executed, false otherwise</returns>
        private static async Task<bool> ExecuteCannoneerPhantomJob()
        {
            // Phantom Fire - standard ranged attack
            if (await PhantomFire())
                return true;

            // Silver Cannon - ranged attack that reduces damage target deals and takes
            if (await SilverCannon())
                return true;

            // Holy Cannon - ranged attack, more damage vs undead
            if (await HolyCannon())
                return true;

            // Shock Cannon - ranged attack that inflicts paralysis
            if (await ShockCannon())
                return true;

            // Dark Cannon - ranged attack that inflicts blind
            if (await DarkCannon())
                return true;

            return false;
        }

        /// <summary>
        /// Cast Offensive Aria - a damage buff that lasts 70 seconds
        /// Only cast when in combat and buff is not already active
        /// Don't cast if Hero's Rime is active (they don't stack)
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OffensiveAria()
        {
            if (!OccultCrescentSettings.Instance.UseOffensiveAria)
                return false;

            // Must be in combat to use this ability
            if (!Core.Me.InCombat)
                return false;

            if (Core.Me.HasAura(OCAuras.OffensiveAria, msLeft: 500))
                return false;

            // Don't cast if Hero's Rime is active (they don't stack)
            if (Core.Me.HasAura(OCAuras.HerosRime))
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
            if (!OccultCrescentSettings.Instance.UseRomeosBallad)
                return false;

            if (!OCSpells.RomeosBallad.CanCast())
                return false;

            if (!Core.Me.InCombat && !Core.Me.HasTarget)
            {
                if (!OccultCrescentSettings.Instance.RomeosBallad_KnowledgeCrystal)
                    return false;

                if (Core.Me.HasAura(OCAuras.RomeosBallad, msLeft: 1500000))
                    return false;

                var targetNpc = GameObjectManager.GetObjectByNPCId(KnowledgeCrystal);

                if (targetNpc != null && targetNpc.IsValid && targetNpc.Distance(Core.Me) <= 5)
                    return await OCSpells.RomeosBallad.Cast(Core.Me);
            }
            // else if (OccultCrescentSettings.Instance.RomeosBallad_InterruptEnemies)
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
            if (!OccultCrescentSettings.Instance.UsePhantomGuard)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!OCSpells.PhantomGuard.CanCast())
                return false;

            // Cast when health is below configured percentage
            if (Core.Me.CurrentHealthPercent > OccultCrescentSettings.Instance.PhantomGuardHealthPercent)
                return false;

            return await OCSpells.PhantomGuard.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Pray - regen effect that lasts for a duration
        /// When cast near Knowledge Crystal, provides party buff (Enduring Fortitude)
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Pray()
        {
            if (!OccultCrescentSettings.Instance.UsePray)
                return false;

            if (!Core.Me.InCombat)
            {
                // Out of combat: only cast near Knowledge Crystal if enabled
                if (!OccultCrescentSettings.Instance.PrayKnowledgeCrystal)
                    return false;

                if (Core.Me.HasAura(OCAuras.EnduringFortitude, msLeft: 1500000))
                    return false;

                var targetNpc = GameObjectManager.GetObjectByNPCId(KnowledgeCrystal);
                if (targetNpc != null && targetNpc.IsValid && targetNpc.Distance(Core.Me) <= 5)
                {
                    if (!OCSpells.Pray.CanCast())
                        return false;

                    return await OCSpells.Pray.Cast(Core.Me);
                }
            }
            else
            {
                // In combat: cast if we don't have the regen effect and HP is below threshold
                if (Core.Me.HasAura(OCAuras.Pray, msLeft: 3000))
                    return false;

                if (Core.Me.CurrentHealthPercent > OccultCrescentSettings.Instance.PrayHealthPercent)
                    return false;

                if (!OCSpells.Pray.CanCast())
                    return false;

                return await OCSpells.Pray.Cast(Core.Me);
            }

            return false;
        }

        /// <summary>
        /// Cast Occult Heal - healing spell for party members
        /// Similar to Clemency or Cure
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultHeal()
        {
            if (!OccultCrescentSettings.Instance.UseOccultHeal)
                return false;

            if (!OCSpells.OccultHeal.CanCast())
                return false;

            GameObject healTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.OccultHealCastOnAllies)
            {
                // Find party member who needs healing
                healTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultHealHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent)
                    .FirstOrDefault();

                // If no allies need healing, check self
                if (healTarget == null && Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultHealHealthPercent)
                    healTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self
                if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultHealHealthPercent)
                    healTarget = Core.Me;
            }

            if (healTarget == null)
                return false;

            return await OCSpells.OccultHeal.Cast(healTarget);
        }

        /// <summary>
        /// Cast Pledge - grants invulnerability stacks to party members
        /// Renders target invulnerable to autoattacks
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Pledge()
        {
            if (!OccultCrescentSettings.Instance.UsePledge)
                return false;

            if (!OCSpells.Pledge.CanCast())
                return false;

            GameObject pledgeTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.PledgeCastOnAllies)
            {
                // Prioritize party members who need protection (low HP)
                pledgeTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.PledgeHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent)
                    .FirstOrDefault();

                // If no low HP targets, cast on self if we need it
                if (pledgeTarget == null && Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.PledgeHealthPercent)
                    pledgeTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self
                if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.PledgeHealthPercent)
                    pledgeTarget = Core.Me;
            }

            if (pledgeTarget == null)
                return false;

            return await OCSpells.Pledge.Cast(pledgeTarget);
        }

        /// <summary>
        /// Cast Phantom Kick - leap attack that grants stacking damage buff
        /// 100 potency AoE, grants up to 3 stacks for increased damage
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> PhantomKick()
        {
            if (!OccultCrescentSettings.Instance.UsePhantomKick)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.PhantomKick.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight() || (Core.Me.CurrentTarget as BattleCharacter)?.IsCasting == true)
                return false;

            // Check melee range restriction if enabled
            if (OccultCrescentSettings.Instance.PhantomKickMeleeRangeOnly && !Core.Me.CurrentTarget.WithinSpellRange(3.0f))
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.PhantomKick.Range))
                return false;

            return await OCSpells.PhantomKick.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast OccultCounter - attack that can only be used after a parry
        /// If it can cast, we should use it immediately
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultCounter()
        {
            if (!OccultCrescentSettings.Instance.UseOccultCounter)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.OccultCounter.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.OccultCounter.Range))
                return false;

            return await OCSpells.OccultCounter.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Counterstance - increases parry rate by 100% for 60s
        /// When cast near Knowledge Crystal, provides movement speed buff
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Counterstance()
        {
            if (!OccultCrescentSettings.Instance.UseCounterstance)
                return false;

            if (!OCSpells.Counterstance.CanCast())
                return false;

            if (!Core.Me.InCombat)
            {
                // Out of combat: only cast near Knowledge Crystal if enabled
                if (!OccultCrescentSettings.Instance.CounterstanceKnowledgeCrystal)
                    return false;

                // TODO: Need parry aura ID to check if we already have the buff
                // For now, assume we should cast if near crystal

                var targetNpc = GameObjectManager.GetObjectByNPCId(KnowledgeCrystal);
                if (targetNpc != null && targetNpc.IsValid && targetNpc.Distance(Core.Me) <= 5)
                {
                    return await OCSpells.Counterstance.Cast(Core.Me);
                }
            }
            else
            {
                // In combat: cast for parry rate buff
                // TODO: Need parry aura ID to check if we already have the buff
                // return await OCSpells.Counterstance.Cast(Core.Me);
            }

            return false;
        }

        /// <summary>
        /// Cast OccultChakra - healing ability
        /// Restores 30% HP normally, or 70% HP if current HP < 30%
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultChakra()
        {
            if (!OccultCrescentSettings.Instance.UseOccultChakra)
                return false;

            if (!OCSpells.OccultChakra.CanCast())
                return false;

            // Cast when health is below configured percentage
            if (Core.Me.CurrentHealthPercent > OccultCrescentSettings.Instance.OccultChakraHealthPercent)
                return false;

            return await OCSpells.OccultChakra.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Mighty March - regen to self and all party members
        /// High cooldown (120s) utility spell
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> MightyMarch()
        {
            if (!OccultCrescentSettings.Instance.UseMightyMarch)
                return false;

            if (!OCSpells.MightyMarch.CanCast())
                return false;

            GameObject marchTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.MightyMarchCastOnAllies)
            {
                // Find party member who needs regen
                marchTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.MightyMarchHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent)
                    .FirstOrDefault();

                // If no allies need regen, check self
                if (marchTarget == null && Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.MightyMarchHealthPercent)
                    marchTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self
                if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.MightyMarchHealthPercent)
                    marchTarget = Core.Me;
            }

            if (marchTarget == null)
                return false;

            return await OCSpells.MightyMarch.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Hero's Rime - increases damage and healing potency of all party by 10%
        /// High priority due to 120s cooldown and party-wide benefit
        /// Can't stack with Offensive Aria
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> HerosRime()
        {
            if (!OccultCrescentSettings.Instance.UseHerosRime)
                return false;

            // Must be in combat to use this ability
            if (!Core.Me.InCombat)
                return false;

            if (Core.Me.HasAura(OCAuras.HerosRime))
                return false;

            if (!OCSpells.HerosRime.CanCast())
                return false;

            return await OCSpells.HerosRime.Cast(Core.Me);
        }

        /// <summary>
        /// Cast Deadly Blow - high damage attack based on missing HP, 30s cooldown
        /// Cast on cooldown when we have a valid target
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> DeadlyBlow()
        {
            if (!OccultCrescentSettings.Instance.UseDeadlyBlow)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.DeadlyBlow.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.DeadlyBlow.Range))
                return false;

            return await OCSpells.DeadlyBlow.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Rage - auto attack current target with high damage
        /// Only use in melee range if enabled, and only if enemy is not casting
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Rage()
        {
            if (!OccultCrescentSettings.Instance.UseRage)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.Rage.CanCast())
                return false;

            // Need a valid attackable target that's not casting
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight() || (Core.Me.CurrentTarget as BattleCharacter)?.IsCasting == true)
                return false;

            // Check melee range restriction if enabled
            if (OccultCrescentSettings.Instance.RageMeleeRangeOnly && !Core.Me.CurrentTarget.WithinSpellRange(3.0f))
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.Rage.Range))
                return false;

            return await OCSpells.Rage.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Phantom Fire - standard ranged attack
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> PhantomFire()
        {
            if (!OccultCrescentSettings.Instance.UsePhantomFire)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.PhantomFire.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.PhantomFire.Range))
                return false;

            return await OCSpells.PhantomFire.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Holy Cannon - ranged attack, more damage vs undead, shares cooldown with Silver Cannon
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> HolyCannon()
        {
            if (!OccultCrescentSettings.Instance.UseHolyCannon)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.HolyCannon.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.HolyCannon.Range))
                return false;

            return await OCSpells.HolyCannon.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Dark Cannon - ranged attack that inflicts blind
        /// Cannot be used at same time as Shock Cannon
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> DarkCannon()
        {
            if (!OccultCrescentSettings.Instance.UseDarkCannon)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.DarkCannon.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.DarkCannon.Range))
                return false;

            return await OCSpells.DarkCannon.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Shock Cannon - ranged attack that inflicts paralysis
        /// Cannot be used at same time as Dark Cannon
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> ShockCannon()
        {
            if (!OccultCrescentSettings.Instance.UseShockCannon)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.ShockCannon.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.ShockCannon.Range))
                return false;

            return await OCSpells.ShockCannon.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast Silver Cannon - ranged attack that reduces damage target deals and takes
        /// Shares cooldown with Holy Cannon
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> SilverCannon()
        {
            if (!OccultCrescentSettings.Instance.UseSilverCannon)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Core.Me.HasTarget)
                return false;

            if (!OCSpells.SilverCannon.CanCast())
                return false;

            // Need a valid attackable target
            if (!Core.Me.CurrentTarget.ValidAttackUnit() || !Core.Me.CurrentTarget.InLineOfSight())
                return false;

            // Check if target is within spell range
            if (!Core.Me.CurrentTarget.WithinSpellRange(OCSpells.SilverCannon.Range))
                return false;

            return await OCSpells.SilverCannon.Cast(Core.Me.CurrentTarget);
        }

        /// <summary>
        /// Cast OccultPotion - completely restores HP of self or target
        /// Costs 100k gil per cast - very restrictive usage
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultPotion()
        {
            if (!OccultCrescentSettings.Instance.UseOccultPotion)
                return false;

            if (!OCSpells.OccultPotion.CanCast())
                return false;

            GameObject potionTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.OccultPotionCastOnAllies)
            {
                // Find party member who desperately needs healing
                potionTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultPotionHealthPercent)
                    .OrderBy(ally => ally.CurrentHealthPercent)
                    .FirstOrDefault();

                // If no allies need healing, check self
                if (potionTarget == null && Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultPotionHealthPercent)
                    potionTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self
                if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultPotionHealthPercent)
                    potionTarget = Core.Me;
            }

            if (potionTarget == null)
                return false;

            return await OCSpells.OccultPotion.Cast(potionTarget);
        }

        /// <summary>
        /// Cast OccultEther - completely restores MP of self or target
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultEther()
        {
            if (!OccultCrescentSettings.Instance.UseOccultEther)
                return false;

            if (!OCSpells.OccultEther.CanCast())
                return false;

            GameObject etherTarget = null;

            // Check if we should cast on allies
            if (OccultCrescentSettings.Instance.OccultEtherCastOnAllies)
            {
                // Find party member who needs MP
                etherTarget = Group.CastableAlliesWithin30.Where(ally =>
                    ally.IsValid &&
                    ally.IsAlive &&
                    ally.CurrentManaPercent <= OccultCrescentSettings.Instance.OccultEtherManaPercent)
                    .OrderBy(ally => ally.CurrentManaPercent)
                    .FirstOrDefault();

                // If no allies need MP, check self
                if (etherTarget == null && Core.Me.CurrentManaPercent <= OccultCrescentSettings.Instance.OccultEtherManaPercent)
                    etherTarget = Core.Me;
            }
            else
            {
                // Self-only mode: only check self
                if (Core.Me.CurrentManaPercent <= OccultCrescentSettings.Instance.OccultEtherManaPercent)
                    etherTarget = Core.Me;
            }

            if (etherTarget == null)
                return false;

            return await OCSpells.OccultEther.Cast(etherTarget);
        }

        /// <summary>
        /// Cast Revive - resurrects a dead party member
        /// Uses the standard healer resurrection pattern
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> Revive()
        {
            if (!OccultCrescentSettings.Instance.UseRevive)
                return false;

            // This might not work, if it doesn't just refactor to use something really simple. 
            return await Roles.Healer.Raise(
                OCSpells.Revive,
                false, // No Swiftcast for Chemist
                true,  // Always slowcast for Chemist
                OccultCrescentSettings.Instance.ReviveOutOfCombat,
                OccultCrescentSettings.Instance.ReviveDelay
            );
        }

        /// <summary>
        /// Cast OccultElixir - completely restores HP and MP of all party members
        /// Costs 300k gil per cast - extremely restrictive usage
        /// </summary>
        /// <returns>True if spell was cast, false otherwise</returns>
        private static async Task<bool> OccultElixir()
        {
            if (!OccultCrescentSettings.Instance.UseOccultElixir)
                return false;

            if (!OCSpells.OccultElixir.CanCast())
                return false;

            // Check if multiple party members (including self) need healing/MP
            var partyMembersNeedingHelp = Group.CastableAlliesWithin30.Where(ally =>
                ally.IsValid &&
                ally.IsAlive &&
                (ally.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultElixirPartyHealthPercent ||
                 ally.CurrentManaPercent <= OccultCrescentSettings.Instance.OccultElixirPartyHealthPercent))
                .Count();

            // Include self in the count
            if (Core.Me.CurrentHealthPercent <= OccultCrescentSettings.Instance.OccultElixirPartyHealthPercent ||
                Core.Me.CurrentManaPercent <= OccultCrescentSettings.Instance.OccultElixirPartyHealthPercent)
                partyMembersNeedingHelp++;

            // Only cast if multiple people need help (justify the 300k cost)
            if (partyMembersNeedingHelp < 2)
                return false;

            return await OCSpells.OccultElixir.Cast(Core.Me);
        }
    }
}