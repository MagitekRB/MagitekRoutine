using ff14bot;
using ff14bot.Enums;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Astrologian;
using Magitek.Utilities;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Astrologian;

namespace Magitek.Logic.Astrologian
{
    internal static class Cards
    {
        public static async Task<bool> PlayCards()
        {
            if (!AstrologianSettings.Instance.Play)
                return false;

            var cards = CurrentCards.ToList();

            // Check if any card is drawn
            if (!cards.Any(c => c != AstrologianCard.None))
                return false;

            if (Globals.InParty && Core.Me.InCombat && AstrologianSettings.Instance.Play)
            {
                // --- PLAY I (Damage Buffs) ---
                if (cards.Contains(AstrologianCard.Balance))
                {
                    return await Spells.PlayI.Masked().Cast(MeleeDpsOrTank());
                }
                if (cards.Contains(AstrologianCard.Spear))
                {
                    return await Spells.PlayI.Masked().Cast(RangedDpsOrHealer());
                }

                // --- PLAY II (Defensives: Bole / Arrow) ---
                if (cards.Contains(AstrologianCard.Bole) || cards.Contains(AstrologianCard.Arrow))
                {
                    var defensiveTarget = Tank();
                    if (defensiveTarget != null && defensiveTarget.CurrentHealthPercent <= AstrologianSettings.Instance.PlayDefensiveCardHealthPercent)
                    {
                        return await Spells.PlayII.Masked().Cast(defensiveTarget);
                    }
                }

                // --- PLAY III (Utility: Ewer / Spire) ---
                if (cards.Contains(AstrologianCard.Ewer) || cards.Contains(AstrologianCard.Spire))
                {
                    var utilityTarget = Tank();
                    if (utilityTarget != null && utilityTarget.CurrentHealthPercent <= AstrologianSettings.Instance.PlayDefensiveCardHealthPercent)
                    {
                        return await Spells.PlayIII.Masked().Cast(utilityTarget);
                    }
                }
            }

            return false;
        }

        public static async Task<bool> Draw()
        {
            // Cards persist out of combat, so drawing before the pull keeps the opener from
            // being cardless. Sanctuaries block it so we don't shuffle cards standing in town.
            if (!Core.Me.InCombat && Globals.InSanctuaryOrSafeZone)
                return false;

            foreach (var card in CurrentCards)
            {
                if (card != AstrologianCard.None)
                    return false;
            }

            // The two draws are one alternating button under the hood (shared recast, and the
            // client swaps the base action to whichever draw is active), so they cannot be
            // toggled separately — one setting governs drawing as a whole.
            if (!AstrologianSettings.Instance.DrawCards)
                return false;

            if (Spells.AstralDraw.IsKnownAndReady())
                return await Spells.AstralDraw.Cast(Core.Me);

            if (Spells.UmbralDraw.IsKnownAndReady())
                return await Spells.UmbralDraw.Cast(Core.Me);

            return false;
        }
        private static GameObject Tank()
        {
            //Get party size
            int partySize = Group.CastableAlliesWithin30.Count();
            //If in light party, allow ally to have more than one card aura.
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && a.IsTank()).OrderBy(GetWeight);

            if (partySize <= 4)
            {
                var extendedAlly = Group.CastableAlliesWithin30.Where(a => a.CurrentHealth > 0 && a.IsTank()).OrderBy(GetWeight);
                return extendedAlly.FirstOrDefault(Core.Me);
            }
            return ally.FirstOrDefault(Core.Me);
        }

        private static GameObject MeleeDpsOrTank()
        {
            //Get party size
            int partySize = Group.CastableAlliesWithin30.Count();
            // The Balance gives melee DPS and tanks the full 6%, but a DPS converts the buff
            // into far more damage than a tank, so DPS sort ahead of tanks inside the bracket.
            // The pool boundary IS the potency bracket, deliberately: off-role recipients get
            // only 3%, and a full-potency tank (~two-thirds of a DPS's output at 6%) out-gains
            // a half-potency ranged DPS — so with no melee DPS alive the tank is the right call.
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && (a.IsTank() || a.IsMeleeDps())).OrderBy(a => a.IsTank() ? 1 : 0).ThenBy(GetWeight);

            if (partySize <= 4)
            {
                var extendedAlly = Group.CastableAlliesWithin30.Where(a => a.CurrentHealth > 0 && (a.IsTank() || a.IsMeleeDps())).OrderBy(a => a.IsTank() ? 1 : 0).ThenBy(GetWeight);
                return extendedAlly.FirstOrDefault(Core.Me);
            }
            return ally.FirstOrDefault(Core.Me);
        }

        private static GameObject RangedDpsOrHealer()
        {
            //Get party size
            int partySize = Group.CastableAlliesWithin30.Count();
            // The Spear gives ranged DPS and healers the full 6%; same reasoning as The Balance,
            // a DPS makes more of the buff than a healer, so DPS sort ahead inside the bracket.
            // Same deliberate pool boundary as The Balance: a half-potency melee DPS does not
            // out-gain a full-potency healer's-bracket recipient, so off-role DPS stay excluded.
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && (a.IsHealer() || a.IsRangedDpsCard())).OrderBy(a => a.IsHealer() ? 1 : 0).ThenBy(GetWeight);

            if (partySize <= 4)
            {
                var extendedAlly = Group.CastableAlliesWithin30.Where(a => a.CurrentHealth > 0 && (a.IsHealer() || a.IsRangedDpsCard())).OrderBy(a => a.IsHealer() ? 1 : 0).ThenBy(GetWeight);
                return extendedAlly.FirstOrDefault(Core.Me);
            }
            return ally.FirstOrDefault(Core.Me);
        }

        private static int GetWeight(Character c)
        {
            switch (c.CurrentJob)
            {
                case ClassJobType.Astrologian: return AstrologianSettings.Instance.AstCardWeight;
                case ClassJobType.Monk:
                case ClassJobType.Pugilist: return AstrologianSettings.Instance.MnkCardWeight;
                case ClassJobType.BlackMage:
                case ClassJobType.Thaumaturge: return AstrologianSettings.Instance.BlmCardWeight;
                case ClassJobType.Dragoon:
                case ClassJobType.Lancer: return AstrologianSettings.Instance.DrgCardWeight;
                case ClassJobType.Samurai: return AstrologianSettings.Instance.SamCardWeight;
                case ClassJobType.Machinist: return AstrologianSettings.Instance.MchCardWeight;
                case ClassJobType.Summoner:
                case ClassJobType.Arcanist: return AstrologianSettings.Instance.SmnCardWeight;
                case ClassJobType.Bard:
                case ClassJobType.Archer: return AstrologianSettings.Instance.BrdCardWeight;
                case ClassJobType.Ninja:
                case ClassJobType.Rogue: return AstrologianSettings.Instance.NinCardWeight;
                case ClassJobType.RedMage: return AstrologianSettings.Instance.RdmCardWeight;
                case ClassJobType.Dancer: return AstrologianSettings.Instance.DncCardWeight;
                case ClassJobType.Paladin:
                case ClassJobType.Gladiator: return AstrologianSettings.Instance.PldCardWeight;
                case ClassJobType.Warrior:
                case ClassJobType.Marauder: return AstrologianSettings.Instance.WarCardWeight;
                case ClassJobType.DarkKnight: return AstrologianSettings.Instance.DrkCardWeight;
                case ClassJobType.Gunbreaker: return AstrologianSettings.Instance.GnbCardWeight;
                case ClassJobType.WhiteMage:
                case ClassJobType.Conjurer: return AstrologianSettings.Instance.WhmCardWeight;
                case ClassJobType.Scholar: return AstrologianSettings.Instance.SchCardWeight;
                case ClassJobType.Reaper: return AstrologianSettings.Instance.RprCardWeight;
                case ClassJobType.Sage: return AstrologianSettings.Instance.SgeCardWeight;
                case ClassJobType.Pictomancer: return AstrologianSettings.Instance.PctCardWeight;
                case ClassJobType.Viper: return AstrologianSettings.Instance.VprCardWeight;
                case ClassJobType.BlueMage: return AstrologianSettings.Instance.BluCardWeight;
            }

            return c.CurrentJob == ClassJobType.Adventurer ? 70 : 0;
        }
    }
}