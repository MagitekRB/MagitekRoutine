using Magitek.Models;
using Magitek.Models.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Magitek.Toggles
{
    internal static class SettingsHandler
    {
        // Extract BaseSettings PvP properties (any property starting with "Pvp_" that is a valid toggle type)
        // Only properties in the #region PvP section of BaseSettings.cs should use the Pvp_ prefix
        public static IEnumerable<ToggleProperty> ExtractBaseSettingsPvpProperties()
        {
            var baseSettings = BaseSettings.Instance;
            return baseSettings.GetType()
                .GetProperties()
                .Where(p => p.Name.StartsWith("Pvp_") && TypeIsValidType(p))
                .Select(r => new ToggleProperty { Name = r.Name, Type = r.TypeToSettingType() })
                .ToList()
                .OrderBy(r => r.Name);
        }

        // Check if a property exists in BaseSettings (any property starting with "Pvp_")
        public static bool IsBaseSettingsProperty(string propertyName)
        {
            return propertyName.StartsWith("Pvp_") &&
                   BaseSettings.Instance.GetType().GetProperty(propertyName) != null;
        }

        // Get the appropriate settings instance (BaseSettings for PvP_ properties, otherwise job settings)
        public static object GetSettingsInstanceForProperty(string propertyName, IRoutineSettings jobSettings)
        {
            if (IsBaseSettingsProperty(propertyName))
                return BaseSettings.Instance;
            return jobSettings;
        }
        // Method to set a property on an IRoutineSetting instance (or BaseSettings for PvP_ properties)
        public static void SetPropertyOnSettingsInstance(List<SettingsToggleSetting> settings, IRoutineSettings jobSettingsInstance, bool checkedOn)
        {
            // does the toggle have multiple settings? If so, we should try to iterate through the settings once
            // and change them at the same time

            if (settings.Count > 1)
            {
                foreach (var setting in settings)
                {
                    var targetInstance = GetSettingsInstanceForProperty(setting.Name, jobSettingsInstance);
                    var property = targetInstance.GetType().GetProperty(setting.Name);

                    if (property == null)
                        continue;

                    SetValueOnProperty(property, setting, targetInstance, checkedOn);
                }
            }
            else
            {
                var setting = settings.FirstOrDefault();

                if (setting == null)
                    return;

                var targetInstance = GetSettingsInstanceForProperty(setting.Name, jobSettingsInstance);
                var property = targetInstance.GetType().GetProperty(setting.Name);

                if (property == null)
                    return;

                SetValueOnProperty(property, setting, targetInstance, checkedOn);
            }
        }

        private static void SetValueOnProperty(PropertyInfo property, SettingsToggleSetting setting, object settingsInstance, bool checkedOn)
        {
            switch (setting.Type)
            {
                case SettingType.Boolean:
                    property.SetValue(settingsInstance,
                        checkedOn
                            ? Convert.ChangeType(setting.BoolCheckedValue, property.PropertyType)
                            : Convert.ChangeType(setting.BoolUncheckedValue, property.PropertyType));
                    break;
                case SettingType.Integer:
                    property.SetValue(settingsInstance,
                        checkedOn
                            ? Convert.ChangeType(setting.IntCheckedValue, property.PropertyType)
                            : Convert.ChangeType(setting.IntUncheckedValue, property.PropertyType));
                    break;
                case SettingType.Float:
                    property.SetValue(settingsInstance,
                        checkedOn
                            ? Convert.ChangeType(setting.FloatCheckedValue, property.PropertyType)
                            : Convert.ChangeType(setting.FloatUncheckedValue, property.PropertyType));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static IEnumerable<ToggleProperty> ExtractPropertyNamesAndTypesFromSettingsInstance(IRoutineSettings settings)
        {
            return settings.GetType().GetProperties().Where(TypeIsValidType).Select(r => new ToggleProperty { Name = r.Name, Type = r.TypeToSettingType() }).ToList().OrderBy(r => r.Name);
        }

        private static bool TypeIsValidType(this PropertyInfo property)
        {
            return property.PropertyType == typeof(bool) || property.PropertyType == typeof(int) || property.PropertyType == typeof(float);
        }

        private static SettingType TypeToSettingType(this PropertyInfo property)
        {
            if (property.PropertyType == typeof(bool))
                return SettingType.Boolean;

            if (property.PropertyType == typeof(int))
                return SettingType.Integer;

            if (property.PropertyType == typeof(float))
                return SettingType.Float;

            return SettingType.None;
        }

        public static bool SettingToggleSettingMatchesProperty(SettingsToggleSetting setting, PropertyInfo property, IRoutineSettings jobSettingsInstance)
        {
            // Get the appropriate instance (BaseSettings for PvP_ properties, otherwise job settings)
            var targetInstance = GetSettingsInstanceForProperty(setting.Name, jobSettingsInstance);

            // Get the property from the correct instance
            var targetProperty = targetInstance.GetType().GetProperty(setting.Name);
            if (targetProperty == null)
                return false;

            switch (setting.Type)
            {
                case SettingType.Boolean:
                    var boolValue = (bool)targetProperty.GetValue(targetInstance, null);
                    return setting.BoolCheckedValue == boolValue;

                case SettingType.Integer:
                    var intValue = (int)targetProperty.GetValue(targetInstance, null);
                    return setting.IntCheckedValue == intValue;

                case SettingType.Float:
                    var floatValue = (float)targetProperty.GetValue(targetInstance, null);
                    return setting.FloatCheckedValue == floatValue;

                case SettingType.None:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
