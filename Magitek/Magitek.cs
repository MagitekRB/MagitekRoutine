//using Clio.Utilities.Collections;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.NeoProfiles;
using ff14bot.Objects;
using Magitek.Extensions;
using Magitek.Logic;
using Magitek.Toggles;
using Magitek.Utilities;
using Magitek.Utilities.CombatMessages;
using Magitek.Utilities.GamelogManager;
using Magitek.Utilities.Managers;
using Magitek.Utilities.Overlays;
using Magitek.ViewModels;
using Magitek.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using TreeSharp;
using Application = System.Windows.Application;
using BaseSettings = Magitek.Models.Account.BaseSettings;
using Debug = Magitek.ViewModels.Debug;
using Regexp = System.Text.RegularExpressions;

namespace Magitek
{
    public class Magitek : CombatRoutine
    {
        private static SettingsWindow _form;
        private DateTime _pulseLimiter, _saveFormTime;
        private ClassJobType CurrentJob { get; set; }
        private ushort CurrentZone { get; set; }
        private static readonly string VersionPath = Path.Combine(Environment.CurrentDirectory, @"Routines\Magitek\Version.txt");

        public override void Initialize()
        {
            Logger.WriteInfo($"Initializing Version: {File.ReadAllText(VersionPath).Trim()} ...");
            ViewModels.BaseSettings.Instance.RoutineSelectedInUi = RotationManager.CurrentRotation.ToString();
            ViewModels.BaseSettings.Instance.SettingsFirstInitialization = true;
            DispelManager.Reset();
            InterruptsAndStunsManager.Reset();
            TreeRoot.OnStart += OnStart;
            TreeRoot.OnStop += OnStop;
            CurrentZone = WorldManager.ZoneId;
            CurrentJob = Core.Me.CurrentJob;
            GameEvents.OnClassChanged += GameEventsOnOnClassChanged;
            GameEvents.OnLevelUp += GameEventsOnOnLevelUp;
            GameEvents.OnMapChanged += GameEventsOnOnMapChanged;

            HookBehaviors();

            Application.Current.Dispatcher.Invoke(delegate
            {
                _form = new SettingsWindow();
                _form.Closed += (_, _) =>
                {
                    _form = null;
                };
            });

            TogglesManager.LoadTogglesForCurrentJob();
            RegisterOpenerHotkey();
            RegisterResetOpenerHotkey();
            RegisterHoldPvpBurstHotkey();
            CombatMessageManager.RegisterMessageStrategiesForClass(Core.Me.CurrentJob);

            // Start overlays if bot is already running (hot-reload scenario)
            if (TreeRoot.IsRunning)
            {
                OverlayManager.StartMainOverlay();
                OverlayManager.StartCombatMessageOverlay();
                Logger.WriteInfo("[Hot-Reload] Overlays restarted after Initialize()");
            }

            Logger.WriteInfo("Initialized");
        }

        private void GameEventsOnOnLevelUp(object sender, EventArgs e)
        {
            #region Zone switching because events aren't reliable apparently

            if (WorldManager.ZoneId != CurrentZone)
            {
                // Set the current zone
                CurrentZone = WorldManager.ZoneId;

                // Run the shit we need to
                GambitsViewModel.Instance.ApplyGambits();
                OpenersViewModel.Instance.ApplyOpeners();
            }

            #endregion
        }

        private void GameEventsOnOnClassChanged(object sender, EventArgs e)
        {
            #region Job switching because events aren't reliable apparently
            if (CurrentJob != Core.Me.CurrentJob)
            {
                // Set our current job
                CurrentJob = Core.Me.CurrentJob;
                Logger.WriteInfo("Job Changed");

                // Run the shit we need to
                Application.Current.Dispatcher.Invoke(delegate
                {
                    GambitsViewModel.Instance.ApplyGambits();
                    OpenersViewModel.Instance.ApplyOpeners();
                    // First unregister all existing Magitek hotkeys
                    UnregisterAllMagitekHotkeys();
                    // Then load the toggles for the current job
                    TogglesManager.LoadTogglesForCurrentJob();
                    // Register opener hotkey
                    RegisterOpenerHotkey();
                    // Register reset opener hotkey
                    RegisterResetOpenerHotkey();
                    // Register hold PvP burst hotkey
                    RegisterHoldPvpBurstHotkey();
                });

                HookBehaviors();
                DispelManager.Reset();
                InterruptsAndStunsManager.Reset();
                CombatMessageManager.RegisterMessageStrategiesForClass(Core.Me.CurrentJob);
            }
            #endregion

            CustomOpenerLogic.ResetOpener();
        }

        private void GameEventsOnOnMapChanged(object sender, EventArgs e)
        {
            if (WorldManager.ZoneId != CurrentZone)
            {
                // Set the current zone
                CurrentZone = WorldManager.ZoneId;

                Application.Current.Dispatcher.Invoke(delegate
                {
                    GambitsViewModel.Instance.ApplyGambits();
                    OpenersViewModel.Instance.ApplyOpeners();
                    // First unregister all existing Magitek hotkeys
                    UnregisterAllMagitekHotkeys();
                    // Then load the toggles for the current job
                    TogglesManager.LoadTogglesForCurrentJob();
                    // Register opener hotkey
                    RegisterOpenerHotkey();
                    // Register reset opener hotkey
                    RegisterResetOpenerHotkey();
                    // Register hold PvP burst hotkey
                    RegisterHoldPvpBurstHotkey();
                });
            }

            CustomOpenerLogic.ResetOpener();
        }

        public void OnStart(BotBase bot)
        {
            // Reset Zoom Limit based on ZoomHack Setting
            ZoomHack.Toggle();

            OpenerLogic.InOpener = false;
            OpenerLogic.OpenerQueue.Clear();
            SpellQueueLogic.SpellQueue.Clear();
            CustomOpenerLogic.ResetOpener();

            // Apply the gambits we have
            GambitsViewModel.Instance.ApplyGambits();
            OpenersViewModel.Instance.ApplyOpeners();

            // Start overlays (protected by try-catch in overlay components)
            OverlayManager.StartMainOverlay();
            OverlayManager.StartCombatMessageOverlay();

            CombatMessageManager.RegisterMessageStrategiesForClass(Core.Me.CurrentJob);
            HookBehaviors();

            GamelogManager.MessageRecevied += GamelogManagerCountdownRecevied;
        }

        public void OnStop(BotBase bot)
        {
            OverlayManager.StopMainOverlay();
            OverlayManager.StopCombatMessageOverlay();
            TogglesViewModel.Instance.SaveToggles();
            GamelogManagerCountdown.StopCooldown();
        }


        public override CapabilityFlags SupportedCapabilities => CapabilityFlags.All;
        public override string Name => "Magitek";
        public override bool WantButton => true;
        public override float PullRange { get; } = 25;

        public override ClassJobType[] Class
        {
            get
            {
                switch (Core.Me.CurrentJob)
                {
                    case ClassJobType.Arcanist:
                    case ClassJobType.Scholar:
                    case ClassJobType.Summoner:
                    case ClassJobType.Archer:
                    case ClassJobType.Bard:
                    case ClassJobType.Thaumaturge:
                    case ClassJobType.BlackMage:
                    case ClassJobType.Conjurer:
                    case ClassJobType.WhiteMage:
                    case ClassJobType.Lancer:
                    case ClassJobType.Dragoon:
                    case ClassJobType.Gladiator:
                    case ClassJobType.Paladin:
                    case ClassJobType.Pugilist:
                    case ClassJobType.Monk:
                    case ClassJobType.Marauder:
                    case ClassJobType.Warrior:
                    case ClassJobType.Rogue:
                    case ClassJobType.Ninja:
                    case ClassJobType.Astrologian:
                    case ClassJobType.Machinist:
                    case ClassJobType.DarkKnight:
                    case ClassJobType.RedMage:
                    case ClassJobType.Samurai:
                    case ClassJobType.Dancer:
                    case ClassJobType.Gunbreaker:
                    case ClassJobType.BlueMage:
                    case ClassJobType.Reaper:
                    case ClassJobType.Sage:
                    case ClassJobType.Pictomancer:
                    case ClassJobType.Viper:
                        return new[] { Core.Me.CurrentJob };
                    default:
                        return new[] { ClassJobType.Adventurer };
                }
            }
        }

        public override void Pulse()
        {
            Tracking.Update();
            Combat.AdjustCombatTime();
            Combat.AdjustDutyTime();

            Debug.Instance.ActionLock = ActionManager.ActionLock;
            Debug.Instance.ActionQueued = ActionManager.ActionQueued;
            Debug.Instance.InCombatTime = (long)Combat.CombatTime.Elapsed.TotalSeconds;
            Debug.Instance.OutOfCombatTime = (int)Combat.OutOfCombatTime.Elapsed.TotalSeconds;
            Debug.Instance.InCombatMovingTime = (int)Combat.MovingInCombatTime.Elapsed.TotalSeconds;
            Debug.Instance.NotMovingInCombatTime = (int)Combat.NotMovingInCombatTime.Elapsed.TotalSeconds;
            Debug.Instance.DutyTime = (long)Combat.DutyTime.Elapsed.TotalSeconds;
            Debug.Instance.DutyState = Duty.State();
            Debug.Instance.CastingGambit = Casting.CastingGambit;

            if (BaseSettings.Instance.DebugHealingLists)
            {
                Debug.Instance.CastableWithin10 = new ObservableCollection<GameObject>(Group.CastableAlliesWithin10);
                Debug.Instance.CastableWithin15 = new ObservableCollection<GameObject>(Group.CastableAlliesWithin15);
                Debug.Instance.CastableWithin30 = new ObservableCollection<GameObject>(Group.CastableAlliesWithin30);
            }

            if (Core.Me.InCombat)
            {
                Debug.Instance.InCombatTimeLeft = Combat.CombatTotalTimeLeft;
                //Debug.Instance.Enmity = new AsyncObservableCollection<Enmity>(EnmityManager.EnmityList);
            }

            if (Core.Me.HasTarget)
            {
                if (BaseSettings.Instance.DebugEnemyInfo)
                {
                    Debug.Instance.IsBoss = Core.Me.CurrentTarget.IsBoss() ? "True" : "False";
                    Debug.Instance.TargetCombatTimeLeft = Core.Me.CurrentTarget.CombatTimeLeft();
                }
            }

            if (CustomOpenerLogic.ResetOpenerIfNeeded())
            {
                Logger.WriteInfo(@"Reset Openers Because We're Out Of Combat");
            }

            if (WorldManager.InPvP && !BaseSettings.Instance.ActivePvpCombatRoutine)
            {
                Logger.WriteInfo("Entering PVP Zone. Switching to PvP CombatRoutine.");
                BaseSettings.Instance.ActivePvpCombatRoutine = true;
                Application.Current.Dispatcher.Invoke(delegate
                {
                    GambitsViewModel.Instance.ApplyGambits();
                    OpenersViewModel.Instance.ApplyOpeners();
                    TogglesManager.LoadTogglesForCurrentJob();
                });
            }
            else if (!WorldManager.InPvP && BaseSettings.Instance.ActivePvpCombatRoutine)
            {
                Logger.WriteInfo("Leaving PVP Zone. Switching to PvE CombatRoutine.");
                BaseSettings.Instance.ActivePvpCombatRoutine = false;
                Application.Current.Dispatcher.Invoke(delegate
                {
                    GambitsViewModel.Instance.ApplyGambits();
                    OpenersViewModel.Instance.ApplyOpeners();
                    TogglesManager.LoadTogglesForCurrentJob();
                });
            }

            var time = DateTime.Now;
            if (time < _pulseLimiter) return;
            _pulseLimiter = time.AddSeconds(1);

            if (time > _saveFormTime)
            {
                Dispelling.Instance.Save();
                InterruptsAndStuns.Instance.Save();
                BaseSettings.Instance.Save();
                TogglesViewModel.Instance.SaveToggles();
                StunTracker.Save();
                _saveFormTime = time.AddSeconds(60);
            }

            CombatMessageManager.UpdateDisplayedMessage();
        }

        public override void ShutDown()
        {
            // Stop overlays FIRST to prevent WPF resource errors from unloaded assembly
            try
            {
                OverlayManager.StopMainOverlay();
                OverlayManager.StopCombatMessageOverlay();
                Logger.WriteInfo("[Hot-Reload] Overlays stopped during shutdown");
            }
            catch (Exception e)
            {
                Logger.WriteInfo($"[Hot-Reload] Error stopping overlays: {e.Message}");
            }

            TreeRoot.OnStart -= OnStart;
            TreeRoot.OnStop -= OnStop;

            // Unhook all TreeHooks to prevent duplicate instances
            try
            {
                TreeHooks.Instance.RemoveHook("Rest", RestBehavior);
                TreeHooks.Instance.RemoveHook("PreCombatBuff", PreCombatBuffBehavior);
                TreeHooks.Instance.RemoveHook("Pull", PullBehavior);
                TreeHooks.Instance.RemoveHook("Heal", HealBehavior);
                TreeHooks.Instance.RemoveHook("CombatBuff", CombatBuffBehavior);
                TreeHooks.Instance.RemoveHook("Combat", CombatBehavior);
                Logger.WriteInfo("[Hot-Reload] TreeHooks removed during shutdown");
            }
            catch (Exception e)
            {
                Logger.WriteInfo($"[Hot-Reload] Error removing TreeHooks: {e.Message}");
            }

            // Unregister GameEvents to prevent old assembly callbacks
            try
            {
                GameEvents.OnClassChanged -= GameEventsOnOnClassChanged;
                GameEvents.OnLevelUp -= GameEventsOnOnLevelUp;
                GameEvents.OnMapChanged -= GameEventsOnOnMapChanged;
                GamelogManager.MessageRecevied -= GamelogManagerCountdownRecevied;
                Logger.WriteInfo("[Hot-Reload] GameEvents unregistered during shutdown");
            }
            catch (Exception e)
            {
                Logger.WriteInfo($"[Hot-Reload] Error unregistering GameEvents: {e.Message}");
            }

            Dispelling.Instance.Save();
            BaseSettings.Instance.Save();
            InterruptsAndStuns.Instance.Save();
            TogglesViewModel.Instance.SaveToggles();
            StunTracker.Save();

            UnregisterAllMagitekHotkeys();
        }

        private void GamelogManagerCountdownRecevied(object sender, ChatEventArgs e)
        {
            if ((int)e.ChatLogEntry.MessageType == 313 || (int)e.ChatLogEntry.MessageType == 185 || MessageType.SystemMessages.Equals(e.ChatLogEntry.MessageType))
            {
                //Start countdown
                var StartCountdownRegex = new Regexp.Regex(@"(Battle commencing in|Début du combat dans|Noch) ([\d]+) (seconds|secondes|Sekunden bis Kampfbeginn!)!(.*)", Regexp.RegexOptions.Compiled);
                var matchStart = StartCountdownRegex.Match(e.ChatLogEntry.FullLine);
                if (matchStart.Success)
                {
                    var groups = matchStart.Groups;
                    var time = groups[2].ToString() != "" ? int.Parse(groups[2].ToString()) : -1;
                    Logger.WriteInfo($@"Fight starting in {time} seconds");
                    GamelogManagerCountdown.RegisterAndStartCountdown(time);
                }

                //Abort countdown
                var AbortCountdownRegex = new Regexp.Regex(@"(.*)(Countdown canceled by|Le compte à rebours a été interrompu|hat den Countdown abgebrochen)(.*)", Regexp.RegexOptions.Compiled);
                var matchAbort = AbortCountdownRegex.Match(e.ChatLogEntry.FullLine);
                if (matchAbort.Success)
                {
                    Logger.WriteInfo($@"Countdown aborted!");
                    GamelogManagerCountdown.StopCooldown();
                }
            }
        }

        public override void OnButtonPress()
        {
            if (Form.IsVisible)
                return;

            // Validate window position before showing
            Models.Account.BaseSettings.ValidateSettingsWindowPosition(1000, 700);

            Form.Show();

            OverlayManager.StartMainOverlay();
        }

        public static SettingsWindow Form
        {
            get
            {
                if (_form != null) return _form;
                _form = new SettingsWindow();
                _form.Closed += (_, _) =>
                {
                    _form = null;
                };
                return _form;
            }
        }


        #region Behavior Composites

        public void HookBehaviors()
        {
            Logger.Write("Hooking behaviors");
            TreeHooks.Instance.ReplaceHook("Rest", RestBehavior);
            TreeHooks.Instance.ReplaceHook("PreCombatBuff", PreCombatBuffBehavior);
            TreeHooks.Instance.ReplaceHook("Pull", PullBehavior);
            TreeHooks.Instance.ReplaceHook("Heal", HealBehavior);
            TreeHooks.Instance.ReplaceHook("CombatBuff", CombatBuffBehavior);
            TreeHooks.Instance.ReplaceHook("Combat", CombatBehavior);
        }

        public override Composite RestBehavior =>
            new Decorator(new PrioritySelector(new Decorator(_ => WorldManager.InPvP || BaseSettings.Instance.ActivePvpCombatRoutine, new ActionRunCoroutine(_ => RotationManager.Rotation.PvP())),
                new ActionRunCoroutine(_ => RotationManager.Rotation.Rest())));

        public override Composite PreCombatBuffBehavior =>
            new Decorator(new PrioritySelector(new Decorator(_ => WorldManager.InPvP || BaseSettings.Instance.ActivePvpCombatRoutine, new ActionRunCoroutine(_ => RotationManager.Rotation.PvP())),
                new ActionRunCoroutine(_ => RotationManager.Rotation.PreCombatBuff())));

        public override Composite PullBehavior =>
            new Decorator(new PrioritySelector(new Decorator(_ => WorldManager.InPvP || BaseSettings.Instance.ActivePvpCombatRoutine, new ActionRunCoroutine(_ => RotationManager.Rotation.PvP())),
                new ActionRunCoroutine(_ => RotationManager.Rotation.Pull())));

        public override Composite HealBehavior =>
            new Decorator(new PrioritySelector(new Decorator(_ => WorldManager.InPvP || BaseSettings.Instance.ActivePvpCombatRoutine, new ActionRunCoroutine(_ => RotationManager.Rotation.PvP())),
                new ActionRunCoroutine(_ => RotationManager.Rotation.Heal())));

        public override Composite CombatBuffBehavior =>
            new Decorator(new PrioritySelector(new Decorator(_ => WorldManager.InPvP || BaseSettings.Instance.ActivePvpCombatRoutine, new ActionRunCoroutine(_ => RotationManager.Rotation.PvP())),
                new ActionRunCoroutine(_ => RotationManager.Rotation.CombatBuff())));

        public override Composite CombatBehavior =>
            new Decorator(new PrioritySelector(new Decorator(_ => WorldManager.InPvP || BaseSettings.Instance.ActivePvpCombatRoutine, new ActionRunCoroutine(_ => RotationManager.Rotation.PvP())),
                new ActionRunCoroutine(_ => RotationManager.Rotation.Combat())));

        #endregion Behavior Composites

        private void RegisterOpenerHotkey()
        {
            // Unregister first to prevent duplicates
            HotkeyManager.Unregister("MagitekUseOpeners");

            // Check if we have a key set
            if (Models.Account.BaseSettings.Instance.UseOpenersKey == Keys.None &&
                Models.Account.BaseSettings.Instance.UseOpenersModkey == ModifierKeys.None)
                return;

            // Register the hotkey
            HotkeyManager.Register("MagitekUseOpeners",
                Models.Account.BaseSettings.Instance.UseOpenersKey,
                Models.Account.BaseSettings.Instance.UseOpenersModkey,
                r =>
                {
                    // Toggle the UseOpeners setting
                    Models.Account.BaseSettings.Instance.UseOpeners = !Models.Account.BaseSettings.Instance.UseOpeners;
                    Logger.WriteInfo($@"[Hotkey] Toggled UseOpeners to {Models.Account.BaseSettings.Instance.UseOpeners}");
                });

            Logger.WriteInfo($@"[Hotkeys] Registered opener hotkey: {Models.Account.BaseSettings.Instance.UseOpenersModkey} + {Models.Account.BaseSettings.Instance.UseOpenersKey}");
        }

        private void RegisterResetOpenerHotkey()
        {
            // Unregister first to prevent duplicates
            HotkeyManager.Unregister("MagitekResetOpeners");

            // Check if we have a key set
            if (Models.Account.BaseSettings.Instance.ResetOpenersKey == Keys.None &&
                Models.Account.BaseSettings.Instance.ResetOpenersModkey == ModifierKeys.None)
                return;

            // Register the hotkey
            HotkeyManager.Register("MagitekResetOpeners",
                Models.Account.BaseSettings.Instance.ResetOpenersKey,
                Models.Account.BaseSettings.Instance.ResetOpenersModkey,
                r =>
                {
                    // Set the ResetOpeners flag to true
                    Models.Account.BaseSettings.Instance.ResetOpeners = true;
                    Logger.WriteInfo($@"[Hotkey] Reset Openers triggered");
                });

            Logger.WriteInfo($@"[Hotkeys] Registered reset opener hotkey: {Models.Account.BaseSettings.Instance.ResetOpenersModkey} + {Models.Account.BaseSettings.Instance.ResetOpenersKey}");
        }

        public static void RegisterHoldPvpBurstHotkey()
        {
            // Unregister first to prevent duplicates
            HotkeyManager.Unregister("MagitekHoldPvpBurst");

            // Check if we have a key set
            if (Models.Account.BaseSettings.Instance.Pvp_HoldBurstKey == Keys.None &&
                Models.Account.BaseSettings.Instance.Pvp_HoldBurstModkey == ModifierKeys.None)
                return;

            // Register the hotkey
            HotkeyManager.Register("MagitekHoldPvpBurst",
                Models.Account.BaseSettings.Instance.Pvp_HoldBurstKey,
                Models.Account.BaseSettings.Instance.Pvp_HoldBurstModkey,
                r =>
                {
                    // Toggle the Pvp_HoldBurst boolean
                    Models.Account.BaseSettings.Instance.Pvp_HoldBurst = !Models.Account.BaseSettings.Instance.Pvp_HoldBurst;
                    Logger.WriteInfo($@"[Hotkey] Hold PvP Burst: {Models.Account.BaseSettings.Instance.Pvp_HoldBurst}");
                });

            Logger.WriteInfo($@"[Hotkeys] Registered Hold PvP Burst hotkey: {Models.Account.BaseSettings.Instance.Pvp_HoldBurstModkey} + {Models.Account.BaseSettings.Instance.Pvp_HoldBurstKey}");
        }

        private void UnregisterAllMagitekHotkeys()
        {
            var hotkeys = HotkeyManager.RegisteredHotkeys.Select(r => r.Name).Where(r => r.Contains("Magitek")).ToList();

            foreach (var hk in hotkeys)
            {
                HotkeyManager.Unregister(hk);
            }
        }
    }
}
