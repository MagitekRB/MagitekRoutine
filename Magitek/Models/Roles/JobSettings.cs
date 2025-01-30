using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.Roles
{
    [AddINotifyPropertyChangedInterface]
    public abstract class JobSettings : JsonSettings
    {
        protected JobSettings(string path) : base(path) { }

        #region General
        [Setting]
        [DefaultValue(true)]
        public bool UseTTD { get; set; }

        [Setting]
        [DefaultValue(13)]
        public int SaveIfEnemyDyingWithin { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool EnemyIsOmni { get; set; }
        #endregion

        #region pvp
        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseRecuperate { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float Pvp_RecuperateHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UsePurify { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseGuard { get; set; }

        [Setting]
        [DefaultValue(40.0f)]
        public float Pvp_GuardHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_GuardCheck { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_InvulnCheck { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_SprintWithoutTarget { get; set; }
        #endregion
    }
}
