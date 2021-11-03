using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Path = System.IO.Path;
using Timer = System.Timers.Timer;
using AdonisUI.Controls;
using MessageBox = AdonisUI.Controls.MessageBox;
using MessageBoxResult = AdonisUI.Controls.MessageBoxResult;
using MessageBoxButton = AdonisUI.Controls.MessageBoxButton;

namespace SatisfactoryServerGUI
{
    /// <summary>
    /// Interaction logic for ServerControls.xaml
    /// </summary>
    public partial class ServerControls : UserControl
    {
        private readonly Timer _timer = new Timer();
        private static Process _proc;
        private int _yCoord = 0;
        private bool _canUpdate;


        public ServerControls()
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(Properties.Settings.Default.ServerPath))
            {
                RootPath = Properties.Settings.Default.ServerPath;
            }
        }

        public string RootPath { get; private set; }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            string startPath = @"C:\";
            if (!string.IsNullOrEmpty(Properties.Settings.Default.ServerPath))
            {
                startPath = Properties.Settings.Default.ServerPath;
                RootPath = startPath;
            }

            var checkPathResult = MessageBox.Show($"Install Path is {startPath}.\nIs this correct?", "Check Server Path", MessageBoxButton.YesNo);

            if (checkPathResult == MessageBoxResult.No)
            {
                var serverPathDialog = new VistaFolderBrowserDialog() { Description = "Choose server path", UseDescriptionForTitle = true};
                serverPathDialog.ShowDialog();
                var filepath = serverPathDialog.SelectedPath;
                Properties.Settings.Default.ServerPath = filepath;
                Properties.Settings.Default.Save();
                RootPath = filepath;
            }
            
            DownloadSteamCmd();
        }

        #region Install/Update
        private void DownloadSteamCmd()
        {
            _canUpdate = false;
            SetEnableBtnState();
            var steamZip = System.IO.Path.Combine(RootPath, "steamcmd.zip");
            var steamExe = System.IO.Path.Combine(RootPath, "steamcmd.exe");
            if (!File.Exists(steamZip) && !File.Exists(steamExe))
            {
                var steamCmdUri = new Uri("http://media.steampowered.com/installer/steamcmd.zip");
                var wc = new WebClient();
                wc.DownloadFileCompleted += SteamCmdOnDownloadFileCompleted;
                wc.DownloadFileAsync(steamCmdUri, System.IO.Path.Combine(RootPath, "steamcmd.zip"));
            }
            else
            {
                DownloadDedicatedServer();
            }
        }

        private void SteamCmdOnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var steamCmdZip = System.IO.Path.Combine(RootPath, "steamcmd.zip");
            ZipFile.ExtractToDirectory(steamCmdZip, RootPath);
            DownloadDedicatedServer();
        }

        private void DownloadDedicatedServer()
        {
            Process p = new Process();
            p.StartInfo.FileName = System.IO.Path.Combine(RootPath, "steamcmd.exe");
            p.StartInfo.Arguments = "+login anonymous +force_install_dir SatisfactoryDedicatedServer +app_update 1690800 validate +exit";
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            _proc = p;
            _timer.Interval = TimeSpan.FromMilliseconds(250).TotalMilliseconds;
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
            MainWindow.Instance.txtStatusBar.Text = "Downloading";
            MainWindow.Instance.imgStatus.Source = LoadBitmapFromResource("resources/Download.png");
        }


        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            GetConsoleOutput();
        }

        private void GetConsoleOutput()
        {
            var steamCmdLog = Path.Combine(RootPath, "steamCMD.log");
            if (_proc.HasExited == true)
            {
                _timer.Stop();
                _canUpdate = true;
                SetEnableBtnState();
                Application.Current.Dispatcher.Invoke(
                    () =>
                    {
                        MainWindow.Instance.txtStatusBar.Text = "Stopped";
                        MainWindow.Instance.imgStatus.Source = LoadBitmapFromResource("resources/Stop.png");
                    });
            }

            var content = Native.ConsoleContentRead.GetContent(_proc.Id, 0, _yCoord, 80);
            if (content == null)
            {
                _canUpdate = true;
                SetEnableBtnState();
                return;
            }
            var sanitized = SanitizeString(content);


            if (sanitized.Length < 5)
            {
                content = Native.ConsoleContentRead.GetContent(_proc.Id, 0, _yCoord + 1, 80);
                sanitized = SanitizeString(content);
                if (sanitized.Length < 5)
                {
                    return;
                }
            }

            var humanDate = DateTime.Now.ToString("dd MMM yyyy HH:mm:ss tt");
            var output = $"{humanDate} {sanitized} \n";

            File.AppendAllText(steamCmdLog,output);
            _yCoord += 1;

            if (sanitized.StartsWith("Steam>", false, CultureInfo.InvariantCulture))
            {
                File.AppendAllText(steamCmdLog, $"{humanDate} Finished!\n");
                _timer.Stop(); //reached end
                _canUpdate = true;
                SetEnableBtnState();

            }

            
            
        }

        private void SetEnableBtnState()
        {
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    btnUpdate.IsEnabled = _canUpdate;
                    btnRestart.IsEnabled = _canUpdate;
                    btnStartStop.IsEnabled = _canUpdate;
                });
        }

        private string SanitizeString(string input)
        {
            var alphanumeric = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            var specialChars = @" []!@#$%^&*()-=_+,./\|<>?;':`~";
            var validChars = $"{alphanumeric}{specialChars}";

            var sb = new StringBuilder();
            foreach (var c in input.Where(c => validChars.Contains(c)))
            {
                sb.Append(c);
            }

            var output = sb.ToString().TrimEnd();
            return output;
        }


        #endregion



        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            var factoryServer = Process.GetProcessesByName("FactoryServer").FirstOrDefault();
            var unrealEngine = Process.GetProcessesByName("UE4Server-Win64-Shipping").FirstOrDefault();
            
            bool factoryEngineStopped = factoryServer == null || factoryServer.HasExited;
            bool unrealStopped = unrealEngine == null || unrealEngine.HasExited;
            

            if (unrealStopped && factoryEngineStopped)
            {
                _canUpdate = false;
                SetEnableBtnState();
                var exePath = Path.Combine(RootPath, @"satisfactorydedicatedserver\FactoryServer.exe");
                if (File.Exists(exePath))
                {
                    
                    Process p = new Process();
                    p.StartInfo.FileName = exePath;
                    p.StartInfo.Arguments = GetArgumentsForConsole();
                    p.StartInfo.UseShellExecute = false;
                    p.Start();

                    MainWindow.Instance.StartTime = DateTime.Now;
                    MainWindow.Instance.UptimeTimer.Start();
                    MainWindow.Instance.txtStatusBar.Text = "Running";
                    MainWindow.Instance.imgStatus.Source = LoadBitmapFromResource("resources/Play.png");
                    _canUpdate = true;
                    SetEnableBtnState();
                }
                else
                {
                    MessageBox.Show("Couldn't find FactoryServer.exe");
                    _canUpdate = true;
                    SetEnableBtnState();
                }
            }
            else
            {
                var result = MessageBox.Show("The server is currently running.\n Are you sure you want to stop the Satisfactory server?", "Stop Server?", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;
                Process.GetProcessesByName("FactoryServer").FirstOrDefault()?.CloseMainWindow();
                Process.GetProcessesByName("FactoryServer").FirstOrDefault()?.Close();
                Process.GetProcessesByName("UE4Server-Win64-Shipping").FirstOrDefault()?.CloseMainWindow();
                Process.GetProcessesByName("UE4Server-Win64-Shipping").FirstOrDefault()?.Close();

                Dispatcher.InvokeAsync(delegate
                {
                    int count = 20;

                    while (count >0)
                    {
                        var factory = Process.GetProcessesByName("FactoryServer").FirstOrDefault();
                        var unreal = Process.GetProcessesByName("UE4Server-Win64-Shipping").FirstOrDefault();
                        bool factoryExited= factory == null || factory.HasExited;
                        bool unrealExited = unreal == null || unreal.HasExited;
                        if (factoryExited && unrealExited)
                        {
                            break;
                        }
                        Thread.Sleep(TimeSpan.FromMilliseconds(250));
                        count--;
                    }
                    Process.GetProcessesByName("FactoryServer").FirstOrDefault()?.Kill(true);
                    Process.GetProcessesByName("UE4Server-Win64-Shipping").FirstOrDefault()?.Kill(true);

                });
                MainWindow.Instance.txtStatusBar.Text = "Stopped";
                MainWindow.Instance.imgStatus.Source = LoadBitmapFromResource("resources/Stop.png");
                _canUpdate = true;
                SetEnableBtnState();
                MainWindow.Instance.UptimeTimer.Stop();
            }
        }

        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to restart the Satisfactory server?", "Restart Server?", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)return;
            _canUpdate = false;
            SetEnableBtnState();
            Process.GetProcessesByName("FactoryServer").FirstOrDefault()?.CloseMainWindow();
            Process.GetProcessesByName("FactoryServer").FirstOrDefault()?.Close();
            Process.GetProcessesByName("UE4Server-Win64-Shipping").FirstOrDefault()?.CloseMainWindow();
            Process.GetProcessesByName("UE4Server-Win64-Shipping").FirstOrDefault()?.Close();

            Dispatcher.InvokeAsync(delegate
            {
                int count = 20;


                while (count > 0)
                {
                    var factory = Process.GetProcessesByName("FactoryServer").FirstOrDefault();
                    var unreal = Process.GetProcessesByName("UE4Server-Win64-Shipping").FirstOrDefault();
                    bool factoryExited = factory == null || factory.HasExited;
                    bool unrealExited = unreal == null || unreal.HasExited;
                    if (factoryExited && unrealExited)
                    {
                        break;
                    }
                    else
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(250));
                        count--;
                    }
                }
                Process.GetProcessesByName("FactoryServer").FirstOrDefault()?.Kill(true);
                Process.GetProcessesByName("UE4Server-Win64-Shipping").FirstOrDefault()?.Kill(true);
                MainWindow.Instance.txtStatusBar.Text = "Stopped";
                MainWindow.Instance.imgStatus.Source = LoadBitmapFromResource("resources/Stop.png");
            });
            MainWindow.Instance.UptimeTimer.Stop();
            MainWindow.Instance.StartTime = DateTime.MinValue;

            var exePath = Path.Combine(RootPath, @"satisfactorydedicatedserver\FactoryServer.exe");
            if (File.Exists(exePath))
            {
                
                Process p = new Process();
                p.StartInfo.FileName = exePath;
                p.StartInfo.Arguments = GetArgumentsForConsole();
                p.StartInfo.UseShellExecute = false;
                p.Start();
                MainWindow.Instance.StartTime = DateTime.Now;
                MainWindow.Instance.UptimeTimer.Start();

                MainWindow.Instance.txtStatusBar.Text = "Running";
                MainWindow.Instance.imgStatus.Source = LoadBitmapFromResource("resources/Play.png");
            }
            else
            {
                MessageBox.Show("Couldn't find FactoryServer.exe");
                btnUpdate.IsEnabled = true;
            }
            _canUpdate = false;
            SetEnableBtnState();
        }

        private string GetArgumentsForConsole()
        {
            string args = String.Empty;
            if (chkPort.IsChecked == true && !string.IsNullOrEmpty(txtPort.Text)) { args += $"-?listen -Port={txtPort.Text}"; }
            if (chkServerQueryPort.IsChecked == true && !string.IsNullOrEmpty(txtServerQueryPort.Text)) { args += $"-ServerQueryPort={txtServerQueryPort.Text} "; }
            if (chkBeaconPort.IsChecked == true && !string.IsNullOrEmpty(txtBeaconPort.Text)) { args += $"-BeaconPort={txtBeaconPort.Text} "; }
            if (chkNoSteam.IsChecked == true) { args += "-nosteam "; }
            if (chkNoGUI.IsChecked == false) { args += "-log "; }
            args += "-unattended";
            return args;
        }

        private void txtServerQueryPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtServerQueryPort.Text = new string(txtServerQueryPort.Text.Where(char.IsDigit).ToArray());
        }

        private void txtPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtPort.Text = new string(txtPort.Text.Where(char.IsDigit).ToArray());
        }

        private void txtBeaconPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtBeaconPort.Text = new string(txtBeaconPort.Text.Where(char.IsDigit).ToArray());
        }

        public static BitmapImage LoadBitmapFromResource(string pathInApplication, Assembly assembly = null)
        {
            if (assembly == null)
            {
                assembly = Assembly.GetCallingAssembly();
            }

            if (pathInApplication[0] == '/')
            {
                pathInApplication = pathInApplication.Substring(1);
            }
            return new BitmapImage(new Uri(@"pack://application:,,,/" + assembly.GetName().Name + ";component/" + pathInApplication, UriKind.Absolute));
        }
    }
}
