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
using System.Threading.Tasks;

namespace AIRUS_Bot_Moderator
{
    /// <summary>
    /// Interaction logic for Authorization.xaml
    /// </summary>
    public partial class Authorization : Window
    {
        public Authorization()
        {
            InitializeComponent();

            lStatus.Content = "Ожидание";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (MainWindow.appId != null)
            {
                wbAuth.Navigate(Properties.Resources.OAuth +
                    "?client_id=" + MainWindow.appId +
                    "&redirect_uri=" + Properties.Resources.Redirect +
                    "&display=popup" +
                    "&scope=wall,groups,offline" +
                    "&response_type=token");
            }
            else
                lStatus.Content = "Проверьте настройки";
        }

        private void wbAuth_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            lStatus.Content = "Загрузка...";
        }

        private void wbAuth_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            GetToken();
        }

        private async void GetToken()
        {
            await Dispatcher.BeginInvoke(new System.Threading.ThreadStart(delegate
            {
                try
                {
                    string[] param = wbAuth.Source.AbsoluteUri.Split('#')[1].Split('&');

                    foreach (string par in param)
                        if (par.IndexOf("access") > -1)
                        {
                            Data.Default.AccessToken = par.Split('=')[1];
                            Data.Default.Save();
                            this.Close();
                            break;
                        }

                    lStatus.Content = "Готово";
                }
                catch (Exception) { lStatus.Content = "err"; }
            }));
        }
    }
}