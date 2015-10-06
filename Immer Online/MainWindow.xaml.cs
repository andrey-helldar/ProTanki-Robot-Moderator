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

namespace VK_BOT_Clear_Wall
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private JObject settings;
        private JObject data;

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
            this.Title = Application.Current.GetType().Assembly.GetName().Name + " v" + Application.Current.GetType().Assembly.GetName().Version.ToString();

            // Если нет идентификатора группы - запрещаем запуск бота
            if (settings["access_token"] == null)
            {
                bStartBot.IsEnabled = false;
            }
        }

        public void Authorization()
        {
            try
            {
                string userId = UserId((string)settings["user"]);

                // Запоминаем ID в настройки
                if (userId != null)
                {
                    JsonSet("id", userId);

                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        // Устанавливаем заголовок приложения
                        this.Title = Application.Current.GetType().Assembly.GetName().Name +
                            " v" + Application.Current.GetType().Assembly.GetName().Version.ToString() +
                            " : " + userId;
                    }));


                    // Запрашиваем данные приложения
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Properties.Resources.Author + JsonGet("group"));
                    request.Method = "GET";
                    request.Accept = "application/json";

                    // Получаем идентификатор приложения
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    StringBuilder output = new StringBuilder();
                    output.Append(reader.ReadToEnd());
                    response.Close();

                    JObject data = JObject.Parse(output.ToString());

                    if (data["error"] == null)
                    {
                        // Открываем окно авторизации
                        Process.Start(Properties.Resources.OAuth +
                        "?client_id=" + (string)data.SelectToken("response.app_id") +
                            "&redirect_uri=" + Properties.Resources.ApiRedirect +
                            "&display=page" +
                            "&scope=notify,friends,audio,status,offline" +
                            "&response_type=token");

                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            bStartBot.IsEnabled = true;
                        }));
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
                        tbLog.Text = "Ошибка получения идентификатора!";
                        bStartBot.IsEnabled = false;
                    }));
                }
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
                byte[] sentData = Encoding.GetEncoding("Utf-8").GetBytes(Data + "&v=5.37&https=1");
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
                POST(Properties.Resources.API + "audio.setBroadcast", "&access_token=" + JsonGet("access_token"));
                Thread.Sleep(350);

                POST(Properties.Resources.API + "audio.setBroadcast", "&access_token=" + JsonGet("access_token"));
                Thread.Sleep(350);

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
        private void ImmerOnline()
        {
            try
            {
                string userId = UserId((string)settings["user"]);

                // Запоминаем ID в настройки
                if (userId != null)
                {
                    Task.Factory.StartNew(() => SetStatus());

                    JsonSet("id", userId);

                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        // Устанавливаем заголовок приложения
                        this.Title = Application.Current.GetType().Assembly.GetName().Name +
                            " v" + Application.Current.GetType().Assembly.GetName().Version.ToString() +
                            " : " + userId;
                    }));


                    // Перебираем записи по шагам
                    while (true)
                    {
                        // Получаем список аудио
                        JObject audios = JObject.Parse(POST(Properties.Resources.API + "audio.get", "&access_token=" + JsonGet("access_token") + "&count=100&offset=0&need_user=0"));

                        if (audios["response"] != null)
                        {
                            foreach (JToken audio in (JArray)audios.SelectToken("response.items"))
                            {
                                // set Online
                                POST(Properties.Resources.API + "account.setOnline", "&access_token=" + JsonGet("access_token") + "&voip=0");
                                Thread.Sleep(350);

                                // Слушаем музыку))
                                POST(Properties.Resources.API + "audio.setBroadcast", "&access_token=" + JsonGet("access_token") +
                                    String.Format("&audio={0}_{1}", (string)audio["owner_id"], (string)audio["id"]));

                                // Лог
                                Dispatcher.BeginInvoke(new ThreadStart(delegate
                                {
                                    tbLog.Text += String.Format("{0} - {1}" + Environment.NewLine, (string)audio["artist"], (string)audio["title"]);
                                }));

                                Thread.Sleep((int)audio["duration"] * 1000);
                            }
                        }
                        else
                        {
                            POST(Properties.Resources.API + "audio.setBroadcast", "&access_token=" + JsonGet("access_token"));
                            Thread.Sleep(350);

                            POST(Properties.Resources.API + "audio.setBroadcast", "&access_token=" + JsonGet("access_token"));
                            Thread.Sleep(350);

                            Thread.Sleep(60000);
                        }
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        tbLog.Text = "Ошибка получения идентификатора!";
                        bStartBot.IsEnabled = false;
                    }));
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
            finally
            {
                Task.Factory.StartNew(() => SetStatus("end"));
            }
        }

        


        /// <summary>
        /// Получаем ID группы или сообщества по имени
        /// </summary>
        /// <param name="name">Имя группы или сообщества</param>
        /// <returns>ID</returns>
        private string UserId(string name)
        {
           /* try
            {
                string Data = "&user_ids=" + name;

                JObject response = JObject.Parse(POST(Properties.Resources.API + "users.get", Data));

                if (response["response"] != null)
                    return (string)response["response"][0]["uid"];
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }

            return null;*/
            return "18199754";
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tbToken.Text.Trim() != String.Empty)
                {
                    JsonSet("access_token", tbToken.Text.Trim());

                    Task.Factory.StartNew(() => ImmerOnline());
                }
                else
                {
                    tbLog.Text = "access_token не может быть пустым!";
                }

            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
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
                            break;

                        default:
                            tbStatus.Text = "Работаем...";
                            bStartBot.IsEnabled = false;
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

        private void textLog(Exception ex)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                tbLog.Text = String.Format("{0}\n\n=============================\n\n{1}", ex.Message, ex.StackTrace);
            }));
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            for (int i = 6800; i < 10000; i++)
            {
                try
                {
                    string Data =
                         "&access_token=" + JsonGet("access_token") +
                         "&owner_id=18199754" +
                         "&post_id=" + i.ToString();

                    JObject response = JObject.Parse(POST(Properties.Resources.API + "wall.restore", Data));

                    Thread.Sleep(350);
                }
                catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
            }
        }
    }
}