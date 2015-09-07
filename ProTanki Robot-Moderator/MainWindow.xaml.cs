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
                            string log = "";

                            // Если нет идентификаторов - запрещаем запуск
                            if (Data.Default.AccessToken.Length == 0)
                                log += "Авторизуйтесь в приложении." + Environment.NewLine;

                            if (Data.Default.Group.Length == 0)
                                log += "Не указано имя группы. Перейдите в настройки приложения." + Environment.NewLine;

                            if (appId == null || appSecret == null)
                                log += "Ошибка получения идентификатора приложения. Требуется перезапуск приложения." + Environment.NewLine;

                            if (groupId == null)
                                log += "Идентификатор группы не получен." + Environment.NewLine;


                            if (log.Length > 0)
                            {
                                await Dispatcher.BeginInvoke(new ThreadStart(delegate
                                {
                                    tbStatusBar.Text = log;
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
                        catch (Exception ex) { TextLog(ex); }
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
            catch (Exception ex) { TextLog(ex); }
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

        public async Task<JObject> POST(string Url, JObject data = null, int errors = 0)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    if (data["v"] == null) data.Add(new JProperty("v", Properties.Resources.Version));
                    if (data["https"] == null) data.Add(new JProperty("https", 1));

                    if (Data.Default.AccessToken.Length > 0)
                        if (data["access_token"] == null) data.Add(new JProperty("access_token", Data.Default.AccessToken));

                    Dictionary<string, string> content = new Dictionary<string, string>();

                    foreach (JToken pair in (JToken)data)
                        content.Add(pair.Path, (string)pair.First);

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                    var response = client.PostAsync(Url, new FormUrlEncodedContent(content)).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        JObject obj = JObject.Parse(await response.Content.ReadAsStringAsync());
                        Task.Delay(350).Wait();

                        if (obj["error"] == null)
                            return obj;
                        else
                        {
                            if ((int)obj.SelectToken("error.error_code") == 100)
                            {
                                Task.Delay(1050).Wait();

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
            catch (HttpRequestException hre) { TextLog(null, hre); }
            catch (Exception ex) { TextLog(ex); }

            Task.Delay(350).Wait();
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
            catch (Exception ex) { TextLog(ex); }
        }

        /// <summary>
        /// Получаем список постов на странице. Перебираем все
        /// </summary>
        /// <returns></returns>
        private void WallGet()
        {
            try
            {
                // Приступаем
                Task.Factory.StartNew(() => SetStatus());
                Task.Factory.StartNew(() => Log(null, 0, true)).Wait();

                // Запускаем лог
                Task.Factory.StartNew(() => ShowLog());


                Dispatcher.BeginInvoke(new ThreadStart(delegate
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
                Task.Factory.StartNew(() => POST(Properties.Resources.API + "stats.trackVisitor", null)).Wait();

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

                    JToken res = (JToken)POST(Properties.Resources.API + "wall.get",
                        new JObject(
                            new JProperty("owner_id", groupId),
                            new JProperty("offset", 0),
                            new JProperty("count", max_posts.ToString()),
                            new JProperty("filter", "all")
                        )
                    ).Result;

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
                            res = POST(Properties.Resources.API + "wall.get",
                                new JObject(
                                    new JProperty("owner_id", groupId),
                                    new JProperty("offset", (max_posts * i).ToString()),
                                    new JProperty("count", max_posts.ToString()),
                                    new JProperty("filter", "all")
                                )
                            ).Result;

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
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            tbStatusBar.Text = (string)ErrorCode((string)res.SelectToken("error.error_code"), true)["error"];
                            bStartBot.IsEnabled = false;
                        }));

                        error = true;
                    }
                }
                else
                {
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                     {
                         tbStatusBar.Text = "Ошибка получения настроек! Перезапустите приложение.";
                         bStartBot.IsEnabled = false;
                     }));

                    error = true;
                }
            }
            catch (Exception ex) { TextLog(ex); }
            finally
            {
                Task.Factory.StartNew(() => SetStatus("end"));
                Task.Factory.StartNew(() => Log("Circles")).Wait();

                if (!error)
                {
                    first = false;

                    // Разблокируем кнопку запуска бота
                    Dispatcher.BeginInvoke(new ThreadStart(delegate
                    {
                        bStartBot.IsEnabled = true;
                    }));

                    if (Data.Default.Ban)
                    {
                        // Сохраняем ID забаненных
                        string dir = @"banned\";
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        Dispatcher.BeginInvoke(new ThreadStart(delegate
                        {
                            File.WriteAllText(dir + String.Format("{0}_circle_{1}.txt", DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss"), (string)log["Circles"]),
                                lbBannedUsers.Items.ToString());
                        }));
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

                                Log("CurrentComment");
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
            catch (Exception ex) { TextLog(ex); }
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
                    Log("Deleted");

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
                    Log("ErrorDelete");

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
            catch (Exception ex) { TextLog(ex); }
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
            catch (Exception ex) { TextLog(ex); }

            return (JObject)ErrorCode("1");
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
            catch (Exception ex) { TextLog(ex); }

            return false;
        }

        private async void ToBan(string user_id)
        {
            try
            {
                if (Data.Default.Ban)
                {
                    JObject data = new JObject(
                        new JProperty("group_id", groupId),
                        new JProperty("user_id", user_id)
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

                    // Добавляем ID юзера в список
                    await Dispatcher.BeginInvoke(new ThreadStart(delegate { lbBannedUsers.Items.Add(Properties.Resources.VK + user_id); }));
                }
            }
            catch (Exception ex) { TextLog(ex); }
        }

        /// <summary>
        /// Проверяем является ли юзер модератором группы
        /// </summary>
        private async Task<JToken> CheckModerAccess(string id = null)
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
                        {
                            return ErrorCode((string)response.SelectToken("error.error_code"));
                        }
                    }
                }
            }
            catch (Exception ex) { TextLog(ex); }

            return ErrorCode("1");
        }

        private async Task<bool> SetStatus(string block = "start")
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
                                SetTimer();
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        bStartBot.IsEnabled = true;
                        TextLog(ex);
                    }
                }));

            return true;
        }

        private async void SetTimer()
        {
            int i = 0;

            while (timer)
            {
                await Dispatcher.BeginInvoke(new ThreadStart(delegate
                 {
                     DateTime dt = new DateTime(1970, 1, 1).AddSeconds(i);
                     tbEndAt.Text = String.Format("0000-00-00 {0}:{1}:{2}", dt.ToString("HH"), dt.ToString("mm"), dt.ToString("ss"));
                     i++;
                 }));

                Task.Delay(1000).Wait();
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
                catch (Exception ex) { TextLog(ex); }
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
                            log[path] = (double)log.SelectToken(path) + 1;
                        else if (path != null && key != 0)
                            log[path] = key;

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
            catch (Exception ex) { TextLog(ex); }
        }

        private void TextLog(Exception ex = null, HttpRequestException hre = null)
        {
            try
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    if (ex != null)
                        tbLog.Text = String.Format("{0}\n\n=============================\n\n{1}", ex.Message, ex.StackTrace);
                    else if (hre != null)
                        tbLog.Text = String.Format("{0}\n\n=============================\n\n{1}", hre.Message, hre.StackTrace);
                }));
            }
            catch (Exception) { }
        }

        private void Timer(int sec = 0)
        {
            for (int i = sec; i >= 0; i--)
            {
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    tbDiff.Text = String.Format("{0}", i.ToString());
                }));

                Task.Delay(1000).Wait();
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
                    case "5": error = "Авторизация пользователя не удалась."; break;
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
            catch (Exception ex) { TextLog(ex); }

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
        private void ShowLog()
        {
            while (true)
            {
                if (!error)
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
                    catch (Exception ex) { TextLog(ex); }
            }
        }
    }
}