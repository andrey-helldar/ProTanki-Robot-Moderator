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
using System.Windows.Shapes;

namespace ProTanki_Robot_Moderator
{
    /// <summary>
    /// Interaction logic for Authorization.xaml
    /// </summary>
    public partial class Authorization : Window
    {
        public Authorization()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (MainWindow.appId != null)
            {
                tb1.Text = Properties.Resources.OAuth +
                    "?client_id=" + MainWindow.appId +
                    "&redirect_uri=" + Properties.Resources.Redirect +
                    "&display=popup" +
                    "&scope=wall,groups,offline" +
                    "&response_type=token";

                wbAuth.Navigate(Properties.Resources.OAuth +
                    "?client_id=" + MainWindow.appId +
                    "&redirect_uri=" + Properties.Resources.Redirect +
                    "&display=popup" +
                    "&scope=wall,groups,offline" +
                    "&response_type=token");
            }
            else
            {

            }
        }
    }
}