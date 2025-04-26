using ff14bot;
using ff14bot.Helpers;
using ff14bot.Managers;
using Magitek.Utilities;
using Magitek.ViewModels;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Magitek.Toggles
{
    internal static class TogglesManager
    {
        public static void LoadTogglesForCurrentJob()
        {
            Logger.WriteInfo($@"[Toggles] {Core.Me.CurrentJob} - Loading Toggle...");

            var settingsMagitekToggles = GetBasicTogglesForJob;
            var settingsCustomToggles = GetCustomTogglesForJob;
            Logger.WriteInfo($@"[Toggles] {Core.Me.CurrentJob} - Found {(settingsMagitekToggles == null ? 0 : settingsMagitekToggles.Count)} Magitek Toggles and {(settingsCustomToggles == null ? 0 : settingsCustomToggles.Count)} Custom Toggles...");

            var settingsAllToggles = settingsMagitekToggles.Concat(settingsCustomToggles).ToList();

            Logger.WriteInfo($@"[Toggles] {Core.Me.CurrentJob} - Loaded {(settingsAllToggles == null ? 0 : settingsAllToggles.Count)} Toggles...");

            SetToggleHotkeys(settingsAllToggles);
            SetTogglesOnOverlay(settingsAllToggles);
            ResetToggles();
        }

        public static void ResetToggles()
        {
            foreach (var toggle in MainOverlayViewModel.Instance.SettingsToggles)
                toggle.SetToggleState();
        }

        private static void SetTogglesOnOverlay(List<SettingsToggle> settingsToggles)
        {
            if (settingsToggles == null || !settingsToggles.Any())
            {
                MainOverlayViewModel.Instance.SettingsToggles = new ObservableCollection<SettingsToggle>();
                return;
            }

            MainOverlayViewModel.Instance.SettingsToggles =
                Models.Account.BaseSettings.Instance.ActivePvpCombatRoutine
                ? new ObservableCollection<SettingsToggle>(settingsToggles.Where(r => r.ToggleShowOnOverlay && r.IsPvpToggle))
                : new ObservableCollection<SettingsToggle>(settingsToggles.Where(r => r.ToggleShowOnOverlay && !r.IsPvpToggle));
        }

        private static void SetToggleHotkeys(IEnumerable<SettingsToggle> settingsToggles)
        {
            if (settingsToggles == null)
                return;

            // First unregister all existing hotkeys that could conflict
            var allRegisteredHotkeys = ff14bot.Managers.HotkeyManager.RegisteredHotkeys
                .Where(h => h.Name.Contains($@"Magitek{Core.Me.CurrentJob}"))
                .ToList();

            foreach (var hotkey in allRegisteredHotkeys)
            {
                ff14bot.Managers.HotkeyManager.Unregister(hotkey.Name);
                Logger.WriteInfo($@"[Toggles] Unregistered existing hotkey: {hotkey.Name}");
            }

            // Now register each toggle's hotkey
            foreach (var settingsToggle in settingsToggles)
            {
                settingsToggle.RegisterHotkey();
            }

            Logger.WriteInfo($@"[Toggles] Successfully set {settingsToggles.Count()} toggle hotkeys for {Core.Me.CurrentJob}");
        }

        private static List<SettingsToggle> GetCustomTogglesForJob
        {
            get
            {
                var togglesFolder = JsonSettings.CharacterSettingsDirectory + "/Magitek/Toggles/";
                var togglesFile = togglesFolder + Core.Me.CurrentJob + "Toggles.json";

                return !File.Exists(togglesFile)
                    ? new List<SettingsToggle>()
                    : new List<SettingsToggle>(JsonConvert.DeserializeObject<List<SettingsToggle>>(File.ReadAllText(togglesFile)));
            }
        }

        private static List<SettingsToggle> GetBasicTogglesForJob
        {
            get
            {
                if (!Models.Account.BaseSettings.Instance.EnableBaseToggle)
                    return new List<SettingsToggle>();

                var assembly = Assembly.GetExecutingAssembly();
                string toggleFile = "Magitek.Resources.Toggles." + Core.Me.CurrentJob + "Toggles.json";

                if (assembly.GetManifestResourceNames().FirstOrDefault(p => p.ToLowerInvariant() == toggleFile.ToLowerInvariant()) == null)
                    return new List<SettingsToggle>();

                string toggleFileString = "";
                using (var stream = assembly.GetManifestResourceStream(toggleFile))
                using (var reader = new StreamReader(stream))
                {
                    toggleFileString = reader.ReadToEnd();
                }
                List<SettingsToggle> toggleList = new List<SettingsToggle>(JsonConvert.DeserializeObject<List<SettingsToggle>>(toggleFileString));

                return toggleList;
            }
        }
    }
}
