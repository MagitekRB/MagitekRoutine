using ff14bot;
using ff14bot.Managers;
using Magitek.Models.BeastMaster;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Magitek.Utilities
{
    /// <summary>
    /// What the Master's Bestiary knows, learned from the game's own answers. RebornBuddy exposes neither the
    /// bestiary nor a "capturable" flag, but the client says everything in chat: Gauge replies with "No pact can be
    /// forged with this target" or one of five odds, Capture on a beast already in the bestiary refuses with "You have
    /// already befriended this beast", and a successful subdual announces the pact. Each verdict is kept per mob
    /// name id in the settings file, so a beast is gauged once and never captured twice.
    /// </summary>
    public static class BeastMasterBestiary
    {
        public const int NoPact = 0;
        public const int Befriended = 1;
        // Gauge's five answers, from "exceedingly difficult" to "no effort at all".
        public const int LowestOdds = 2;
        public const int HighestOdds = 6;

        // System messages (Gauge's answers, "already befriended"), error messages (Capture refused) and character
        // progress (the pact itself).
        private const int SystemMessages = 57;
        private const int ErrorMessages = 60;
        private const int CharacterProgress = 64;

        private const int AnswerWindowSeconds = 8;

        private static bool _listening;
        private static uint _pendingNpcId;
        private static string _pendingName;
        private static DateTime _pendingSince = DateTime.MinValue;
        private static uint _markedNpcId;
        private static string _markedName;

        private static readonly Regex QuotedName = new Regex("[“„\"]([^“”\"]+)[”“\"]", RegexOptions.Compiled);

        private sealed class Phrase
        {
            public string Text;
            public int Verdict;
        }

        private static Phrase P(string text, int verdict) => new Phrase { Text = text, Verdict = verdict };

        // Order matters where one answer contains another ("exceedingly difficult" before "difficult").
        private static readonly Phrase[] Answers =
        {
            P("No pact can be forged", NoPact), P("Impossible de capturer la cible", NoPact), P("ist ein Pakt nicht möglich", NoPact), P("ことができない相手", NoPact),
            P("already befriended this beast", Befriended), P("déjà capturé cette bête", Befriended), P("bereits einen Pakt geschlossen", Befriended), P("登録済み", Befriended), P("すでに仲間にしている", Befriended),
            P("exceedingly difficult", 2), P("très difficile", 2), P("sehr schwierig", 2), P("とても難しい", 2),
            P("no effort at all", 6), P("jeu d'enfant", 6), P("sehr einfach", 6), P("とても簡単", 6),
            P("will not be easy", 4), P("ne sera pas évident", 4), P("nicht einfach sein", 4), P("簡単ではない", 4),
            P("will be difficult", 3), P("devrait être difficle", 3), P("dürfte schwierig sein", 3), P("のは難しい", 3),
            P("should be easy", 5), P("devrait être simple", 5), P("dürfte einfach sein", 5), P("簡単なようだ", 5),
        };

        private static readonly string[] PactForgedPhrases =
        {
            "Pact successfully forged", "Vous avez capturé votre cible", "einen Pakt mit einer Bestie", "契約することに成功",
        };

        public static void Start()
        {
            if (_listening)
                return;
            ff14bot.Managers.GamelogManager.MessageRecevied += OnMessage;
            _listening = true;
        }

        public static void Stop()
        {
            if (!_listening)
                return;
            ff14bot.Managers.GamelogManager.MessageRecevied -= OnMessage;
            _listening = false;
        }

        /// <summary>The recorded verdict for a mob name id, or null when that beast has never been gauged.</summary>
        public static int? Verdict(uint npcId)
        {
            var verdicts = BeastMasterSettings.Instance.CaptureVerdicts;
            if (verdicts != null && verdicts.TryGetValue(npcId, out var verdict))
                return verdict;
            return null;
        }

        /// <summary>Gauge or Capture went out on this beast and the chat has not answered yet.</summary>
        public static bool AwaitingAnswer(uint npcId) =>
            _pendingNpcId == npcId && (DateTime.Now - _pendingSince).TotalSeconds < AnswerWindowSeconds;

        /// <summary>The next answer in chat belongs to this beast.</summary>
        public static void Expect(uint npcId, string name)
        {
            _pendingNpcId = npcId;
            _pendingName = name;
            _pendingSince = DateTime.Now;
        }

        /// <summary>Capture landed: the pact, if it comes, is with this beast.</summary>
        public static void Marked(uint npcId, string name)
        {
            _markedNpcId = npcId;
            _markedName = name;
        }

        public static string Describe(int verdict)
        {
            switch (verdict)
            {
                case NoPact: return "no pact can be forged";
                case Befriended: return "already in the bestiary";
                case 2: return "capture exceedingly difficult";
                case 3: return "capture difficult";
                case 4: return "capture not easy";
                case 5: return "capture easy";
                case 6: return "capture takes no effort";
                default: return "unknown";
            }
        }

        private static void OnMessage(object sender, ChatEventArgs e)
        {
            try
            {
                var entry = e?.ChatLogEntry;
                if (entry == null)
                    return;

                // The bot reports the chat kind with the source bits on top (a system message from Gauge came
                // through as 2745); the kind itself is the low seven bits.
                var kind = Convert.ToInt32(entry.MessageType) & 0x7F;
                if (kind != SystemMessages && kind != ErrorMessages && kind != CharacterProgress)
                    return;

                var text = entry.FullLine ?? entry.Contents ?? string.Empty;

                if (kind == CharacterProgress)
                {
                    foreach (var phrase in PactForgedPhrases)
                    {
                        if (text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                        PactForged(text);
                        return;
                    }
                    return;
                }

                foreach (var answer in Answers)
                {
                    if (text.IndexOf(answer.Text, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    // The answer is for the beast we just gauged or captured; failing that, the beast being targeted
                    // (the player gauged it by hand).
                    if (AwaitingAnswer(_pendingNpcId))
                    {
                        Record(_pendingNpcId, answer.Verdict, _pendingName);
                        _pendingSince = DateTime.MinValue;
                        return;
                    }

                    var target = Core.Me?.CurrentTarget as ff14bot.Objects.BattleCharacter;
                    if (target != null && target.IsNpc)
                        Record(target.NpcId, answer.Verdict, target.EnglishName);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteInfo($"[Beastmaster] Could not read a chat line: {ex.Message}");
            }
        }

        private static void PactForged(string text)
        {
            var match = QuotedName.Match(text);
            var beast = match.Success ? match.Groups[1].Value.Trim() : _markedName;

            if (_markedNpcId != 0)
                Record(_markedNpcId, Befriended, _markedName ?? beast);

            var settings = BeastMasterSettings.Instance;
            if (!string.IsNullOrEmpty(beast) && (settings.BefriendedBeasts == null || !settings.BefriendedBeasts.Contains(beast)))
            {
                var copy = settings.BefriendedBeasts == null ? new List<string>() : new List<string>(settings.BefriendedBeasts);
                copy.Add(beast);
                settings.BefriendedBeasts = copy;
            }

            Logger.WriteInfo($"[Beastmaster] Pact forged with {beast ?? "a beast"}: it now has a place in the bestiary.");
            _markedNpcId = 0;
            _markedName = null;
        }

        private static void Record(uint npcId, int verdict, string name)
        {
            var settings = BeastMasterSettings.Instance;

            // Copy-on-write: the routine reads the dictionary on its own pulse, so it never sees a half-edited one,
            // and assigning the property goes through the generated setter, which saves.
            var copy = settings.CaptureVerdicts == null ? new Dictionary<uint, int>() : new Dictionary<uint, int>(settings.CaptureVerdicts);
            copy[npcId] = verdict;
            settings.CaptureVerdicts = copy;

            Logger.WriteInfo($"[Beastmaster] {name ?? "Target"}: {Describe(verdict)}.");
        }
    }
}
