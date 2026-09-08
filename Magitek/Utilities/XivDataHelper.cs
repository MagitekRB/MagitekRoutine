using Magitek.Models.BeastMaster;
using Magitek.Models.WebResources;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace Magitek.Utilities
{
    internal static class XivDataHelper
    {
        static XivDataHelper()
        {
            var assembly = Assembly.GetExecutingAssembly();

            // The Beastmaster bestiary (7.56 client data): each familiar's Trick affinity, controlled ability and
            // kinship. Guarded so a missing resource degrades to an empty list.
            const string familiarsFile = "Magitek.Resources.BeastMasterFamiliars.json";
            BeastMasterFamiliars = new List<BeastMasterFamiliar>();
            if (assembly.GetManifestResourceNames().Any(n => n == familiarsFile))
            {
                using (var stream = assembly.GetManifestResourceStream(familiarsFile))
                using (var reader = new StreamReader(stream))
                {
                    BeastMasterFamiliars = JsonConvert.DeserializeObject<List<BeastMasterFamiliar>>(reader.ReadToEnd()) ?? new List<BeastMasterFamiliar>();
                }
            }

            const string statusFile = "Magitek.Resources.StatusList.json";

            string statuses;

            using (var stream = assembly.GetManifestResourceStream(statusFile))

            using (var reader = new StreamReader(stream))
            {
                statuses = reader.ReadToEnd();
            }

            XivDbStatuses = new List<XivDbItem>(JsonConvert.DeserializeObject<List<XivDbItem>>(statuses));

            const string actionFile = "Magitek.Resources.ActionList.json";

            string actions;

            using (var stream = assembly.GetManifestResourceStream(actionFile))

            using (var reader = new StreamReader(stream))
            {
                actions = reader.ReadToEnd();
            }

            XivDbActions = new List<XivDbItem>(JsonConvert.DeserializeObject<List<XivDbItem>>(actions));

            const string bossFile = "Magitek.Resources.BossDictionary.json";

            string bosses;

            using (var stream = assembly.GetManifestResourceStream(bossFile))

            using (var reader = new StreamReader(stream))
            {
                bosses = reader.ReadToEnd();
            }

            BossDictionary = new Dictionary<uint, string>(JsonConvert.DeserializeObject<Dictionary<uint, string>>(bosses));

            const string bossNameFile = "Magitek.Resources.BossNames.json";

            string bossesNames;

            using (var stream = assembly.GetManifestResourceStream(bossNameFile))

            using (var reader = new StreamReader(stream))
            {
                bossesNames = reader.ReadToEnd();
            }

            BossNames = new HashSet<string>(BossDictionary.Values);
            BossNames.UnionWith(JsonConvert.DeserializeObject<List<string>>(bossesNames));

            // Every BNpcBase row with IsOmnidirectional = true. Keyed by GameObject.BaseId
            // (the BNpcBase row), NOT NpcId (the BNpcName row)
            const string omnidirectionalFile = "Magitek.Resources.OmnidirectionalBaseIds.json";

            string omnidirectionalBaseIds;

            using (var stream = assembly.GetManifestResourceStream(omnidirectionalFile))

            using (var reader = new StreamReader(stream))
            {
                omnidirectionalBaseIds = reader.ReadToEnd();
            }

            OmnidirectionalBaseIds = new HashSet<uint>(JsonConvert.DeserializeObject<List<uint>>(omnidirectionalBaseIds));

            FightLogicEncounters.Encounters.SelectMany(encounter => encounter.Enemies)
                .Where(enemy => enemy.Name != null)
                .ToList()
                .ForEach(enemy => BossNames.Add(enemy.Name));
        }


        public static readonly List<BeastMasterFamiliar> BeastMasterFamiliars;
        public static readonly List<XivDbItem> XivDbStatuses;
        public static readonly List<XivDbItem> XivDbActions;
        public static Dictionary<uint, string> BossDictionary;
        public static HashSet<string> BossNames;
        public static HashSet<uint> OmnidirectionalBaseIds;
    }
}
