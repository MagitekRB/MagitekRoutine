using Buddy.Coroutines;
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

            if (Spells.StarryMuse.IsKnown() && Spells.Swiftcast.CanCast(Core.Me))
                return (Core.Me.HasAura(Auras.StarryMuse) || Spells.Swiftcast.AdjustedCooldown <= Spells.StarryMuse.Cooldown);
            else
                return Spells.Swiftcast.CanCast(Core.Me);
        }

        public static async Task<bool> SwitfcastMotif()
        {
            if (SwiftcastMotifCheck())
                return await Roles.Healer.Swiftcast();
            
            return false;
        }

        public static bool MotifCanCast(SpellData motif, SpellData muse)
        {
            //if ((muse.Cooldown - muse.AdjustedCooldown).TotalMilliseconds > (motif.AdjustedCastTime.TotalMilliseconds + 500))
            //    return false;
            //if (muse.Charges < 1)
            //    return false;

            // hack since muse.Cooldown doesn't seem to be accurate, as it's doubling 
            // the Cooldown time for some reason with a single cast of the Muse.
            // The same information can be derived from Charges, so I used that instead.
            if (SwiftcastMotifCheck())
                return muse.Charges >= 1;
            else
            {
                var castTime = motif.AdjustedCastTime.TotalMilliseconds;
                var precastCooldown = (castTime + Globals.AnimationLockMs + BaseSettings.Instance.UserLatencyOffset + 500) / (muse.AdjustedCooldown.TotalMilliseconds);
                var precastThreshold = 1 - precastCooldown;
                if (muse.Charges < precastThreshold) return false;
                return true;
            }
        }

        public static async Task<bool> CreatureMotif()
        {
            if (!PictomancerSettings.Instance.UseMotifs)
                return false;

            var motif = Spells.CreatureMotif.Masked();
            var muse = Spells.LivingMuse.Masked();

            if (!MotifCanCast(motif, muse))
                return false;

            // PomMotif -> WingMotif -> ClawMotif -> MawMotif
            if (motif.IsKnownAndReady() && motif.CanCast())
            {
                await SwitfcastMotif();
                return await motif.Cast(Core.Me);
            }

            return false;
        }

        public static async Task<bool> CreatureMuse()
        {
            if (!PictomancerSettings.Instance.UseMuses)
                return false;

            var muse = Spells.LivingMuse.Masked();

            if (muse.IsKnown/*AndReady*/() && muse.CanCast(Core.Me.CurrentTarget))
                return await muse.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> MogoftheAges()
        {
            if (!PictomancerSettings.Instance.UseMogOfTheAges)
                return false;

            if (PictomancerSettings.Instance.SaveMogForStarry
                && Utilities.Routines.Pictomancer.StarryOffCooldownSoon())
                return false;

            if (Utilities.Routines.Pictomancer.CheckTTDIsEnemyDyingSoon())
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

            var motif = Spells.WeaponMotif.Masked();
            var muse = Spells.SteelMuse.Masked();

            if (!MotifCanCast(motif, muse))
                return false;

            if (motif.IsKnownAndReady() && motif.CanCast())
            {
                await SwitfcastMotif();
                return await motif.Cast(Core.Me);
            }

            return false;
        }

        public static async Task<bool> StrikingMuse()
        {
            if (!PictomancerSettings.Instance.UseMuses)
                return false;

            var muse = Spells.SteelMuse.Masked();

            if (muse.IsKnown/*AndReady*/() && muse.CanCast(Core.Me.CurrentTarget))
                return await muse.Cast(Core.Me.CurrentTarget);

            return false;
        }

        public static async Task<bool> HammerStamp()
        {
            if (!PictomancerSettings.Instance.UseHammers)
                return false;

            var hammerAura = Core.Me.CharacterAuras.Where(r => r.CasterId == Core.Player.ObjectId && r.Id == Auras.HammerTime).FirstOrDefault();

            if (hammerAura == null)
                return false;

            var hammerTimeLeft = hammerAura.TimespanLeft.TotalMilliseconds;
            var hammersLeft = hammerAura.Value;
            var hammerCastTime = Spells.HammerStamp.AdjustedCastTime.TotalMilliseconds + Globals.AnimationLockMs + BaseSettings.Instance.UserLatencyOffset;
            var totalHammerCastTime = hammerCastTime * hammersLeft;

            if (PictomancerSettings.Instance.SaveHammerForStarry 
                && Utilities.Routines.Pictomancer.StarryOffCooldownSoon()
                && totalHammerCastTime > Utilities.Routines.Pictomancer.StarryCooldownRemaining())
                return false;

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

            var motif = Spells.LandscapeMotif.Masked();
            var muse = Spells.ScenicMuse.Masked();

            if (!MotifCanCast(motif, muse))
                return false;

            if (motif.IsKnownAndReady() && motif.CanCast())
                return await motif.Cast(Core.Me);

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

            if (Utilities.Routines.Pictomancer.CheckTTDIsEnemyDyingSoon())
                return false;

            var muse = Spells.ScenicMuse.Masked();

            if (muse.IsKnown/*AndReady*/() && muse.CanCast(Core.Me)){
                if (Globals.InParty)
                {
                    var couldStar = Group.CastableAlliesWithin30.Count(r => !r.HasAura(Auras.StarryMuse));
                    var starNeededCount = PictomancerSettings.Instance.StarrySkyCount;

                    if (PictomancerSettings.Instance.StarrySkyEntireParty)
                        starNeededCount = Group.CastableParty.Count();

                    if (couldStar >= starNeededCount)
                        return await muse.Cast(Core.Me);
                    else
                        return false;
                }

                return await muse.Cast(Core.Me);
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

            if (Utilities.Routines.Pictomancer.CheckTTDIsEnemyDyingSoon())
                return false;

            return await Spells.StarPrism.Cast(Core.Me.CurrentTarget);
        }
    }
}
