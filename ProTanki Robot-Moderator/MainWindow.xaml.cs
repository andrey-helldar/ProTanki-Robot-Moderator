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
using System.Net.Http;
using System.Net.Http.Headers;
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

        bool error = false;

        Task wallGet = null;

        Stopwatch sWatch = new Stopwatch();

        // Иконка в трее
        private System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();


        public MainWindow()
        {
            InitializeComponent();

            // Устанавливаем заголовок
            this.Title = Application.Current.GetType().Assembly.GetName().Name +
                " v" + Application.Current.GetType().Assembly.GetName().Version.ToString();

            // Выводим иконку в трее
            Task.Factory.StartNew(() => NotifyIcon(true));

            // Загружаем данные
            Task.Factory.StartNew(() => LoadingData());
        }

        private async void NotifyIcon(bool first = false)
        {
            await Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                try
                {
                    Stream iconStream = Application.GetResourceStream(new Uri(String.Format(@"pack://application:,,,/{0};component/Resources/favicon.ico", Application.Current.GetType().Assembly.GetName().Name))).Stream;
                    if (iconStream != null) notifyIcon.Icon = new System.Drawing.Icon(iconStream);
                    notifyIcon.Visible = true;
                    notifyIcon.Text = Application.Current.GetType().Assembly.GetName().Name + " v" + Application.Current.GetType().Assembly.GetName().Version.ToString();
                    notifyIcon.Click += delegate(object sender, EventArgs args)
                    {
                        this.Show();
                        this.WindowState = WindowState.Normal;
                    };
                }
                catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)); }
            }));


            string mess = "Приветствую тебя";

            if (Data.Default.AccessToken.Length > 0)
            {
                JObject obj = await POST(Properties.Resources.API + "users.get", null);

                if (obj["response"] != null)
                    mess += String.Format(", {0}!", (string)obj["response"].First["first_name"]);
                else
                    mess += "!";
            }
            else
                mess += "!";

            Task.Factory.StartNew(() => ShowNotify(mess));
        }

        private async void ShowNotify(string text)
        {
            await Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                try
                {
                    notifyIcon.ShowBalloonTip(
                        5000,
                        Application.Current.GetType().Assembly.GetName().Name,
                        text,
                        System.Windows.Forms.ToolTipIcon.Info
                    );
                }
                catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)); }
            }));
        }

        //private void LoadingData(bool open = true)
        private async void LoadingData(bool open = true)
        {
            bool botBtn = true;

            try
            {
                await Dispatcher.BeginInvoke(new ThreadStart(delegate()
                {
                    // Отключаем кнопки
                    bAuthorize.IsEnabled = false;
                    bSettings.IsEnabled = false;
                    bStartBot.IsEnabled = false;

                    // Устанавливаем статус
                    tbStatusBar.Text = "Загрузка данных...";
                }));

                // Если настройки указаны
                if (Data.Default.Group.Length > 0)
                {
                    // Отправляем запрос
                    HttpClient client = new HttpClient();

                    // Парсим ответ
                    JObject data = JObject.Parse(await client.GetStringAsync(Properties.Resources.Author + Data.Default.Group));

                    if (data["error"] == null)
                    {
                        APPId = (string)data.SelectToken("response.app_id");
                        APPSecret = (string)data.SelectToken("response.app_secret");

                        this.Closing += delegate { APPId = null; };
                        this.Closing += delegate { APPSecret = null; };
                        this.Closing += delegate { notifyIcon = null; };

                        // Получаем идентификатор группы
                        JObject group = await GetGroup();

                        if (group["error"] == null)
                        {
                            groupId = (string)group["id"];

                            // Устанавливаем заголовок приложения
                            await Dispatcher.BeginInvoke(new ThreadStart(delegate
                            {
                                this.Title = Application.Current.GetType().Assembly.GetName().Name +
                                    " v" + Application.Current.GetType().Assembly.GetName().Version.ToString() +
                                    " : " + (string)group["name"];
                            }));

                            // Проверяем идентификаторы
                            string log_err = "";

                            // Если нет идентификаторов - запрещаем запуск
                            if (Data.Default.AccessToken.Length == 0)
                                log_err += "Авторизуйтесь в приложении." + Environment.NewLine;

                            if (Data.Default.Group.Length == 0)
                                log_err += "Не указано имя группы. Перейдите в настройки приложения." + Environment.NewLine;

                            if (appId == null || appSecret == null)
                                log_err += "Ошибка получения идентификатора приложения. Требуется перезапуск приложения." + Environment.NewLine;

                            if (groupId == null)
                                log_err += "Идентификатор группы не получен." + Environment.NewLine;


                            if (log_err.Length > 0)
                            {
                                await Dispatcher.BeginInvoke(new ThreadStart(delegate
                                {
                                    tbStatusBar.Text = log_err;
                                    botBtn = false;
                                }));
                            }
                        }
                        else
                        {
                            await Dispatcher.BeginInvoke(new ThreadStart(delegate
                           {
                               var qw = (string)group.SelectToken("error.error_code");
                               tbStatusBar.Text = (string)ErrorCode((string)group.SelectToken("error.error_code"), true)["error"];
                               botBtn = false;
                           }));
                        }


                        // Проверяем лайк на записи о боте
                        try
                        {
                            if (Data.Default.AccessToken.Length > 0)
                            {
                                JObject authorLike = await POST(Properties.Resources.API + "likes.isLiked",
                                    new JObject(
                                        new JProperty("type", "post"),
                                        new JProperty("owner_id", Properties.Resources.AuthorGroup),
                                        new JProperty("item_id", Properties.Resources.AuthorPost)
                                ));

                                // Если лайк не стоит - ставим
                                if (authorLike["error"] == null)
                                {
                                    if ((int)authorLike.SelectToken("response.liked") == 0)
                                    {
                                        await POST(Properties.Resources.API + "likes.add",
                                        new JObject(
                                            new JProperty("type", "post"),
                                            new JProperty("owner_id", Properties.Resources.AuthorGroup),
                                            new JProperty("item_id", Properties.Resources.AuthorPost),
                                            new JProperty("access_key", Data.Default.AccessToken)
                                    ));
                                    }
                                }
                                else
                                {
                                    await Dispatcher.BeginInvoke(new ThreadStart(delegate
                                   {
                                       tbStatusBar.Text = (string)ErrorCode((string)authorLike.SelectToken("error.error_code"), true)["error"];
                                   }));
                                }
                            }
                        }
                        catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }
                    }
                    else
                    {
                        await Dispatcher.BeginInvoke(new ThreadStart(delegate
                       {
                           tbStatusBar.Text = (string)ErrorCode((string)data.SelectToken("error.error_code"), true)["error"];
                           botBtn = false;
                       }));
                    }
                }
                else
                {
                    await Dispatcher.BeginInvoke(new ThreadStart(delegate
                      {
                          tbStatusBar.Text = "Не указан идентификатор группы!";
                          botBtn = false;
                      }));

                    if (open)
                        OpenSettings();
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }
            finally
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    bAuthorize.IsEnabled = true;
                    bSettings.IsEnabled = true;

                    // Если процесс запущен, запрещаем активацию кнопки старта бота
                    if ((wallGet != null) && (
                        wallGet.IsCompleted == false ||
                        wallGet.Status == TaskStatus.Running ||
                        wallGet.Status == TaskStatus.WaitingToRun ||
                        wallGet.Status == TaskStatus.WaitingForActivation
                        ))
                        botBtn = false;

                    bStartBot.IsEnabled = botBtn;

                    if (tbStatusBar.Text == "Загрузка данных...")
                        tbStatusBar.Text = "Готово";
                })).Wait();
            }
        }

        public async Task<JObject> POST(string Url, JObject data = null, int errors = 0)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    if (data != null)
                    {
                        if (data["v"] == null) data.Add(new JProperty("v", Properties.Resources.Version));
                        if (data["https"] == null) data.Add(new JProperty("https", 1));

                        if (Data.Default.AccessToken.Length > 0)
                            if (data["access_token"] == null) data.Add(new JProperty("access_token", Data.Default.AccessToken));
                    }
                    else
                    {
                        data = new JObject(
                            new JProperty("v", Properties.Resources.Version),
                            new JProperty("https", 1),
                            new JProperty("access_token", Data.Default.AccessToken)
                        );
                    }

                    Dictionary<string, string> content = new Dictionary<string, string>();

                    foreach (JToken pair in (JToken)data)
                        content.Add(pair.Path, (string)pair.First);

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                    var response = await client.PostAsync(Url, new FormUrlEncodedContent(content));

                    if (response.IsSuccessStatusCode)
                    {
                        JObject obj = JObject.Parse(response.Content.ReadAsStringAsync().Result);
                        Thread.Sleep(350);

                        if (obj["error"] == null)
                            return obj;
                        else
                        {
                            if ((int)obj.SelectToken("error.error_code") == 100)
                            {
                                Thread.Sleep(1050);

                                if (errors < Data.Default.MaxPostErrors)
                                {
                                    errors++;
                                    return await POST(Url, data, errors);
                                }
                            }
                        }

                        return obj;
                    }
                }
            }
            catch (HttpRequestException hre) { Task.Factory.StartNew(() => TextLog(null, hre)).Wait(); }
            catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }

            Thread.Sleep(350);
            return (JObject)ErrorCode("1");
        }

        private void bAuthorize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new Authorization().ShowDialog();
                tbLog.Text = "";

                Task.Factory.StartNew(() => LoadingData(false));
            }
            catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }
        }

        /// <summary>
        /// Получаем список постов на странице. Перебираем все
        /// </summary>
        /// <returns></returns>
        private async void WallGet(bool firstStart = false)
        {
            try
            {
                // Первый запуск?
                if (firstStart)
                    // Обнуляем логи
                    await Task.Factory.StartNew(() => Log(null, 0, true, true));


                // Приступаем
                await Task.Factory.StartNew(() => SetStatus());
                await Task.Factory.StartNew(() => Log(null, 0, true));


                await Dispatcher.BeginInvoke(new ThreadStart(delegate
                 {
                     // Очищаем список заблокированных аккаунтов
                     lbBannedUsers.Items.Clear();

                     // Если стоит галка "банить", то:
                     if (!Data.Default.Ban)
                         lbBannedUsers.Items.Add("Отключено в настройках");

                     // Отключаем кнопку запуска бота
                     bStartBot.IsEnabled = false;
                 }));

                // Отправляем индикатор запуска
                await Task.Factory.StartNew(() => POST(Properties.Resources.API + "stats.trackVisitor", null));

                // Запоминаем ID в настройки
                if (
                    Data.Default.AccessToken.Length > 0 &&
                    appId != null &&
                    appSecret != null &&
                    groupId != null
                    )
                {
                    // Устанавливаем переменную
                    int max_posts = Data.Default.Posts > 100 || Data.Default.Posts == 0 ? 100 : Data.Default.Posts;

                    JToken res = await POST(Properties.Resources.API + "wall.get",
                        new JObject(
                            new JProperty("owner_id", groupId),
                            new JProperty("offset", 0),
                            new JProperty("count", max_posts.ToString()),
                            new JProperty("filter", "all")
                        )
                    );

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
                        await Task.Factory.StartNew(() => SetProgress(true, count));

                        // Запоминаем статистику
                        await Task.Factory.StartNew(() => Log("AllPosts", (double)count));

                        // Перебираем записи по шагам
                        for (int i = 0; i < step; i++)
                        {
                            res = await POST(Properties.Resources.API + "wall.get",
                                new JObject(
                                    new JProperty("owner_id", groupId),
                                    new JProperty("offset", (max_posts * i).ToString()),
                                    new JProperty("count", max_posts.ToString()),
                                    new JProperty("filter", "all")
                                )
                            );

                            if (res["response"] != null)
                            {
                                await Task.Factory.StartNew(() => Log("AllPosts", (double)count));

                                res = (JToken)res.SelectToken("response.items");

                                for (int j = 0; j < res.Count(); j++)
                                {
                                    await Task.Factory.StartNew(() => Log("CurrentPost"));

                                    // Если в посте есть комменты - читаем его, иначе нафиг время тратить)))
                                    if ((int)res[j]["comments"]["count"] > 0)
                                    {
                                        // Читаем комменты к записи
                                        WallGetComments((string)res[j]["id"]);
                                    }

                                    // Изменяем положение прогресс бара
                                    await Task.Factory.StartNew(() => SetProgress());
                                }
                            }
                            else
                            {
                                await Dispatcher.BeginInvoke(new ThreadStart(delegate
                                {
                                    tbLog.Text = String.Format("Error: {0}\n{1}", (string)res.SelectToken("error.error_code"), (string)res.SelectToken("error.error_msg"));
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
                        await Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            tbStatusBar.Text = (string)ErrorCode((string)res.SelectToken("error.error_code"), true)["error"];
                            bStartBot.IsEnabled = false;
                        }));

                        error = true;
                    }
                }
                else
                {
                    await Dispatcher.BeginInvoke(new ThreadStart(delegate
                     {
                         tbStatusBar.Text = "Ошибка получения настроек! Перезапустите приложение.";
                         bStartBot.IsEnabled = false;
                     }));

                    error = true;
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }
            finally
            {
                if (!error)
                {
                    first = false;

                    // Обновляем статистику
                    log["AllComments"] = Math.Round((double)log["AllComments"] + (double)log["CurrentComment"], 0);
                    log["AllDeleted"] = Math.Round((double)log["AllDeleted"] + (double)log["Deleted"], 0);
                    log["AllErrorDelete"] = Math.Round((double)log["AllErrorDelete"] + (double)log["ErrorDelete"], 0);

                    Task.Factory.StartNew(() => Log("Circles")).Wait();
                    Task.Factory.StartNew(() => SetStatus("end"));

                    string time = "";
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            time = tbEndAt.Text;
                        })).Wait();

                    // Выводим инфу в иконку
                    if (Data.Default.Notify)
                    {
                        Task.Factory.StartNew(() => ShowNotify(String.Format(
                            "Общее количество циклов: {0}\n" +
                            "Продолжительность: {1}\n" +
                            "Постов: {2}\n" +
                            "Комментариев: {3}\n" +
                            "Удалено комментариев: {4} / {5}%",
                            (string)log["Circles"],
                            time,
                            (string)log["AllPosts"],
                            (string)log["CurrentComment"],
                            (string)log["Deleted"],
                            (Math.Round(((double)log["Deleted"] / (double)log["CurrentComment"]) * 100, 3)).ToString()
                            )));
                    }


                    // Ждем и повторяем
                    if (!Data.Default.Deactivate)
                        Timer(Data.Default.Sleep == 0 ? Data.Default.SleepDefault : Data.Default.Sleep);
                }
            }
        }

        /// <summary>
        /// Получаем комменты к конкретной записи и проверяем дату жизни и лайки)))
        /// </summary>
        /// <param name="postId"></param>
        private async void WallGetComments(string postId)
        {
            try
            {
                JObject result = await POST(Properties.Resources.API + "wall.getComments",
                    new JObject(
                        new JProperty("owner_id", groupId),
                        new JProperty("post_id", postId),
                        new JProperty("offset", 0),
                        new JProperty("count", 100),
                        new JProperty("need_likes", 0),
                        new JProperty("sort", "desc"),
                        new JProperty("preview_length", 0)
                    )
                );

                if (result["error"] != null)
                {
                    await Dispatcher.BeginInvoke(new ThreadStart(delegate
                      {
                          tbLog.Text = String.Format("Error: {0}\n{1}\nPost ID: {2}",
                              (string)result.SelectToken("error.error_code"),
                              (string)result.SelectToken("error.error_msg"),
                              postId);
                      }));
                }
                else
                {
                    JToken res = (JToken)result.SelectToken("response");

                    //  Получаем общее количество комментов
                    int count = (int)res["count"];

                    if (count > 0)
                    {
                        // Вычисляем количество шагов для комментов
                        int step = 0;
                        if (count % 100 == 0)
                            step = count / 100;
                        else
                            step = (count / 100) + 1;

                        // Перебираем записи по шагам
                        for (int i = 0; i < step; i++)
                        {
                            res = await POST(Properties.Resources.API + "wall.getComments",
                                new JObject(
                                    new JProperty("owner_id", groupId),
                                    new JProperty("post_id", postId),
                                    new JProperty("offset", (100 * i).ToString()),
                                    new JProperty("count", 100),
                                    new JProperty("need_likes", 0),
                                    new JProperty("sort", "desc"),
                                    new JProperty("preview_length", 0)
                                )
                            );

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

                                    await Task.Factory.StartNew(() => Log("CurrentComment"));
                                }
                            }
                            else
                            {
                                await Dispatcher.BeginInvoke(new ThreadStart(delegate
                                 {
                                     tbLog.Text = String.Format("Error: {0}\n{1}\nPost ID:{2}",
                                         (string)result.SelectToken("error.error_code"),
                                         (string)result.SelectToken("error.error_msg"),
                                         postId);
                                     bStartBot.IsEnabled = false;
                                 }));

                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }
        }

        /// <summary>
        /// Удаляем коммент, если он не прошел отбор
        /// </summary>
        /// <param name="commentId"></param>
        private async void WallDeleteComment(string commentId, JToken token = null, string postId = null)
        {
            try
            {
                JObject response = await POST(Properties.Resources.API + "wall.deleteComment",
                    new JObject(
                        new JProperty("owner_id", groupId),
                        new JProperty("comment_id", commentId)
                    )
                );

                if (response["response"] != null)
                {
                    await Task.Factory.StartNew(() => Log("Deleted"));

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
                    await Task.Factory.StartNew(() => Log("ErrorDelete"));

                    if (token != null)
                    {
                        string dir = String.Format(@"errors\{0}\", postId);

                        // Если директории нет - создаем
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        // Записываем лог
                        File.WriteAllText(dir + commentId + ".json", String.Format("{0}\n\n{1}", token.ToString(), response.ToString()));
                    }
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }
        }


        /// <summary>
        /// Получаем ID группы или сообщества по имени
        /// </summary>
        /// <param name="name">Имя группы или сообщества</param>
        /// <returns>ID</returns>
        private async Task<JObject> GetGroup()
        {
            try
            {
                if (Data.Default.Group.Length > 0)
                {
                    JObject response = await POST(Properties.Resources.API + "groups.getById",
                        new JObject(
                            new JProperty("group_ids", Data.Default.Group)
                        )
                    );

                    if (response["response"] != null)
                    {
                        // Получаем ответ
                        JToken token = await CheckModerAccess((string)response["response"][0]["id"]);

                        // Проверяем имеет ли юзер права модера группы
                        if (token == null)
                        {
                            return new JObject(
                                new JProperty("id", "-" + (string)response["response"][0]["id"]),
                                new JProperty("name", (string)response["response"][0]["name"])
                            );
                        }
                        else
                            return (JObject)token;
                    }
                    else
                        return (JObject)ErrorCode((string)response.SelectToken("error.error_code"));
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }

            return (JObject)ErrorCode("1");
        }

        private void bStartBot_Click(object sender, RoutedEventArgs e)
        {
            wallGet = Task.Factory.StartNew(() => WallGet(true));
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

                    // Проверяем количество символов в комментарии
                    if (text.Length < Data.Default.Length)
                        return true;

                    // Если список слов пуст - заполняем дефолтным
                    if (Data.Default.WordsDelete.Length == 0)
                        Data.Default.WordsDelete = Data.Default.WordsDeleteDefault;

                    // Проверяем текст на вхождение слов для удаления
                    if (((JArray)JObject.Parse(Data.Default.WordsDelete)["words"]).Count > 0)
                    {
                        JArray words = (JArray)JObject.Parse(Data.Default.WordsDelete)["words"];
                        foreach (string word in words)
                            if (text.IndexOf(word.ToLower()) > -1 || text.Length < Data.Default.Length)
                            {
                                // Возвращаем ответ на удаление комментария
                                return true;
                            }
                    }

                    // Если список слов пуст - заполняем дефолтным
                    if (Data.Default.WordsBan.Length == 0)
                        Data.Default.WordsBan = Data.Default.WordsBanDefault;

                    // Проверяем текст на вхождение слов для БАНА
                    if (((JArray)JObject.Parse(Data.Default.WordsBan)["words"]).Count > 0)
                    {
                        JArray wordsBan = (JArray)JObject.Parse(Data.Default.WordsBan)["words"];
                        foreach (string word in wordsBan)
                            if (text.IndexOf(word.ToLower()) > -1 || text.Length < Data.Default.Length)
                            {
                                // Если коммент младше 1 месяца, то
                                if ((Int32)token["date"] > (Int32)(DateTime.UtcNow.AddMonths(-1).Subtract(new DateTime(1970, 1, 1))).TotalSeconds)
                                    // Отправляем пользователя в бан
                                    ToBan(token);

                                // Возвращаем ответ на удаление комментария
                                return true;
                            }
                    }

                    // Проверяем возраст комментария
                    if (Data.Default.Delete)
                        if ((Int32)token["date"] < (Int32)(DateTime.UtcNow.AddDays(Data.Default.DeleteDays * -1).Subtract(new DateTime(1970, 1, 1))).TotalSeconds)
                            return true;

                    // Проверяем лайки
                    if (Data.Default.Likes)
                        if ((int)token.SelectToken("likes.count") < Data.Default.LikesCount &&
                            (Int32)token["date"] < (Int32)(DateTime.UtcNow.AddMinutes(Data.Default.LikesOld * -1).Subtract(new DateTime(1970, 1, 1, 0, 0, 0))).TotalSeconds)
                            return true;
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }

            return false;
        }

        private async void ToBan(JToken token)
        {
            try
            {
                if (Data.Default.Ban)
                {
                    JObject data = new JObject(
                        new JProperty("group_id", groupId),
                        new JProperty("user_id", (string)token["from_id"]),
                        new JProperty("reason", 1),
                        new JProperty("comment", String.Format("{0} :: {1}",
                            Application.Current.GetType().Assembly.GetName().Name,
                            (string)token["text"]
                        ))
                    );

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
                        data["end_date"] = time.ToString();


                    // Отправляем запрос
                    await POST(Properties.Resources.API + "groups.banUser", data);

                    await Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        // Добавляем ID юзера в список
                        lbBannedUsers.Items.Add("id" + (string)token["from_id"]);

                        string dir = @"banned";
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                        File.AppendAllText(String.Format(@"{0}\id{1}.json", dir, (string)token["from_id"]), token.ToString() + Environment.NewLine + Environment.NewLine);
                    }));
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }
        }

        /// <summary>
        /// Проверяем является ли юзер модератором группы
        /// </summary>
        private async Task<JObject> CheckModerAccess(string id = null)
        {
            try
            {
                if (id != null)
                {
                    if (Data.Default.Group.Length > 0)
                    {
                        JObject response = await POST(Properties.Resources.API + "groups.get",
                            new JObject(
                                new JProperty("extended", 1),
                                new JProperty("filter", "moder"),
                                new JProperty("offset", 0),
                                new JProperty("count", 100)
                            )
                        );

                        if (response["response"] != null)
                        {
                            if ((int)response.SelectToken("response.count") > 0)
                                foreach (JToken group in (JToken)response.SelectToken("response.items"))
                                    if ((string)group["id"] == id)
                                        return null;
                        }
                        else
                            return (JObject)ErrorCode((string)response.SelectToken("error.error_code"));
                    }
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }

            return (JObject)ErrorCode("1");
        }

        private async void SetStatus(string block = "start")
        {
            await Dispatcher.BeginInvoke(new ThreadStart(delegate
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
                              Task.Factory.StartNew(() => ShowLog());
                              break;
                      }
                  }
                  catch (Exception ex)
                  {
                      bStartBot.IsEnabled = true;
                      Task.Factory.StartNew(() => TextLog(ex));
                  }
              }));
        }

        private async void SetTimer()
        {
            int i = 0;

            while (timer)
            {
                await Dispatcher.BeginInvoke(new ThreadStart(delegate
                 {
                     DateTime dt = new DateTime(1970, 1, 1).AddSeconds(i);
                     tbEndAt.Text = String.Format("0000-00-{0:00} {1}:{2}:{3}", (dt.Day - 1).ToString(), dt.ToString("HH"), dt.ToString("mm"), dt.ToString("ss"));
                     i++;
                 }));

                Thread.Sleep(1000);
            }
        }

        private async void SetProgress(bool set = false, int value = 100)
        {
            await Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                try
                {
                    if (set == true)
                    {
                        pbStatus.Maximum = value;
                        pbStatus.Value = 0;
                    }
                    else
                        pbStatus.Value += 1;
                }
                catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }
            }));
        }

        private async void Log(string path = null, double key = 0, bool clear = false, bool time = false)
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
                            log[path] = (double)log.SelectToken(path) + 1;
                        else if (path != null && key != 0)
                            log[path] = key;

                        // Выводим логи на экран
                        await Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            logAllPosts.Text = (string)log.SelectToken("CurrentPost") + " / " + (string)log.SelectToken("AllPosts");
                            logAllComments.Text = (string)log.SelectToken("CurrentComment");
                            logDeleted.Text = String.Format("{0} / {1}%", (string)log["Deleted"], (Math.Round(((double)log["Deleted"] / (double)log["CurrentComment"]) * 100, 3)).ToString());
                            logErrorDelete.Text = String.Format("{0} / {1}%", (string)log["ErrorDelete"], (Math.Round(((double)log["ErrorDelete"] / (double)log["CurrentComment"]) * 100, 3)).ToString());
                        }));
                    }
                }
            }
            catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }
        }

        private async void TextLog(Exception ex = null, HttpRequestException hre = null)
        {
            try
            {
                await Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    if (ex != null)
                        tbLog.Text = String.Format("{0}\n\n=============================\n\n{1}", ex.Message, ex.StackTrace);
                    else if (hre != null)
                        tbLog.Text = String.Format("{0}\n\n=============================\n\n{1}", hre.Message, hre.StackTrace);
                }));
            }
            catch (Exception) { }
        }

        private async void Timer(int sec = 0)
        {
            for (int i = sec; i >= 0; i--)
            {
                await Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    tbDiff.Text = String.Format("{0}", i.ToString());
                }));

                Thread.Sleep(1000);
            }

            Task.Factory.StartNew(() => WallGet());
        }

        private void bSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
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
                Task.Factory.StartNew(() => LoadingData(false));
            }));
        }

        private JToken ErrorCode(string code, bool str = false)
        {
            try
            {
                string error = "";

                switch (code)
                {
                    case "1": error = "Произошла неизвестная ошибка. Попробуйте повторить запрос позже."; break;
                    case "2": error = "Приложение выключено."; break;
                    case "3": error = "Передан неизвестный метод."; break;
                    case "4": error = "Неверная подпись."; break;
                    case "5": error = "Требуется авторизация пользователя."; break;
                    case "6": error = "Слишком много запросов в секунду."; break;
                    case "7": error = "Нет прав для выполнения этого действия."; break;
                    case "8": error = "Неверный запрос."; break;
                    case "9": error = "Слишком много однотипных действий."; break;
                    case "10": error = "Произошла внутренняя ошибка сервера."; break;
                    case "11": error = "В тестовом режиме приложение должно быть выключено или пользователь должен быть залогинен."; break;
                    case "14": error = "Требуется ввод кода с картинки (Captcha)."; break;
                    case "15": error = "Доступ запрещён."; break;
                    case "16": error = "Требуется выполнение запросов по протоколу HTTPS, т.к. пользователь включил настройку, требующую работу через безопасное соединение."; break;
                    case "17": error = "Требуется валидация пользователя."; break;
                    case "20": error = "Данное действие запрещено для не Standalone приложений."; break;
                    case "21": error = "Данное действие разрешено только для Standalone и Open API приложений."; break;
                    case "23": error = "Метод был выключен."; break;
                    case "24": error = "Требуется подтверждение со стороны пользователя."; break;
                    case "100": error = "Один из необходимых параметров был не передан или неверен."; break;
                    case "101": error = "Неверный API ID приложения."; break;
                    case "113": error = "Неверный идентификатор пользователя."; break;
                    case "150": error = "Неверный timestamp."; break;
                    case "203": error = "Доступ к группе запрещён."; break;
                    default: error = "Произошла неизвестная ошибка"; break;
                }

                if (str)
                    return new JObject(
                        new JProperty("error",
                            String.Format("{0} : {1}", code, error)
                        )
                    );
                else
                    return new JObject(
                     new JProperty("error",
                         new JObject(
                             new JProperty("error_code", code),
                             new JProperty("error_msg", error)
                         )
                     ));
            }
            catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }

            if (str)
                return new JObject(
                    new JProperty("error",
                        String.Format("{0} : {1}", code, "Произошла неизвестная ошибка")
                    )
                );
            else
                return new JObject(
                 new JProperty("error",
                     new JObject(
                         new JProperty("error_code", code),
                         new JProperty("error_msg", "Произошла неизвестная ошибка")
                     )
                 ));
        }

        /// <summary>
        /// Выводим лог в динамике
        /// </summary>
        private async void ShowLog()
        {
            while (timer)
            {
                if (!error)
                    try
                    {
                        await Dispatcher.BeginInvoke(new ThreadStart(delegate
                         {
                             TimeSpan ts = sWatch.Elapsed;

                             tbLog.Text = "Начало работы: " + (string)log["Starting"] + Environment.NewLine;
                             tbLog.Text += "Общее время работы: " +
                                 String.Format("{0}d {1:00}:{2:00}:{3:00}", ts.Days, ts.Hours, ts.Minutes, ts.Seconds) +
                                 Environment.NewLine + Environment.NewLine;

                             tbLog.Text += "Начало цикла: " + tbStartAt.Text + Environment.NewLine;
                             tbLog.Text += "Завершение цикла: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine;
                             tbLog.Text += "Продолжительность цикла: " + tbEndAt.Text + Environment.NewLine;
                             tbLog.Text += "Общее количество циклов: " + (string)log["Circles"] + Environment.NewLine + Environment.NewLine;

                             tbLog.Text += "Постов: " + (string)log["AllPosts"] + Environment.NewLine;
                             tbLog.Text += "Комментариев: " + (string)log["CurrentComment"] + Environment.NewLine;
                             tbLog.Text += "Удалено комментариев: " + String.Format("{0} / {1}%\n", (string)log["Deleted"], (Math.Round(((double)log["Deleted"] / (double)log["CurrentComment"]) * 100, 3)).ToString());
                             tbLog.Text += "Ошибок удаления: " + String.Format("{0} / {1}%", (string)log["ErrorDelete"], (Math.Round(((double)log["ErrorDelete"] / (double)log["CurrentComment"]) * 100, 3)).ToString()) + Environment.NewLine + Environment.NewLine;

                             tbLog.Text += "Всего комментариев: " + (string)log["AllComments"] + Environment.NewLine;
                             tbLog.Text += "Всего удалено: " + String.Format("{0} / {1}%\n", (string)log["AllDeleted"], (Math.Round(((double)log["AllDeleted"] / (double)log["AllComments"]) * 100, 3)).ToString());
                             tbLog.Text += "Всего ошибок удаления: " + String.Format("{0} / {1}%", (string)log["AllErrorDelete"], (Math.Round(((double)log["AllErrorDelete"] / (double)log["AllComments"]) * 100, 3)).ToString());
                         }));
                    }
                    catch (Exception ex) { Task.Factory.StartNew(() => TextLog(ex)).Wait(); }

                await Task.Delay(1000);
            }
        }
    }
}