using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SatisfactoryServerGUI
{
    /// <summary>
    /// Interaction logic for ServerSettings.xaml
    /// </summary>
    public partial class ServerSettings : UserControl
    {
        public ServerSettings()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var serverPathDialog = new VistaFolderBrowserDialog() { Description = "Choose server path", UseDescriptionForTitle = true };
            serverPathDialog.ShowDialog();
            var filepath = serverPathDialog.SelectedPath;
            Properties.Settings.Default.ServerPath = filepath;
            Properties.Settings.Default.Save();

            MessageBox.Show($"Updated Server Path\nPlease make sure to update the server", "Updated Server Path", MessageBoxButton.OK);

        }
    }
}
