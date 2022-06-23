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
        internal static ServerControls Instance { get; private set; }

        public ServerControls()
        {
            InitializeComponent();
            Instance = this;
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

            var dh = new DownloadHelper();
            dh.DownloadServerFiles(RootPath, Models.BetaVersion.Public);
        }


        internal static void SetStatus(string statusText, string resourcePath)
        {
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    MainWindow.Instance.txtStatusBar.Text = statusText;
                    MainWindow.Instance.imgStatus.Source = LoadBitmapFromResource(resourcePath);
                });
        }

        internal static void SetBtnState(bool active)
        {
            Application.Current.Dispatcher.Invoke(
                () =>
                {
                    ServerControls.Instance.btnUpdate.IsEnabled = active;
                    ServerControls.Instance.btnRestart.IsEnabled = active;
                    ServerControls.Instance.btnStartStop.IsEnabled = active;
                });
        }



        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            var factoryServer = Process.GetProcessesByName("FactoryServer").FirstOrDefault();
            var unrealEngine = Process.GetProcessesByName("UE4Server-Win64-Shipping").FirstOrDefault();
            
            bool factoryEngineStopped = factoryServer == null || factoryServer.HasExited;
            bool unrealStopped = unrealEngine == null || unrealEngine.HasExited;
            

            if (unrealStopped && factoryEngineStopped)
            {
                SetBtnState(false);
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
                    SetBtnState(true);
                }
                else
                {
                    MessageBox.Show("Couldn't find FactoryServer.exe");
                    SetBtnState(true);
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
                SetBtnState(true);
                MainWindow.Instance.UptimeTimer.Stop();
            }
        }

        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to restart the Satisfactory server?", "Restart Server?", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)return;
            SetBtnState(false);
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
            SetBtnState(false);
        }

        private string GetArgumentsForConsole()
        {
            string args = string.Empty;
            if (chkPort.IsChecked == true && !string.IsNullOrEmpty(txtPort.Text)) { args += $"-?listen -Port={txtPort.Text}"; }
            if (chkServerQueryPort.IsChecked == true && !string.IsNullOrEmpty(txtServerQueryPort.Text)) { args += $"-ServerQueryPort={txtServerQueryPort.Text} "; }
            if (chkBeaconPort.IsChecked == true && !string.IsNullOrEmpty(txtBeaconPort.Text)) { args += $"-BeaconPort={txtBeaconPort.Text} "; }
            if (chkNoSteam.IsChecked == true) { args += "-nosteam "; }
            if (chkNoGUI.IsChecked == false) { args += "-log "; }
            args += $"-beta {cmbServerVersion.Text} ";
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
