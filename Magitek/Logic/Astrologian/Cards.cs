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

        public static AstrologianCard GetDrawnCard()
        {
            var drawnCards = CurrentCards;

            foreach (var card in drawnCards)             {
                if (card == AstrologianCard.None)
                    continue;

                return card;
            }

            return AstrologianCard.None;
        }
        public static async Task<bool> PlayCards()
        {
            var drawnCard = GetDrawnCard();
            
            var cardDrawn = drawnCard != AstrologianCard.None;

            /*
            Looks like Arcana is now filled with either the Crown Draw, Or the Arcana Draw with Arcana Draw taking priority.
            
            The Card ID's have changed... but there's some goof with Reborn where whether or not you have Lord, Lady, or nothing, the Card ID drawn changes:
                None = 0, 112, 128
                Balance = 1, 113, 129.
                Bole = 2, 114, 130.
                Arrow = 3, 115, 131.
                Spear = 4, 116, 132.
                Ewer = 5, 117, 133.
                Spire = 6, 118, 134.

            There's some temporary workarounds above until reborn has this fixed.

            */

            //if (ActionManager.CanCast(Spells.Draw, Core.Me)
            //    && AstrologianSettings.Instance.UseDraw
            //    && !cardDrawn)
            //     if (await Spells.Draw.Cast(Core.Me))
            //       await Coroutine.Wait(700, () => GetDrawnCard() != AstrologianCard.None);
                
            if (!cardDrawn)
                return false;

            //if (Core.Me.InCombat && Spells.MinorArcana.IsKnownAndReady() && AstrologianSettings.Instance.UseMinorArcana)
            //    if (!Core.Me.HasAnyAura(new uint[] { Auras.LadyOfCrownsDrawn, Auras.LordOfCrownsDrawn }))
            //        return await Spells.MinorArcana.Cast(Core.Me);

            //if (Combat.CombatTotalTimeLeft <= AstrologianSettings.Instance.DontPlayWhenCombatTimeIsLessThan)
            //    return false;

            //if (await RedrawOrDrawAgain(drawnCard))
            //    return true;
            
            if (Globals.InParty && Core.Me.InCombat && AstrologianSettings.Instance.Play)
            {
                switch (drawnCard)
                {
                    case AstrologianCard.Balance:
                        return await Spells.PlayI.Masked().Cast(MeleeDpsOrTank());
                    case AstrologianCard.Spear:
                        return await Spells.PlayI.Masked().Cast(RangedDpsOrHealer());
                    case AstrologianCard.Bole:
                        return await Spells.PlayII.Masked().Cast(Tank());
                    case AstrologianCard.Arrow:
                        return await Spells.PlayII.Masked().Cast(Tank());
                    case AstrologianCard.Ewer:
                        return await Spells.PlayIII.Masked().Cast(Tank());
                    case AstrologianCard.Spire:
                        return await Spells.PlayIII.Masked().Cast(Tank());
                }
            }

            if (!AstrologianSettings.Instance.Play)
                return false;

            if (!Globals.InParty && Core.Me.InCombat)
                switch (drawnCard)
                {
                    case AstrologianCard.Balance:
                    case AstrologianCard.Spear:
                        return await Spells.PlayI.Masked().Cast(Core.Me);
                    case AstrologianCard.Bole:
                    case AstrologianCard.Arrow:
                        return await Spells.PlayII.Masked().Cast(Core.Me);
                    case AstrologianCard.Ewer:
                    case AstrologianCard.Spire:
                        return await Spells.PlayIII.Masked().Cast(Core.Me);
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
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && (a.IsTank())).OrderBy(GetWeight);

            return ally.FirstOrDefault(Core.Me);
        }

        private static GameObject MeleeDpsOrTank()
        {
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && (a.IsTank() || a.IsMeleeDps())).OrderBy(GetWeight);
            
            return ally.FirstOrDefault(Core.Me);
        }

        private static GameObject RangedDpsOrHealer()
        {
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && (a.IsHealer() || a.IsRangedDpsCard())).OrderBy(GetWeight);

            return ally.FirstOrDefault(Core.Me);
        }

        private static int GetWeight(Character c)
        {
            switch (c.CurrentJob)
            {
                case ClassJobType.Astrologian:
                    return AstrologianSettings.Instance.AstCardWeight;

                case ClassJobType.Monk:
                case ClassJobType.Pugilist:
                    return AstrologianSettings.Instance.MnkCardWeight;

                case ClassJobType.BlackMage:
                case ClassJobType.Thaumaturge:
                    return AstrologianSettings.Instance.BlmCardWeight;

                case ClassJobType.Dragoon:
                case ClassJobType.Lancer:
                    return AstrologianSettings.Instance.DrgCardWeight;

                case ClassJobType.Samurai:
                    return AstrologianSettings.Instance.SamCardWeight;

                case ClassJobType.Machinist:
                    return AstrologianSettings.Instance.MchCardWeight;

                case ClassJobType.Summoner:
                case ClassJobType.Arcanist:
                    return AstrologianSettings.Instance.SmnCardWeight;

                case ClassJobType.Bard:
                case ClassJobType.Archer:
                    return AstrologianSettings.Instance.BrdCardWeight;

                case ClassJobType.Ninja:
                case ClassJobType.Rogue:
                    return AstrologianSettings.Instance.NinCardWeight;

                case ClassJobType.RedMage:
                    return AstrologianSettings.Instance.RdmCardWeight;

                case ClassJobType.Dancer:
                    return AstrologianSettings.Instance.DncCardWeight;

                case ClassJobType.Paladin:
                case ClassJobType.Gladiator:
                    return AstrologianSettings.Instance.PldCardWeight;

                case ClassJobType.Warrior:
                case ClassJobType.Marauder:
                    return AstrologianSettings.Instance.WarCardWeight;

                case ClassJobType.DarkKnight:
                    return AstrologianSettings.Instance.DrkCardWeight;

                case ClassJobType.Gunbreaker:
                    return AstrologianSettings.Instance.GnbCardWeight;

                case ClassJobType.WhiteMage:
                case ClassJobType.Conjurer:
                    return AstrologianSettings.Instance.WhmCardWeight;

                case ClassJobType.Scholar:
                    return AstrologianSettings.Instance.SchCardWeight;
                
                case ClassJobType.Reaper:
                    return AstrologianSettings.Instance.RprCardWeight;
                
                case ClassJobType.Sage:
                    return AstrologianSettings.Instance.SgeCardWeight;
                
                case ClassJobType.Pictomancer:
                    return AstrologianSettings.Instance.PctCardWeight;

                case ClassJobType.Viper:
                    return AstrologianSettings.Instance.VprCardWeight;

                case ClassJobType.BlueMage:
                    return AstrologianSettings.Instance.BluCardWeight;
            }

            return c.CurrentJob == ClassJobType.Adventurer ? 70 : 0;
        }
    }
}
