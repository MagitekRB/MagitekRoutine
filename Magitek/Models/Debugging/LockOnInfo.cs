using Magitek.Commands;
using Magitek.Extensions;
using Magitek.Models.WebResources;
using Magitek.ViewModels;
using PropertyChanged;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Magitek.Models.Debugging
{
    [AddINotifyPropertyChangedInterface]
    public class LockOnInfo
    {
        public LockOnInfo(uint id, string castedByName, ushort zoneId, string zoneName)
        {
            Id = id;
            CastedById = 0; // Default value, will be updated when available
            CastedByName = castedByName;
            TargetedPlayerName = "Unknown"; // Default value
            ZoneId = zoneId;
            ZoneName = zoneName;
            InFightLogicBuilder = "[+] FightLogic LockOn";
        }

        public uint Id { get; set; }
        public uint CastedById { get; set; }
        public string CastedByName { get; set; }
        public string TargetedPlayerName { get; set; }
        public ushort ZoneId { get; set; }
        public string ZoneName { get; set; }
        public string InFightLogicBuilder { get; set; }

        public ICommand AddToFightLogicBuilder => new DelegateCommand<LockOnInfo>(info =>
        {
            if (info == null)
                return;

            // Check for duplicates based on LockOn ID, CastedById, and TargetedPlayerName
            if (Debug.Instance.FightLogicBuilderLockOns.Any(r => r.Id == info.Id &&
                                                             r.CastedById == info.CastedById &&
                                                             r.TargetedPlayerName == info.TargetedPlayerName))
            {
                Debug.Instance.FightLogicBuilderLockOns.Remove(info);
                InFightLogicBuilder = "[+] FightLogic LockOn";
            }
            else
            {
                Debug.Instance.FightLogicBuilderLockOns.Add(info);
                InFightLogicBuilder = "[-] FightLogic LockOn";
            }
        });
    }
}