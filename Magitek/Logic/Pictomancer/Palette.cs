using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Models.Pictomancer;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Auras = Magitek.Utilities.Auras;

using PictomancerRoutine = Magitek.Utilities.Routines.Pictomancer;

namespace Magitek.Logic.Pictomancer
{
    internal static class Palette
    {
        public static async Task<bool> PrePaintPalettes(bool prebuff)
        {
            if (!PictomancerSettings.Instance.UseMotifs)
                return false;

            if (prebuff && !PictomancerSettings.Instance.PrePaletteOutOfCombat)
                return false;

            if (prebuff && PictomancerSettings.Instance.PrePaletteOutOfCombatOnlyInDuty && !Globals.InActiveDuty)
                return false;

            if (!prebuff && !PictomancerSettings.Instance.PaletteDuringDowntime)
                return false;

            var creature = Spells.CreatureMotif.Masked();

            // creatures
            if (creature.IsKnownAndReady() && creature.CanCast())
                return await creature.Cast(Core.Me);

            var weapon = Spells.WeaponMotif.Masked();

            // hammers
            if (weapon.IsKnownAndReady() && weapon.CanCast())
                return await weapon.Cast(Core.Me);

            var landscape = Spells.LandscapeMotif.Masked();

            // stars
            if (landscape.IsKnownAndReady() && landscape.CanCast())
                return await landscape.Cast(Core.Me);

            return false;
        }

        public static bool SwiftcastMotifCheck()
        {
            if (!PictomancerSettings.Instance.SwiftcastMotifs)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Spells.Swiftcast.IsKnownAndReady())
                return false;

            return Spells.Swiftcast.CanCast(Core.Me);
        }

        public static async Task<bool> SwitfcastMotif()
        {
            if (SwiftcastMotifCheck())
                return await Roles.Healer.Swiftcast();

            return false;
        }

        public static bool MotifCanCast(SpellData motif, SpellData muse, bool swiftcast)
        {
            //if ((muse.Cooldown - muse.AdjustedCooldown).TotalMilliseconds > (motif.AdjustedCastTime.TotalMilliseconds + Globals.AnimationLockMs + BaseSettings.Instance.UserLatencyOffset))
            //    return false;
            //if (muse.Charges < 1)
            //    return false;

            // hack since muse.Cooldown doesn't seem to be accurate, as it's doubling 
            // the Cooldown time for some reason with a single cast of the Muse.
            // The same information can be derived from Charges, so I used that instead.
            if (swiftcast && SwiftcastMotifCheck())
                return muse.Charges >= 1;
            else
            {
                var castTime = motif.AdjustedCastTime.TotalMilliseconds;
                var precastCooldown = (castTime + Globals.AnimationLockMs + BaseSettings.Instance.UserLatencyOffset) / (muse.AdjustedCooldown.TotalMilliseconds);
                var precastThreshold = 1 - precastCooldown;
                if (muse.Charges < precastThreshold) return false;
                return true;
            }
        }

        public static async Task<bool> CreatureMotif()
        {
            if (!PictomancerSettings.Instance.UseMotifs)
                return false;

            if (ActionResourceManager.Pictomancer.CreatureMotifDrawn)
                return false;

            if (PictomancerRoutine.CheckTTDIsEnemyDyingSoon())
                return false;

            var motif = Spells.CreatureMotif.Masked();
            var muse = Spells.LivingMuse.Masked();

            bool swiftcast = false;

            if (PictomancerSettings.Instance.SwiftcastCreatureMotifs)
                //&& (motif == Spells.WingMotif || motif == Spells.MawMotif))
                swiftcast = true;

            if (PictomancerSettings.Instance.SwiftcastMotifsOnlyWhenMoving && !MovementManager.IsMoving)
                swiftcast = false;

            if (!MotifCanCast(motif, muse, swiftcast))
                return false;

            // PomMotif -> WingMotif -> ClawMotif -> MawMotif
            if (motif.IsKnownAndReady() && motif.CanCast())
            {
                if (swiftcast)
                    await SwitfcastMotif();
                return await motif.Cast(Core.Me);
            }

            return false;
        }

        public static async Task<bool> CreatureMuse()
        {
            if (!PictomancerSettings.Instance.UseMuses)
                return false;

            if (ActionResourceManager.Pictomancer.MadeenPortraitReady
                || ActionResourceManager.Pictomancer.MooglePortraitReady)
                return false;

            if (PictomancerRoutine.CheckTTDIsEnemyDyingSoon())
                return false;

            var muse = Spells.LivingMuse.Masked();

            if (!PictomancerRoutine.UseSimplifiedRotation)
            {
                if (PictomancerSettings.Instance.SaveMogForStarry
                    && Spells.StarryMuse.IsKnown()
                    && !Core.Me.HasAura(Auras.StarryMuse, true)
                    && (ActionResourceManager.Pictomancer.CanvasFlagsState.HasFlag(ActionResourceManager.Pictomancer.CanvasFlags.Wing)
                       || ActionResourceManager.Pictomancer.CanvasFlagsState.HasFlag(ActionResourceManager.Pictomancer.CanvasFlags.Maw))
                    && muse.Charges < 3)
                {
                    var cooldownForCharge = muse.AdjustedCooldown.TotalMilliseconds;
                    var cooldown2charge = cooldownForCharge * 2;
                    var nextChargeTime = muse.CooldownToNextCharge();
                    var currentCharges = Math.Floor(muse.Charges);

                    var nextMogInMs = cooldown2charge - (cooldownForCharge - nextChargeTime);

                    if (currentCharges > 0)
                        nextMogInMs -= (cooldownForCharge * (currentCharges - 1));

                    //Logger.WriteInfo($"Next Mog in {nextMogInMs}ms. Starry Cooldown {PictomancerRoutine.StarryCooldownRemaining()}");

                    if (nextMogInMs > PictomancerRoutine.StarryCooldownRemaining())
                        return false;
                }
            }

            if (muse.IsKnown/*AndReady*/() && muse.CanCast(Core.Me.CurrentTarget))
                return await muse.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> MogoftheAges()
        {
            if (!PictomancerSettings.Instance.UseMogOfTheAges)
                return false;

            if (!PictomancerSettings.Instance.UseMogDuringHyperphantasia && Core.Me.HasAura(Auras.Hyperphantasia))
                return false;

            // save mogs in sub-100 content
            if (!PictomancerRoutine.UseSimplifiedRotation)
            {
                if (PictomancerSettings.Instance.SaveMogForStarry
                    && PictomancerRoutine.StarryOffCooldownSoon(20000)
                    && !Spells.StarPrism.IsKnown())
                    return false;
            }

            if (PictomancerRoutine.CheckTTDIsEnemyDyingSoon())
                return false;

            if (Spells.RetributionoftheMadeen.IsKnownAndReady() && Spells.RetributionoftheMadeen.CanCast())
                return await Spells.RetributionoftheMadeen.Cast(Core.Me.CurrentTarget);

            if (Spells.MogoftheAges.IsKnownAndReady() && Spells.MogoftheAges.CanCast())
                return await Spells.MogoftheAges.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> WeaponMotif()
        {
            if (!PictomancerSettings.Instance.UseMotifs)
                return false;

            if (!PictomancerSettings.Instance.UseHammerDuringHyperphantasia && Core.Me.HasAura(Auras.Hyperphantasia, true))
                return false;

            if (PictomancerRoutine.CheckTTDIsEnemyDyingSoon())
                return false;

            var motif = Spells.WeaponMotif.Masked();
            var muse = Spells.SteelMuse.Masked();

            bool swiftcast = false;

            if (PictomancerSettings.Instance.SwiftcastWeaponMotifs)
                swiftcast = true;

            if (PictomancerSettings.Instance.SwiftcastMotifsOnlyWhenMoving && !MovementManager.IsMoving)
                swiftcast = false;

            if (!MotifCanCast(motif, muse, swiftcast))
                return false;

            if (motif.IsKnownAndReady() && motif.CanCast())
            {
                if (swiftcast)
                    await SwitfcastMotif();
                return await motif.Cast(Core.Me);
            }

            return false;
        }

        public static async Task<bool> StrikingMuse()
        {
            if (!PictomancerSettings.Instance.UseMuses)
                return false;

            if (PictomancerRoutine.CheckTTDIsEnemyDyingSoon())
                return false;

            if (Core.Me.HasAura(Auras.HammerTime))
                return false;

            var muse = Spells.SteelMuse.Masked();

            if (!PictomancerRoutine.UseSimplifiedRotation)
            {
                var starryCooldown = PictomancerRoutine.StarryCooldownRemaining();
                var msToNextCharge = muse.CooldownToNextCharge();

                if (PictomancerSettings.Instance.SaveHammerForMovement
                    && (!PictomancerSettings.Instance.SaveHammerForMovementOnlyBoss || PictomancerSettings.Instance.SaveHammerForMovementOnlyBoss && Core.Me.CurrentTarget.IsBoss())
                    && muse.MaxCharges >= 2
                    && !MovementManager.IsMoving
                    && muse.Charges < 1.95
                    && !(PictomancerSettings.Instance.SaveHammerForStarry && Spells.StarryMuse.IsKnown() && Core.Me.HasAura(Auras.StarryMuse, true)))
                    return false;

                if (PictomancerSettings.Instance.SaveHammerForStarry
                    && Spells.StarryMuse.IsKnown()
                    && !Core.Me.HasAura(Auras.StarryMuse, true)
                    && starryCooldown <= msToNextCharge
                    && muse.Charges < 2)
                    return false;

                //Logger.WriteInfo($"Starry Cooldown: {starryCooldown}ms, Next Charge: {msToNextCharge}ms, Charges: {muse.Charges}");
            }

            if (muse.IsKnown/*AndReady*/() && muse.CanCast(Core.Me))
                return await muse.CastAura(Core.Me, Auras.HammerTime);

            return false;
        }

        public static async Task<bool> HammerStamp()
        {
            if (!PictomancerSettings.Instance.UseHammers)
                return false;

            //var hammerAura = Core.Me.CharacterAuras.Where(r => r.CasterId == Core.Player.ObjectId && r.Id == Auras.HammerTime).FirstOrDefault();

            //if (hammerAura == null)
            //    return false;
            if (!Core.Me.HasAura(Auras.HammerTime, true))
                return false;

            if (!PictomancerSettings.Instance.UseHammerDuringHyperphantasia && Core.Me.HasAura(Auras.Hyperphantasia, true))
                return false;

            //var hammerTimeLeft = hammerAura.TimespanLeft.TotalMilliseconds;
            //var hammersLeft = hammerAura.Value;
            //var hammerCastTime = Spells.HammerStamp.AdjustedCastTime.TotalMilliseconds + Globals.AnimationLockMs + BaseSettings.Instance.UserLatencyOffset;
            //var totalHammerCastTime = hammerCastTime * hammersLeft;

            //if (PictomancerSettings.Instance.SaveHammerForStarry
            //    && PictomancerRoutine.StarryOffCooldownSoon()
            //    && totalHammerCastTime > PictomancerRoutine.StarryCooldownRemaining())
            //    return false;

            var hammer = Spells.HammerStamp.Masked();

            if (hammer.IsKnown/*AndReady*/() && hammer.CanCast(Core.Me.CurrentTarget))
                return await hammer.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> LandscapeMotif()
        {
            if (!PictomancerSettings.Instance.UseMotifs)
                return false;

            if (!PictomancerSettings.Instance.UseStarrySky)
                return false;

            if (Core.Me.HasAura(Auras.StarryMuse, true))
                return false;

            if (PictomancerRoutine.CheckTTDIsEnemyDyingSoon())
                return false;

            var motif = Spells.LandscapeMotif.Masked();
            var muse = Spells.ScenicMuse.Masked();

            bool swiftcast = false;

            if (PictomancerSettings.Instance.SwiftcastLandscapeMotifs)
                swiftcast = true;

            if (PictomancerSettings.Instance.SwiftcastMotifsOnlyWhenMoving && !MovementManager.IsMoving)
                swiftcast = false;

            if (!MotifCanCast(motif, muse, swiftcast))
                return false;

            if (motif.IsKnownAndReady() && motif.CanCast())
            {
                if (swiftcast)
                    await SwitfcastMotif();
                return await motif.Cast(Core.Me);
            }

            return false;
        }

        public static async Task<bool> ScenicMuse()
        {
            if (!PictomancerSettings.Instance.UseMuses)
                return false;

            if (!PictomancerSettings.Instance.UseStarrySky)
                return false;

            if (MovementManager.IsMoving && !PictomancerSettings.Instance.UseStarrySkyWhileMoving)
                return false;

            if (PictomancerRoutine.CheckTTDIsEnemyDyingSoon())
                return false;

            if (!PictomancerRoutine.GlobalCooldown.CanWeave())
                return false;

            if (!PictomancerRoutine.UseSimplifiedRotation)
            {
                if (Core.Me.ClassLevel >= Spells.StarryMuse.LevelAcquired)
                {
                    if (PictomancerSettings.Instance.SaveStarryForHammers)
                    {
                        // Condition to continue: Weapon motif is drawn and we have 1 or more charges of Striking Muse.
                        bool hasHammer = (ActionResourceManager.Pictomancer.WeaponMotifDrawn
                                         && Spells.StrikingMuse.Charges >= 1);

                        // If the condition is not met, return false.
                        if (!hasHammer)
                        {
                            // Condition to continue: We have 3 charges of Hammer, then it's okay to use Starry Sky and save the hammers for the window. 
                            var hammerAura = Core.Me.CharacterAuras.Where(r => r.CasterId == Core.Player.ObjectId && r.Id == Auras.HammerTime).FirstOrDefault();

                            if (hammerAura == null || hammerAura.Value < 3)
                            {
                                //Logger.WriteInfo("Not enough charges of Hammer to save for Starry Sky.");
                                return false;
                            }
                        }
                    }

                    if (PictomancerSettings.Instance.SaveStarryForMog)
                    {
                        // Condition 1: Continue if we have a Madeen portrait or Moogle portrait AND a creature motif is drawn.
                        bool hasPortraitAndCreatureMotif =
                            (ActionResourceManager.Pictomancer.MadeenPortraitReady || ActionResourceManager.Pictomancer.MooglePortraitReady)
                            && ActionResourceManager.Pictomancer.CreatureMotifDrawn;

                        // Condition 2: Continue if we have a Maw or Wing on the canvas.
                        bool hasMawOrWingOnCanvas =
                            ActionResourceManager.Pictomancer.CanvasFlagsState.HasFlag(ActionResourceManager.Pictomancer.CanvasFlags.Maw)
                            || ActionResourceManager.Pictomancer.CanvasFlagsState.HasFlag(ActionResourceManager.Pictomancer.CanvasFlags.Wing);

                        // If neither Condition 1 nor Condition 2 is satisfied, return false.
                        if (!hasPortraitAndCreatureMotif && !hasMawOrWingOnCanvas)
                        {
                            //Logger.WriteInfo($"Not enough conditions met to save Starry Sky for Mog of the Ages. hasPortraitAndCreatureMotif: {hasPortraitAndCreatureMotif}, hasMawOrWingOnCanvas: {hasMawOrWingOnCanvas}");
                            return false;
                        }
                    }
                }
            }

            var muse = Spells.ScenicMuse.Masked();

            if (muse.IsKnown/*AndReady*/() && muse.CanCast(Core.Me))
            {
                if (Globals.InParty)
                {
                    if (Core.Me.HasAura(Auras.StarryMuse))
                        return false;

                    var couldStar = Group.CastableAlliesWithin30.Count(r => !r.HasAura(Auras.StarryMuse));
                    var starNeededCount = PictomancerSettings.Instance.StarrySkyCount;

                    if (PictomancerSettings.Instance.StarrySkyEntireParty)
                        starNeededCount = Group.CastableParty.Count();

                    if (couldStar >= starNeededCount)
                        return await muse.Cast(Core.Me);
                    else
                        return false;
                }

                return await muse.CastAura(Core.Me, Auras.StarryMuse);
            }

            return false;
        }

        public static async Task<bool> RainbowDrip()
        {
            if (!PictomancerSettings.Instance.UseRainbowDrip)
                return false;

            if (!Spells.RainbowDrip.IsKnownAndReady())
                return false;

            if (!Spells.RainbowDrip.CanCast(Core.Me.CurrentTarget))
                return false;

            if (!Core.Me.HasAura(Auras.RainbowBright))
                return false;

            if (Core.Me.HasAura(Auras.HammerTime)
                && Core.Me.GetAuraById(Auras.RainbowBright).TimespanLeft.TotalMilliseconds < 1000)
                return false;

            return await Spells.RainbowDrip.Cast(Core.Me.CurrentTarget);
        }

        public static async Task<bool> StarPrism()
        {
            if (!PictomancerSettings.Instance.UseStarPrism)
                return false;

            if (!Spells.StarPrism.IsKnownAndReady())
                return false;

            if (!Spells.StarPrism.CanCast(Core.Me.CurrentTarget))
                return false;

            if (!Core.Me.HasAura(Auras.Starstruck))
                return false;

            return await Spells.StarPrism.Cast(Core.Me.CurrentTarget);
        }
    }
}

