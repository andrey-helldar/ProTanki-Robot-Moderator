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
                            "&scope=wall,groups,friends,offline" +
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

                    string Data =
                     "&access_token=" + JsonGet("access_token") +
                        "&owner_id=" + (string)settings["id"] +
                        "&offset=0" +
                        "&count=100" +
                        "&filter=all";

                    string result = POST(Properties.Resources.API + "wall.get", Data);

                    if (result != null)
                    {
                        JToken res = JObject.Parse(result).SelectToken("response");

                        //  Получаем общее количество записей
                        int count = (int)res[0];

                        // Вычисляем количество шагов для поста
                        int step = 0;

                        if (count <= 100)
                        {
                            step = 1;
                        }
                        else
                        {
                            if (count % 100 == 0)
                                step = count / 100;
                            else
                                step = (count / 100) + 1;
                        }

                        // Устанавливаем значение прогресс бара
                        Task.Factory.StartNew(() => SetProgress(true, count));

                        // Перебираем записи по шагам
                        for (int i = 0; i < step; i++)
                        {
                            Data =
                     "&access_token=" + JsonGet("access_token") +
                                "&owner_id=" + (string)settings["id"] +
                                "&offset=" + (100 * i).ToString() +
                                "&count=100" +
                                "&filter=all";

                            result = POST(Properties.Resources.API + "wall.get", Data);

                            if (result != null)
                            {
                                res = JObject.Parse(result).SelectToken("response");

                                for (int j = 1; j < res.Count(); j++)
                                {
                                    // Если в посте есть комменты - читаем его, иначе нафиг время тратить)))
                                    if (
                                        ((int)res[j]["likes"]["count"] < 10 && (double)res[j]["date"] < 1408831383) ||
                                        ((int)res[j]["likes"]["user_likes"] == 0)
                                        )
                                    {
                                        // Читаем комменты к записи
                                        WallDelete((string)res[j]["id"]);
                                    }

                                    // Изменяем положение прогресс бара
                                    Task.Factory.StartNew(() => SetProgress());
                                }
                            }

                            Thread.Sleep(350);
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
        /// Удаляем коммент, если он не прошел отбор
        /// </summary>
        /// <param name="commentId"></param>
        private void WallDelete(string postId, JToken token = null)
        {
            try
            {
                string Data =
                     "&access_token=" + JsonGet("access_token") +
                     "&owner_id=" + (string)settings["id"] +
                     "&post_id=" + postId;

                JObject response = JObject.Parse(POST(Properties.Resources.API + "wall.delete", Data));

                Thread.Sleep(350);
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
        }


        /// <summary>
        /// Получаем ID группы или сообщества по имени
        /// </summary>
        /// <param name="name">Имя группы или сообщества</param>
        /// <returns>ID</returns>
        private string UserId(string name)
        {
            try
            {
                string Data = "&user_ids=" + name;

                JObject response = JObject.Parse(POST(Properties.Resources.API + "users.get", Data));

                if (response["response"] != null)
                    return (string)response["response"][0]["uid"];
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }

            return null;
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