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
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using Path = System.IO.Path;

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
        }


        public String RootPath { get; private set; }





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
                var serverPathDialog = new VistaFolderBrowserDialog() { Description = "Choose server path", UseDescriptionForTitle = true, SelectedPath = startPath };
                serverPathDialog.ShowDialog();
                var filepath = serverPathDialog.SelectedPath;
                Properties.Settings.Default.ServerPath = filepath;
                Properties.Settings.Default.Save();
                RootPath = filepath;
            }

            DownloadSteamCmd();
        }

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
        }

        
        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            GetConsoleOutput();
        }

        private void GetConsoleOutput()
        {
            if (_proc.HasExited == true)
            {
                _timer.Stop();
                _canUpdate = true;
                SetEnableBtnState();
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
                content = Native.ConsoleContentRead.GetContent(_proc.Id, 0, _yCoord +1, 80);
                sanitized = SanitizeString(content);
                if (sanitized.Length < 5)
                {
                    return;
                }
            }

            Debug.WriteLine($"content: {sanitized}");
            ConsoleLogs.Instance.Log($"content: {sanitized}", ConsoleLogs.LogChoice.SteamCmd);
            _yCoord += 1;

            if (sanitized.StartsWith("Steam>", false, CultureInfo.InvariantCulture))
            {
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

        
    }
}
