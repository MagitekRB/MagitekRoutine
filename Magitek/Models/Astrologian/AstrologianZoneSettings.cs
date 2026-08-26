using Magitek.Models.Account;
using PropertyChanged;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace Magitek.Models.Astrologian
{
    [AddINotifyPropertyChangedInterface]
    public class AstrologianZoneSettings : JsonSettings
    {
        public AstrologianZoneSettings() : base(ZoneSettingsPath()) { }

        // This file lived under the Scholar folder until the dispel-settings fix moved it
        // home. Carry an existing file over once so nobody's saved zone settings reset;
        // the old copy is left in place.
        private static string ZoneSettingsPath()
        {
            var newPath = CharacterSettingsDirectory + "/Magitek/Astrologian/AstrologianZoneSettings.json";
            var oldPath = CharacterSettingsDirectory + "/Magitek/Scholar/AstrologianZoneSettings.json";

            if (!File.Exists(newPath) && File.Exists(oldPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                File.Copy(oldPath, newPath);
            }

            return newPath;
        }
        public static AstrologianZoneSettings Instance { get; set; } = new AstrologianZoneSettings();

        [Setting]
        public Dictionary<ushort, ZoneSetting> ZoneSettings { get; set; }
    }
}
