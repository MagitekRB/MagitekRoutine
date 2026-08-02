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

            // Rotations guard on this and then cast straight at CurrentTarget, so every immunity rule has
            // to apply here, not just the mark one. NotInvulnerable() chains them all, so a boss immune to
            // our damage type, dubbed against us, or unreachable without a buff stops the rotation instead
            // of letting it pour damage into something it cannot hurt.
            return unit.CanAttack && unit.NotInvulnerable();
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

        // Named for what it actually asks. A dispel strips BENEFICIAL statuses from an enemy, so a helper
        // used to decide whether to dispel must ignore debuffs — otherwise a dispel action re-fires forever
        // on an enemy that merely carries a dispellable debuff it can never remove, such as the Time Mage's
        // own Slow / Occult Mage Masher. Calling that "HasDispellableAura" read as the opposite of what it
        // did; healers' Esuna path is a separate concern and uses NeedsDispel/HasAnyDispellableAura.
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

        // A live hostile unit, without asking whether our damage can reach it. Kept separate from
        // ValidAttackUnit because the two answer different questions: this one is "does it threaten us",
        // which is what fight logic needs — an enemy we are immune-locked out of damaging still casts, and
        // its tank buster still lands on the party.
        public static bool ValidThreatUnit(this GameObject unit)
        {
            return unit != null && unit.IsValid && unit.IsTargetable && unit.CanAttack && unit.CurrentHealth > 0;
        }

        // A unit worth attacking is one we can actually hurt, so the immunity rules belong here rather than
        // being repeated by every caller. This is what closes the gap for paths that never reach
        // ThoroughCanAttack — most notably the Occult Crescent phantom-job actions, which run from
        // RotationManager before the job rotation's own check.
        //
        // Cheap enough to sit in this hot path: every predicate NotInvulnerable chains early-outs to true
        // outside its own encounter — a zone-id compare, two aura lookups, and one pass each over our own
        // and the target's (short) aura lists. Nothing here scans the object table.
        public static bool ValidAttackUnit(this GameObject unit)
        {
            return unit.ValidThreatUnit() && unit.NotInvulnerable();
        }


        public static bool NotInvulnerable(this GameObject unit)
        {
            return unit != null
                && !unit.HasAnyAura(Auras.Invincibility)
                && unit.DamageableByMyMark()
                && unit.DamageableByMyDuelRole()
                && unit.DamageableByMyDamageType()
                && unit.DamageableGivenMyBuffs();
        }

        // Enemies that can only be damaged while we hold a particular buff, where the target itself carries
        // no status to key off — so it has to be matched by zone and name instead.
        //
        // Deliberately a tiny static table rather than fight-logic catalogue data: NotInvulnerable() runs
        // for every enemy on every pulse, and scanning the catalogue per call would be far too expensive.
        // A zone-id compare costs essentially nothing everywhere else.
        private const ushort LabyrinthOfTheAncientsZoneId = 174;

        private static readonly HashSet<string> RequiresAstralRealignment = new HashSet<string>(StringComparer.Ordinal)
        {
            "Thanatos", // exact match keeps Eureka Orthos' "Orthos Thanatos" out of this
        };

        public static bool DamageableGivenMyBuffs(this GameObject unit)
        {
            if (unit == null)
                return false;

            if (WorldManager.ZoneId != LabyrinthOfTheAncientsZoneId)
                return true; // cheap early-out: this only applies in one fight

            var me = Core.Me;

            if (me == null)
                return true;

            if (RequiresAstralRealignment.Contains(unit.EnglishName) && !me.HasAura(Auras.AstralRealignment))
                return false;

            return true;
        }

        // Jeuno's Ark Angels duel: a target dubbed a Villain nullifies damage from anyone not carrying the
        // matching Hero status. This is the reverse of DamageableByMyMark — there our own mark selected the
        // one enemy we could hit, here the enemy's own status decides whether we count. A target with no
        // Villain status is damageable by anyone, so this stays inert outside that fight.
        public static bool DamageableByMyDuelRole(this GameObject unit)
        {
            var target = unit as Character;

            if (target == null)
                return unit != null;

            var me = Core.Me;

            if (me == null)
                return true;

            uint requiredHeroAura = 0;

            foreach (var aura in target.CharacterAuras)
                if (Auras.DuelVillainRequiredHeroAura.TryGetValue(aura.Id, out requiredHeroAura))
                    break;

            if (requiredHeroAura == 0)
                return true; // not duelling -> anyone can damage it (the common case)

            return me.HasAura(requiredHeroAura);
        }

        // Damage-type immunity: the target is only invulnerable to *some* jobs, so this is answered from
        // our own job rather than from the target alone. The Void Ark's Sawtooth and Irminsul each take one
        // of these at random, which is why nothing here is keyed to an enemy name.
        //
        // Note the asymmetry: Magic Resistance blocks healers and casters, but Ranged Resistance blocks
        // PHYSICAL ranged only — a caster's ranged attacks are magical and still land, so they must not be
        // filtered out by it. Blue Mage appears in the physical-ranged list for role purposes but deals
        // magic damage, so it is treated as a caster on both counts.
        public static bool DamageableByMyDamageType(this GameObject unit)
        {
            if (unit == null)
                return false;

            var me = Core.Me;

            if (me == null)
                return true;

            var job = me.CurrentJob;

            // Blue Mage is the exception on both counts, which is why it cannot simply be moved between the
            // two lists. It belongs in MagicDamageJobs so Ranged Resistance below does not filter it out —
            // its ranged attacks are magical — but it is also the one job carrying genuinely physical
            // attacks: Sharpened Knife is slashing and Triple Trident piercing, and both land through Magic
            // Resistance. Blanket-blocking it would shut down the whole rotation over the spells it cannot
            // use while discarding the ones it can.
            if (MagicDamageJobs.Contains(job) && job != ClassJobType.BlueMage && unit.HasAnyAura(Auras.MagicImmunity))
                return false;

            if (RangedPhysicalDps.Contains(job) && !MagicDamageJobs.Contains(job) && unit.HasAnyAura(Auras.RangedImmunity))
                return false;

            return true;
        }

        // The Meso Terminal Headsman pull tethers each player with a "Cell Block" mark; while marked, only
        // the Headsman carrying the matching "Guard on Duty" letter takes that player's damage — every other
        // target is immune ("Attacks against other targets are nullified"). Fast-outs to true whenever we
        // carry no Cell Block mark (i.e. everywhere but that one mechanic), so the shared NotInvulnerable
        // hot path stays a single cheap scan of our own aura list in the common case.
        public static bool DamageableByMyMark(this GameObject unit)
        {
            if (unit == null)
                return false;

            var me = Core.Me;
            if (me == null)
                return true;

            // If we carry a Cell Block mark, note the Guard on Duty status an enemy must have to be
            // damageable by us. One pass over our own (small) aura list.
            uint requiredEnemyAura = 0;
            foreach (var aura in me.CharacterAuras)
            {
                if (Auras.MarkMatchDamageableEnemyAura.TryGetValue(aura.Id, out requiredEnemyAura))
                    break;
            }

            // Not marked -> normal targeting (the overwhelmingly common case).
            if (requiredEnemyAura == 0)
                return true;

            // Marked -> only the enemy with the matching Guard on Duty letter takes our damage.
            return unit.HasAura(requiredEnemyAura);
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

        public static bool IsMainTank(this GameObject unit)
        {
            var gameObject = unit as Character;
            return gameObject != null
                && Tanks.Contains(gameObject.CurrentJob)
                && (gameObject.BeingTargetedBy(Core.Me.CurrentTarget)
                    || gameObject.BeingTargetedBy(gameObject.TargetGameObject)
                    || PartyManager.RawMembers.Where(r => r != null).Select(r => r.BattleCharacter).Count(r => r != null && Tanks.Contains(r.CurrentJob)) == 1);
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
            var playerHeading = GameSettingsManager.FaceTargetOnAction && BaseSettings.Instance.UseAutoFaceChecks ?
                MathEx.NormalizeRadian(MathHelper.CalculateHeading(playerLocation, Core.Me.CurrentTarget.Location) + (float)Math.PI)
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

        // Jobs whose damage is magical — healers plus the casters. Used for damage-type immunity checks;
        // Blue Mage is here (not with physical ranged) because its spells deal magic damage.
        private static readonly List<ClassJobType> MagicDamageJobs = new List<ClassJobType>()
        {
            ClassJobType.Arcanist,
            ClassJobType.Scholar,
            ClassJobType.Conjurer,
            ClassJobType.WhiteMage,
            ClassJobType.Astrologian,
            ClassJobType.Sage,
            ClassJobType.Thaumaturge,
            ClassJobType.BlackMage,
            ClassJobType.Summoner,
            ClassJobType.RedMage,
            ClassJobType.Pictomancer,
            ClassJobType.BlueMage,
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
