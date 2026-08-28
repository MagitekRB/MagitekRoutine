using Magitek.Enumerations;
using System;
using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.Roles
{
    [AddINotifyPropertyChangedInterface]
    public abstract class JobSettings : JsonSettings
    {
        protected JobSettings(string path) : base(path) { }

        protected override void Migrate()
        {
            base.Migrate();

            // EnemyIsOmni was a manual "treat every target as omnidirectional" override, replaced
            // by Positionals now that the omnidirectional flag can be read off the target. Anyone
            // who left it on keeps that behaviour. Clearing it makes this run exactly once, so a
            // later switch back to Auto is not undone on the next load.
#pragma warning disable CS0618
            if (EnemyIsOmni)
            {
                Positionals = PositionalStrategy.Never;
                EnemyIsOmni = false;
                Save();
            }
#pragma warning restore CS0618
        }

        #region General
        [Setting]
        [DefaultValue(true)]
        public bool UseTTD { get; set; }

        [Setting]
        [DefaultValue(13)]
        public int SaveIfEnemyDyingWithin { get; set; }

        [Setting]
        [DefaultValue(PositionalStrategy.Auto)]
        public PositionalStrategy Positionals { get; set; }

        // Legacy property for migration - will be removed in a future version
        [Setting]
        [DefaultValue(false)]
        [Obsolete("Use Positionals instead")]
        public bool EnemyIsOmni { get; set; }

        #endregion

        #region pvp
        // Per-job Utilities (displayed in each job's PVP Utilities section)
        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UsePurify { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_AutoGuardWildfire { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_AutoGuardKuzushi { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseRoleActions { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseRecuperate { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float Pvp_RecuperateHealthPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool Pvp_UseGuard { get; set; }

        [Setting]
        [DefaultValue(40.0f)]
        public float Pvp_GuardHealthPercent { get; set; }

        [Setting]
        [DefaultValue(7)]
        public int Pvp_MaxAlliesTargetingLimit { get; set; }
        #endregion
    }
}
