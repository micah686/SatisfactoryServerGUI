using System;
using System.Windows;
using System.Windows.Data;
using AdonisUI;
using AdonisUI.Controls;
using Ookii.Dialogs.Wpf;
using MessageBox = AdonisUI.Controls.MessageBox;
using Timer = System.Timers.Timer;

namespace SatisfactoryServerGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisWindow
    {
        public static MainWindow Instance { get; private set; }
        public Timer UptimeTimer = new Timer();
        public DateTime StartTime;
        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
        }

        private void AdonisWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AdonisUI.ResourceLocator.SetColorScheme(Application.Current.Resources, ResourceLocator.DarkColorScheme);



            if (string.IsNullOrEmpty(Properties.Settings.Default.ServerPath))
            {
                MessageBox.Show("Please choose a path where you want the dedicated server to install to.", "");
                var serverPathDialog = new VistaFolderBrowserDialog() { Description = "Choose server path", UseDescriptionForTitle = true };
                serverPathDialog.ShowDialog();
                var filepath = serverPathDialog.SelectedPath;
                Properties.Settings.Default.ServerPath = filepath;
                Properties.Settings.Default.Save();

                MessageBox.Show($"Path set to:\n{Properties.Settings.Default.ServerPath}", "");
            }
        }
    }

    public class VisibilityConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((Boolean)value)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
