using Magitek.Enumerations;
using PropertyChanged;
using System.ComponentModel;
using System.Configuration;

namespace Magitek.Models.Roles
{
    [AddINotifyPropertyChangedInterface]
    public abstract class MagicDpsSettings : JobSettings
    {
        protected MagicDpsSettings(string path) : base(path) { }

        [Setting]
        [DefaultValue(true)]
        public bool UseLucidDreaming { get; set; }

        [Setting]
        [DefaultValue(60.0f)]
        public float LucidDreamingMinimumManaPercent { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UseStunOrInterrupt { get; set; }

        [Setting]
        [DefaultValue(InterruptStrategy.Never)]
        public InterruptStrategy Strategy { get; set; }

        [Setting]
        [DefaultValue(false)]
        public bool UsePotion { get; set; }

        [Setting]
        [DefaultValue(PotionEnum.None)]
        public PotionEnum PotionTypeAndGradeLevel { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicAddle { get; set; }

        [Setting]
        [DefaultValue(true)]
        public bool FightLogicKnockback { get; set; }
    }
}
