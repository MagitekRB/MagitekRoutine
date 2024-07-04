using Buddy.Coroutines;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Account;
using Magitek.Models.Pictomancer;
using Magitek.Models.Reaper;
using Magitek.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreeSharp;
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

        public static async Task<bool> SwitfcastMotif()
        {
            if (!PictomancerSettings.Instance.SwiftcastMotifs)
                return false;

            if (!Core.Me.InCombat)
                return false;

            if (!Spells.Swiftcast.IsKnownAndReady())
                return false;

            if (Spells.StarryMuse.IsKnown())
                if (Core.Me.HasAura(Auras.StarryMuse) || Spells.Swiftcast.AdjustedCooldown <= Spells.StarryMuse.Cooldown)
                    return await Roles.Healer.Swiftcast();
            else
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
            var castTime = motif.AdjustedCastTime.TotalMilliseconds;
            var precastCooldown = (castTime + 750) / muse.AdjustedCooldown.TotalMilliseconds;
            var precastThreshold = 1 - precastCooldown;
            if (muse.Charges < precastThreshold) return false;
            return true;
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

            if (PictomancerSettings.Instance.SaveHammerForStarry 
                && Utilities.Routines.Pictomancer.StarryOffCooldownSoon())
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
                if (Spells.Swiftcast.IsKnownAndReady())
                    if (!await Roles.Healer.Swiftcast())
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
