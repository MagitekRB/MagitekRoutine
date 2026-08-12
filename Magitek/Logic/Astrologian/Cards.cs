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

            // Dump cards if next draw is within 5 seconds so we don't overcap
            // Astral and Umbral share a recast timer
            bool forceDump = false;
            if (Core.Me.InCombat)
            {
                if (Spells.AstralDraw.IsKnown() && Spells.AstralDraw.Cooldown.TotalSeconds < 5) forceDump = true;
                if (Spells.UmbralDraw.IsKnown() && Spells.UmbralDraw.Cooldown.TotalSeconds < 5) forceDump = true;
            }

            if (Globals.InParty && Core.Me.InCombat)
            {
                // PLAY I - Damage Cards (Always use immediately in combat)
                if (cards.Contains(AstrologianCard.Balance))
                    return await Spells.PlayI.Masked().Cast(MeleeDpsOrTank());
                if (cards.Contains(AstrologianCard.Spear))
                    return await Spells.PlayI.Masked().Cast(RangedDpsOrHealer());

                // PLAY II & PLAY III - Defensive Cards
                // Only use if a tank is missing health, or if we need to force dump before next draw
                var defensiveTarget = Group.CastableTanks.FirstOrDefault(t => t.CurrentHealthPercent <= 80) 
                                      ?? (forceDump ? Tank() : null);

                if (defensiveTarget != null)
                {
                    if (cards.Contains(AstrologianCard.Arrow) || cards.Contains(AstrologianCard.Bole))
                        return await Spells.PlayII.Masked().Cast(defensiveTarget);

                    if (cards.Contains(AstrologianCard.Spire) || cards.Contains(AstrologianCard.Ewer))
                        return await Spells.PlayIII.Masked().Cast(defensiveTarget);
                }
            }
            else if (!Globals.InParty && Core.Me.InCombat)
            {
                // Solo Play - Dump on self
                bool selfNeedsDef = Core.Me.CurrentHealthPercent <= 80 || forceDump;
                
                if (cards.Contains(AstrologianCard.Balance) || cards.Contains(AstrologianCard.Spear))
                    return await Spells.PlayI.Masked().Cast(Core.Me);

                if (selfNeedsDef)
                {
                    if (cards.Contains(AstrologianCard.Arrow) || cards.Contains(AstrologianCard.Bole))
                        return await Spells.PlayII.Masked().Cast(Core.Me);

                    if (cards.Contains(AstrologianCard.Spire) || cards.Contains(AstrologianCard.Ewer))
                        return await Spells.PlayIII.Masked().Cast(Core.Me);
                }
            }

            return false;
        }

        public static async Task<bool> Draw()
        {
            if (!Core.Me.InCombat)
                return false;

            foreach (var card in CurrentCards)
            {
                if (card != AstrologianCard.None)
                    return false;
            }

            if (Spells.AstralDraw.IsKnownAndReady())
                return await Spells.AstralDraw.Cast(Core.Me);

            if (Spells.UmbralDraw.IsKnownAndReady())
                return await Spells.UmbralDraw.Cast(Core.Me);

            return false;
        }

        private static GameObject Tank()
        {
            int partySize = Group.CastableAlliesWithin30.Count();
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
            int partySize = Group.CastableAlliesWithin30.Count();
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && (a.IsTank() || a.IsMeleeDps())).OrderBy(GetWeight);

            if (partySize <= 4)
            {
                var extendedAlly = Group.CastableAlliesWithin30.Where(a => a.CurrentHealth > 0 && (a.IsTank() || a.IsMeleeDps())).OrderBy(GetWeight);
                return extendedAlly.FirstOrDefault(Core.Me);
            }
            return ally.FirstOrDefault(Core.Me);
        }

        private static GameObject RangedDpsOrHealer()
        {
            int partySize = Group.CastableAlliesWithin30.Count();
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && (a.IsHealer() || a.IsRangedDpsCard())).OrderBy(GetWeight);

            if (partySize <= 4)
            {
                var extendedAlly = Group.CastableAlliesWithin30.Where(a => a.CurrentHealth > 0 && (a.IsHealer() || a.IsRangedDpsCard())).OrderBy(GetWeight);
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