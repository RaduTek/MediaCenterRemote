using System;
using System.Collections.Generic;
using System.Linq;
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

namespace MCERemote.Controller.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : PageView
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void PageView_Loaded(object sender, RoutedEventArgs e)
        {
            ReceiverHostTextBox.Text = Properties.Settings.Default.ReceiverHost;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ReceiverHost = ReceiverHostTextBox.Text;
            Properties.Settings.Default.Save();

            Connection.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
