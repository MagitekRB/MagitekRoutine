using Buddy.Coroutines;
using Clio.Common;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Enumerations;
using Magitek.Models.Account;
using Magitek.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Extensions
{
    public static class GameObjectExtensions
    {
        public static bool ThoroughCanAttack(this GameObject unit)
        {
            if (unit == null)
                return false;

            if (WorldManager.ZoneId == 732)
            {
                return unit.Type != GameObjectType.Pc;
            }

            // Every rotation guards on this and then casts straight at CurrentTarget, so gating the
            // damage rules here covers all 22 of them in one place. Combat.Enemies is deliberately left
            // alone: the enemy stays visible to Provoke, interrupts and the rest of the defensive paths,
            // it just stops being something we pour damage into.
            return unit.CanAttack && unit.CanBeDamagedByMe();
        }

        public static bool BeingTargeted(this GameObject unit)
        {
            if (unit == null)
                return false;

            return Combat.Enemies.Any(x => x.TargetCharacter == unit);
        }

        public static bool BeingTargetedBy(this GameObject unit, GameObject other)
        {
            if (unit == null || other == null)
                return false;

            var lp = other as Character;
            return lp != null && lp.TargetGameObject == unit;
        }

        public static bool WithinSpellRange(this GameObject unit, float range)
        {
            if (unit == null)
                return false;

            return (Core.Me.Distance2D(unit) - Core.Me.CombatReach - unit.CombatReach) <= range;
        }
        public static bool WithinSpellRange(this GameObject unit, double range)
        {
            if (unit == null)
                return false;

            return (Core.Me.Distance2D(unit) - Core.Me.CombatReach - unit.CombatReach) <= range;
        }

        public static async Task<bool> UseItem(this GameObject unit, uint itemId, bool lookForMedicated = false)
        {
            var item = InventoryManager.FilledSlots.FirstOrDefault(r => r.RawItemId == itemId);

            if (item == null)
                return false;

            if (!item.CanUse(unit))
                return false;

            while (item.CanUse(unit))
            {
                item.UseItem();
                await Coroutine.Yield();
            }

            // Potions give a Medicated aura
            if (lookForMedicated)
            {
                await Coroutine.Wait(3000, () => unit.HasAura(Auras.Medicated));
            }

            return true;
        }

        public static int CombatTimeLeft(this GameObject unit)
        {
            if (unit == null)
                return 0;

            if (unit.EnglishName.Contains("Dummy"))
                return 9999;

            var haveUnit = Tracking.EnemyInfos.Any(r => r.Unit == unit);

            return haveUnit ? Convert.ToInt32(Tracking.EnemyInfos.First(r => r.Unit == unit).CombatTimeLeft) : 0;
        }

        public static double TimeInCombat(this GameObject unit)
        {
            if (unit == null)
                return 0;

            var haveUnit = Tracking.EnemyInfos.Any(r => r.Unit == unit);

            return haveUnit ? Tracking.EnemyInfos.First(r => r.Unit == unit).TimeInCombat : 0;
        }

        public static bool HasAura(this GameObject unit, uint spell, bool isMyAura = false, int msLeft = 0)
        {
            var unitAsCharacter = unit as Character;

            if (unitAsCharacter == null || !unitAsCharacter.IsValid)
            {
                return false;
            }

            var auras = isMyAura
                ? unitAsCharacter.CharacterAuras.Where(r => r.CasterId == Core.Player.ObjectId && r.Id == spell)
                : unitAsCharacter.CharacterAuras.Where(r => r.Id == spell);

            return auras.Any(aura => aura.TimespanLeft.TotalMilliseconds >= msLeft);
        }

        public static bool HasAuraExpiringWithin(this GameObject unit, uint spell, bool isMyAura = false, int msRemaining = 0)
        {
            var unitAsCharacter = unit as Character;

            if (unitAsCharacter == null || !unitAsCharacter.IsValid)
            {
                return false;
            }

            var auras = isMyAura
                ? unitAsCharacter.CharacterAuras.Where(r => r.CasterId == Core.Player.ObjectId && r.Id == spell)
                : unitAsCharacter.CharacterAuras.Where(r => r.Id == spell);

            return auras.Any(aura => aura.TimespanLeft.TotalMilliseconds <= msRemaining && aura.TimespanLeft.TotalMilliseconds >= 0);
        }

        public static bool HasAuraCharge(this GameObject unit, uint spell, bool isMyAura = false)
        {
            var unitAsCharacter = unit as Character;

            if (unitAsCharacter == null || !unitAsCharacter.IsValid)
            {
                return false;
            }

            var auras = isMyAura
                ? unitAsCharacter.CharacterAuras.Where(r => r.CasterId == Core.Player.ObjectId && r.Id == spell)
                : unitAsCharacter.CharacterAuras.Where(r => r.Id == spell);

            return auras.Any(aura => aura.Value == 1);
        }

        public static bool HasAnyAura(this GameObject unit, uint[] auras, bool isMyAura = false, int msLeft = 0)
        {
            var unitAsCharacter = unit as Character;

            if (unitAsCharacter == null || !unitAsCharacter.IsValid)
                return false;

            return isMyAura
                ? unitAsCharacter.CharacterAuras.Any(r => r.CasterId == Core.Player.ObjectId && auras.Contains(r.Id) && r.TimespanLeft.TotalMilliseconds >= msLeft)
                : unitAsCharacter.CharacterAuras.Any(r => auras.Contains(r.Id) && r.TimespanLeft.TotalMilliseconds >= msLeft);

        }

        /// <summary>
        /// Whether the unit already carries a magical barrier that another one would waste.
        /// <para>
        /// Adloquium and Succor state that the effect "cannot be stacked with certain sage barrier
        /// effects", so Galvanize and the Eukrasian barriers are mutually exclusive — re-shielding someone
        /// who has either throws the cast away. Catalyze is deliberately excluded: it sits alongside
        /// Galvanize rather than replacing it, so its presence says nothing about whether a fresh barrier
        /// would land.
        /// </para>
        /// </summary>
        /// <param name="msLeft">Only count a barrier with at least this long remaining, so one about to
        /// lapse doesn't block a replacement.</param>
        // A dispel strips a BENEFICIAL status from an enemy, so a helper used to decide whether to
        // dispel has to ignore debuffs. Named for what it actually asks: the old name read as the
        // opposite of what it did, and let a dispel re-fire forever on an enemy that merely carries a
        // dispellable debuff no dispel can remove. Cleansing an ally is a separate concern, served by
        // HasAnyDispellableAura on CharacterExtensions.
        public static bool HasDispellableBuff(this GameObject unit)
        {
            var unitAsCharacter = unit as Character;

            if (unitAsCharacter == null || !unitAsCharacter.IsValid)
                return false;

            return unitAsCharacter.CharacterAuras.Any(r => r.TimespanLeft.TotalMilliseconds >= 0 && r.IsDispellable && !r.IsDebuff);
        }

        public static bool HasAnyAura(this GameObject unit, List<uint> auras, bool isMyAura = false, int msLeft = 0)
        {
            var unitAsCharacter = unit as Character;

            if (unitAsCharacter == null || !unitAsCharacter.IsValid)
            {
                return false;
            }

            return isMyAura
                ? unitAsCharacter.CharacterAuras.Any(r => r.CasterId == Core.Player.ObjectId && auras.Contains(r.Id) && r.TimespanLeft.TotalMilliseconds >= msLeft)
                : unitAsCharacter.CharacterAuras.Any(r => auras.Contains(r.Id) && r.TimespanLeft.TotalMilliseconds >= msLeft);
        }

        public static int CountAuras(this GameObject unit, List<uint> auras, bool isMyAura = false, int msLeft = 0)
        {
            var unitAsCharacter = unit as Character;

            if (unitAsCharacter == null || !unitAsCharacter.IsValid)
            {
                return 0;
            }

            return isMyAura
                ? unitAsCharacter.CharacterAuras.Count(r => r.CasterId == Core.Player.ObjectId && auras.Contains(r.Id) && r.TimespanLeft.TotalMilliseconds >= msLeft)
                : unitAsCharacter.CharacterAuras.Count(r => auras.Contains(r.Id) && r.TimespanLeft.TotalMilliseconds >= msLeft);
        }

        public static bool HasAllAuras(this GameObject unit, List<uint> auras, bool areMyAuras = false, int msLeft = 0)
        {
            var unitAsCharacter = unit as Character;

            if (unitAsCharacter == null || !unitAsCharacter.IsValid)
            {
                return false;
            }

            return areMyAuras
                ? unitAsCharacter.CharacterAuras.Where(x => x.CasterId == Core.Player.ObjectId && (x.TimespanLeft.TotalMilliseconds >= msLeft || x.TimespanLeft.TotalMilliseconds < 0)).Select(r => r.Id).ToList().Intersect(auras).Count() == auras.Count
                : unitAsCharacter.CharacterAuras.Where(x => (x.TimespanLeft.TotalMilliseconds >= msLeft || x.TimespanLeft.TotalMilliseconds < 0)).Select(r => r.Id).ToList().Intersect(auras).Count() == auras.Count;
        }

        public static bool ValidAttackUnit(this GameObject unit)
        {
            return unit != null && unit.IsValid && unit.IsTargetable && unit.CanAttack && unit.CurrentHealth > 0;
        }

        public static bool NotInvulnerable(this GameObject unit)
        {
            return unit != null && !unit.HasAnyAura(Auras.Invincibility);
        }

        // A unit we can actually hurt. Kept separate from ValidAttackUnit (which stays a pure
        // "is it a live hostile", used by defensive Occult Crescent paths) and from NotInvulnerable
        // (which stays aura-only, because Tracking.Update builds Combat.Enemies from it — widening it
        // would silently refilter that collection under all ~200 of its readers).
        //
        // Use this at offensive call sites that cast at CurrentTarget without passing through
        // ThoroughCanAttack, which in practice means the Occult Crescent phantom-job actions.
        public static bool ValidDamageTarget(this GameObject unit)
        {
            return unit.ValidAttackUnit() && unit.CanBeDamagedByMe();
        }

        // Whether our damage can reach this unit at all right now. The rules live in
        // Utilities/ImmunityLogic.cs with their encounter data in Utilities/ImmunityEncounters.cs —
        // fight-specific knowledge does not belong in a generic extension file.
        public static bool CanBeDamagedByMe(this GameObject unit)
        {
            return ImmunityLogic.CanBeDamagedBy(unit, Core.Me);
        }


        public static IEnumerable<BattleCharacter> EnemiesNearby(this GameObject unit, float distance)
        {
            if (unit == null || Core.Me == null)
                return Enumerable.Empty<BattleCharacter>();

            var meCombatReach = Core.Me.CombatReach;
            var unitCombatReach = unit.CombatReach;

            return Combat.Enemies.Where(r => r != null && r.Distance(unit) <= distance + meCombatReach + unitCombatReach);
        }

        public static IEnumerable<BattleCharacter> EnemiesNearbyOoc(this GameObject unit, float distance)
        {
            if (unit == null)
                return Enumerable.Empty<BattleCharacter>();

            return GameObjectManager.GetObjectsOfType<BattleCharacter>().Where(r => r != null && r.IsTargetable && r.CurrentHealth > 0 && r.CanAttack && r.Distance(unit) <= distance);
        }

        public static IEnumerable<BattleCharacter> EnemiesNearbyWithMyAura(this GameObject unit, float distance, uint aura)
        {
            if (unit == null)
                return Enumerable.Empty<BattleCharacter>();

            return Combat.Enemies.Where(r => r != null && r.Distance(unit) <= distance && r.HasAura(aura, true));
        }

        public static bool IsMelee(this GameObject unit)
        {
            return unit.IsTank() || unit.IsMeleeDps();
        }

        public static bool IsRanged(this GameObject unit)
        {
            return unit.IsHealer() || unit.IsRangedDps();
        }

        public static bool IsTank(this GameObject unit)
        {
            var gameObject = unit as Character;
            return gameObject != null && Tanks.Contains(gameObject.CurrentJob);
        }

        // "Main tank" is only answerable from a positive signal: this tank is holding what we are pointed
        // at, or whatever it is fighting is pointed back at it. When no tank we can reach gives that
        // signal we cannot tell them apart, and then every tank counts rather than none of them — a
        // party-wide mitigation that never fires is worse than one that fires for the wrong tank.
        //
        // The old tie-break was "the party contains exactly one tank", which answers false for BOTH tanks
        // of a two-tank party. An Occult Crescent critical encounter proved the cost: the boss is held by
        // someone outside the party, so neither tank ever produced a signal, and Kerachole, Panhaima and
        // Aquaveil stayed locked off for the whole fight while the ungated barriers kept firing.
        //
        // The roster is the castable tanks in heal range, not the raw party list, for the reason spelled
        // out at FightLogic.SharedTankBuster: a tank we cannot reach must not get a vote on who the
        // reachable tank is. It also keeps both sides of the comparison in one population, since the
        // castable lists follow the alliance while we heal it and the raw party list does not.
        public static bool IsMainTank(this GameObject unit)
        {
            var gameObject = unit as Character;

            if (gameObject == null || !Tanks.Contains(gameObject.CurrentJob))
                return false;

            if (gameObject.BeingTargetedBy(Core.Me.CurrentTarget)
                || gameObject.BeingTargetedBy(gameObject.TargetGameObject))
                return true;

            return !Group.CastableTanks.Any(r => r.ObjectId != gameObject.ObjectId
                                              && r.WithinSpellRange(30)
                                              && (r.BeingTargetedBy(Core.Me.CurrentTarget)
                                                  || r.BeingTargetedBy(r.TargetGameObject)));
        }

        public static bool IsTank(this GameObject unit, bool mainTank)
        {
            return mainTank ? unit.IsMainTank() : unit.IsTank();
        }

        public static bool IsHealer(this GameObject unit)
        {
            var gameObject = unit as Character;
            return gameObject != null && Healers.Contains(gameObject.CurrentJob);
        }

        public static bool IsDps(this GameObject unit)
        {
            var gameObject = unit as Character;
            return gameObject != null && Dps.Contains(gameObject.CurrentJob);
        }

        public static bool IsRangedPhysicalDps(this GameObject unit)
        {
            var gameObject = unit as Character;
            return gameObject != null && RangedPhysicalDps.Contains(gameObject.CurrentJob);
        }

        public static bool IsBlueMage(this GameObject unit)
        {
            var gameObject = unit as Character;
            return gameObject != null && ClassJobType.BlueMage.Equals(gameObject.CurrentJob);
        }

        public static bool IsBlueMageHealer(this GameObject unit)
        {
            var gameObject = unit as Character;
            return gameObject != null && ClassJobType.BlueMage.Equals(gameObject.CurrentJob) && gameObject.HasAura(Auras.AetherialMimicryHealer);
        }

        public static bool IsBlueMageTank(this GameObject unit)
        {
            var gameObject = unit as Character;
            return gameObject != null && ClassJobType.BlueMage.Equals(gameObject.CurrentJob) && gameObject.HasAura(Auras.AetherialMimicryTank);
        }

        public static bool IsBlueMageDps(this GameObject unit)
        {
            var gameObject = unit as Character;
            return gameObject != null && ClassJobType.BlueMage.Equals(gameObject.CurrentJob) && gameObject.HasAura(Auras.AetherialMimicryDps);
        }

        public static bool IsRangedDpsCard(this GameObject unit)
        {
            var gameObject = unit as Character;
            return gameObject != null && RangedDps.Contains(gameObject.CurrentJob);
        }

        public static bool IsMeleeDps(this GameObject unit)
        {
            var gameObject = unit as Character;
            return gameObject != null && MeleeDps.Contains(gameObject.CurrentJob);
        }

        public static bool IsRangedDps(this GameObject unit)
        {
            var gameObject = unit as Character;
            return gameObject != null && RangedDps.Contains(gameObject.CurrentJob);
        }

        public static bool HasMyRegen(this GameObject unit)
        {
            return unit.HasAnyAura(new uint[]
            {
                Auras.Regen,
                Auras.Regen2,
                Auras.AspectedBenefic,
                Auras.AspectedHelios,
                Auras.HeliosConjunction,
            });
        }

        public static TankImmunityCheck CheckTankImmunity(this Character unit)
        {

            switch (unit.CurrentJob)
            {
                case ClassJobType.Warrior:
                    return AuraCheck(Auras.Holmgang);
                case ClassJobType.Paladin:
                    return AuraCheck(Auras.HallowedGround);
                case ClassJobType.DarkKnight:
                    return AuraCheck(Auras.LivingDead);
                case ClassJobType.Gunbreaker:
                    return AuraCheck(Auras.Superbolide);
                default:
                    return TankImmunityCheck.HealThem;
            }

            TankImmunityCheck AuraCheck(uint aura)
            {
                if (!unit.HasAura(aura)) return TankImmunityCheck.HealThem;
                var result = unit.CharacterAuras.Any(
                    x => x.Id == aura && x.TimespanLeft.TotalMilliseconds <= 2000)
                    ? TankImmunityCheck.HealThem
                    : TankImmunityCheck.DontHealThem;

                return result;
            }
        }

        public enum TankImmunityCheck
        {
            DontHealThem,
            HealThem
        }

        public static bool HealthCheck(this GameObject tar, int healthSetting, float healthSettingPercent)
        {
            if (tar == null)
                return false;

            if (tar.IsBoss())
                return true;

            if (tar.EnglishName.Contains("Dummy"))
                return true;

            if (tar.CurrentHealth < healthSetting || tar.CurrentHealthPercent < healthSettingPercent)
                return false;

            // If our target has more health than our setting and more health percent than our health percent setting, return true
            if (tar.CurrentHealth > healthSetting && tar.CurrentHealthPercent > healthSettingPercent)
                return true;

            // If our target has more hp percent than our hp percent setting but has less health than our health setting, return true
            if (tar.CurrentHealthPercent > healthSettingPercent && tar.CurrentHealth < healthSetting)
                return true;

            // if our target has more health than our setting but less health percent than our hp percent setting, return true
            if (tar.CurrentHealth > healthSetting && tar.CurrentHealthPercent < healthSettingPercent)
                return true;

            // if our target has less health than our setting and less health than our percent setting, return false

            return tar.CurrentHealth >= healthSetting || !(tar.CurrentHealthPercent < healthSettingPercent);
        }

        public static AstrologianSect Sect(this GameObject unit)
        {
            if (unit.HasAura(Auras.DiurnalSect)) return AstrologianSect.Diurnal;
            if (unit.HasAura(Auras.NocturnalSect)) return AstrologianSect.Nocturnal;
            return AstrologianSect.None;
        }

        public static bool IsBoss(this GameObject unit)
        {
            return unit != null && (
                XivDataHelper.BossDictionary.ContainsKey(unit.NpcId)
                || XivDataHelper.BossNames.Contains(unit.EnglishName)
                || unit.EnglishName.Contains("Dummy")
            );
        }

        public static bool IsWarMachina(this GameObject unit)
        {
            return unit != null && (unit.EnglishName.Contains("Raven")
                                || unit.EnglishName.Contains("Falcon")
                                || unit.EnglishName.Contains("Icebound Tomelith")
                                || unit.EnglishName.Contains("Interceptor"));
        }

        public static float GetResurrectionWeight(this GameObject c)
        {
            if (c.IsHealer() || c.IsBlueMageHealer())
                return 100;

            if (c.IsTank() || c.IsBlueMageTank())
                return 90;

            if (c.IsDps() || c.IsBlueMageDps())
            {
                var cha = c as Character;
                // Intentionally use LevelAcquired for other characters; IsKnown only reflects Core.Me unlocks.
                if (cha.CurrentJob == ClassJobType.RedMage && cha.ClassLevel >= Spells.Verraise.LevelAcquired)
                    return 80;
                if (cha.CurrentJob == ClassJobType.Summoner && cha.ClassLevel >= Spells.Resurrection.LevelAcquired)
                    return 70;
                return 60;
            }

            return 0;
        }

        public static float GetHealingWeight(this GameObject c)
        {
            if (!BaseSettings.Instance.UseWeightedHealingPriority)
                return 1;

            var cha = c as Character;

            var roleWeight = cha.IsTank() ?
                BaseSettings.Instance.WeightedTankRole :
                cha.IsHealer() ?
                BaseSettings.Instance.WeightedHealerRole :
                cha.CurrentJob == ClassJobType.RedMage || cha.CurrentJob == ClassJobType.Summoner ?
                BaseSettings.Instance.WeightedRezMageRole :
                BaseSettings.Instance.WeightedDpsRole;
            var selfWeight = c == Core.Me ? BaseSettings.Instance.WeightedSelf : 1.0f;
            var regens = CharacterExtensions.HealerRegens;
            var shields = CharacterExtensions.HealerShields;
            var ignores = CharacterExtensions.BuffIgnore;
            var auras = cha.CharacterAuras.Where(a => !ignores.Contains(a.Id));
            var debuffWeight = (float)Math.Pow(BaseSettings.Instance.WeightedDebuff, auras.Count(r => r.IsDebuff));
            var buffWeight = (float)Math.Pow(BaseSettings.Instance.WeightedBuff, auras.Count(r => !r.IsDebuff && !regens.Contains(r.Id) && !shields.Contains(r.Id)));
            var regenWeight = (float)Math.Pow(BaseSettings.Instance.WeightedRegen, auras.Count(r => regens.Contains(r.Id)));
            var shieldWeight = (float)Math.Pow(BaseSettings.Instance.WeightedShield, auras.Count(r => shields.Contains(r.Id)));
            var weaknessWeight = (float)Math.Pow(BaseSettings.Instance.WeightedWeakness, cha.HasAura(Auras.Weakness) ? 1f : 0f);
            var distanceMinWeight = BaseSettings.Instance.WeightedDistanceMin;
            var distanceMaxWeight = BaseSettings.Instance.WeightedDistanceMax;
            var distanceWeight = distanceMinWeight + (distanceMaxWeight - distanceMinWeight) * (Core.Me.Distance(c) / 30);
            /*
             * Logger.WriteInfo($"{c.Name} - \n" +
                $"hp {c.CurrentHealthPercent}\n" +
                $"self {selfWeight}\n" +
                $"role {roleWeight}\n" +
                $"debuff {debuffWeight}\n" +
                $"regen {regenWeight}\n" +
                $"shield {shieldWeight}\n" +
                $"weakness {weaknessWeight}\n" +
                $"distance {distanceWeight}\n");
            */

            var weight = c.CurrentHealthPercent
                * selfWeight
                * roleWeight
                * debuffWeight
                * buffWeight
                * regenWeight
                * shieldWeight
                * weaknessWeight
                * distanceWeight;

            return weight;
        }

        //This method return the heading from the player to the target object in radians.
        //A circle has 2*Pi radians, so an angle of 90 degrees would be Pi/2, and an angle
        //of 30 degrees would be Pi/6, etc.
        //It returns the absolute value, so targets to the left and right both return
        //positive values. A target directly in front would return 0.
        public static float RadiansFromPlayerHeading(this GameObject target)
        {
            var playerLocation = Core.Me.Location;
            // Snapshot the current target: this helper runs inside stun/interrupt candidate scans, which is
            // exactly when enemies are dying — the target can clear between the caller's null check and this
            // read. With no target to auto-face, the player's actual heading is the truth anyway.
            var facingTarget = Core.Me.CurrentTarget;
            var playerHeading = GameSettingsManager.FaceTargetOnAction && BaseSettings.Instance.UseAutoFaceChecks && facingTarget != null ?
                MathEx.NormalizeRadian(MathHelper.CalculateHeading(playerLocation, facingTarget.Location) + (float)Math.PI)
                :
                Core.Me.Heading;
            var targetLocation = target.Location;
            var d = Math.Abs(MathEx.NormalizeRadian(playerHeading - MathEx.NormalizeRadian(MathHelper.CalculateHeading(playerLocation, targetLocation) + (float)Math.PI)));

            if (d > Math.PI)
            {
                d = Math.Abs(d - 2 * (float)Math.PI);
            }

            return d;
        }

        public static bool InView(this GameObject target)
        {
            if (target == null)
                return false;

            if (target == Core.Me)
                return true;

            return target.RadiansFromPlayerHeading() < 0.78539f; //This is Pi/4 radians, or 45 degrees left or right
        }

        public static bool InActualView(this GameObject target)
        {
            if (target == null)
                return false;

            if (target == Core.Me)
                return true;

            var playerLocation = Core.Me.Location;
            var playerHeading = Core.Me.Heading; // Always use actual heading
            var targetLocation = target.Location;
            var d = Math.Abs(MathEx.NormalizeRadian(playerHeading - MathEx.NormalizeRadian(MathHelper.CalculateHeading(playerLocation, targetLocation) + (float)Math.PI)));

            if (d > Math.PI)
            {
                d = Math.Abs(d - 2 * (float)Math.PI);
            }

            return d < 0.78539f; //This is Pi/4 radians, or 45 degrees left or right
        }

        public static bool InCustomRadiantCone(this GameObject target, float angle)
        {
            if (target == null)
                return false;

            if (target == Core.Me)
                return true;

            return target.RadiansFromPlayerHeading() < angle;
        }

        public static bool InCustomDegreeCone(this GameObject target, int angle)
        {
            if (target == null)
                return false;

            if (target == Core.Me)
                return true;

            float radians = ((float)Math.PI / 180) * angle;

            return target.RadiansFromPlayerHeading() < radians;
        }


        private static readonly List<ClassJobType> Tanks = new List<ClassJobType>()
        {
            ClassJobType.Gladiator,
            ClassJobType.Paladin,
            ClassJobType.Marauder,
            ClassJobType.Warrior,
            ClassJobType.DarkKnight,
            ClassJobType.Gunbreaker,
            ClassJobType.BlueMage,
        };

        private static readonly List<ClassJobType> Healers = new List<ClassJobType>()
        {
            ClassJobType.Arcanist,
            ClassJobType.Scholar,
            ClassJobType.Conjurer,
            ClassJobType.WhiteMage,
            ClassJobType.Astrologian,
            ClassJobType.BlueMage,
            ClassJobType.Sage,
        };

        private static readonly List<ClassJobType> Dps = new List<ClassJobType>()
        {
            ClassJobType.Archer,
            ClassJobType.Bard,
            ClassJobType.Thaumaturge,
            ClassJobType.BlackMage,
            ClassJobType.Lancer,
            ClassJobType.Dragoon,
            ClassJobType.Pugilist,
            ClassJobType.Monk,
            ClassJobType.Rogue,
            ClassJobType.Ninja,
            ClassJobType.Machinist,
            ClassJobType.RedMage,
            ClassJobType.Samurai,
            ClassJobType.Summoner,
            ClassJobType.Dancer,
            ClassJobType.BlueMage,
            ClassJobType.Reaper,
            ClassJobType.Pictomancer,
            ClassJobType.Viper
        };

        private static readonly List<ClassJobType> RangedPhysicalDps = new List<ClassJobType>()
        {
            ClassJobType.Archer,
            ClassJobType.Bard,
            ClassJobType.Machinist,
            ClassJobType.Dancer,
            ClassJobType.BlueMage,
        };

        private static readonly List<ClassJobType> MeleeDps = new List<ClassJobType>()
        {
            ClassJobType.Lancer,
            ClassJobType.Dragoon,
            ClassJobType.Pugilist,
            ClassJobType.Monk,
            ClassJobType.Rogue,
            ClassJobType.Ninja,
            ClassJobType.Samurai,
            ClassJobType.BlueMage,
            ClassJobType.Reaper,
            ClassJobType.Viper
        };

        private static readonly List<ClassJobType> RangedDps = new List<ClassJobType>()
        {
            ClassJobType.Archer,
            ClassJobType.Bard,
            ClassJobType.Machinist,
            ClassJobType.Dancer,
            ClassJobType.Thaumaturge,
            ClassJobType.BlackMage,
            ClassJobType.Machinist,
            ClassJobType.RedMage,
            ClassJobType.Summoner,
            ClassJobType.BlueMage,
            ClassJobType.Pictomancer
        };
    }
}
