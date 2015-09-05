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

namespace AIRUS_Bot_Moderator
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
                    Data.Default.Likes = (bool)cbLikes.IsChecked;
                    Data.Default.LikesCount = Convert.ToInt16(tbLikesCount.Text.Trim());
                    Data.Default.LikesOld = Convert.ToInt16(tbLikesOld.Text.Trim());

                    if (tbWords.Text.Trim().Length > 0)
                    {
                        JArray array = new JArray();

                        for (int i = 0; i < tbWords.LineCount; i++)
                            if (tbWords.GetLineText(i).Trim().Replace(Environment.NewLine, "").Length > 0)
                                array.Add(tbWords.GetLineText(i).Trim().Replace(Environment.NewLine, ""));

                        JObject obj = new JObject();
                        obj["words"] = array;

                        Data.Default.Words = obj.ToString();
                        Data.Default.Save();
                    }
                }
                catch (Exception) { }
                finally { this.Close(); }
            }));
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
                    tbDeleteDays.IsEnabled = Data.Default.Delete; // Если стоит галка удаления сообщений - активируем поле
                    cbLikes.IsChecked = Data.Default.Likes; // Удалять сообщения по количеству лайков
                    tbLikesCount.Text = Data.Default.LikesCount.ToString(); // Минимальное количество лайков
                    tbLikesOld.Text = Data.Default.LikesOld.ToString(); // Время ожидания лайков
                    // Активируем поля
                    tbLikesCount.IsEnabled = Data.Default.Likes;
                    tbLikesOld.IsEnabled = Data.Default.Likes;

                    // Загружаем слова
                    if (Data.Default.Words.Length > 2)
                    {
                        JArray array = (JArray)JObject.Parse(Data.Default.Words)["words"];

                        foreach (string word in array)
                            tbWords.Text += word + Environment.NewLine;
                    }
                }
                catch (Exception ex)
                {
                    tbWords.Text = String.Format("{0}\n\n{1}", ex.Message, ex.StackTrace);
                }
            }));
        }

        private void cbBan_Click(object sender, RoutedEventArgs e)
        {
            cbBanPeriod.IsEnabled = (bool)cbBan.IsChecked;
        }

        private void cbDelete_Click(object sender, RoutedEventArgs e)
        {
            tbDeleteDays.IsEnabled = (bool)cbDelete.IsChecked;
        }

        private void cbLikes_Click(object sender, RoutedEventArgs e)
        {
            tbLikesCount.IsEnabled = (bool)cbLikes.IsChecked;
            tbLikesOld.IsEnabled = (bool)cbLikes.IsChecked;
        }
    }
}