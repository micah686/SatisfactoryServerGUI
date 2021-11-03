using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;

namespace SatisfactoryServerGUI
{
    /// <summary>
    /// Interaction logic for ConsoleLogs.xaml
    /// </summary>
    public partial class ConsoleLogs : UserControl
    {
        private LogChoice _logChoice = LogChoice.None;
        private string _factoryLogPath = "";
        private string _steamLogPath = "";
        private static FileSystemWatcher _factoryWatcher;
        private static FileSystemWatcher _steamWatcher;


        public ConsoleLogs()
        {
            InitializeComponent();
            AddLogFilters();
            //_timer = new Timer(delegate { Refresh(); }, this, 1000, 1000);

            var rootPath = Properties.Settings.Default.ServerPath;
            if (string.IsNullOrEmpty(rootPath)) { return; }
            _factoryLogPath = Path.Combine(rootPath, @"satisfactorydedicatedserver\FactoryGame\Saved\Logs\FactoryGame.log");
            _steamLogPath = Path.Combine(rootPath, "steamCMD.log");
            StartWatchers();
        }

        private void LogChoiceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboboxItem = (ComboBoxItem)LogChoiceCombo.SelectedItem;
            var value = comboboxItem.Content.ToString();
            if (GridLogSatisfactory == null) return;

            var rootPath = Properties.Settings.Default.ServerPath;
            if (string.IsNullOrEmpty(rootPath)) { return; }
            if (value == "SatisfactoryServer")
            {
                GridLogSatisfactory.Visibility = Visibility.Visible;
                GridLogSteam.Visibility = Visibility.Hidden;
                _logChoice = LogChoice.Satisfactory;
                UpdateFactoryLog();
            }
            else if (value == "SteamCMD")
            {
                GridLogSatisfactory.Visibility = Visibility.Hidden;
                GridLogSteam.Visibility = Visibility.Visible;
                _logChoice = LogChoice.SteamCmd;
                UpdateSteamLog();
            }
        }

        private void ComboFilterLog_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateFactoryLog();
        }

        private void AddLogFilters()
        {
            ComboFilterLog.ItemsSource = Enum.GetValues(typeof(FactoryLogPrefix));
            ComboFilterLog.SelectedIndex = 0;
        }


        private void StartWatchers()
        {
            if (!File.Exists(_factoryLogPath) || !File.Exists(_steamLogPath))
            {
                MessageBox.Show("Failed to start log watcher.\n Couldn't find the Satisfactory or SteamCMD log paths.", "Failed to Read Logs");
                return;
            }
            
            _factoryWatcher = new FileSystemWatcher();
            _factoryWatcher.Path = Path.GetDirectoryName(_factoryLogPath);
            _factoryWatcher.Filter = Path.GetFileName(_factoryLogPath);
            _factoryWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size; //more options
            _factoryWatcher.Changed += FactoryWatcherOnChanged;
            _factoryWatcher.EnableRaisingEvents = true;

            _steamWatcher = new FileSystemWatcher();
            _steamWatcher.Path = Path.GetDirectoryName(_steamLogPath);
            _steamWatcher.Filter = Path.GetFileName(_steamLogPath);
            _steamWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size; //more options
            _steamWatcher.Changed += SteamWatcherOnChanged;
            _steamWatcher.EnableRaisingEvents = true;
        }

        private void SteamWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            UpdateSteamLog();
        }

        private void FactoryWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            UpdateFactoryLog();
        }


        private void UpdateFactoryLog()
        {
            if (_logChoice != LogChoice.Satisfactory) return;
            if (!File.Exists(_factoryLogPath)) return;
            var logData = "";
            using (StreamReader reader = new StreamReader(new FileStream(_factoryLogPath,
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                logData = reader.ReadToEnd();
                logData = FilterFactoryLogs(logData);
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogSatisfactory.Text = logData;
                if (chkScrollEnd.IsChecked == true) { LogSatisfactory.ScrollToEnd(); }
            });
        }

        private void UpdateSteamLog()
        {
            if (_logChoice != LogChoice.SteamCmd) return;
            if (!File.Exists(_steamLogPath)) return;
            var logData = "";
            using (StreamReader reader = new StreamReader(new FileStream(_steamLogPath,
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                logData = reader.ReadToEnd();
            }
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogSteam.Text = logData;
                if (chkScrollEnd.IsChecked == true) { LogSteam.ScrollToEnd(); }
            });
        }

        private string FilterFactoryLogs(string logData)
        {
            FactoryLogPrefix factoryFilter = FactoryLogPrefix.All;
            Application.Current.Dispatcher.Invoke(() =>
            {
                factoryFilter = (FactoryLogPrefix)ComboFilterLog.SelectedItem;
            });
            string filteredLogs = "";
            if (factoryFilter == FactoryLogPrefix.All)
            {
                filteredLogs = logData;
            }
            else
            {
                var lines = logData.Split(Environment.NewLine);
                var filterString = factoryFilter.ToString();
                foreach (var line in lines)
                {
                    if (line.Contains(filterString))
                    {
                        filteredLogs += $"{line}{Environment.NewLine}";
                    }
                }

                filteredLogs = filteredLogs.Trim(Environment.NewLine.ToCharArray());
            }
            return filteredLogs;

        }

        public enum LogChoice
        {
            Satisfactory,
            SteamCmd,
            None
        }

        public enum FactoryLogPrefix
        {
            All,
            LogInit,
            LogPakFile,
            LogMemory,
            LogWindows,
            LogStreaming,
            LogNetCore,
            LogNet,
            LogLoad,
            LogReplicationGraph,
            LogGameState,
            LogGameMode,
            LogNavigation,
            LogGame,
            LogSave,
            LogOnline,
            LogSteamShared,
            LogGenericPlatformMisc,
            LogWorld,
            LogNiagara
        }

        
    }
}
