using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace ListModeClientWPF
{
    /// <summary>
    /// Interaction logic for InputDialog.xaml
    /// </summary>
    public partial class InputDialog : Window
    {
        public string ServerIP = "127.0.0.1";
        public int ServerPort = 5050;
        public InputDialog()
        {
            InitializeComponent();

            buttonEnter.PreviewMouseDown += ButtonEnter_PreviewMouseDown;
        }

        private void ButtonEnter_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ServerIP = textBoxServerIp.Text;
            if (!Int32.TryParse(textBoxServerPort.Text, out ServerPort))
                MessageBox.Show("Enter valid port", "Port parsing error", MessageBoxButton.OK, MessageBoxImage.Error);
            else Close();
        }
    }
}
