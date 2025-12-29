using ff14bot;
using ff14bot.Objects;
using Magitek.Enumerations;
using Magitek.Extensions;
using Magitek.Logic.Roles;
using Magitek.Models.Paladin;
using Magitek.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;
using PaladinRoutine = Magitek.Utilities.Routines.Paladin;


namespace Magitek.Logic.Paladin
{
    internal static class SingleTarget
    {
        public static async Task<bool> Interrupt()
        {
            List<SpellData> extraStun = new List<SpellData>();

            if (PaladinSettings.Instance.ShieldBash)
                extraStun.Add(Spells.ShieldBash);

            return await Tank.Interrupt(PaladinSettings.Instance, extraStuns: extraStun);
        }

        public static async Task<bool> ShieldLob()
        {
            if (PaladinSettings.Instance.UseShieldLobToPullExtraEnemies)
            {
                var pullTarget = Combat.Enemies.FirstOrDefault(r => r.ValidAttackUnit() && !r.Tapped
                                                        && r.WithinSpellRange(Spells.ShieldLob.Range)
                                                        && !r.WithinSpellRange(Spells.FastBlade.Range)
                                                        && r.TargetGameObject != Core.Me);

                if (pullTarget != null)
                    return await Spells.ShieldLob.Cast(pullTarget);
            }

            if (!PaladinSettings.Instance.UseShieldLob)
            {
                if (PaladinSettings.Instance.UseShieldLobToPull && !Core.Me.InCombat)
                {
                    return await Spells.ShieldLob.Cast(Core.Me.CurrentTarget);
                }
                return false;
            }

            return await Spells.ShieldLob.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> ShieldLobOnLostAggro()
        {
            if (Globals.OnPvpMap)
                return false;

            if (!PaladinSettings.Instance.UseIronWill)
                return false;

            if (!PaladinSettings.Instance.UseShieldLobOnLostAggro)
                return false;

            var shieldLobTarget = Combat.Enemies.FirstOrDefault(r =>
                !r.WithinSpellRange(Spells.FastBlade.Range)
                && r.WithinSpellRange(Spells.ShieldLob.Range)
                && r.TargetGameObject != Core.Me);

            if (shieldLobTarget == null)
                return false;

            if (shieldLobTarget.TargetGameObject == null)
                return false;

            if (!await Spells.ShieldLob.Cast(shieldLobTarget))
                return false;

            Logger.Write($@"[Magitek] ShieldLob On {shieldLobTarget.Name} To Pull Aggro");
            return true;
        }

        public static async Task<bool> HolySpirit()
        {
            if (!Spells.HolySpirit.IsKnownAndReady())
                return false;

            if (PaladinSettings.Instance.UseHolySpiritToPull && !Core.Me.InCombat)
                return await Spells.HolySpirit.Cast(Core.Me.CurrentTarget);

            if (PaladinSettings.Instance.UseHolySpiritWhenOutOfMeleeRange)
            {
                if (!Core.Me.CurrentTarget.WithinSpellRange(Spells.FastBlade.Range))
                {
                    if (Core.Me.CurrentManaPercent < PaladinSettings.Instance.HolySpiritWhenOutOfMeleeRangeMinMpPercent)
                        return false;

                    if (PaladinSettings.Instance.UseHolySpiritWhenOutOfMeleeRangeWithDivineMightOnly)
                    {
                        if (!Core.Me.HasAura(Auras.DivineMight))
                            return false;
                    }
                    return await Spells.HolySpirit.Cast(Core.Me.CurrentTarget);
                }
            }

            if (!PaladinSettings.Instance.UseHolySpirit)
                return false;

            if (Core.Me.HasAura(Auras.Requiescat) && !Spells.BladeOfFaith.IsKnown())
            {
                return await Spells.HolySpirit.Cast(Core.Me.CurrentTarget);
            }

            if (!Core.Me.HasAura(Auras.DivineMight))
                return false;

            // During FoF: Use Holy Spirit before Atonement, but only if we won't get Sepulchre by the time FoF ends
            // Outside FoF filler rotation: RA → Atonement → Fast Blade → Riot Blade → Supplication → Holy Spirit → Sepulchre → RA
            if (Spells.Atonement.IsKnown())
            {
                // During FoF: Block Holy Spirit if we can reach Sepulchre before FoF ends (prioritize getting to Sepulchre)
                if (Core.Me.HasAura(Auras.FightOrFlight) && PaladinRoutine.CanReachSepulchreBeforeFoFEnds())
                    return false;

                // Outside FoF: Block Holy Spirit only if we have Atonement or Supplication (allow Holy Spirit before Sepulchre)
                if (!Core.Me.HasAura(Auras.FightOrFlight) && (Core.Me.HasAura(Auras.AtonementReady) || Core.Me.HasAura(Auras.SupplicationReady)))
                    return false;
            }

            // During FoF, prioritize using Divine Might empowered Holy Spirit
            if (Core.Me.HasAura(Auras.FightOrFlight))
                return await Spells.HolySpirit.Cast(Core.Me.CurrentTarget);

            // Outside FoF: Bank Divine Might until next Royal Authority is ready (if setting enabled)
            if (PaladinSettings.Instance.BankResourcesForReopener && PaladinRoutine.ShouldBankResourcesForRoyalAuthority())
                return false;

            // Only use Divine Might empowered Holy Spirit outside FoF if Royal Authority combo is ready
            if (!PaladinRoutine.CanContinueComboAfter(Spells.RiotBlade) && !Core.Me.HasAura(Auras.DivineMight, msLeft: 3000))
                return false;

            return await Spells.HolySpirit.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> Intervene() //Dash
        {
            return await Tank.Dash(
                Spells.Intervene,
                PaladinSettings.Instance.UseIntervene,
                PaladinSettings.Instance.InterveneUseForMobility,
                PaladinSettings.Instance.InterveneUseForDps,
                PaladinSettings.Instance.InterveneOnlyDuringBurst,
                PaladinSettings.Instance.SaveInterveneCharges,
                () =>
                {
                    bool hasFightOrFlight = Spells.FightorFlight.IsKnown() && Core.Me.HasAura(Auras.FightOrFlight);
                    bool hasRequiescat = Spells.Requiescat.IsKnown() && Core.Me.HasAura(Auras.Requiescat);
                    return hasFightOrFlight && hasRequiescat && (PaladinRoutine.RequiescatStackCount < 4);
                },
                () => PaladinRoutine.GlobalCooldown.CanWeave(1),
                Spells.FastBlade.Range // Melee range for checking if in melee
            );
        }

        public static async Task<bool> Requiescat()
        {
            if (!PaladinRoutine.ToggleAndSpellCheck(PaladinSettings.Instance.UseRequiescat, Spells.Requiescat))
                return false;

            //If many target, cast Requiescat outside FoF
            /*if (Combat.Enemies.Count(x => x.Distance(Core.Me) <= 5 + x.CombatReach) >= PaladinSettings.Instance.TotalEclipseEnemies)
                return await Spells.Requiescat.Cast(Core.Me.CurrentTarget);
            */
            if (!Core.Me.HasAura(Auras.FightOrFlight))
                return false;

            if (Spells.Imperator.IsKnown())
                if (Spells.Imperator.Masked() == Spells.Imperator)
                    return await Spells.Imperator.CastAura(Core.Me.CurrentTarget, Auras.Requiescat, auraTarget: Core.Me);
                else
                    return await Spells.Imperator.Cast(Core.Me.CurrentTarget);
            else
                return await Spells.Requiescat.CastAura(Core.Me.CurrentTarget, Auras.Requiescat, auraTarget: Core.Me);
        }

        public static async Task<bool> Atonement()
        {
            if (!PaladinSettings.Instance.UseAtonement)
                return false;

            if (Core.Me.HasAura(Auras.Requiescat))
                return false;

            if (!Core.Me.HasAnyAura(new uint[] { Auras.AtonementReady, Auras.SupplicationReady, Auras.SepulchreReady }))
                return false;

            // Priority: Sepulchre > Supplication > Atonement
            if (Core.Me.HasAura(Auras.SepulchreReady))
            {
                // During FoF, use immediately. Outside FoF, bank if RA is 1-2 GCDs away (if setting enabled)
                if (!Core.Me.HasAura(Auras.FightOrFlight) && PaladinSettings.Instance.BankResourcesForReopener && PaladinRoutine.ShouldBankResourcesForRoyalAuthority())
                    return false;

                return await Spells.Sepulchre.Cast(Core.Me.CurrentTarget);
            }
            if (Core.Me.HasAura(Auras.SupplicationReady))
            {
                // During FoF, use immediately. Outside FoF, bank if RA is 1-2 GCDs away (if setting enabled)
                if (!Core.Me.HasAura(Auras.FightOrFlight) && PaladinSettings.Instance.BankResourcesForReopener && PaladinRoutine.ShouldBankResourcesForRoyalAuthority())
                    return false;

                return await Spells.Supplication.Cast(Core.Me.CurrentTarget);
            }

            // During FoF, prioritize using Atonement
            if (Core.Me.HasAura(Auras.FightOrFlight))
                return await Spells.Atonement.Cast(Core.Me.CurrentTarget);

            // Outside FoF: Use Atonement immediately after RA (don't bank it)
            // Filler rotation: RA → Atonement → Fast Blade → Riot Blade → Supplication → Holy Spirit → Sepulchre → RA
            // Only Supplication and Sepulchre are banked until next RA is ready
            if (Core.Me.HasAura(Auras.AtonementReady))
                return await Spells.Atonement.Cast(Core.Me.CurrentTarget);
            else
                return false;
        }

        public static async Task<bool> GoringBlade()
        {
            if (!PaladinSettings.Instance.UseGoringBlade)
                return false;

            if (!Core.Me.HasAura(Auras.FightOrFlight))
                return false;

            if (Spells.Requiescat.IsKnownAndReady() || Spells.Imperator.IsKnownAndReady())
                return false;


            return await Spells.GoringBlade.Cast(Core.Me.CurrentTarget);
        }

        /*************************************************************************************
         *                                    Combo
         * ***********************************************************************************/
        public static async Task<bool> RoyalAuthority()
        {
            if (!PaladinRoutine.CanContinueComboAfter(Spells.RiotBlade))
                return false;

            if (Core.Me.HasAnyAura(new uint[] { Auras.AtonementReady, Auras.SupplicationReady, Auras.SepulchreReady }))
                return false;

            return await PaladinRoutine.RoyalAuthority.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> RiotBlade()
        {
            if (!PaladinRoutine.CanContinueComboAfter(Spells.FastBlade))
                return false;

            return await Spells.RiotBlade.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> FastBlade()
        {
            return await Spells.FastBlade.Cast(Core.Me.CurrentTarget);
        }
    }
}