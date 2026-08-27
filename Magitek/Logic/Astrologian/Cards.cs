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
        // Internal: the Lady of Crowns dump in Heals shares the same window.
        internal const int DrawDumpWindowSeconds = 10;

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
            // The utility cards split by function. The Bole (10% damage taken cut) and the
            // Spire (400-potency barrier) are anticipatory: fight logic plays them at an
            // incoming tankbuster and otherwise they are HELD - the dump window before each
            // draw flushes whatever was never needed, so a held card cannot rot. The Arrow
            // and the Ewer respond to damage already taken and carry their own controls.
            // The dump ignores every toggle so a disabled card can never wedge the draw.
            if (playIICard == AstrologianCard.Bole)
            {
                var busterTarget = FightLogic.Peek.EnemyIsCastingTankBuster();
                // Same one-mitigation-per-buster rule as the responder: a victim already
                // carrying Exaltation or an Intersection shield is covered - hold the Bole
                // for the next buster (the dump still flushes it before the draw).
                var victimCovered = busterTarget != null
                    && (busterTarget.HasAura(Auras.Exaltation) || busterTarget.HasAura(Auras.CelestialIntersection));
                var target = (GameObject)busterTarget ?? MainTankOrFallback();

                if (dumping || (busterTarget != null && !victimCovered))
                    if (await Spells.PlayII.Masked().Cast(target))
                        return true;
            }

            if (playIICard == AstrologianCard.Arrow)
            {
                // The Arrow raises healing RECEIVED by 10%: the buster victim is about to eat
                // a hit and the heals that follow it; failing that, the most wounded ally
                // below the Arrow's threshold is where the healing is already going.
                var busterTarget = FightLogic.Peek.EnemyIsCastingTankBuster();
                var wounded = Group.CastableAlliesWithin30
                    .Where(a => a.CurrentHealth > 0
                        && a.CurrentHealthPercent <= AstrologianSettings.Instance.ArrowHealthPercent)
                    .OrderBy(a => a.CurrentHealthPercent)
                    .FirstOrDefault();
                // In the dump the threshold no longer applies - the card is leaving either
                // way, so the most wounded ally, hurt or not, beats a full-health tank.
                var lowestForArrow = Group.CastableAlliesWithin30
                    .Where(a => a.CurrentHealth > 0)
                    .OrderBy(a => a.CurrentHealthPercent)
                    .FirstOrDefault();
                var target = (GameObject)busterTarget ?? wounded ?? (GameObject)lowestForArrow ?? MainTankOrFallback();

                if (dumping
                    || (AstrologianSettings.Instance.PlayArrow && (busterTarget != null || wounded != null)))
                    if (await Spells.PlayII.Masked().Cast(target))
                        return true;
            }

            if (playIIICard == AstrologianCard.Spire)
            {
                var busterTarget = FightLogic.Peek.EnemyIsCastingTankBuster();
                // A catalogued raidwide is as good a reason as a buster: the barrier goes to
                // whoever can least afford the incoming hit.
                var aoeIncoming = FightLogic.Peek.EnemyIsCastingAoe();
                var lowestForSpire = Group.CastableAlliesWithin30
                    .Where(a => a.CurrentHealth > 0)
                    .OrderBy(a => a.CurrentHealthPercent)
                    .FirstOrDefault();
                var target = (GameObject)busterTarget
                    ?? (aoeIncoming ? (GameObject)lowestForSpire : null)
                    ?? MainTankOrFallback();

                if (dumping || busterTarget != null || aoeIncoming)
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
                    || (AstrologianSettings.Instance.PlayEwer
                        && target.CurrentHealthPercent <= AstrologianSettings.Instance.EwerHealthPercent))
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
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && a.IsTank()).OrderBy(GetAstralWeight);

            //If in light party, allow ally to have more than one card aura.
            if (partySize <= 4)
            {
                var extendedAlly = Group.CastableAlliesWithin30.Where(a => a.CurrentHealth > 0 && a.IsTank()).OrderBy(GetAstralWeight);
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
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && !a.HasAura(Auras.Weakness) && (a.IsTank() || a.IsMeleeDps())).OrderBy(a => a.IsTank() ? 1 : 0).ThenBy(GetAstralWeight);

            //If in light party, allow ally to have more than one card aura.
            if (partySize <= 4)
            {
                var extendedAlly = Group.CastableAlliesWithin30.Where(a => a.CurrentHealth > 0 && !a.HasAura(Auras.Weakness) && (a.IsTank() || a.IsMeleeDps())).OrderBy(a => a.IsTank() ? 1 : 0).ThenBy(GetAstralWeight);
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
            var ally = Group.CastableAlliesWithin30.Where(a => !a.HasAnyCardAura() && a.CurrentHealth > 0 && !a.HasAura(Auras.Weakness) && (a.IsHealer() || a.IsRangedDpsCard())).OrderBy(a => a.IsHealer() ? 1 : 0).ThenBy(GetUmbralWeight);

            //If in light party, allow ally to have more than one card aura.
            if (partySize <= 4)
            {
                var extendedAlly = Group.CastableAlliesWithin30.Where(a => a.CurrentHealth > 0 && !a.HasAura(Auras.Weakness) && (a.IsHealer() || a.IsRangedDpsCard())).OrderBy(a => a.IsHealer() ? 1 : 0).ThenBy(GetUmbralWeight);
                return extendedAlly.FirstOrDefault(Core.Me);
            }
            return ally.FirstOrDefault(Core.Me);
        }

        // Two tables, one per hand: the Balance empowers melee DPS and tanks, the Spear ranged
        // DPS and healers, so each card ranks only the half of the party it gives its full 6%.
        // Blue Mage counts as every role in game data and appears in both tables. A job outside
        // the queried table returns 0 and sorts first, same as the old unknown-job behavior.
        private static int GetAstralWeight(Character c)
        {
            switch (c.CurrentJob)
            {
                case ClassJobType.Monk:
                case ClassJobType.Pugilist:
                    return AstrologianSettings.Instance.MnkAstralCardWeight;

                case ClassJobType.Dragoon:
                case ClassJobType.Lancer:
                    return AstrologianSettings.Instance.DrgAstralCardWeight;

                case ClassJobType.Ninja:
                case ClassJobType.Rogue:
                    return AstrologianSettings.Instance.NinAstralCardWeight;

                case ClassJobType.Samurai:
                    return AstrologianSettings.Instance.SamAstralCardWeight;

                case ClassJobType.Reaper:
                    return AstrologianSettings.Instance.RprAstralCardWeight;

                case ClassJobType.Viper:
                    return AstrologianSettings.Instance.VprAstralCardWeight;

                case ClassJobType.Paladin:
                case ClassJobType.Gladiator:
                    return AstrologianSettings.Instance.PldAstralCardWeight;

                case ClassJobType.Warrior:
                case ClassJobType.Marauder:
                    return AstrologianSettings.Instance.WarAstralCardWeight;

                case ClassJobType.DarkKnight:
                    return AstrologianSettings.Instance.DrkAstralCardWeight;

                case ClassJobType.Gunbreaker:
                    return AstrologianSettings.Instance.GnbAstralCardWeight;

                case ClassJobType.BlueMage:
                    return AstrologianSettings.Instance.BluAstralCardWeight;
            }

            return c.CurrentJob == ClassJobType.Adventurer ? 70 : 0;
        }

        private static int GetUmbralWeight(Character c)
        {
            switch (c.CurrentJob)
            {
                case ClassJobType.Bard:
                case ClassJobType.Archer:
                    return AstrologianSettings.Instance.BrdUmbralCardWeight;

                case ClassJobType.Machinist:
                    return AstrologianSettings.Instance.MchUmbralCardWeight;

                case ClassJobType.Dancer:
                    return AstrologianSettings.Instance.DncUmbralCardWeight;

                case ClassJobType.BlackMage:
                case ClassJobType.Thaumaturge:
                    return AstrologianSettings.Instance.BlmUmbralCardWeight;

                case ClassJobType.Summoner:
                case ClassJobType.Arcanist:
                    return AstrologianSettings.Instance.SmnUmbralCardWeight;

                case ClassJobType.RedMage:
                    return AstrologianSettings.Instance.RdmUmbralCardWeight;

                case ClassJobType.Pictomancer:
                    return AstrologianSettings.Instance.PctUmbralCardWeight;

                case ClassJobType.WhiteMage:
                case ClassJobType.Conjurer:
                    return AstrologianSettings.Instance.WhmUmbralCardWeight;

                case ClassJobType.Scholar:
                    return AstrologianSettings.Instance.SchUmbralCardWeight;

                case ClassJobType.Astrologian:
                    return AstrologianSettings.Instance.AstUmbralCardWeight;

                case ClassJobType.Sage:
                    return AstrologianSettings.Instance.SgeUmbralCardWeight;

                case ClassJobType.BlueMage:
                    return AstrologianSettings.Instance.BluUmbralCardWeight;
            }

            return c.CurrentJob == ClassJobType.Adventurer ? 70 : 0;
        }
    }
}
