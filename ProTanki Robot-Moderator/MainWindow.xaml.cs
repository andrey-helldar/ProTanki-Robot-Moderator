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
        private JObject log = new JObject(
            new JProperty("CurrentPost", 0),
            new JProperty("CurrentComment", 0),
            new JProperty("AllPosts", 0),
            new JProperty("Deleted", 0),
            new JProperty("ErrorDelete", 0),
            new JProperty("Circles", 0),
            new JProperty("Starting", 0),
            new JProperty("AllComments", 0),
            new JProperty("AllDeleted", 0),
            new JProperty("AllErrorDelete", 0)
        );

        public static string appId { get { return APPId; } }
        private static string APPId = null;

        public static string appSecret { get { return APPSecret; } }
        private static string APPSecret = null;

        private string groupId = null;
        private bool groupAdmin = false;

        private bool timer = false;
        private bool first = true;

        Stopwatch sWatch = new Stopwatch();


        public MainWindow()
        {
            InitializeComponent();

            // Устанавливаем заголовок
            this.Title = Application.Current.GetType().Assembly.GetName().Name +
                " v" + Application.Current.GetType().Assembly.GetName().Version.ToString();

            // Загружаем данные
            Task.Factory.StartNew(() => LoadingData());
        }

        private void LoadingData()
        {
            try
            {
                if (Data.Default.Group != "0")
                {
                    // Отправляем запрос
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Properties.Resources.Author + Data.Default.Group);
                    request.Method = "GET";
                    request.Accept = "application/json";

                    // Получаем идентификатор приложения
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    StringBuilder output = new StringBuilder();
                    output.Append(reader.ReadToEnd());
                    response.Close();

                    // Парсим ответ
                    JObject data = JObject.Parse(output.ToString());

                    if (data["error"] == null)
                    {
                        APPId = (string)data.SelectToken("response.app_id");
                        APPSecret = (string)data.SelectToken("response.app_secret");

                        this.Closing += delegate { APPId = null; };
                        this.Closing += delegate { APPSecret = null; };

                        // Получаем идентификатор группы
                        JObject group = GetGroup(Data.Default.Group);

                        if (group != null)
                        {
                            groupId = (string)group["id"];
                            groupAdmin = (bool)group["admin"];

                            // Устанавливаем заголовок приложения
                            Dispatcher.BeginInvoke(new ThreadStart(delegate
                            {
                                this.Title = Application.Current.GetType().Assembly.GetName().Name +
                                    " v" + Application.Current.GetType().Assembly.GetName().Version.ToString() +
                                    " : " + (string)group["name"];
                            }));

                            // Если нет идентификаторов - запрещаем запуск
                            if (
                                Data.Default.AccessToken == "0" ||
                                Data.Default.Group == "0" ||
                                appId == null ||
                                appSecret == null ||
                                groupId == null
                                )
                            {
                                Dispatcher.BeginInvoke(new ThreadStart(delegate
                                {
                                    tbLog.Text = "Ошибка получения данных!\nПерезапустите приложение либо свяжитесь с разработчиком.";
                                    bStartBot.IsEnabled = false;
                                }));
                            }

                            // Если юзер - не админ
                            if (!groupAdmin)
                            {
                                Dispatcher.BeginInvoke(new ThreadStart(delegate
                                {
                                    tbLog.Text = "Нет прав администратора/модератора группы.";
                                    bStartBot.IsEnabled = false;
                                }));
                            }
                        }
                        else
                        {
                            Dispatcher.BeginInvoke(new ThreadStart(delegate
                            {
                                tbLog.Text = "Ошибка получения id группы!";
                                bStartBot.IsEnabled = false;
                            }));
                        }
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            tbLog.Text = (string)data["error"];
                            bStartBot.IsEnabled = false;
                        }));
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        tbLog.Text = "Не указан идентификатор группы!";
                        bStartBot.IsEnabled = false;
                    }));
                }
            }
            catch (Exception ex)
            {
                Task.Factory.StartNew(() => textLog(ex)).Wait();

                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    tbLog.Text = "Ошибка обработки данных!\nПерезапустите приложение либо свяжитесь с разработчиком.";
                    bStartBot.IsEnabled = false;
                }));
            }
        }

        public string POST(string Url, string Data)
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
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }

            return null;
        }

        private void bAuthorize_Click(object sender, RoutedEventArgs e)
        {
            //Task.Factory.StartNew(() => Authorization());

            Authorization auth = new Authorization();
            auth.ShowInTaskbar = false;
            auth.ShowDialog();
        }

        /// <summary>
        /// Получаем список постов на странице. Перебираем все
        /// </summary>
        /// <returns></returns>
        private void WallGet()
        {
            bool error = false;

            try
            {
                // Приступаем
                Task.Factory.StartNew(() => SetStatus());
                Task.Factory.StartNew(() => Log(null, 0, true)).Wait();

                // Запоминаем ID в настройки
                if (
                    Data.Default.AccessToken != "0" &&
                    appId != null &&
                    appSecret != null &&
                    groupId != null
                    )
                {
                    // Устанавливаем переменную
                    int max_posts = Data.Default.Posts > 100 || Data.Default.Posts == 0 ? 100 : Data.Default.Posts;

                    string data =
                        "&v=" + Properties.Resources.Version +
                        "&https=1" +
                        "&access_token=" + Data.Default.AccessToken +
                        "&owner_id=" + groupId +
                        "&offset=0" +
                        "&count=" + max_posts.ToString() +
                        "&filter=all";

                    string result = POST(Properties.Resources.API + "wall.get", data);
                    Thread.Sleep(350);

                    if (result != null)
                    {
                        JToken res = JObject.Parse(result);

                        if (res["response"] != null)
                        {
                            res = (JToken)res.SelectToken("response");

                            //  Получаем общее количество записей
                            int count = (int)res["count"];

                            // Вычисляем количество шагов для поста
                            int step = 0;

                            if (count <= 100)
                            {
                                step = 1;
                            }
                            else
                            {
                                if (Data.Default.Posts > 0 && !first)
                                    count = Data.Default.Posts;

                                if (count % max_posts == 0)
                                    step = count / max_posts;
                                else
                                    step = (count / max_posts) + 1;
                            }

                            // Устанавливаем значение прогресс бара
                            Task.Factory.StartNew(() => SetProgress(true, count));

                            // Запоминаем статистику
                            Task.Factory.StartNew(() => Log("AllPosts", (double)count)).Wait();

                            // Перебираем записи по шагам
                            for (int i = 0; i < step; i++)
                            {
                                data =
                                    "&v=" + Properties.Resources.Version +
                                    "&https=1" +
                                    "&access_token=" + Data.Default.AccessToken +
                                    "&owner_id=" + groupId +
                                    "&offset=" + (max_posts * i).ToString() +
                                    "&count=" + max_posts.ToString() +
                                    "&filter=all";

                                result = POST(Properties.Resources.API + "wall.get", data);
                                Thread.Sleep(350);

                                if (result != null)
                                {
                                    res = JObject.Parse(result);

                                    if (res["response"] != null)
                                    {
                                        Task.Factory.StartNew(() => Log("AllPosts", (double)count)).Wait();

                                        res = (JToken)res.SelectToken("response.items");

                                        for (int j = 0; j < res.Count(); j++)
                                        {
                                            Task.Factory.StartNew(() => Log("CurrentPost")).Wait();

                                            // Если в посте есть комменты - читаем его, иначе нафиг время тратить)))
                                            if ((int)res[j]["comments"]["count"] > 0)
                                            {
                                                // Читаем комменты к записи
                                                WallGetComments((string)res[j]["id"]);
                                            }

                                            // Изменяем положение прогресс бара
                                            Task.Factory.StartNew(() => SetProgress());
                                        }
                                    }
                                    else
                                    {
                                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                                        {
                                            tbLog.Text = String.Format("Error: {0}\n{1}", (string)res.SelectToken("error.error_code"), (string)res.SelectToken("error.error_msg"));
                                            bStartBot.IsEnabled = false;
                                        }));

                                        error = true;

                                        // Принудительно выходим из цикла
                                        break;
                                    }
                                }
                                else
                                {
                                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                                    {
                                        tbLog.Text = "Ошибка получения данных!";
                                        bStartBot.IsEnabled = false;
                                    }));

                                    error = true;

                                    // Принудительно выходим из цикла
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Dispatcher.BeginInvoke(new ThreadStart(delegate
                            {
                                tbLog.Text = String.Format("Error: {0}\n{1}", (string)res.SelectToken("error.error_code"), (string)res.SelectToken("error.error_msg"));
                                bStartBot.IsEnabled = false;
                            }));

                            error = true;
                        }
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            tbLog.Text = "Ошибка загрузки словаря!";
                            bStartBot.IsEnabled = false;
                        }));

                        error = true;
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        tbLog.Text = "Ошибка получения настроек!\nПерезапустите приложение";
                        bStartBot.IsEnabled = false;
                    }));

                    error = true;
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
            finally
            {
                Task.Factory.StartNew(() => SetStatus("end"));
                Task.Factory.StartNew(() => Log("Circles")).Wait();

                if (!error)
                {
                    first = false;

                    // Выводим статистику в блок
                    try
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            tbLog.Text = "Начало работы: " + (string)log["Starting"] + Environment.NewLine;
                            tbLog.Text += "Общее время работы: " + sWatch.Elapsed.ToString() + Environment.NewLine + Environment.NewLine;

                            tbLog.Text += "Начало цикла: " + tbStartAt.Text + Environment.NewLine;
                            tbLog.Text += "Завершение цикла: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine;
                            tbLog.Text += "Продолжительность цикла: " + tbEndAt.Text + Environment.NewLine;
                            tbLog.Text += "Общее количество циклов: " + (string)log["Circles"] + Environment.NewLine + Environment.NewLine;

                            tbLog.Text += "Постов: " + (string)log["AllPosts"] + Environment.NewLine;
                            tbLog.Text += "Комментариев: " + (string)log["CurrentComment"] + Environment.NewLine;
                            tbLog.Text += "Удалено комментариев: " + String.Format("{0} / {1}%\n", (string)log["Deleted"], (Math.Round(((double)log["Deleted"] / (double)log["CurrentComment"]) * 100, 3)).ToString());
                            tbLog.Text += "Ошибок удаления: " + String.Format("{0} / {1}%", (string)log["ErrorDelete"], (Math.Round(((double)log["ErrorDelete"] / (double)log["CurrentComment"]) * 100, 3)).ToString()) + Environment.NewLine + Environment.NewLine;

                            log["AllComments"] = Math.Round((double)log["AllComments"] + (double)log["CurrentComment"], 0);
                            log["AllDeleted"] = Math.Round((double)log["AllDeleted"] + (double)log["Deleted"], 0);
                            log["AllErrorDelete"] = Math.Round((double)log["AllErrorDelete"] + (double)log["ErrorDelete"], 0);

                            tbLog.Text += "Всего комментариев: " + (string)log["AllComments"] + Environment.NewLine;
                            tbLog.Text += "Всего удалено: " + String.Format("{0} / {1}%\n", (string)log["AllDeleted"], (Math.Round(((double)log["AllDeleted"] / (double)log["AllComments"]) * 100, 3)).ToString());
                            tbLog.Text += "Всего ошибок удаления: " + String.Format("{0} / {1}%", (string)log["AllErrorDelete"], (Math.Round(((double)log["AllErrorDelete"] / (double)log["AllComments"]) * 100, 3)).ToString());
                        }));
                    }
                    catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
                    finally
                    {
                        if (Data.Default.Close)
                            Dispatcher.BeginInvoke(new ThreadStart(delegate
                            {
                                this.Close();
                            }));
                        else
                            // Ждем и повторяем
                            Task.Factory.StartNew(() => Timer(Data.Default.Sleep == 0 ? Data.Default.SleepDefault : Data.Default.Sleep));
                    }
                }
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
                string data =
                    "&v=" + Properties.Resources.Version +
                    "&https=1" +
                    "&access_token=" + Data.Default.AccessToken +
                    "&owner_id=" + groupId +
                    "&post_id=" + postId +
                    "&offset=0" +
                    "&count=100" +
                    "&need_likes=0" +
                    "&sort=desc" +
                    "&preview_length=0";

                string result = POST(Properties.Resources.API + "wall.getComments", data);
                Thread.Sleep(350);

                if (result != null)
                {
                    if (JObject.Parse(result)["error"] != null)
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            tbLog.Text = String.Format("Error: {0}\n{1}", (string)JObject.Parse(result).SelectToken("error.error_code"), (string)JObject.Parse(result).SelectToken("error.error_msg"));
                        }));
                    }
                    else
                    {
                        JToken res = JObject.Parse(result).SelectToken("response");

                        //  Получаем общее количество комментов
                        int count = (int)res["count"];

                        // Вычисляем количество шагов для комментов
                        int step = 0;
                        if (count % 100 == 0)
                            step = count / 100;
                        else
                            step = (count / 100) + 1;

                        // Перебираем записи по шагам
                        for (int i = 0; i < step; i++)
                        {
                            data =
                                "&v=" + Properties.Resources.Version +
                                "&https=1" +
                                "&access_token=" + Data.Default.AccessToken +
                                "&owner_id=" + groupId +
                                "&post_id=" + postId +
                                "&offset=" + (100 * i).ToString() +
                                "&count=100" +
                                "&need_likes=0" +
                                "&sort=desc" +
                                "&preview_length=0";

                            result = POST(Properties.Resources.API + "wall.getComments", data);
                            Thread.Sleep(350);

                            if (result != null)
                            {
                                res = JObject.Parse(result);

                                if (res["response"] != null)
                                {
                                    res = (JToken)res.SelectToken("response.items");

                                    for (int j = 0; j < res.Count(); j++)
                                    {
                                        // Проверяем наличие слов для бана
                                        if (ToDelete((string)res[j]["text"]))
                                        {
                                            // Удаляем коммент
                                            WallDeleteComment((string)res[j]["id"], (JToken)res[j], postId);
                                        }

                                        Task.Factory.StartNew(() => Log("CurrentComment")).Wait();
                                    }
                                }
                                else
                                {
                                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                                    {
                                        tbLog.Text = String.Format("Error: {0}\n{1}", (string)JObject.Parse(result).SelectToken("error.error_code"), (string)JObject.Parse(result).SelectToken("error.error_msg"));
                                        bStartBot.IsEnabled = false;
                                    }));

                                    break;
                                }
                            }
                            else
                            {
                                Dispatcher.BeginInvoke(new ThreadStart(delegate
                                {
                                    tbLog.Text = "Ошибка получения комментариев!";
                                    bStartBot.IsEnabled = false;
                                }));

                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
        }

        /// <summary>
        /// Удаляем коммент, если он не прошел отбор
        /// </summary>
        /// <param name="commentId"></param>
        private void WallDeleteComment(string commentId, JToken token = null, string postId = null)
        {
            try
            {
                string data =
                     "&v=" + Properties.Resources.Version +
                     "&https=1" +
                     "&access_token=" + Data.Default.AccessToken +
                     "&owner_id=" + groupId +
                     "&comment_id=" + commentId;

                JObject response = JObject.Parse(POST(Properties.Resources.API + "wall.deleteComment", data));
                Thread.Sleep(350);

                if (response["response"] != null)
                {
                    Task.Factory.StartNew(() => Log("Deleted")).Wait();

                    if (token != null)
                    {
                        string dir = String.Format(@"success\{0}\", postId);

                        // Если директории нет - создаем
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        // Записываем лог
                        File.WriteAllText(dir + commentId + ".json", token.ToString());
                    }
                }
                else
                {
                    Task.Factory.StartNew(() => Log("ErrorDelete")).Wait();

                    if (token != null)
                    {
                        string dir = String.Format(@"errors\{0}\", postId);

                        // Если директории нет - создаем
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        // Записываем лог
                        File.WriteAllText(dir + commentId + ".json", token.ToString());
                    }
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
        }


        /// <summary>
        /// Получаем ID группы или сообщества по имени
        /// </summary>
        /// <param name="name">Имя группы или сообщества</param>
        /// <returns>ID</returns>
        private JObject GetGroup(string name = null)
        {
            try
            {
                if (name != null)
                {
                    string data =
                         "&https=1" +
                         "&access_token=" + Data.Default.AccessToken +
                         "&group_ids=" + name;

                    JObject response = JObject.Parse(POST(Properties.Resources.API + "groups.getById", data));
                    Thread.Sleep(350);

                    if (response["response"] != null)
                    {
                        return new JObject(
                            new JProperty("id", "-" + (string)response["response"][0]["id"]),
                            new JProperty("name", (string)response["response"][0]["name"]),
                            new JProperty("admin", (int)response["response"][0]["is_admin"] == 1)
                        );
                    }
                    else
                        return (JObject)response["error"];
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }

            return null;
        }

        private void bStartBot_Click(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => Log(null, 0, true, true)).Wait();
            Task.Factory.StartNew(() => WallGet());
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
        private bool ToDelete(string text = "")
        {
            try
            {
                if (text != "")
                {
                    text = text.ToLower().Trim();

                    // Является ли текст цельной ссылкой
                    if (
                        text.IndexOf(" ") == -1 &&
                        text.IndexOf("video") > -1 &&
                        text.Length > 30
                        )
                        return true;

                    // Проверяем текст на продажу
                    JArray words = Data.Default.Words;

                    foreach (string word in words)
                        if (text.IndexOf(word.ToLower()) > -1 || text.Length < Data.Default.Length)
                            return true;
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }

            return false;
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
                               tbStatus.Text = "Отдыхаем";
                               bStartBot.IsEnabled = true;

                               timer = false;
                               break;

                           default:
                               tbStartAt.Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                               tbStatus.Text = "Работаем...";
                               bStartBot.IsEnabled = false;

                               timer = true;
                               Task.Factory.StartNew(() => SetTimer());
                               break;
                       }
                   }
                   catch (Exception ex)
                   {
                       bStartBot.IsEnabled = true;
                       Task.Factory.StartNew(() => textLog(ex)).Wait();
                   }
               }));
        }

        private void SetTimer()
        {
            int i = 0;

            while (timer)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    DateTime dt = new DateTime(1970, 1, 1).AddSeconds(i);
                    tbEndAt.Text = String.Format("0000-00-00 {0}:{1}:{2}", dt.ToString("HH"), dt.ToString("mm"), dt.ToString("ss"));
                    i++;
                }));

                Thread.Sleep(1000);
            }
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
                   catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
               }));
        }

        private void Log(string path = null, double key = 0, bool clear = false, bool time = false)
        {
            try
            {
                if (time)
                {
                    sWatch.Start();

                    log["Starting"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                    log["Circles"] = 0;

                    log["AllComments"] = 0;
                    log["AllDeleted"] = 0;
                    log["AllErrorDelete"] = 0;
                }
                else
                {
                    // Если массив новый - сбрасываем его
                    if (clear)
                    {
                        log["CurrentPost"] = 0;
                        log["CurrentComment"] = 0;
                        log["AllPosts"] = 0;
                        log["Deleted"] = 0;
                        log["ErrorDelete"] = 0;
                    }
                    else
                    {
                        // Записываем логи
                        if (path != null && key == 0)
                        {
                            log[path] = (double)log.SelectToken(path) + 1;
                        }
                        else if (path != null && key != 0)
                        {
                            log[path] = key;
                        }

                        // Выводим логи на экран
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                       {
                           logAllPosts.Text = (string)log.SelectToken("CurrentPost") + " / " + (string)log.SelectToken("AllPosts");
                           logAllComments.Text = (string)log.SelectToken("CurrentComment");
                           logDeleted.Text = String.Format("{0} / {1}%", (string)log["Deleted"], (Math.Round(((double)log["Deleted"] / (double)log["CurrentComment"]) * 100, 3)).ToString());
                           logErrorDelete.Text = String.Format("{0} / {1}%", (string)log["ErrorDelete"], (Math.Round(((double)log["ErrorDelete"] / (double)log["CurrentComment"]) * 100, 3)).ToString());
                       }));
                    }
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
        }

        private void textLog(Exception ex)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
                   {
                       tbLog.Text = String.Format("{0}\n\n=============================\n\n{1}", ex.Message, ex.StackTrace);
                   }));
        }

        private void Timer(int sec = 0)
        {
            for (int i = sec; i >= 0; i--)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    tbDiff.Text = String.Format("{0}", i.ToString());
                }));

                Thread.Sleep(1000);
            }

            Task.Factory.StartNew(() => WallGet());
        }
    }
}