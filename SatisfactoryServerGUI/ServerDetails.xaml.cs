using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using MessageBox = AdonisUI.Controls.MessageBox;

namespace SatisfactoryServerGUI
{
    /// <summary>
    /// Interaction logic for ServerDetails.xaml
    /// </summary>
    public partial class ServerDetails : UserControl
    {
        private string _savesFolder = "";
        private FileSystemWatcher _saveWatcher = new FileSystemWatcher();

        public ServerDetails()
        {
            InitializeComponent();

            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _savesFolder = Path.Combine(appDataFolder, @"FactoryGame\Saved\SaveGames\server");
            GetIps();
            MainWindow.UptimeTimer.Interval = TimeSpan.FromMilliseconds(100).TotalMilliseconds;
            MainWindow.UptimeTimer.Elapsed += UptimeTimerOnElapsed;
        }

        private void Grid_GotFocus(object sender, RoutedEventArgs e)
        {
            if (ServerStateHelper.IsServerRunning())
            {                
                StartWatchers();
            }
        }

        private void Grid_LostFocus(object sender, RoutedEventArgs e)
        {
            _saveWatcher.Dispose();
        }


        private void GetIps()
        {
            try
            {
                string externalIpString = new WebClient().DownloadString("https://api.ipify.org/").Replace("\\r\\n", "")
                    .Replace("\\n", "").Trim();
                if (!string.IsNullOrEmpty(externalIpString))
                {
                    txtExternIP.Text = externalIpString;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }


            string localIP = "";
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }

            txtInternIP.Text = localIP;
        }


        private void UptimeTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;
            var elapsed = DateTime.Now - MainWindow.StartTime;
            if (elapsed.TotalSeconds < 1) return;
            var totalDays = elapsed.TotalDays;
            var totalYears = Math.Truncate(totalDays / 365);
            var totalMonths = Math.Truncate((totalDays % 365) / 30);
            var remainingDays = Math.Truncate((totalDays % 365) % 30);            

            var formatted = $"{totalYears} years, {totalMonths} months, {remainingDays} days, {elapsed.Hours} hours, {elapsed.Minutes} minutes, {elapsed.Seconds} seconds";

            
            Application.Current.Dispatcher.Invoke(() =>
            {
                txtUptime.Text = formatted;
            });
            
            
        }

        private void StartWatchers()
        {
            if (!Directory.Exists(_savesFolder))
            {
                MessageBox.Show("Failed to start log watcher.\n Couldn't find the Satisfactory Saves folder", "Failed to read Save folder");
                return;
            }

            _saveWatcher.Path = System.IO.Path.GetDirectoryName(_savesFolder);
            _saveWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size; //more options
            _saveWatcher.Changed += SaveWatcherOnChanged;
            _saveWatcher.EnableRaisingEvents = true;

            
            
        }

        private void SaveWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            var humanDate = DateTime.Now.ToString("dd MMM yyyy HH:mm:ss tt");
            Application.Current.Dispatcher.Invoke(() =>
            {
                txtLastSave.Text = humanDate;
            });
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            GetIps();
        }
        

        
    }
}
