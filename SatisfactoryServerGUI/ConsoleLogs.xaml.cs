using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace SatisfactoryServerGUI
{
    /// <summary>
    /// Interaction logic for ConsoleLogs.xaml
    /// </summary>
    public partial class ConsoleLogs : UserControl
    {
        private readonly Queue<string> _satisfactoryLogQueue = new Queue<string>();
        private readonly Queue<string> _steamLogQueue = new Queue<string>();
        private Timer _timer;
        private bool _synced;
        private LogChoice _logChoice;
        public int MaxLines { get; set; } = 1000;
        public static ConsoleLogs Instance { get; private set; } = null;


        public ConsoleLogs()
        {
            InitializeComponent();
            Instance ??= this;
            AddLogFilters();
            _timer = new Timer(delegate { Refresh(); }, this, 1000, 1000);
            
           
        }

        private void LogChoiceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboboxItem = (ComboBoxItem)LogChoiceCombo.SelectedItem;
            var value = comboboxItem.Content.ToString();
            if (GridLogSatisfactory == null) return;

            if (value == "SatisfactoryServer")
            {
                GridLogSatisfactory.Visibility = Visibility.Visible;
                GridLogSteam.Visibility = Visibility.Hidden;
                _logChoice = LogChoice.Satisfactory;
            }
            else if (value == "SteamCMD")
            {
                GridLogSatisfactory.Visibility = Visibility.Hidden;
                GridLogSteam.Visibility = Visibility.Visible;
                _logChoice = LogChoice.SteamCmd;
            }
        }

        private void AddLogFilters()
        {
            ComboFilterLog.ItemsSource = Enum.GetValues(typeof(FactoryLogPrefix));
        }


        #region LoggingFramework
        private void Refresh()
        {
            if (_logChoice == LogChoice.Satisfactory)
            {
                lock (_satisfactoryLogQueue)
                {
                    if (!_synced)
                    {

                        Dispatcher.Invoke(() =>
                        {
                            LogSatisfactory.Text = FilterLogs();
                            LogSatisfactory.ScrollToEnd();
                        });
                        _synced = true;
                    }
                }
            }
            else//Steam
            {
                lock (_steamLogQueue)
                {
                    if (!_synced)
                    {
                        var sb = new StringBuilder();
                        foreach (var line in _steamLogQueue)
                        {
                            sb.AppendLine(line);
                        }

                        Dispatcher.Invoke(() =>
                        {
                            LogSteam.Text = sb.ToString();
                            LogSteam.ScrollToEnd();
                        });
                        _synced = true;
                    }
                }
            }
        }

        public void Log(string str, LogChoice choice)
        {
            if (choice == LogChoice.Satisfactory)
            {
                lock (_satisfactoryLogQueue)
                {
                    _satisfactoryLogQueue.Enqueue(str);
                    while (_satisfactoryLogQueue.Count > MaxLines)
                    {
                        _satisfactoryLogQueue.Dequeue();
                    }
                    _synced = false;
                }
            }
            else
            {
                lock (_steamLogQueue)
                {
                    _steamLogQueue.Enqueue(str);
                    while (_steamLogQueue.Count > MaxLines)
                    {
                        _steamLogQueue.Dequeue();
                    }
                    _synced = false;
                }
            }
        }

        public void Clear()
        {
            if (_logChoice == LogChoice.Satisfactory)
            {
                lock (_satisfactoryLogQueue)
                {
                    _satisfactoryLogQueue.Clear();
                    _synced = false;
                }
            }
            else
            {
                lock (_steamLogQueue)
                {
                    _steamLogQueue.Clear();
                    _synced = false;
                }
            }
        }


        private string FilterLogs()
        {
            var comboboxItem = (FactoryLogPrefix)ComboFilterLog.SelectedItem;
            var value = comboboxItem.ToString();
            if (value == "All")
            {
                var sb = new StringBuilder();
                foreach (var line in _satisfactoryLogQueue)
                {
                    sb.AppendLine(line);
                }
                return sb.ToString();
            }
            else
            {
                var sb = new StringBuilder();
                foreach (var line in _satisfactoryLogQueue)
                {
                    if (line.Contains(value))
                    {
                        sb.AppendLine(line);
                    }
                }
                return sb.ToString();
            }
        }

        #endregion


        public enum LogChoice
        {
            Satisfactory,
            SteamCmd
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
