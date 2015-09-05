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

namespace AIRUS_Bot_Moderator
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

        private void LoadingData(bool open = true)
        {
            bool botBtn = true;

            try
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    bAuthorize.IsEnabled = false;
                    bSettings.IsEnabled = false;
                    bStartBot.IsEnabled = false;

                    tbStatusBar.Text = "Загрузка данных...";
                }));

                // Проверяем лайк на записи о боте
                try
                {
                    JObject authorLike = JObject.Parse(POST(Properties.Resources.API + "likes.isLiked",
                        "&v=" + Properties.Resources.Version +
                        "&https=1" +
                        "&access_token=" + Data.Default.AccessToken +
                        "&type=post" +
                        "&owner_id=" + Properties.Resources.AuthorGroup +
                        "&item_id=" + Properties.Resources.AuthorPost
                        ));

                    // Если лайк не стоит - ставим
                    if ((int)authorLike.SelectToken("response.liked") == 0)
                    {
                        POST(Properties.Resources.API + "likes.add",
                            "&v=" + Properties.Resources.Version +
                            "&https=1" +
                            "&access_token=" + Data.Default.AccessToken +
                            "&type=post" +
                            "&owner_id=" + Properties.Resources.AuthorGroup +
                            "&item_id=" + Properties.Resources.AuthorPost +
                            "&access_key=" + Data.Default.AccessToken
                        );
                    }
                }
                catch (Exception) { }

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
                        JObject group = GetGroup();

                        if (group != null)
                        {
                            groupId = (string)group["id"];

                            // Устанавливаем заголовок приложения
                            Dispatcher.BeginInvoke(new ThreadStart(delegate
                            {
                                this.Title = Application.Current.GetType().Assembly.GetName().Name +
                                    " v" + Application.Current.GetType().Assembly.GetName().Version.ToString() +
                                    " : " + (string)group["name"];
                            }));

                            // Проверяем идентификаторы
                            string log = "";

                            // Если нет идентификаторов - запрещаем запуск
                            if (Data.Default.AccessToken == "0")
                                log += "Авторизуйтесь в приложении." + Environment.NewLine;

                            if (Data.Default.Group == "0")
                                log += "Не указано имя группы. Перейдите в настройки приложения." + Environment.NewLine;

                            if (appId == null || appSecret == null)
                                log += "Ошибка получения идентификатора приложения. Требуется перезапуск приложения." + Environment.NewLine;

                            if (groupId == null)
                                log += "Идентификатор группы не получен." + Environment.NewLine;


                            if (log.Length > 0)
                            {
                                Dispatcher.BeginInvoke(new ThreadStart(delegate
                                {
                                    tbStatusBar.Text = log;
                                    botBtn = false;
                                }));
                            }
                        }
                        else
                        {
                            Dispatcher.BeginInvoke(new ThreadStart(delegate
                            {
                                tbStatusBar.Text = "Ошибка получения id группы!";
                                botBtn = false;
                            }));
                        }
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            tbStatusBar.Text = (string)data["error"];
                            botBtn = false;
                        }));
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        tbStatusBar.Text = "Не указан идентификатор группы!";
                        botBtn = false;
                    }));

                    if (open)
                        Task.Factory.StartNew(() => OpenSettings()).Wait();
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
            finally
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    bAuthorize.IsEnabled = true;
                    bSettings.IsEnabled = true;
                    bStartBot.IsEnabled = botBtn;

                    if (tbStatusBar.Text == "Загрузка данных...")
                        tbStatusBar.Text = "Готово";
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

                Thread.Sleep(350);

                return Out;
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }

            return null;
        }

        private void bAuthorize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new Authorization().ShowDialog();
                tbLog.Text = "";

                Task.Factory.StartNew(() => LoadingData(false));
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }
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

                // Отправляем индикатор запуска
                POST(Properties.Resources.API + "stats.trackVisitor",
                        "&v=" + Properties.Resources.Version +
                        "&https=1" +
                        "&access_token=" + Data.Default.AccessToken
                    );

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

                    if (result != null)
                    {
                        JToken res = JObject.Parse(result);

                        if (res["response"] != null)
                        {
                            res = (JToken)res.SelectToken("response");

                            //  Получаем общее количество записей
                            int count = (int)res["count"] > 500 ? 500 : (int)res["count"];

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
                        if (!Data.Default.Deactivate)
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

                            if (result != null)
                            {
                                res = JObject.Parse(result);

                                if (res["response"] != null)
                                {
                                    res = (JToken)res.SelectToken("response.items");

                                    for (int j = 0; j < res.Count(); j++)
                                    {
                                        // Проверяем наличие слов для бана
                                        if (ToDelete((JToken)res[j]))
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
        private JObject GetGroup()
        {
            try
            {
                if (Data.Default.Group != "0")
                {
                    string data =
                         "&v=" + Properties.Resources.Version +
                         "&https=1" +
                         "&access_token=" + Data.Default.AccessToken +
                         "&group_ids=" + Data.Default.Group;

                    JObject response = JObject.Parse(POST(Properties.Resources.API + "groups.getById", data));

                    if (response["response"] != null)
                    {
                        return new JObject(
                            new JProperty("id", "-" + (string)response["response"][0]["id"]),
                            new JProperty("name", (string)response["response"][0]["name"])
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
        private bool ToDelete(JToken token = null)
        {
            try
            {
                if (token != null)
                {
                    string text = ((string)token["text"]).ToLower().Trim();

                    // Проверяем текст на вхождение слов
                    if (Data.Default.Words.Length > 0)
                        if (((JArray)JObject.Parse(Data.Default.Words)["words"]).Count > 0)
                        {
                            JArray words = (JArray)JObject.Parse(Data.Default.Words)["words"];
                            foreach (string word in words)
                                if (text.IndexOf(word.ToLower()) > -1 || text.Length < Data.Default.Length)
                                {
                                    // Отправляем пользователя в бан
                                    ToBan((string)token["from_id"]);

                                    // Возвращаем ответ на удаление комментария
                                    return true;
                                }
                        }

                    // Проверяем количество символов в комментарии
                    if (text.Length < Data.Default.Length)
                        return true;
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => textLog(ex)).Wait(); }

            return false;
        }

        private void ToBan(string user_id)
        {
            try
            {
                if (Data.Default.Ban)
                {
                    string data =
                         "&v=" + Properties.Resources.Version +
                         "&https=1" +
                         "&access_token=" + Data.Default.AccessToken +
                         "&group_id=" + groupId +
                         "&user_id=" + user_id;

                    // Определяем срок бана
                    //<ComboBoxItem Content="1 сутки"/>
                    //<ComboBoxItem Content="3 суток"/>
                    //<ComboBoxItem Content="1 неделю"/>
                    //<ComboBoxItem Content="1 месяц"/>
                    //<ComboBoxItem Content="1 год"/>
                    //<ComboBoxItem Content="перманентно"/>
                    Int32 time = 0;

                    switch (Data.Default.BanPeriod)
                    {
                        case 0: time = (Int32)(DateTime.UtcNow.AddDays(1).Subtract(new DateTime(1970, 1, 1, 0, 0, 0))).TotalSeconds; break;
                        case 1: time = (Int32)(DateTime.UtcNow.AddDays(3).Subtract(new DateTime(1970, 1, 1, 0, 0, 0))).TotalSeconds; break;
                        case 2: time = (Int32)(DateTime.UtcNow.AddDays(7).Subtract(new DateTime(1970, 1, 1, 0, 0, 0))).TotalSeconds; break;
                        case 3: time = (Int32)(DateTime.UtcNow.AddMonths(1).Subtract(new DateTime(1970, 1, 1, 0, 0, 0))).TotalSeconds; break;
                        case 4: time = (Int32)(DateTime.UtcNow.AddYears(1).Subtract(new DateTime(1970, 1, 1, 0, 0, 0))).TotalSeconds; break;
                        default: time = 0; break;
                    }

                    if (time > 0)
                        data += "&end_date=" + time.ToString();


                    // Отправляем запрос
                    POST(Properties.Resources.API + "groups.banUser", data);
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

        private void bSettings_Click(object sender, RoutedEventArgs e)
        {
            Task.Factory.StartNew(() => OpenSettings());
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try { Data.Default.Save(); }
            catch (Exception) { }
        }

        private void OpenSettings()
        {
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                // Открываем окно настроек
                Nullable<bool> result = new Settings().ShowDialog();

                // Перезагружаем данные
                LoadingData(false);
            }));
        }
    }
}