using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                Task.Factory.StartNew(() => ToLog("\tStarting")).Wait();
                Task.Factory.StartNew(() => ToLog("========================")).Wait();

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
                            "&offset=" + i.ToString() +
                            "&count=" + Properties.Resources.Count +
                            "&filter=all";

                        result = POST(Properties.Resources.API + "wall.get", Data);

                        if (result != null)
                        {
                            res = JObject.Parse(result).SelectToken("response");

                            for (int j = 1; j < res.Count(); j++)
                            {
                                // Если в посте есть комменты - читаем его, иначе нафиг время тратить)))
                                if ((int)res[j]["comments"]["count"] > 0)
                                {
                                    // Если в записи отсутствует стоп-слово, читаем комменты
                                    if (((string)res[j]["text"]).IndexOf(Properties.Resources.StopWord) == -1)
                                    {
                                        // Читаем комменты к записи
                                        WallGetComments((string)res[j]["id"]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { File.WriteAllText("err.txt", ex.Message); }
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

                    //  Получаем общее количество записей
                    int count = (int)res[0];

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
                            "&offset=" + i.ToString() +
                            "&count=" + Properties.Resources.Count +
                            "&need_likes=1" +
                            "&sort=asc" +
                            "&preview_length=0";

                        res = JObject.Parse(result).SelectToken("response");

                        for (int j = 1; j < res.Count(); j++)
                        {
                            // Если пост содержит меньше XX лайков - приступаем к его обработке
                            if ((int)res[j]["likes"]["count"] < Convert.ToInt16(Properties.Resources.Likes))
                            {
                                // Конвертируем дату коммента
                                DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                                dt = dt.AddSeconds((double)res[j]["date"]);

                                // Если коммент больше XX минут - удаляем его
                                if (DateTime.Now.Subtract(dt).Minutes > Convert.ToInt16(Properties.Resources.Live))
                                {
                                    // Удаляем коммент
                                    WallDeleteComment((string)res[j]["id"]);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { File.WriteAllText("err.txt", ex.Message); }
        }

        /// <summary>
        /// Удаляем коммент, если он не прошел отбор
        /// </summary>
        /// <param name="commentId"></param>
        private void WallDeleteComment(string commentId)
        {
            try
            {
                string Data =
                            "&access_token" + JsonGet("access_token") +
                            "&owner_id=" + Properties.Resources.ID +
                            "&comment_id=" + commentId;

                POST(Properties.Resources.API + "wall.deleteComment", Data);
            }
            catch (Exception) { }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => WallGet());
        }

        private void ToLog(string message)
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                try { tbLog.Text += message + Environment.NewLine; }
                catch (Exception) { }
            }));
        }
    }
}
