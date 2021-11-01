using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MessageBox = AdonisUI.Controls.MessageBox;
using Path = System.IO.Path;

namespace SatisfactoryServerGUI
{
    /// <summary>
    /// Interaction logic for ServerFolders.xaml
    /// </summary>
    public partial class ServerFolders : UserControl
    {
        private readonly string _rootPath;
        public ServerFolders()
        {
            InitializeComponent();
            _rootPath = Properties.Settings.Default.ServerPath;
        }

        private void btnRootFolder_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(_rootPath))
            {
                Process p = new Process();
                p.StartInfo.FileName = "explorer.exe";
                p.StartInfo.Arguments = _rootPath;
                p.Start();
            }
            else
            {
                MessageBox.Show("Could not find Root Folder", "Folder not found!");
            }
        }

        private void btnServerSaves_Click(object sender, RoutedEventArgs e)
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var savesPath = Path.Combine(appDataFolder, @"FactoryGame\Saved\SaveGames\server");
            if (Directory.Exists(savesPath))
            {
                Process p = new Process();
                p.StartInfo.FileName = "explorer.exe";
                p.StartInfo.Arguments = savesPath;
                p.Start();
            }
            else
            {
                MessageBox.Show("Could not find Saves Folder", "Folder not found!");
            }
        }

        private void btnServerRoot_Click(object sender, RoutedEventArgs e)
        {
            var serverRoot = Path.Combine(_rootPath, "satisfactorydedicatedserver");
            if (Directory.Exists(serverRoot))
            {
                Process p = new Process();
                p.StartInfo.FileName = "explorer.exe";
                p.StartInfo.Arguments = serverRoot;
                p.Start();
            }
            else
            {
                MessageBox.Show("Could not find Server Root Folder", "Folder not found!");
            }
        }

        private void btnServerLogs_Click(object sender, RoutedEventArgs e)
        {
            var logs = Path.Combine(_rootPath, @"satisfactorydedicatedserver\FactoryGame\Saved\Logs");
            if (Directory.Exists(logs))
            {
                Process p = new Process();
                p.StartInfo.FileName = "explorer.exe";
                p.StartInfo.Arguments = logs;
                p.Start();
            }
            else
            {
                MessageBox.Show("Could not find Server Logs Folder", "Folder not found!");
            }
        }
    }
}
