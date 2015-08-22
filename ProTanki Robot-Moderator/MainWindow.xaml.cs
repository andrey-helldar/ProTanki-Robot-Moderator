using System;
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
        private JObject settings;
        private JObject data;

        private JObject log = new JObject(
            new JProperty("CurrentPost", 0),
            new JProperty("CurrentComment", 0),
            new JProperty("AllPosts", 0),
            new JProperty("AllComments", 0),
            new JProperty("Deleted", 0),
            new JProperty("ErrorDelete", 0)
        );

        private bool timer = false;

        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists("settings.json"))
            {
                settings = JObject.Parse(File.ReadAllText("settings.json"));
                tbToken.Text = JsonGet("access_token");
            }
            else
                JsonSet("access_token", "");

            // Устанавливаем заголовок
            this.Title += " v" + Application.Current.GetType().Assembly.GetName().Version.ToString();
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
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
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
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }

            return null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => Authorization());
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                JsonSet("access_token", tbToken.Text);
                File.WriteAllText("settings.json", settings.ToString());
            }
            catch (Exception ex) { File.WriteAllText("errors.log", ex.StackTrace); }
        }

        private void JsonSet(string path, string key)
        {
            try
            {
                if (settings != null)
                {
                    if (settings.SelectToken(path) == null)
                    {
                        JObject jo = (JObject)settings[path];
                        jo.Add(new JProperty(path, key));
                    }
                    else
                        settings[path] = key;
                }
                else
                {
                    settings = new JObject();
                    settings.Add(new JProperty(path, key));
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
        }

        private string JsonGet(string path)
        {
            try { return (string)settings.SelectToken(path); }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
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
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    tbLog.Text = String.Empty;
                }));

                // Подгружаем список слов при каждом запуске бота
                if (File.Exists("data.json"))
                {
                    data = JObject.Parse(File.ReadAllText("data.json"));

                    // Приступаем
                    Task.Factory.StartNew(() => SetStatus());
                    Task.Factory.StartNew(() => Log(null, null, true)).Wait();

                    string Data =
                        "&owner_id=" + (string)settings["id"] +
                        "&offset=0" +
                        "&count=" + ((int)settings["posts"] > 100 ? "100" : (string)settings["posts"]) +
                        "&filter=all";

                    string result = POST(Properties.Resources.API + "wall.get", Data);

                    if (result != null)
                    {
                        JToken res = JObject.Parse(result).SelectToken("response");

                        //  Получаем общее количество записей
                        int count = (int)res[0];

                        // Вычисляем количество шагов для поста
                        int step = 0;

                        if (count < (int)settings["max_posts"])
                        {
                            step = 1;
                        }
                        else
                        {
                            count = (int)settings["max_posts"];

                            if (count % (int)settings["posts"] == 0)
                                step = count / (int)settings["posts"];
                            else
                                step = (count / (int)settings["posts"]) + 1;
                        }

                        // Устанавливаем значение прогресс бара
                        Task.Factory.StartNew(() => SetProgress(true, count));

                        // Запоминаем статистику
                        Task.Factory.StartNew(() => Log("AllPosts", count.ToString())).Wait();

                        // Перебираем записи по шагам
                        for (int i = 0; i < step; i++)
                        {
                            Data =
                                "&owner_id=" + (string)settings["id"] +
                                "&offset=" + ((int)settings["posts"] * i).ToString() +
                                "&count=" + ((int)settings["posts"] > 100 ? "100" : (string)settings["posts"]) +
                                "&filter=all";

                            result = POST(Properties.Resources.API + "wall.get", Data);

                            if (result != null)
                            {
                                res = JObject.Parse(result).SelectToken("response");

                                Task.Factory.StartNew(() => Log("AllPosts", count.ToString())).Wait();

                                for (int j = 1; j < res.Count(); j++)
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

                            Thread.Sleep(350);
                        }
                    }
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
            finally
            {
                Task.Factory.StartNew(() => SetStatus("end"));

                // Ждем 1 минуту и повторяем
                Task.Factory.StartNew(() => Timer(settings["sleep"] == null ? 30 : (int)settings["sleep"]));
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
                    "&owner_id=" + (string)settings["id"] +
                    "&post_id=" + postId +
                    "&offset=0" +
                    "&count=100" +
                    "&need_likes=0" +
                    "&sort=desc" +
                    "&preview_length=0";

                string result = POST(Properties.Resources.API + "wall.getComments", Data);

                if (result != null)
                {
                    if (JObject.Parse(result)["error"] != null)
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            tbLog.Text = String.Format("Code: {0}\nMsg: {1}", (string)JObject.Parse(result)["error_code"], (string)JObject.Parse(result)["error_msg"]);
                        }));
                    }
                    else
                    {
                        JToken res = JObject.Parse(result).SelectToken("response");

                        //  Получаем общее количество комментов
                        int count = (int)res[0];

                        // Запоминаем количество комментариев
                        Task.Factory.StartNew(() => Log("AllComments", (string)res[0])).Wait();

                        // Вычисляем количество шагов для комментов
                        int step = 0;
                        if (count % 100 == 0)
                            step = count / 100;
                        else
                            step = (count / 100) + 1;


                        // Перебираем записи по шагам
                        for (int i = 0; i < step; i++)
                        {
                            Data =
                                "&owner_id=" + (string)settings["id"] +
                                "&post_id=" + postId +
                                "&offset=" + (100 * i).ToString() +
                                "&count=100" +
                                "&need_likes=0" +
                                "&sort=desc" +
                                "&preview_length=0";

                            res = JObject.Parse(result).SelectToken("response");

                            for (int j = 1; j < res.Count(); j++)
                            {
                                // Проверяем наличие слов для бана
                                if (ToDelete((string)res[j]["text"]))
                                {
                                    // Удаляем коммент
                                    WallDeleteComment((string)res[j]["cid"], (JToken)res[j]);
                                }

                                Task.Factory.StartNew(() => Log("CurrentComment")).Wait();
                            }

                            Thread.Sleep(350);
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
        private void WallDeleteComment(string commentId, JToken token = null)
        {
            try
            {
                string Data =
                     "&access_token=" + JsonGet("access_token") +
                     "&owner_id=" + (string)settings["id"] +
                     "&comment_id=" + commentId;

                JObject response = JObject.Parse(POST(Properties.Resources.API + "wall.deleteComment", Data));

                if (response["response"] != null)
                {
                    Task.Factory.StartNew(() => Log("Deleted")).Wait();
                }
                else
                {
                    Task.Factory.StartNew(() => Log("ErrorDelete")).Wait();

                    if (token != null)
                    {
                        // Если директории нет - создаем
                        if (!Directory.Exists("errors"))
                            Directory.CreateDirectory("errors");

                        // Записываем лог о бане
                        File.WriteAllText(@"errors\" + commentId + ".txt", token.ToString());
                    }
                }

                Thread.Sleep(350);
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tbToken.Text.Trim() != String.Empty)
                {
                    JsonSet("access_token", tbToken.Text.Trim());

                    Task.Factory.StartNew(() => WallGet());
                }
                else
                {
                    tbLog.Text = "access_token не может быть пустым!";
                }

            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
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
        private bool ToDelete(string text)
        {
            try
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
                JArray words = (JArray)data["words"];

                foreach (string word in words)
                    if (text.IndexOf(word.ToLower()) > -1 || text.Length < Convert.ToInt16(Properties.Resources.Length))
                        return true;
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

        private void Log(string path = null, string key = null, bool clear = false)
        {
            try
            {
                // Если массив новый - сбрасываем его
                if (clear)
                {
                    log = new JObject(
                        new JProperty("CurrentPost", 0),
                        new JProperty("CurrentComment", 0),
                        new JProperty("AllPosts", 0),
                        new JProperty("AllComments", 0),
                        new JProperty("Deleted", 0),
                        new JProperty("ErrorDelete", 0)
                    );
                }
                else
                {
                    // Записываем логи
                    if (path != null && key == null)
                    {
                        log[path] = (double)log.SelectToken(path) + 1;
                    }
                    else if (path != null && key != null)
                    {
                        switch (path)
                        {
                            case "AllComments": log[path] = (double)log.SelectToken(path) + Convert.ToDouble(key); break;

                            default: log[path] = key; break;
                        }
                    }

                    // Выводим логи на экран
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                   {
                       logAllPosts.Text = (string)log.SelectToken("CurrentPost") + " / " + (string)log.SelectToken("AllPosts");
                       logAllComments.Text = (string)log.SelectToken("CurrentComment") + " / " + (string)log.SelectToken("AllComments");
                       logDeleted.Text = (string)log.SelectToken("Deleted") + " / " + (Math.Round((double)log.SelectToken("Deleted") / (double)log.SelectToken("AllComments"), 2)).ToString();
                       logErrorDelete.Text = (string)log.SelectToken("ErrorDelete") + " / " + (Math.Round((double)log.SelectToken("ErrorDelete") / (double)log.SelectToken("AllComments"), 2)).ToString();
                   }));
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
                    tbDiff.Text = String.Format("00:00:{0}", i.ToString());
                }));

                Thread.Sleep(1000);
            }

            Task.Factory.StartNew(() => WallGet());
        }
    }
}