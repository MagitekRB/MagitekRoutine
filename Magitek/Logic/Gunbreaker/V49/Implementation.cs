using ff14bot;
using Magitek.Extensions;
using Magitek.Logic.Gunbreaker;
using Magitek.Logic.Roles;
using Magitek.Models.Gunbreaker;
using Magitek.Utilities;
using System.Threading.Tasks;
using GunbreakerRoutine = Magitek.Utilities.Routines.Gunbreaker;
using Healing = Magitek.Logic.Gunbreaker.V49.Heal;
using Pvp = Magitek.Logic.Gunbreaker.Pvp;

namespace Magitek.Logic.Gunbreaker.V49
{
    /// <summary>
    /// V49 Gunbreaker rotation implementation (restored from tag v1.0.49 - the working version before the problematic commit).
    /// </summary>
    public class Implementation : IGunbreakerImplementation
    {
        public static Implementation Instance { get; } = new Implementation();

        public Task<bool> Rest()
        {
            return Task.FromResult(false);
        }

        public async Task<bool> PreCombatBuff()
        {
            return await Buff.RoyalGuard();
        }

        public async Task<bool> Pull()
        {
            return await Combat();
        }

        public async Task<bool> Heal()
        {
            return await Healing.Aurora();
        }

        public Task<bool> CombatBuff()
        {
            return Task.FromResult(false);
        }

        public async Task<bool> Combat()
        {
            if (!Core.Me.HasTarget || !Core.Me.CurrentTarget.ThoroughCanAttack())
                return false;

            //LimitBreak
            if (Defensive.ForceLimitBreak()) return true;

            if (await CommonFightLogic.FightLogic_TankDefensive(GunbreakerSettings.Instance.FightLogicDefensives, GunbreakerRoutine.DefensiveSpells, GunbreakerRoutine.Defensives)) return true;
            if (await CommonFightLogic.FightLogic_PartyShield(GunbreakerSettings.Instance.FightLogicPartyShield, Spells.HeartofLight, true, aura: Auras.HeartofLight)) return true;
            if (await CommonFightLogic.FightLogic_Debuff(GunbreakerSettings.Instance.FightLogicReprisal, Spells.Reprisal, true, aura: Auras.Reprisal)) return true;

            //Utility
            if (await Buff.RoyalGuard()) return true;
            if (await Tank.Interrupt(GunbreakerSettings.Instance)) return true;

            if (GunbreakerRoutine.GlobalCooldown.CanWeave())
            {
                //Potion
                if (await Buff.UsePotion()) return true;

                //Defensive Buff
                if (await Defensive.Superbolide()) return true;
                if (await Healing.Aurora()) return true;
                if (await Defensive.Nebula()) return true;
                if (await Defensive.Rampart()) return true;
                if (await Defensive.Camouflage()) return true;
                if (await Defensive.Reprisal()) return true;
                if (await Defensive.HeartofLight()) return true;
                if (await Defensive.HeartofCorundum()) return true;
                if (await Tank.ArmsLength(GunbreakerSettings.Instance)) return true;
            }

            if (GunbreakerRoutine.GlobalCooldown.CanWeave())
            {
                //oGCD - Buffs
                if (await Buff.NoMercy()) return true;
                if (await Buff.Bloodfest()) return true;
                //oGCD - Damage
                if (await SingleTarget.BlastingZone()) return true;
                //OGCD dots
                if (await Aoe.BowShock()) return true;

            }

            //oGCD to use with BurstStrike
            if (await Aoe.FatedBrand()) return true;
            if (await SingleTarget.Hypervelocity()) return true;

            //GCD to use when No Mercy
            if (await SingleTarget.SonicBreak()) return true;

            //oGCD to use inside Combo 2
            if (await SingleTarget.EyeGouge()) return true;
            if (await SingleTarget.AbdomenTear()) return true;
            if (await SingleTarget.JugularRip()) return true;

            //Pull or get back aggro with LightningShot
            if (await SingleTarget.LightningShotToPullOrAggro()) return true;
            if (await SingleTarget.LightningShotToDps()) return true;

            //Burst
            if (await Aoe.DoubleDown()) return true;

            //Combo 2
            if (await SingleTarget.SavageClaw()) return true;
            if (await SingleTarget.WickedTalon()) return true;
            if (await SingleTarget.GnashingFang()) return true;

            //LionHeart Combo
            if (await SingleTarget.LionHeart()) return true;
            if (await SingleTarget.NobleBlood()) return true;
            if (await SingleTarget.ReignOfBeasts()) return true;

            //Combo 3
            if (await SingleTarget.BurstStrike()) return true;

            //AOE
            if (await Aoe.FatedCircle()) return true;
            if (await Aoe.DemonSlaughter()) return true;
            if (await Aoe.DemonSlice()) return true;

            //Combo 1 Filler
            if (await SingleTarget.SolidBarrel()) return true;
            if (await SingleTarget.BrutalShell()) return true;

            return await SingleTarget.KeenEdge();
        }

        public async Task<bool> PvP()
        {
            // Use shared Pvp logic from latest implementation
            if (await CommonPvp.CommonTasks(GunbreakerSettings.Instance)) return true;

            // BURST CHECK: Wrap everything except basic combo
            if (CommonPvp.ShouldUseBurst())
            {
                if (await Pvp.HeartOfCorundumPvp()) return true;
                if (await Pvp.ContinuationPvp()) return true;
                if (await Pvp.BlastingZonePvp()) return true;
                if (await Pvp.FatedCirclePvp()) return true;
                if (await Pvp.RelentlessRushPvp()) return true;

                if (!CommonPvp.GuardCheck(GunbreakerSettings.Instance))
                {
                    if (await Pvp.RoughDividePvp()) return true;
                    if (await Pvp.WickedTalonPvp()) return true;
                    if (await Pvp.SavageClawPvp()) return true;
                    if (await Pvp.GnashingFangPvp()) return true;
                }

                if (await Pvp.BurstStrikePvp()) return true;
            }

            // Basic Combo (ungated)
            if (await Pvp.SolidBarrelPvp()) return true;
            if (await Pvp.BrutalShelPvp()) return true;
            return (await Pvp.KeenEdgePvp());
        }
    }
}
