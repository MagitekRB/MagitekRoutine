using ff14bot;
using Magitek.Extensions;
using Magitek.Logic.Paladin;
using Magitek.Logic.Roles;
using Magitek.Models.Paladin;
using Magitek.Utilities;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using Healing = Magitek.Logic.Paladin.Heal;
using PaladinRoutine = Magitek.Utilities.Routines.Paladin;

namespace Magitek.Rotations
{
    public static class Paladin
    {
        public static Task<bool> Rest()
        {
            var needRest = Core.Me.CurrentHealthPercent < PaladinSettings.Instance.RestHealthPercent;
            return Task.FromResult(needRest);
        }

        public static async Task<bool> PreCombatBuff()
        {
            return await Buff.IronWill();
        }

        public static async Task<bool> Pull()
        {
            return await Combat();
        }

        public static async Task<bool> Heal()
        {
            return await Healing.Clemency();
        }

        public static Task<bool> CombatBuff()
        {
            return Task.FromResult(false);
        }

        public static async Task<bool> Combat()
        {
            // Ahead of everything, defensives included: Passage of Arms is a held channel that ANY other
            // action ends, which is what the check further down guards against. The routine never casts it —
            // the player does — so this is purely "stay out of the way". It reads only Core.Me, so it is
            // safe above the target guard. Same reasoning as Machinist's Flamethrower.
            if (Core.Me.HasAura(Auras.PassageOfArms))
                return false;

            // Above the attack check: a tank limit break is party mitigation, aimed at Core.Me rather
            // than the target (Tank.ForceLimitBreak), so an enemy we cannot damage is no reason to
            // withhold it. The DPS limit breaks stay below the guard — those are damage, and they
            // resolve at the target's location.
            //LimitBreak
            if (Defensive.ForceLimitBreak()) return true;

            if (await CommonFightLogic.FightLogic_TankDefensive(PaladinSettings.Instance.FightLogicDefensives, PaladinRoutine.DefensiveFastSpells, PaladinRoutine.Defensives, castTimeRemainingMs: 3000)) return true;
            if (await CommonFightLogic.FightLogic_TankDefensive(PaladinSettings.Instance.FightLogicDefensives, PaladinRoutine.DefensiveSpells, PaladinRoutine.Defensives)) return true;
            if (await CommonFightLogic.FightLogic_PartyShield(PaladinSettings.Instance.FightLogicPartyShield, Spells.DivineVeil, true, aura: Auras.DivineVeil)) return true;
            if (await CommonFightLogic.FightLogic_Debuff(PaladinSettings.Instance.FightLogicReprisal, Spells.Reprisal, true, aura: Auras.Reprisal, range: Spells.Reprisal.Radius)) return true;
            if (await CommonFightLogic.FightLogic_Knockback(PaladinSettings.Instance.FightLogicKnockback, Spells.ArmsLength, true, aura: Auras.ArmsLength)) return true;

            var canAttack = Core.Me.HasTarget && Core.Me.CurrentTarget.ThoroughCanAttack();

            // Above the attack guard: an enemy our damage cannot reach can still be casting something
            // interruptible, and still hurts the party — so interrupts and mitigation run before the
            // guard. Weave discipline still applies while attacking; with no attackable target the GCD
            // is idle, so the weave check alone would be false exactly when these are needed most.
            // Internal order of the mitigation block is unchanged; it now simply precedes the Potion
            // and damage oGCDs instead of sharing their weave block. (The top-level Passage of Arms
            // bail already protects everything here.)
            if (await SingleTarget.Interrupt()) return true;

            if (!canAttack || PaladinRoutine.GlobalCooldown.CanWeave())
            {
                //Defensive Buff
                if (await Defensive.HallowedGround()) return true;
                if (await Defensive.Sentinel()) return true;
                if (await Defensive.Rampart()) return true;
                if (await Defensive.Reprisal()) return true;
                if (await Defensive.Sheltron()) return true;
                if (await Defensive.DivineVeil()) return true;
                if (await Tank.ArmsLength(PaladinSettings.Instance)) return true;

                //Cover
                if (await Defensive.Intervention()) return true;
                if (await Defensive.Cover()) return true;
            }

            // Stance first: an enemy immune to our damage still attacks and builds enmity pressure,
            // so a missing tank stance must be restorable before the attack guard.
            if (!Core.Me.HasAura(Auras.PassageOfArms) && await Buff.IronWill()) return true;

            if (!canAttack)
                return false;

            if (!Core.Me.HasAura(Auras.PassageOfArms))
            {
                //Utility

                if (PaladinRoutine.GlobalCooldown.CanWeave())
                {
                    //Potion
                    if (await Buff.UsePotion()) return true;

                    //Damage Buff
                    if (await Buff.FightOrFlight()) return true;

                    //oGCDS
                    if (await SingleTarget.Requiescat()) return true;
                    if (await Aoe.CircleOfScorn()) return true;
                    if (await Aoe.Expiacion()) return true;
                    if (await SingleTarget.Intervene()) return true; //dash
                }

                //Combo AOE (Single Target or Multi Target)
                if (await Aoe.BladeOfHonor()) return true;
                if (await Aoe.BladeOfValor()) return true;
                if (await Aoe.BladeOfTruth()) return true;
                if (await Aoe.BladeOfFaith()) return true;
                if (await Aoe.Confiteor()) return true;

                if (await SingleTarget.ShieldLobOnLostAggro()) return true;
                if (await SingleTarget.GoringBlade()) return true;

                //Under Divine Might Aura to have no cast or stacks of Sword Oath
                if (await Aoe.HolyCircle()) return true;
                if (await SingleTarget.HolySpirit()) return true;

                //Combo Action AOE
                if (await Aoe.Prominence()) return true;
                if (await Aoe.TotalEclipse()) return true;

                // Use resources after Royal Authority (Atonement/Supplication/Sepulchre)
                if (await SingleTarget.Atonement()) return true;

                //Combo Action Single Target - Prioritize Royal Authority when combo is ready
                //Filler rotation: RA → Atonement → Fast Blade → Riot Blade → Supplication → Holy Spirit → Sepulchre → RA
                if (await SingleTarget.RoyalAuthority()) return true;

                // Continue combo: Fast Blade → Riot Blade
                if (await SingleTarget.RiotBlade()) return true;
                if (await SingleTarget.FastBlade()) return true;

                return await SingleTarget.ShieldLob();
            }
            else
            {
                return false;
            }
        }
        public static async Task<bool> PvP()
        {
            if (await CommonPvp.CommonTasks(PaladinSettings.Instance)) return true;

            // Protect a low-HP ally (Guardian) — defensive, fires regardless of burst/Guard state.
            if (await Pvp.GuardianPvp()) return true;

            if (CommonPvp.ShouldUseBurst())
            {
                // Self-targeted defensives/utility — fine regardless of the target's Guard
                if (await Pvp.PhalanxPvp()) return true;
                if (await Pvp.HolySheltronPvp()) return true;

                // Shield Smite has its own Guard handling (Pvp_ShieldSmiteOnlyOnGuard)
                if (await Pvp.ShieldSmitePvp()) return true;

                // Offensive burst — don't dump into a Guarded/invulnerable target (99% mitigated in 7.5)
                if (!CommonPvp.GuardCheck(PaladinSettings.Instance))
                {
                    if (await Pvp.BladeofValorPvp()) return true;
                    if (await Pvp.BladeofTruthPvp()) return true;
                    if (await Pvp.BladeofFaithPvp()) return true;
                    if (await Pvp.HolySpiritPvp()) return true;
                    if (await Pvp.ImperatorPvp()) return true;
                    if (await Pvp.IntervenePvp()) return true;
                }
            }

            // Combo follow-ups stay outside ShouldUseBurst so the chain completes once started (per design),
            // but are still skipped against a Guarded target so we don't waste them into 99% mitigation.
            if (!CommonPvp.GuardCheck(PaladinSettings.Instance))
            {
                if (await Pvp.AtonementPvp()) return true;
                if (await Pvp.SupplicationPvp()) return true;
                if (await Pvp.SepulchrePvp()) return true;
                if (await Pvp.ConfiteorPvp()) return true;
            }

            // Basic combo fallback (ONLY ungated abilities)
            if (await Pvp.RoyalAuthorityPvp()) return true;
            if (await Pvp.RiotBladePvp()) return true;
            return (await Pvp.FastBladePvp());
        }
    }
}
