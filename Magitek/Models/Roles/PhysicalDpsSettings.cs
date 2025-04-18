﻿using Magitek.Enumerations;
using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.Roles
{
    [AddINotifyPropertyChangedInterface]
    public abstract class PhysicalDpsSettings : JobSettings
    {
        protected PhysicalDpsSettings(string path) : base(path) { }

        [Setting]
        [DefaultValue(false)]
        public bool UsePotion { get; set; }

        [Setting]
        [DefaultValue(PotionEnum.None)]
        public PotionEnum PotionTypeAndGradeLevel { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UsePeloton { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseSecondWind { get; set; }

        [Setting]
        [DefaultValue(50.0f)]
        public float SecondWindHpPercent { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseStunOrInterrupt { get; set; }

        [Setting]
        [DefaultValue(InterruptStrategy.Never)]
        public InterruptStrategy Strategy { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseEnliven { get; set; }

        [Setting]
        [DefaultValue(40.0f)]
        public float EnlivenTpPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseBloodbath { get; set; }

        [Setting]
        [DefaultValue(70.0f)]
        public float BloodbathHpPercent { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseTrueNorth { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool UseFeint { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool ForceArmsLength { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicFeint { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicKnockback { get; set; }
    }
}
