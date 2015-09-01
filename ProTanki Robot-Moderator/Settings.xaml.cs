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
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ProTanki_Robot_Moderator
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void FormSettings_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => LoadingData());
        }

        private void bSaving_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void FormSettings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Task.Factory.StartNew(() => SavingData());
        }

        private void LoadingData()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                try
                {
                    tbGroup.Text = Data.Default.Group; // Имя группы
                    cbDeactivate.IsChecked = Data.Default.Deactivate; // Деактивация бота
                    tbPosts.Text = Data.Default.Posts.ToString(); // Количество постов
                    tbSleep.Text = Data.Default.Sleep.ToString(); // Ожидание между циклами
                    tbLength.Text = Data.Default.Length.ToString(); // Минимальное количество символов в комментарии
                    cbBan.IsChecked = Data.Default.Ban; // Банить аккаунты
                    cbBanPeriod.SelectedIndex = Data.Default.BanPeriod; // Период бана
                    cbDelete.IsChecked = Data.Default.Delete; // Удалять старые сообщения
                    tbDeleteDays.Text = Data.Default.DeleteDays.ToString(); // Количество дней, по истечении которого сообщение будет считаться старым

                    // Загружаем слова
                    if (Data.Default.Words.Count > 0)
                        foreach (string word in (JArray)Data.Default.Words)
                            tbWords.Text += word + Environment.NewLine;
                }
                catch (Exception) { }
            }));
        }

        private void SavingData()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                try
                {
                    Data.Default.Group = tbGroup.Text.Trim();
                    Data.Default.Deactivate = (bool)cbDeactivate.IsChecked;
                    Data.Default.Posts = Convert.ToInt16(tbPosts.Text.Trim());
                    Data.Default.Sleep = Convert.ToInt16(tbSleep.Text.Trim());
                    Data.Default.Length = Convert.ToInt16(tbLength.Text.Trim());
                    Data.Default.Ban = (bool)cbBan.IsChecked;
                    Data.Default.BanPeriod = cbBanPeriod.SelectedIndex;
                    Data.Default.Delete = (bool)cbDelete.IsChecked;
                    Data.Default.DeleteDays = Convert.ToInt16(tbDeleteDays.Text.Trim());
                }
                catch (Exception) { }
                finally { Data.Default.Save(); }
            }));
        }
    }
}