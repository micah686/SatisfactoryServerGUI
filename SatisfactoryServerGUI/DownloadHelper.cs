using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using Timer = System.Timers.Timer;
using System.Linq;
using System.Globalization;
using System.Windows;

namespace SatisfactoryServerGUI
{
    internal class DownloadHelper
    {
        private readonly Timer _timer = new Timer();
        private static Process _proc;
        private static string _rootPath = string.Empty;
        private static int _yCoord = 0;
        internal void DownloadServerFiles(string rootPath, Models.BetaVersion version)
        {
            _rootPath = rootPath;
            DownloadSteamCmd();
            DownloadDedicatedServer(version);
        }
        
        private void DownloadSteamCmd()
        {
            var filePath = Path.Combine(_rootPath, "steamcmd.exe");
            if (File.Exists(filePath)) return;
            var steamCmdUri = new Uri("http://media.steampowered.com/installer/steamcmd.zip");
            var wc = new WebClient();
            using (var stream = new MemoryStream(wc.DownloadData(steamCmdUri)))
            {
                const int STEAMCMD_LENGTH = 774825;
                if(stream.Length >= STEAMCMD_LENGTH)
                {
                    try
                    {
                        var zipArchive = new ZipArchive(stream);                        
                        zipArchive.Entries[0].ExtractToFile(filePath, false);
                    }
                    catch (Exception)
                    {
                        //TODO:Write better debug here
                        throw;
                    }                                        
                }                
            }
        }

        private void DownloadDedicatedServer(Models.BetaVersion version)
        {
            
            var proc = new Process();
            proc.StartInfo.FileName = Path.Combine(_rootPath, "steamcmd.exe");
            var arguments = $"+login anonymous +force_install_dir SatisfactoryDedicatedServer +app_update 1690800 -beta {version} validate +exit";
            proc.StartInfo.Arguments = arguments;
            proc.StartInfo.CreateNoWindow = true;
            _proc = proc;
            _proc.Start();
            _timer.Interval = TimeSpan.FromMilliseconds(250).TotalMilliseconds;
            _timer.Elapsed += TimerOnElapsed;
            _timer.Start();
            //set state to downloading
            ServerControls.SetStatus("Downloading", "resources/Download.png");

        }

        private void TimerOnElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            GetConsoleOutput();
        }

        private void GetConsoleOutput()
        {
            var steamCmdLog = Path.Combine(_rootPath, "steamCMD.log");
            if (_proc.HasExited)
            {
                _timer.Stop();
                ServerControls.SetStatus("Stopped", "resources/Stop.png");
                ServerControls.SetBtnState(true);
            }
            const int X_COORD = 0;
            const int CONTENT_LENGTH = 80;
            const int NO_LINE_CONTENT = 5;

            var content = Native.ConsoleContentRead.GetContent(_proc.Id, X_COORD, _yCoord, CONTENT_LENGTH);
            if(content == null)
            {
                ServerControls.SetStatus("Stopped", "resources/Stop.png");
                ServerControls.SetBtnState(true);
                return;
            }
            var sanitized = SanitizeString(content);
            if(sanitized.Length < NO_LINE_CONTENT)
            {
                content = Native.ConsoleContentRead.GetContent(_proc.Id, X_COORD, _yCoord + 1, CONTENT_LENGTH);
                sanitized = SanitizeString(content);
                if (sanitized.Length < NO_LINE_CONTENT) return;
            }

            var humanDate = DateTime.Now.ToString("dd MMM yyyy HH:mm:ss tt");
            var output = $"{humanDate} {sanitized} \n";
            File.AppendAllText(steamCmdLog, output);
            _yCoord += 1;

            if (sanitized.StartsWith("Steam>", false, CultureInfo.InvariantCulture))
            {
                File.AppendAllText(steamCmdLog, $"{humanDate} Finished!\n");
                _timer.Stop(); //reached end
                ServerControls.SetStatus("Stopped", "resources/Stop.png");
                ServerControls.SetBtnState(true);
            }
        }

        private static string SanitizeString(string input)
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
