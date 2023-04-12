using System;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

namespace DiscordBot
{
    class Program
    {
        DiscordSocketClient client;

        // Изменение дефолтного статического метода main на асинхронный аналог
        static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();
        //Создание экземпляра класса DictionaryCommands
        readonly DictionaryCommands dc = new DictionaryCommands();
        //Создание экземпляра класса DictionaryGismeteo
        readonly DictionaryGismeteo Dictionary = new DictionaryGismeteo();
        //Создание экземпляра класса WebClient
        readonly WebClient wc = new WebClient() { Encoding = Encoding.UTF8 };

        private async Task MainAsync()
        {   
            //Создание экземпляра класса DiscordSocketClient
            client = new DiscordSocketClient();
            // Переход к операции при получении нового сообщения
            client.MessageReceived += CommandsHandler;
            client.Log += Log;
            
            var token = ""; /*Вставить свой токен*/

            // Получение токена
            await client.LoginAsync(TokenType.Bot, token);
            // Запуск бота
            await client.StartAsync();

            Console.ReadLine();
        }
        
        /// <summary>
        /// Логирование системных событий
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
        /// <summary>
        /// Основная операция по обработке новых сообщений
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private Task CommandsHandler(SocketMessage msg)
        {
            // Фильтрация сообщений от других ботов
            if (msg.Author.IsBot) return Task.CompletedTask;

            // Попытка преобразования сообщения в одну из существующих команд DictionaryCommands.CommandsDB
            string Choice = SearchingCommand(dc, msg);
            switch (Choice)
            {
                case "!help":
                    Info(msg);
                    break;
                case "!привет":
                    msg.Channel.SendMessageAsync($"Привет, {msg.Author}!");
                    break;
                case "!cities":
                    WeatherBD(Dictionary, msg);
                    break;
                case "!myfiles":
                    ShowFiles(msg);
                    break;
                case "!save":
                    if (msg.Attachments.Count < 1) msg.Channel.SendMessageAsync("Не вижу вложения в этом сообщении.");
                    else SavingFiles(msg, wc);
                    break;
                case "!giveme":
                    UploadFile((msg.Content.Replace("!giveme", "")).TrimStart(), msg);
                    break;
            }
            Weather(msg.Content, Dictionary, msg, wc);

            return Task.CompletedTask;
        }
        /// <summary>
        /// Поиск доступных команд в DictionaryCommands.CommandsDB
        /// </summary>
        /// <param name="dc"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        static string SearchingCommand(DictionaryCommands dc, SocketMessage msg)
        {
            string Result = "";
            string[] Values = dc.CommandsDB.Values.ToArray();
            string[] Keys = dc.CommandsDB.Keys.ToArray();
            int countCommands = dc.CommandsDB.Count;

            for (int i = 0; i < countCommands; i++)
            {
                Result = Regex.Match(msg.Content.ToLower(), Keys[i]).Groups[0].Value;
                if (Result == Keys[i])
                {
                    Result = Values[i];
                    break;
                }
            }
            return Result;
        }
        /// <summary>
        /// Отправка сообщения в чат со справочной информацией
        /// </summary>
        /// <param name="msg"></param>
        async void Info(SocketMessage msg)
        {
            var builder = new EmbedBuilder()
                .WithColor(new Color(54, 33, 150))
                .WithTitle(":warning: Справка :warning:")
                .AddField("!help", ":space_invader: Список доступных команд", false)
                .AddField("!привет", ":space_invader: Приветствие от бота,\n" +
                                     "технически бот может попытаться приветствовать пользователя,\n" +
                                     "если увидит в чате обычное приветствие", false)
                .AddField("<Название города>", ":space_invader: Если в сообщении присутствует только название города,\n" +
                                               "бот покажет состояние погоды на данный момент в этом городе", false)
                .AddField("!cities", ":space_invader: Список городов, где можно узнать погоду", false)
                .AddField("!save", ":space_invader: Команда должна быть указана в любом месте\n" +
                                   "сопроводительного сообщения при импорте файла в чат,\n" +
                                   "чтобы бот сохранил файл в надежном месте\n", false)
                .AddField("!myfiles", ":space_invader: Показывает список сохраненных файлов", false)
                .AddField("!giveme <имя файла>", ":space_invader: Загружает в чат указанный файл", false)
                .WithCurrentTimestamp();
            var embed = builder.Build();
            await msg.Channel.SendMessageAsync(null, false, embed);
        }

        #region Files
        /// <summary>
        /// Загрузка выбранного файла в чат канала
        /// </summary>
        /// <param name="FileName"></param>
        /// <param name="msg"></param>
        async void UploadFile(string FileName, SocketMessage msg)
        {
            string Folder = Convert.ToString(msg.Author.Id);
            string FilePath = $"{Folder}/{FileName}";

            string[] Files = Directory.GetFiles(Folder);
            string file = "";

            foreach (string f in Files)
            {
                file = Path.GetFileName(f);
                if (FileName == file)
                {
                    await msg.Channel.SendMessageAsync("sending...:zzz:");
                    byte[] buffer = (new WebClient().DownloadData(FilePath));
                    using (MemoryStream ms = new MemoryStream(buffer))
                    {
                        await msg.Channel.SendFileAsync(ms, FileName);
                    }
                    break;
                }
            }
        }
        /// <summary>
        /// Показывает список файлов в папке <AuthorId>, если участник чата ранее отправлял какие-либо файлы
        /// </summary>
        /// <param name="msg"></param>
        async void ShowFiles(SocketMessage msg)
        {
            string Folder = Convert.ToString(msg.Author.Id);
            string List = "";
            if (!Directory.Exists(Folder))
            { await msg.Channel.SendMessageAsync("Я не помню, чтобы вы просили меня сохранять файлы :thinking:"); }
            else
            {
                string[] Files = Directory.GetFiles(Folder);
                foreach (string f in Files) { List += Path.GetFileName(f) + "\n"; }

                var builder = new EmbedBuilder()
                .WithColor(new Color(54, 33, 150))
                .AddField($"Список файлов пользователя {msg.Author.Username}", List, false)
                .WithCurrentTimestamp();
                var embed = builder.Build();
                await msg.Channel.SendMessageAsync(null, false, embed);
            }
        }
        /// <summary>
        /// Сохраняет файл из чата в папке <AuthorID> на PC
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="wc"></param>
        static void SavingFiles(SocketMessage msg, WebClient wc)
        {
            string MainUrl = @"https://cdn.discordapp.com/attachments/";
            ulong ChannelID = msg.Channel.Id;
            ulong MessageID = msg.Attachments.ElementAt(0).Id;
            string FileName = msg.Attachments.ElementAt(0).ToString();
            ulong AuthorID = msg.Author.Id;
            string Folder = Convert.ToString(AuthorID);
            string Author = msg.Author.Username;

            // Логирование события в cmd
            Console.WriteLine($"\nUsing SavingFiles>: AuthorID: {AuthorID}, AuthorName: {Author}\n" +
                              $"Using SavingFiles>: ChannelID: {ChannelID}, MessageID: {MessageID}, FileName: {FileName}\n" +
                              $"Using SavingFiles>: Text: {msg.Content}\n");

            if (!Directory.Exists(Folder))
            { Directory.CreateDirectory(Folder); }

            wc.DownloadFileTaskAsync($@"{MainUrl}{ChannelID}/{MessageID}/{FileName}", $@"{AuthorID}/{FileName}");
            msg.Channel.SendMessageAsync($"Файл {msg.Attachments.ElementAt(0).ToString()} сохранен :white_check_mark:");
        }
        #endregion

        #region Weather
        /// <summary>
        /// Поиск информации о погоде на сайте www.gismeteo.ru
        /// </summary>
        /// <param name="City"></param>
        /// <param name="Dictionary"></param>
        /// <param name="msg"></param>
        async void Weather(string City, DictionaryGismeteo Dictionary, SocketMessage msg, WebClient wc)
        {
            // Проверка наличия названия города в DictionaryGismeteo.GismeteoDB
            bool b = Dictionary.GismeteoDB.ContainsKey(City);

            if (b == false)
            { return; }

            string GismeteoDB = Dictionary.GismeteoDB[City];

            string url = $@"https://www.gismeteo.ru/weather-{GismeteoDB}/now/";

            string[] separators1 = { "now-astro-sunrise" };
            string[] separators2 = { "info-item water" };
            string[] separators3 = { "now-" };

            // Получение кода страницы сайта
            string Response = wc.DownloadString(url).ToString();

            // Ограничение области поиска
            string[] words = Response.Split(separators1, System.StringSplitOptions.RemoveEmptyEntries);
            words = words[1].Split(separators2, System.StringSplitOptions.RemoveEmptyEntries);
            words = words[0].Split(separators3, System.StringSplitOptions.RemoveEmptyEntries);

            //Поиск данных
            string Dawn = Regex.Match(words[0], @"time\"">([0-9]+\:[0-9]+)").Groups[1].Value;
            string Dusk = Regex.Match(words[1], @"time\"">([0-9]+\:[0-9]+)").Groups[1].Value;
            string Wether = words[4].Replace(@"desc"">", "");
            Wether = Wether.Replace(@"</div><div class=""", "");
            string Temp;
            bool BelowZero =    Regex.IsMatch(words[3], @"unit unit_temperature_c"">&minus;([0-9]+) </span>");
            if (BelowZero == true)
            { Temp = "-" +      Regex.Match(words[3], @"unit unit_temperature_c"">&minus;([0-9]+) </span>").Groups[1].Value + " :snowflake:"; }
            else { Temp =       Regex.Match(words[3], @"unit unit_temperature_c"">(\+[0-9]+) </span>").Groups[1].Value + " :sunny:"; }
            string Pressure1 =  Regex.Match(words[7], @"unit unit_pressure_mm_hg_atm"">([0-9]+)").Groups[1].Value + " мм рт. ст.";
            string Pressure2 =  Regex.Match(words[7], @"nit unit_pressure_h_pa"">([0-9]+)").Groups[1].Value + " гПа";
            string Humidity =   Regex.Match(words[9], @"Влажность</div><div class=\""item-value\"">([0-9]+)").Groups[1].Value + "%";
                                                      
            var builder = new EmbedBuilder()
                .WithColor(new Color(54, 33, 150))
                .WithTitle($"{City} - {Wether}")
                .AddField("Температура по ощущениям", $"{Temp}", false)
                .AddField("Давление", $"{Pressure1}, {Pressure2}", false)
                .AddField("Влажность", $"{Humidity}", false)
                .AddField("Рассвет", $"{Dawn}", false)
                .AddField("Закат", $"{Dusk}", false)
                .WithCurrentTimestamp();
            var embed = builder.Build();
            await msg.Channel.SendMessageAsync(null, false, embed);
        }
        /// <summary>
        /// Запись ключей DictionaryGismeteo.GismeteoDB в переменную, для вывода в чат одним сообщением
        /// </summary>
        /// <param name="Dictionary"></param>
        /// <returns></returns>
        async void WeatherBD(DictionaryGismeteo Dictionary, SocketMessage msg)
        {
            string GismeteoList = "";
            foreach (string s in Dictionary.GismeteoDB.Keys)
            {
                GismeteoList += s + "\n";
            }

            var builder = new EmbedBuilder()
                .WithColor(new Color(54, 33, 150))
                .AddField($"Список городов", GismeteoList, false)
                .WithCurrentTimestamp();
            var embed = builder.Build();
            await msg.Channel.SendMessageAsync(null, false, embed);

        }
        #endregion
    }
}