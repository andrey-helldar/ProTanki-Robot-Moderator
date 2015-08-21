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
        private JObject obj;
        private JObject data;

        private JObject log = new JObject(
            new JProperty("CurrentPost", 0),
            new JProperty("CurrentComment", 0),
            new JProperty("AllPosts", 0),
            new JProperty("AllComments", 0),
            new JProperty("Deleted", 0),
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

            if (File.Exists("data.json"))
            {
                data = JObject.Parse(File.ReadAllText("data.json"));
            }
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
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)); }

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
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)); }
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
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    tbLog.Text = String.Empty;
                }));

                Task.Factory.StartNew(() => SetStatus());
                Task.Factory.StartNew(() => Log(null, null, true));

                string Data =
                    "&owner_id=" + (string)data["id"] +
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

                    // Запоминаем статистику
                    Task.Factory.StartNew(() => Log("AllPosts", count.ToString()));

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
                            "&owner_id=" + (string)data["id"] +
                            "&offset=" + (Convert.ToInt32(Properties.Resources.Count) * i).ToString() +
                            "&count=" + Properties.Resources.Count +
                            "&filter=all";

                        result = POST(Properties.Resources.API + "wall.get", Data);

                        if (result != null)
                        {
                            res = JObject.Parse(result).SelectToken("response");

                            Task.Factory.StartNew(() => Log("AllPosts", count.ToString()));

                            for (int j = 1; j < res.Count(); j++)
                            {
                                // Если в посте есть комменты - читаем его, иначе нафиг время тратить)))
                                if ((int)res[j]["comments"]["count"] > 0)
                                {
                                    // Читаем комменты к записи
                                    WallGetComments((string)res[j]["id"]);
                                }

                                // Изменяем положение прогресс бара
                                Task.Factory.StartNew(() => SetProgress());
                                Task.Factory.StartNew(() => Log("CurrentPost")).Wait();
                            }
                        }

                        Thread.Sleep(500);
                    }
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)); }
            finally
            {
                Task.Factory.StartNew(() => SetStatus("end"));

                // Ждем 1 минуту и повторяем
                Task.Factory.StartNew(() => Timer(30));
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
                    "&owner_id=" + (string)data["id"] +
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

                    // Запоминаем количество комментариев
                    Task.Factory.StartNew(() => Log("AllComments", count.ToString()));

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
                            "&owner_id=" + (string)data["id"] +
                            "&post_id=" + postId +
                            "&offset=" + (Convert.ToInt32(Properties.Resources.Count) * i).ToString() +
                            "&count=" + Properties.Resources.Count +
                            "&need_likes=1" +
                            "&sort=asc" +
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

                            Task.Factory.StartNew(() => Log("CurrentComment"));

                            Thread.Sleep(500);
                        }
                    }
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)); }
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
                     "&owner_id=" + (string)data["id"] +
                     "&comment_id=" + commentId;

                JObject response = JObject.Parse(POST(Properties.Resources.API + "wall.deleteComment", Data));

                if (response["response"] != null)
                {
                    Task.Factory.StartNew(() => Log("Deleted"));
                }
                else
                {
                    Task.Factory.StartNew(() => Log("ErrorDelete"));

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
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)); }
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
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)); }
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
                text = text.ToLower();

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
                    if (text.IndexOf(word) > -1)
                        return true;
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)); }

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
                               tbEndAt.Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                               tbStatus.Text = "Отдыхаем";
                               bStartBot.IsEnabled = true;
                               break;

                           default:
                               tbStartAt.Text = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                               tbStatus.Text = "Работаем...";
                               bStartBot.IsEnabled = false;
                               break;
                       }
                   }
                   catch (Exception ex)
                   {
                       bStartBot.IsEnabled = true;
                       Task.Factory.StartNew(() => textLog(ex));
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
                   catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)); }
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
                       logDeleted.Text = (string)log.SelectToken("Deleted");
                       logErrorDelete.Text = (string)log.SelectToken("ErrorDelete");
                   }));
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)); }
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