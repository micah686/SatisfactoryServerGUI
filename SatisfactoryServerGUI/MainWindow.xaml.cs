using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AdonisUI;
using AdonisUI.Controls;
using Ookii.Dialogs.Wpf;

namespace SatisfactoryServerGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            AdonisUI.ResourceLocator.SetColorScheme(Application.Current.Resources, ResourceLocator.DarkColorScheme);

            if (string.IsNullOrEmpty(Properties.Settings.Default.ServerPath))
            {
                var serverPathDialog = new VistaFolderBrowserDialog() { Description = "Choose server path", UseDescriptionForTitle = true};
                serverPathDialog.ShowDialog();
                var filepath = serverPathDialog.SelectedPath;
                Properties.Settings.Default.ServerPath = filepath;
                Properties.Settings.Default.Save();
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
