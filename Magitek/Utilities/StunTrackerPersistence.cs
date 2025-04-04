using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using ff14bot.Helpers;

namespace Magitek.Utilities
{
    public class StunTrackerPersistence
    {
        private static readonly string PersistenceFilePath = Path.Combine(JsonSettings.CharacterSettingsDirectory, "Magitek", "StunTrackerData.json");

        public class StunTrackerData
        {
            public HashSet<uint> UnstunnableEnemyIds { get; set; } = new HashSet<uint>();
            public HashSet<uint> StunnableEnemyIds { get; set; } = new HashSet<uint>();
        }

        public static void SaveData(HashSet<uint> unstunnableEnemyIds, HashSet<uint> stunnableEnemyIds)
        {
            try
            {
                var data = new StunTrackerData
                {
                    UnstunnableEnemyIds = unstunnableEnemyIds,
                    StunnableEnemyIds = stunnableEnemyIds
                };

                string directoryPath = Path.GetDirectoryName(PersistenceFilePath);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(PersistenceFilePath, json);

                Logger.WriteInfo("[StunTracker] Persistence data saved successfully");
            }
            catch (Exception ex)
            {
                Logger.WriteInfo($"[StunTracker] Failed to save persistence data: {ex.Message}");
            }
        }

        public static StunTrackerData LoadData()
        {
            try
            {
                if (!File.Exists(PersistenceFilePath))
                {
                    Logger.WriteInfo("[StunTracker] No persistence data found, starting fresh");
                    return new StunTrackerData();
                }

                string json = File.ReadAllText(PersistenceFilePath);
                var data = JsonConvert.DeserializeObject<StunTrackerData>(json);

                Logger.WriteInfo($"[StunTracker] Loaded {data.UnstunnableEnemyIds.Count} unstunnable and {data.StunnableEnemyIds.Count} stunnable enemy IDs");
                return data;
            }
            catch (Exception ex)
            {
                Logger.WriteInfo($"[StunTracker] Failed to load persistence data: {ex.Message}");
                return new StunTrackerData();
            }
        }
    }
}