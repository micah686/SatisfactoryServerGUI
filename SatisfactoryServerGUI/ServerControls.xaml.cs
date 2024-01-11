using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Ookii.Dialogs.Wpf;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Path = System.IO.Path;
using Timer = System.Timers.Timer;
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
        internal static ServerControls Instance { get; private set; }
        public string RootPath { get; private set; }
        private static bool _init = false;
        public ServerControls()
        {
            InitializeComponent();
            Instance = this;
            if (!string.IsNullOrEmpty(Properties.Settings.Default.ServerPath))
            {
                RootPath = Properties.Settings.Default.ServerPath;
            }            
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            if (ServerStateHelper.IsServerRunning() && _init == false)
            {
                MainWindow.Instance.txtStatusBar.Text = "Running";
                MainWindow.Instance.imgStatus.Source = LoadBitmapFromResource("resources/Play.png");
                MainWindow.UptimeTimer.Start();
                _init = true;
            }
        }

        

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
            if (!ServerStateHelper.IsServerRunning())
            {
                StartServer();
            }
            else
            {
                var result = MessageBox.Show("The server is currently running.\n Are you sure you want to stop the Satisfactory server?", "Stop Server?", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;
                ServerStateHelper.StopServer();
                MainWindow.Instance.txtStatusBar.Text = "Stopped";
                MainWindow.Instance.imgStatus.Source = LoadBitmapFromResource("resources/Stop.png");
                SetBtnState(true);
                MainWindow.UptimeTimer.Stop();
            }
        }

        private void btnRestart_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to restart the Satisfactory server?", "Restart Server?", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)return;
            SetBtnState(false);

            ServerStateHelper.StopServer();
            MainWindow.UptimeTimer.Stop();
            MainWindow.StartTime = DateTime.Now;

            StartServer();            
        }


        private void StartServer()
        {
            SetBtnState(false);
            var exePath = Path.Combine(RootPath, @$"satisfactorydedicatedserver\FactoryServer.exe");
            if (File.Exists(exePath))
            {

                Process p = new Process();
                p.StartInfo.FileName = exePath;
                p.StartInfo.Arguments = GetArgumentsForConsole();
                p.StartInfo.UseShellExecute = false;
                p.Start();


                MainWindow.StartTime = DateTime.Now;
                MainWindow.UptimeTimer.Start();
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

        private string GetArgumentsForConsole()
        {
            string args = ServerSettings.OptionsString;
            if (chkNoSteam.IsChecked == true) { args += "-nosteam "; }
            if (chkNoGUI.IsChecked == false) { args += "-log "; }
            args += $"-beta {cmbServerVersion.Text} ";
            args += "-unattended";
            return args;
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
