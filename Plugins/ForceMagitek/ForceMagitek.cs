using ff14bot;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Interfaces;
using ff14bot.Managers;
using ff14bot.Enums;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace ForceMagitek
{
    public class ForceMagitek : IBotPlugin
    {
        public string Author => "cheesegoldfish";
        public string Description => "Forces use of Magitek";
        public Version Version => new Version(1, 0, 0);
        public string Name => "ForceMagitek";
        public bool WantButton => true;
        public string want = "Magitek";
        public string ButtonText => "Switch! - " + want;

        public void OnEnabled()
        {
            RoutineManager.PickRoutineFired += delegate (object sender, EventArgs e)
            {
                RoutineManager.PreferedRoutine = "Magitek";
            };
        }

        public void OnButtonPress() { }
        public void OnPulse() { }
        public void OnInitialize() { }
        public void OnShutdown() { }
        public void OnDisabled() { }
        public bool Equals(IBotPlugin other)
        {
            throw new NotImplementedException();
        }
    }
}