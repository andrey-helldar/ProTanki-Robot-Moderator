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
using System.IO;
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
            LoadingData();
        }

        private async void bSaving_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.BeginInvoke(new ThreadStart(delegate
             {
                 try
                 {
                     // Сохраняем ID группы
                     if (tbGroup.Text.Trim().IndexOf("/") == -1)
                         Data.Default.Group = tbGroup.Text.Trim();
                     else
                         Data.Default.Group = tbGroup.Text.Trim().Split('/').Last();

                     Data.Default.Deactivate = (bool)cbDeactivate.IsChecked;
                     Data.Default.Notify = (bool)cbNotify.IsChecked;
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

                     // Сохраняем список слов для удаления
                     if (tbWords.Text.Trim().Length > 0)
                     {
                         JArray array = new JArray();

                         for (int i = 0; i < tbWords.LineCount; i++)
                             if (tbWords.GetLineText(i).Trim().Replace(Environment.NewLine, "").Length > 0)
                                 array.Add(tbWords.GetLineText(i).Trim().Replace(Environment.NewLine, ""));

                         JObject obj = new JObject();
                         obj["words"] = array;

                         Data.Default.WordsDelete = obj.ToString();

                         // Сохраняем бэкап
                         string fileDelete = "delete.bak";
                         if (File.Exists(fileDelete)) File.Delete(fileDelete);
                         File.WriteAllText(fileDelete, obj.ToString());
                     }
                     else
                         Data.Default.WordsDelete = Data.Default.WordsDeleteDefault;

                     // Сохраняем список слов для удаления и БАНА
                     if (tbWordsBan.Text.Trim().Length > 0)
                     {
                         JArray array = new JArray();

                         for (int i = 0; i < tbWordsBan.LineCount; i++)
                             if (tbWordsBan.GetLineText(i).Trim().Replace(Environment.NewLine, "").Length > 0)
                                 array.Add(tbWordsBan.GetLineText(i).Trim().Replace(Environment.NewLine, ""));

                         JObject obj = new JObject();
                         obj["words"] = array;

                         Data.Default.WordsBan = obj.ToString();

                         // Сохраняем бэкап
                         string fileBan = "ban.bak";
                         if (File.Exists(fileBan)) File.Delete(fileBan);
                         File.WriteAllText(fileBan, obj.ToString());
                     }
                     else
                         Data.Default.WordsBan = Data.Default.WordsBanDefault;

                     // Сохраняем данные
                     Data.Default.Save();
                 }
                 catch (Exception) { }
                 finally { this.Close(); }
             }));
        }

        private async void LoadingData()
        {
            await Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                try
                {
                    tbGroup.Text = Data.Default.Group; // Выводим имя группы
                    cbDeactivate.IsChecked = Data.Default.Deactivate; // Деактивация бота
                    cbNotify.IsChecked = Data.Default.Notify; // Уведомлять ли юзера всплывающими сообщениями
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

                    // Загружаем слова для удаления
                    string fileWords = "delete.bak";
                    string bakWords = "";
                    if (File.Exists(fileWords)) bakWords = File.ReadAllText(fileWords).Trim();

                    JArray array = (JArray)JObject.Parse(Data.Default.WordsDelete.Length > 2 ? Data.Default.WordsDelete :
                        (bakWords.Length > 2 ? bakWords : Data.Default.WordsDeleteDefault))["words"];
                    foreach (string word in array) tbWords.Text += word + Environment.NewLine;

                    // Загружаем слова для удаления и БАНА
                    string fileWordsBan = "ban.bak";
                    string bakWordsBan = "";
                    if (File.Exists(fileWordsBan)) bakWordsBan = File.ReadAllText(fileWordsBan).Trim();

                    array = (JArray)JObject.Parse(Data.Default.WordsBan.Length > 2 ? Data.Default.WordsBan : (
                        bakWordsBan.Length > 2 ? bakWordsBan : Data.Default.WordsBanDefault))["words"];
                    foreach (string word in array) tbWordsBan.Text += word + Environment.NewLine;
                }
                catch (Exception ex) { tbWords.Text = String.Format("{0}\n\n{1}", ex.Message, ex.StackTrace); }
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