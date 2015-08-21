﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using System.Net;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace ProTanki_Robot_Moderator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private JObject obj;

        private JObject log = new JObject(
            new JProperty("AllPosts", 0),
            new JProperty("AllComments", 0),
            new JProperty("Teenage", 0),
            new JProperty("Deleted", 0),
            new JProperty("BanPermanent", 0),
            new JProperty("BanMonth", 0),
            new JProperty("BanWeek", 0),
            new JProperty("ErrorDelete", 0)
        );

        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists("settings.data"))
            {
                obj = JObject.Parse(File.ReadAllText("settings.data"));
                tbToken.Text = JsonGet("access_token");
            }
            else
                JsonSet("access_token", "");
        }

        public void Authorization()
        {
            try
            {
                Process.Start(Properties.Resources.OAuth +
                "?client_id=" + Properties.Resources.AppID +
                    "&redirect_uri=" + Properties.Resources.ApiRedirect +
                    "&display=page" +
                    "&scope=wall,groups,friends,offline" +
                    "&response_type=token");
            }
            catch (Exception) { }
        }

        private string POST(string Url, string Data)
        {
            try
            {
                string Out = "";

                WebRequest req = WebRequest.Create(Url);
                req.Method = "POST";
                req.Timeout = 100000;
                req.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                byte[] sentData = Encoding.GetEncoding("Utf-8").GetBytes(Data);
                req.ContentLength = sentData.Length;
                Stream sendStream = req.GetRequestStream();
                sendStream.Write(sentData, 0, sentData.Length);
                sendStream.Close();
                WebResponse res = req.GetResponse();
                Stream ReceiveStream = res.GetResponseStream();
                StreamReader sr = new StreamReader(ReceiveStream, Encoding.UTF8);
                Char[] read = new Char[256];
                int count = sr.Read(read, 0, 256);
                while (count > 0)
                {
                    String str = new String(read, 0, count);
                    Out += str;
                    count = sr.Read(read, 0, 256);
                }

                return Out;
            }
            catch (WebException ex) { return ex.Message; }
            catch (Exception ex) { return ex.Message; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Authorization();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                JsonSet("access_token", tbToken.Text);
                File.WriteAllText("settings.data", obj.ToString());
            }
            catch (Exception ex) { File.WriteAllText("errors.log", ex.StackTrace); }
        }

        private void JsonSet(string path, string key)
        {
            try
            {
                if (obj != null)
                {
                    if (obj.SelectToken(path) == null)
                    {
                        JObject jo = (JObject)obj[path];
                        jo.Add(new JProperty(path, key));
                    }
                    else
                        obj[path] = key;
                }
                else
                {
                    obj = new JObject();
                    obj.Add(new JProperty(path, key));
                }
            }
            catch (Exception ex) { File.WriteAllText(path + "_" + key + ".txt", ex.Message); }
        }

        private string JsonGet(string path)
        {
            try { return (string)obj.SelectToken(path); }
            catch (Exception) { }
            return null;
        }


        /// <summary>
        /// Получаем список постов на странице. Перебираем все
        /// </summary>
        /// <returns></returns>
        private void WallGet()
        {
            try
            {
                Task.Factory.StartNew(() => SetStatus()).Wait();

                Task.Factory.StartNew(() => ToLog("\tStarting")).Wait();
                Task.Factory.StartNew(() => ToLog()).Wait();
                Task.Factory.StartNew(() => ToLog("Group ID: " + Properties.Resources.ID.Remove(0, 1))).Wait();
                Task.Factory.StartNew(() => ToLog()).Wait();

                string Data =
                    "&owner_id=" + Properties.Resources.ID +
                    "&offset=0" +
                    "&count=" + Properties.Resources.Count +
                    "&filter=all";

                string result = POST(Properties.Resources.API + "wall.get", Data);

                if (result != null)
                {
                    JToken res = JObject.Parse(result).SelectToken("response");

                    //  Получаем общее количество записей
                    int count = (int)res[0];

                    // Устанавливаем значение прогресс бара
                    Task.Factory.StartNew(() => SetProgress(true, count));

                    Task.Factory.StartNew(() => ToLog("Всего записей на стене: " + count.ToString())).Wait();

                    // Вычисляем количество шагов для поста
                    int step = 0;
                    if (count % Convert.ToInt32(Properties.Resources.Count) == 0)
                        step = count / Convert.ToInt32(Properties.Resources.Count);
                    else
                        step = (count / Convert.ToInt32(Properties.Resources.Count)) + 1;

                    // Перебираем записи по шагам
                    for (int i = 0; i < step; i++)
                    {
                        Data =
                            "&owner_id=" + Properties.Resources.ID +
                            "&offset=" + (Convert.ToInt32(Properties.Resources.Count) * i).ToString() +
                            "&count=" + Properties.Resources.Count +
                            "&filter=all";

                        result = POST(Properties.Resources.API + "wall.get", Data);

                        if (result != null)
                        {
                            res = JObject.Parse(result).SelectToken("response");

                            for (int j = 1; j < res.Count(); j++)
                            {
                                Task.Factory.StartNew(() => ToLog("Обрабатываем пост #" + (string)res[j]["id"])).Wait();

                                // Если в посте есть комменты - читаем его, иначе нафиг время тратить)))
                                if ((int)res[j]["comments"]["count"] > 0)
                                {
                                    // Если в записи отсутствует стоп-слово, читаем комменты
                                    if (((string)res[j]["text"]).IndexOf(Properties.Resources.StopWord) == -1)
                                    {
                                        // Читаем комменты к записи
                                        WallGetComments((string)res[j]["id"]);
                                    }
                                    else
                                        Task.Factory.StartNew(() => ToLog("\tПост " + (string)res[j]["id"] + " содержит стоп-слово")).Wait();
                                }

                                // Изменяем положение прогресс бара
                                Task.Factory.StartNew(() => SetProgress());
                            }
                        }

                        Thread.Sleep(500);
                    }
                }

                Task.Factory.StartNew(() => ToLog()).Wait();
                Task.Factory.StartNew(() => ToLog("\tStopped")).Wait();
                Task.Factory.StartNew(() => ToLog("\t" + DateTime.UtcNow.ToLongTimeString())).Wait();
            }
            catch (Exception ex) { Task.Factory.StartNew(() => ToLog(ex.Message)).Wait(); }
            finally
            {
                Task.Factory.StartNew(() => SetStatus("end")).Wait();
            }
        }

        /// <summary>
        /// Получаем комменты к конкретной записи и проверяем дату жизни и лайки)))
        /// </summary>
        /// <param name="postId"></param>
        private void WallGetComments(string postId)
        {
            try
            {
                string Data =
                    "&owner_id=" + Properties.Resources.ID +
                    "&post_id=" + postId +
                    "&offset=0" +
                    "&count=" + Properties.Resources.Count +
                    "&need_likes=1" +
                    "&sort=asc" +
                    "&preview_length=0";

                string result = POST(Properties.Resources.API + "wall.getComments", Data);

                if (result != null)
                {
                    JToken res = JObject.Parse(result).SelectToken("response");

                    //  Получаем общее количество комментов
                    int count = (int)res[0];

                    Task.Factory.StartNew(() => ToLog("\tКомментов в посте: " + count.ToString())).Wait();

                    // Вычисляем количество шагов для комментов
                    int step = 0;
                    if (count % Convert.ToInt32(Properties.Resources.Count) == 0)
                        step = count / Convert.ToInt32(Properties.Resources.Count);
                    else
                        step = (count / Convert.ToInt32(Properties.Resources.Count)) + 1;


                    // Перебираем записи по шагам
                    for (int i = 0; i < step; i++)
                    {
                        Data =
                            "&owner_id=" + Properties.Resources.ID +
                            "&post_id=" + postId +
                            "&offset=" + (Convert.ToInt32(Properties.Resources.Count) * i).ToString() +
                            "&count=" + Properties.Resources.Count +
                            "&need_likes=1" +
                            "&sort=asc" +
                            "&preview_length=0";

                        res = JObject.Parse(result).SelectToken("response");

                        for (int j = 1; j < res.Count(); j++)
                        {
                            Task.Factory.StartNew(() => ToLog("\t\tОбрабатываем коммент #" + (string)res[j]["cid"])).Wait();

                            // Проверяем наличие слов для бана
                            double ban = ToBan((string)res[j]["text"]);
                            if (ban > -1)
                            {
                                // Слово найдено - отправляем в бан
                                GroupsBanUser((string)res[j]["from_id"], (JToken)res[j], ban);
                            }
                            else
                            {
                                // Если пост содержит меньше XX лайков - приступаем к его обработке
                                if ((int)res[j]["likes"]["count"] < Convert.ToInt16(Properties.Resources.Likes))
                                {
                                    // Конвертируем дату коммента
                                    DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                                    dt = dt.AddSeconds((double)res[j]["date"]);

                                    // Если коммент больше XX минут - удаляем его
                                    if (
                                        DateTime.UtcNow.Subtract(dt).TotalMinutes > Convert.ToInt16(Properties.Resources.Live) ||
                                        ((string)res[j]["text"]).Length > Convert.ToInt16(Properties.Resources.Length)
                                        )
                                    {
                                        Task.Factory.StartNew(() => ToLog("\t\t\tПодготавливаем удаление #" + (string)res[j]["cid"])).Wait();

                                        // Удаляем коммент
                                        WallDeleteComment((string)res[j]["cid"], (JToken)res[j]);
                                    }
                                    else
                                        Task.Factory.StartNew(() => ToLog("\t\t\tКоммент еще молодой (" + (Math.Round(DateTime.UtcNow.Subtract(dt).TotalMinutes, 0)).ToString() + " минут)")).Wait();
                                }
                                else
                                    Task.Factory.StartNew(() => ToLog("\t\t\tКоммент содержит больше " + Properties.Resources.Likes + " лайков")).Wait();
                            }

                            Thread.Sleep(500);
                        }
                    }
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => ToLog(ex.Message)).Wait(); }
        }

        /// <summary>
        /// Удаляем коммент, если он не прошел отбор
        /// </summary>
        /// <param name="commentId"></param>
        private void WallDeleteComment(string commentId, JToken token = null)
        {
            try
            {
                string Data =
                     "&access_token=" + JsonGet("access_token") +
                     "&owner_id=" + Properties.Resources.ID +
                     "&comment_id=" + commentId;

                JObject response = JObject.Parse(POST(Properties.Resources.API + "wall.deleteComment", Data));

                if (response["response"] != null)
                {
                    if ((int)response.SelectToken("response") == 1)
                        Task.Factory.StartNew(() => ToLog("\t\t\tКомментарий #" + commentId + " удален")).Wait();
                }
                else
                {
                    Task.Factory.StartNew(() => ToLog(String.Format("\t\t\tОшибка: {0}: {1}", (string)response["error"]["error_code"], (string)response["error"]["error_msg"]))).Wait();

                    if (token != null)
                    {
                        // Если директории нет - создаем
                        if (!Directory.Exists("errors"))
                            Directory.CreateDirectory("errors");

                        // Записываем лог о бане
                        File.WriteAllText(@"errors\" + commentId + ".txt", token.ToString());
                    }
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => ToLog(ex.Message)).Wait(); }
        }

        /// <summary>
        /// Добавляем юзера в бан
        /// </summary>
        /// <param name="user_id"></param>
        private void GroupsBanUser(string user_id, JToken token = null, double time = 0)
        {
            try
            {
                // Удаляем комментарий
                WallDeleteComment((string)token["cid"]);

                string Data =
                         "&access_token=" + JsonGet("access_token") +
                         "&group_id=" + Properties.Resources.ID.Remove(0, 1) +
                         "&user_id=" + user_id +
                         "&reason=1" +
                         "&comment=" + Properties.Resources.Reason + " (" + (string)token["text"] + ")" +
                         "&comment_visible=1";

                if (time > 0)
                    Data += "&end_date=" + time;

                JObject response = JObject.Parse(POST(Properties.Resources.API + "groups.banUser", Data));

                if (response["response"] != null)
                {
                    if ((int)response.SelectToken("response") == 1)
                        Task.Factory.StartNew(() => ToLog("\t\t\tПользователь" + user_id + " отправлен в бессрочный отпуск)")).Wait();

                    // Если директории нет - создаем
                    if (!Directory.Exists("bans"))
                        Directory.CreateDirectory("bans");

                    // Записываем лог о бане
                    File.WriteAllText(@"bans\" + user_id + ".txt", token.ToString());
                }
                else
                    Task.Factory.StartNew(() => ToLog(String.Format("\t\t\tОшибка: {0}: {1}", (string)response["error"]["error_code"], (string)response["error"]["error_msg"]))).Wait();
            }
            catch (Exception) { }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                JsonSet("access_token", tbToken.Text.Trim());
                Task.Factory.StartNew(() => WallGet());
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Проверяем текст на запрещенные слова
        /// </summary>
        /// <param name="text"></param>
        /// <returns>
        ///     -1          Юзер чист. Бана нет
        ///     0           Бессрочный бан
        ///     timestamp   дата разблокировки
        /// </returns>
        private double ToBan(string text)
        {
            try
            {
                text = text.ToLower();

                // Сообщения о продаже
                string[] wordsSale = {
                    "акки",
                    "продам",
                    "cTене",
                };

                // "Легкие" маты - неделя бана
                string[] wordProfanityWeek = {
                    "хуй",
                    //"блять",
                    "ебать",
                    "пиздец",
                    "пизда",
                    "сука",
                };

                // "Тяжелые" маты - месяц бана
                string[] wordProfanityMonth = {
                    "иди на х**",
                    "обсосок",
                    "ебучий",
                    "гандон",
                    "гондон"
                };

                // Является ли текст цельной ссылкой
                if (
                    text.IndexOf(" ") == -1 &&
                    text.IndexOf("video") > -1 &&
                    text.Length > 30
                    )
                    return 0;

                // Проверяем текст на продажу
                foreach (string word in wordsSale)
                    if (text.IndexOf(word) > -1)
                        return 0;

                // Проверяем текст на тяжелые маты
                foreach (string word in wordProfanityMonth)
                    if (text.IndexOf(word) > -1)
                        return (double)((DateTime.UtcNow.AddMonths(1)).Subtract(new DateTime(1970, 1, 1)).TotalSeconds);

                // Проверяем текст на легкие маты
                foreach (string word in wordProfanityWeek)
                    if (text.IndexOf(word) > -1)
                        return (double)((DateTime.UtcNow.AddDays(7)).Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
            }
            catch (Exception) { }

            return -1;
        }

        private void SetStatus(string block = "start")
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
               {
                   try
                   {
                       switch (block)
                       {
                           case "end":
                               tbEndAt.Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                               tbStatus.Text = "Отдыхаем";
                               bStartBot.IsEnabled = true;

                               // Вычисляем продолжительность работы
                               tbDiff.Text = DateTime.UtcNow.Subtract(DateTime.Parse(tbStartAt.Text)).ToString("HH:mm:ss");
                               break;

                           default:
                               tbStartAt.Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                               tbStatus.Text = "Работаем...";
                               bStartBot.IsEnabled = false;
                               break;
                       }
                   }
                   catch (Exception)
                   {
                       bStartBot.IsEnabled = true;
                   }
               }));
        }

        private void SetProgress(bool set = false, int value = 100)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
               {
                   try
                   {
                       if (set == true)
                       {
                           pbStatus.Maximum = value;
                           pbStatus.Value = 0;
                       }
                       else
                       {
                           pbStatus.Value += 1;
                       }
                   }
                   catch (Exception) { }
                   finally
                   {
                       tbProgress.Text = String.Format("{0} / {1}", pbStatus.Value.ToString(), pbStatus.Maximum.ToString());
                   }
               }));
        }

        private void Log(string path, string key, bool clear = false)
        {
            try
            {
                // Если массив новый - сбрасываем его
                if (clear)
                {
                    log = new JObject(
                        new JProperty("AllPosts", 0),
                        new JProperty("AllComments", 0),
                        new JProperty("Teenage", 0),
                        new JProperty("Deleted", 0),
                        new JProperty("BanPermanent", 0),
                        new JProperty("BanMonth", 0),
                        new JProperty("BanWeek", 0),
                        new JProperty("ErrorDelete", 0)
                    );
                }

                // Записываем логи
                if (log != null)
                {
                    if (log.SelectToken(path) == null)
                    {
                        JObject jo = (JObject)log[path];
                        jo.Add(new JProperty(path, key));
                    }
                    else
                        log[path] = key;
                }
                else
                {
                    log = new JObject();
                    log.Add(new JProperty(path, key));
                }


                // Выводим логи на экран
                Dispatcher.BeginInvoke(new ThreadStart(delegate
               {
                   logAllPosts.Text = (string)log.SelectToken("AllPosts");
                   logAllComments.Text = (string)log.SelectToken("AllComments");
                   logTeenage.Text = (string)log.SelectToken("Teenage");
                   logDeleted.Text = (string)log.SelectToken("Deleted");
                   logBanPermanent.Text = (string)log.SelectToken("BanPermanent");
                   logBanMonth.Text = (string)log.SelectToken("BanPermanent");
                   logBanWeek.Text = (string)log.SelectToken("BanMonth");
                   logErrorDelete.Text = (string)log.SelectToken("ErrorDelete");
               }));
            }
            catch (Exception) { }
        }
    }
}
