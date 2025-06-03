﻿using Magitek.Commands;
using Magitek.Toggles;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Magitek.ViewModels
{
    [AddINotifyPropertyChangedInterface]
    public class MainOverlayViewModel
    {
        private static MainOverlayViewModel _instance;

        public static MainOverlayViewModel Instance => _instance ?? (_instance = new MainOverlayViewModel());

        public BaseSettings GeneralSettings => BaseSettings.Instance;

        public ObservableCollection<SettingsToggle> SettingsToggles { get; set; }

        public ICommand OpenMainSettingsForm => new DelegateCommand(() =>
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                // Validate window position before showing
                Models.Account.BaseSettings.ValidateSettingsWindowPosition(1000, 700);
                Magitek.Form.Show();
            });
        });

        public ICommand EnablePvpOverlay => new DelegateCommand(() =>
        {
            TogglesManager.LoadTogglesForCurrentJob();
        });
    }
}
