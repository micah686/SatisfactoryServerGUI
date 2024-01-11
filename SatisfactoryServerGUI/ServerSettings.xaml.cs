using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

        public static string OptionsString = string.Empty;

        private void ServerPath_Click(object sender, RoutedEventArgs e)
        {
            var serverPathDialog = new VistaFolderBrowserDialog() { Description = "Choose server path", UseDescriptionForTitle = true };
            serverPathDialog.ShowDialog();
            var filepath = serverPathDialog.SelectedPath;
            Properties.Settings.Default.ServerPath = filepath;
            Properties.Settings.Default.Save();

            MessageBox.Show($"Updated Server Path\nPlease make sure to update the server", "Updated Server Path", MessageBoxButton.OK);

        }

        private void txtServerQueryPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtServerQueryPort.Text = new string(txtServerQueryPort.Text.Where(char.IsDigit).ToArray());
        }

        private void txtBeaconPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtBeaconPort.Text = new string(txtBeaconPort.Text.Where(char.IsDigit).ToArray());
        }

        private void txPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtPort.Text = new string(txtPort.Text.Where(char.IsDigit).ToArray());
        }

        private void chkMultiHome_Checked(object sender, RoutedEventArgs e)
        {
            chkDisablePacketRouting.IsChecked = false;
            chkDisablePacketRouting.IsEnabled = false;
        }

        private void chkMultiHome_Unchecked(object sender, RoutedEventArgs e)
        {
            chkDisablePacketRouting.IsEnabled = true;
        }

        private void txtMultiHome_LostFocus(object sender, RoutedEventArgs e)
        {
            var isIp = CheckIPValid(txtMultiHome.Text);
            if(!isIp)
            {
                txtMultiHome.Text = "127.0.0.1";
            }
        }

        public Boolean CheckIPValid(String strIP)
        {
            //  Split string by ".", check that array length is 4
            string[] arrOctets = strIP.Split('.');
            if (arrOctets.Length != 4)
                return false;

            //Check each substring checking that parses to byte
            byte obyte = 0;
            foreach (string strOctet in arrOctets)
                if (!byte.TryParse(strOctet, out obyte))
                    return false;

            return true;
        }

        private void btnUpdateSettings_Click(object sender, RoutedEventArgs e)
        {
            if (chkServerQueryPort.IsChecked == true && !string.IsNullOrEmpty(txtServerQueryPort.Text)) OptionsString += $"-ServerQueryPort={txtServerQueryPort.Text} ";
            if (chkBeaconPort.IsChecked == true && !string.IsNullOrEmpty(txtBeaconPort.Text)) OptionsString += $"-BeaconPort={txtBeaconPort.Text} ";
            if (chkPort.IsChecked == true && !string.IsNullOrEmpty(txtPort.Text)) OptionsString += $"-Port={txtPort.Text} ";
            if (chkMultiHome.IsChecked == true && !string.IsNullOrEmpty(txtMultiHome.Text)) OptionsString += $"-multihome={txtMultiHome.Text} ";
            if ((bool)chkDisablePacketRouting.IsChecked) OptionsString += "-DisablePacketRouting ";
            if ((bool)chkDisableSeasonal.IsChecked) OptionsString += "-DisableSeasonalEvents ";
        }
    }
}
