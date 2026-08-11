using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using ff14bot.Helpers;

namespace Magitek.Utilities
{
    /// <summary>
    /// Storage for OccultDebuffImmunityTracker.
    ///
    /// Two layers, both in the same shape - a debuff name mapped to the NPCs known to resist it,
    /// as npcId -> name (the BossDictionary.json convention, so the file documents itself):
    ///
    ///   {
    ///     "OccultToad": { "12345": "Cursed Concretion" },
    ///     "Slow":       {}
    ///   }
    ///
    ///   1: A baseline shipped with Magitek as an embedded resource (Resources/OccultDebuffImmunity.json).
    ///      Read-only, hand-maintained, ships empty. Everyone gets it on update.
    ///   2: Whatever this character has learned, written next to the other character settings.
    ///      Only this file is ever written.
    ///
    /// Loads union the two, so a shipped entry can never be un-learned by a bad observation.
    /// </summary>
    public class OccultDebuffImmunityPersistence
    {
        private const string BaselineResource = "Magitek.Resources.OccultDebuffImmunity.json";

        private static readonly string PersistenceFilePath = Path.Combine(JsonSettings.CharacterSettingsDirectory, "Magitek", "OccultDebuffImmunityData.json");

        public class OccultDebuffImmunityData
        {
            /// <summary>Debuff name -> (npcId -> enemy name). Names are for humans; only the keys are read.</summary>
            public Dictionary<string, Dictionary<uint, string>> Immune { get; set; } = new Dictionary<string, Dictionary<uint, string>>();

            /// <summary>Debuff name -> (npcId -> enemy name) for enemies observed taking the debuff.</summary>
            public Dictionary<string, Dictionary<uint, string>> Susceptible { get; set; } = new Dictionary<string, Dictionary<uint, string>>();
        }

        public static void SaveData(OccultDebuffImmunityData data)
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(PersistenceFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(PersistenceFilePath, json);

                Logger.WriteInfo("[OccultDebuffImmunity] Persistence data saved successfully");
            }
            catch (Exception ex)
            {
                Logger.WriteInfo($"[OccultDebuffImmunity] Failed to save persistence data: {ex.Message}");
            }
        }

        public static OccultDebuffImmunityData LoadData()
        {
            var data = new OccultDebuffImmunityData();

            // Layer 1: the baseline that ships with Magitek.
            foreach (var entry in LoadBaseline())
            {
                if (!data.Immune.TryGetValue(entry.Key, out var bucket))
                {
                    bucket = new Dictionary<uint, string>();
                    data.Immune[entry.Key] = bucket;
                }

                foreach (var npc in entry.Value)
                {
                    bucket[npc.Key] = npc.Value;
                }
            }

            // Layer 2: what this character has learned.
            try
            {
                if (!File.Exists(PersistenceFilePath))
                {
                    Logger.WriteInfo("[OccultDebuffImmunity] No learned data found, starting from the shipped baseline only");
                    return data;
                }

                string json = File.ReadAllText(PersistenceFilePath);
                var learned = JsonConvert.DeserializeObject<OccultDebuffImmunityData>(json);

                if (learned?.Immune != null)
                {
                    foreach (var entry in learned.Immune)
                    {
                        if (!data.Immune.TryGetValue(entry.Key, out var bucket))
                        {
                            bucket = new Dictionary<uint, string>();
                            data.Immune[entry.Key] = bucket;
                        }

                        foreach (var npc in entry.Value)
                        {
                            bucket[npc.Key] = npc.Value;
                        }
                    }
                }

                if (learned?.Susceptible != null)
                {
                    foreach (var entry in learned.Susceptible)
                    {
                        if (!data.Susceptible.TryGetValue(entry.Key, out var bucket))
                        {
                            bucket = new Dictionary<uint, string>();
                            data.Susceptible[entry.Key] = bucket;
                        }

                        foreach (var npc in entry.Value)
                        {
                            bucket[npc.Key] = npc.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteInfo($"[OccultDebuffImmunity] Failed to load learned data: {ex.Message}");
            }

            return data;
        }

        private static Dictionary<string, Dictionary<uint, string>> LoadBaseline()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                using (var stream = assembly.GetManifestResourceStream(BaselineResource))
                {
                    if (stream == null)
                        return new Dictionary<string, Dictionary<uint, string>>();

                    using (var reader = new StreamReader(stream))
                    {
                        var baseline = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<uint, string>>>(reader.ReadToEnd());
                        return baseline ?? new Dictionary<string, Dictionary<uint, string>>();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteInfo($"[OccultDebuffImmunity] Failed to load shipped baseline: {ex.Message}");
                return new Dictionary<string, Dictionary<uint, string>>();
            }
        }
    }
}
