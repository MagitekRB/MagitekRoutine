using ff14bot;
using ff14bot.Enums;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Models.Astrologian;
using Magitek.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using static ff14bot.Managers.ActionResourceManager.Astrologian;
using Auras = Magitek.Utilities.Auras;

namespace Magitek.Logic.Astrologian
{
    internal static class Cards
    {
        // The next draw overwrites the hand, so play out anything held this close to it.
        private const int DrawDumpWindowSeconds = 10;

        // Failsafe: a play condition stuck false used to wedge the draw permanently.
        private const int DrawStallBreakSeconds = 15;

        // Divination will wait at most this long for a draw to put a damage card in hand.
        private const int HoldDivinationForDrawSeconds = 10;

        private static DateTime _readyDrawBlockedSince = DateTime.MinValue;

        // Mirror of the Play I hold: no damage card in hand and a draw about to land means
        // waiting puts the fresh Balance or Spear inside the window.
        public static bool HoldDivinationForDraw()
        {
            if (!AstrologianSettings.Instance.AlignCardsWithDivination)
                return false;

            // Holding only makes sense when the routine itself will draw and play the fresh
            // card - with either toggle off the draw this waits for can never complete
            if (!AstrologianSettings.Instance.DrawCards || !AstrologianSettings.Instance.Play)
                return false;

            if (CurrentCards.Any(card => card == AstrologianCard.Balance || card == AstrologianCard.Spear))
                return false;

            var drawCooldown = DrawCooldownRemainingSeconds();

            return drawCooldown != null && drawCooldown <= HoldDivinationForDrawSeconds;
        }

        public static async Task<bool> PlayCards()
        {
            if (!AstrologianSettings.Instance.Play)
                return false;

            if (!Core.Me.InCombat)
                return false;

            // A card's identity tells us its slot.
            var playICard = AstrologianCard.None;
            var playIICard = AstrologianCard.None;
            var playIIICard = AstrologianCard.None;

            foreach (var card in CurrentCards)
            {
                switch (card)
                {
                    case AstrologianCard.Balance:
                    case AstrologianCard.Spear:
                        playICard = card;
                        break;

                    case AstrologianCard.Bole:
                    case AstrologianCard.Arrow:
                        playIICard = card;
                        break;

                    case AstrologianCard.Ewer:
                    case AstrologianCard.Spire:
                        playIIICard = card;
                        break;
                }
            }

            if (playICard == AstrologianCard.None
                && playIICard == AstrologianCard.None
                && playIIICard == AstrologianCard.None)
                return false;

            var drawCooldown = DrawCooldownRemainingSeconds();

            // No draw known (deep sync) means nothing can overwrite the hand, so never dump.
            var dumping = drawCooldown != null && drawCooldown <= DrawDumpWindowSeconds;

            // Damage card first: once its Divination hold releases, every weave window it
            // spends behind Arrow or Spire is burst window lost. While held it returns
            // false and the utility cards get the windows anyway.
            if (playICard != AstrologianCard.None)
            {
                // Compared against the draw cooldown, not a constant: holding only when
                // Divination lands first is what stops the hold from ever delaying a draw.
                var holdForDivination = AstrologianSettings.Instance.AlignCardsWithDivination
                                        && AstrologianSettings.Instance.Divination
                                        && Spells.Divination.IsKnown()
                                        && !Core.Me.HasAura(Auras.Divination, true)
                                        && drawCooldown != null
                                        && Spells.Divination.Cooldown.TotalSeconds <= drawCooldown
                                        && !dumping;

                if (!holdForDivination)
                {
                    var target = playICard == AstrologianCard.Balance ? MeleeDpsOrTank() : RangedDpsOrHealer();

                    if (await Spells.PlayI.Masked().Cast(target))
                        return true;
                }
            }

            // Peek, not the responder: a card is additive and must not consume the mechanic.
            if (playIICard != AstrologianCard.None)
            {
                var busterTarget = FightLogic.Peek.EnemyIsCastingTankBuster();
                var target = (GameObject)busterTarget ?? MainTankOrFallback();

                if (dumping
                    || busterTarget != null
                    || target.CurrentHealthPercent <= AstrologianSettings.Instance.PlayUtilityCardHealthPercent)
                    if (await Spells.PlayII.Masked().Cast(target))
                        return true;
            }

            if (playIIICard == AstrologianCard.Spire)
            {
                var busterTarget = FightLogic.Peek.EnemyIsCastingTankBuster();
                var target = (GameObject)busterTarget ?? MainTankOrFallback();

                if (dumping
                    || busterTarget != null
                    || target.CurrentHealthPercent <= AstrologianSettings.Instance.PlayUtilityCardHealthPercent)
                    if (await Spells.PlayIII.Masked().Cast(target))
                        return true;
            }

            if (playIIICard == AstrologianCard.Ewer)
            {
                var target = (GameObject)Group.CastableAlliesWithin30
                                 .Where(a => a.CurrentHealth > 0)
                                 .OrderBy(a => a.CurrentHealthPercent)
                                 .FirstOrDefault()
                             ?? MainTankOrFallback();

                if (dumping
                    || target.CurrentHealthPercent <= AstrologianSettings.Instance.PlayUtilityCardHealthPercent)
                    if (await Spells.PlayIII.Masked().Cast(target))
                        return true;
            }

            return false;
        }

        public static async Task<bool> Draw()
        {
            // Cards persist out of combat, so drawing before the pull keeps the opener from
            // being cardless. Sanctuaries block it so we don't shuffle cards standing in town.
            if (!Core.Me.InCombat && Globals.InSanctuaryOrSafeZone)
                return false;

            // The two draws are one alternating button under the hood (shared recast, and the
            // client swaps the base action to whichever draw is active), so they cannot be
            // toggled separately — one setting governs drawing as a whole.
            if (!AstrologianSettings.Instance.DrawCards)
                return false;

            var readyDraw = Spells.AstralDraw.IsKnownAndReady() ? Spells.AstralDraw
                : Spells.UmbralDraw.IsKnownAndReady() ? Spells.UmbralDraw
                : null;

            var handEmpty = CurrentCards.All(card => card == AstrologianCard.None);

            if (handEmpty)
            {
                _readyDrawBlockedSince = DateTime.MinValue;

                if (readyDraw == null)
                    return false;

                return await readyDraw.Cast(Core.Me);
            }

            // Out of combat nothing can wedge — plays are combat-gated — so a held hand just waits for the pull.
            if (readyDraw == null || !Core.Me.InCombat)
            {
                _readyDrawBlockedSince = DateTime.MinValue;
                return false;
            }

            // Stall-breaker: a ready draw blocked by a non-empty hand for this long means
            // the play conditions have wedged, so draw anyway and accept the overwrite —
            // the draw's MP restore and fresh cards beat whatever is being held.
            if (_readyDrawBlockedSince == DateTime.MinValue)
            {
                _readyDrawBlockedSince = DateTime.Now;
                return false;
            }

            if (DateTime.Now - _readyDrawBlockedSince <= TimeSpan.FromSeconds(DrawStallBreakSeconds))
                return false;

            if (!await readyDraw.Cast(Core.Me))
                return false;

            _readyDrawBlockedSince = DateTime.MinValue;
            return true;
        }

        // Remaining time on the shared draw recast, or null when no draw is known. The two
        // draws alternate as one button and share the recast, so whichever reports as known
        // carries the real remaining time; taking the minimum over the known ones covers
        // either of them (or both) being known without caring which is active.
        private static double? DrawCooldownRemainingSeconds()
        {
            double? remaining = null;

            if (Spells.AstralDraw.IsKnown())
                remaining = Spells.AstralDraw.Cooldown.TotalSeconds;

            if (Spells.UmbralDraw.IsKnown())
            {
                var umbral = Spells.UmbralDraw.Cooldown.TotalSeconds;

                if (remaining == null || umbral < remaining)
                    remaining = umbral;
            }

            return remaining;
        }

        private static GameObject MainTankOrFallback()
        {
            var mainTank = Group.CastableAlliesWithin30.FirstOrDefault(a => a.CurrentHealth > 0 && a.IsTank(mainTank: true));

            if (mainTank != null)
                return mainTank;

            return Tank();
        }

        private static GameObject Tank()
        {
            //Get party size
            int partySize = Group.CastableAlliesWithin30.Count();
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && a.IsTank()).OrderBy(GetWeight);

            //If in light party, allow ally to have more than one card aura.
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
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && !a.HasAura(Auras.Weakness) && (a.IsTank() || a.IsMeleeDps())).OrderBy(a => a.IsTank() ? 1 : 0).ThenBy(GetWeight);

            //If in light party, allow ally to have more than one card aura.
            if (partySize <= 4)
            {
                var extendedAlly = Group.CastableAlliesWithin30.Where(a => a.CurrentHealth > 0 && !a.HasAura(Auras.Weakness) && (a.IsTank() || a.IsMeleeDps())).OrderBy(a => a.IsTank() ? 1 : 0).ThenBy(GetWeight);
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
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && !a.HasAura(Auras.Weakness) && (a.IsHealer() || a.IsRangedDpsCard())).OrderBy(a => a.IsHealer() ? 1 : 0).ThenBy(GetWeight);

            //If in light party, allow ally to have more than one card aura.
            if (partySize <= 4)
            {
                var extendedAlly = Group.CastableAlliesWithin30.Where(a => a.CurrentHealth > 0 && !a.HasAura(Auras.Weakness) && (a.IsHealer() || a.IsRangedDpsCard())).OrderBy(a => a.IsHealer() ? 1 : 0).ThenBy(GetWeight);
                return extendedAlly.FirstOrDefault(Core.Me);
            }
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
