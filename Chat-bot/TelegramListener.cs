using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using Chat_bot.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Chat_bot
{
    public class TelegramListener : IChatListener
    {
        
        private readonly string token = Properties.Settings.Default.TelegramKey;
        private const string baseUrl = "https://api.telegram.org/bot";
        
        public void ListenChat()
        {
            //сущности для нахождения списка релевантных треков + ссылки на ютуб
            MusixmatchFinder musicFinder = new MusixmatchFinder();
            YoutubeListener youtube = new YoutubeListener();
            //получение сдвига
            int offset = GetOffset();
            string offsetString = "?offset=" + GetOffset().ToString();
            string methodUrl = baseUrl + token + "/getUpdates";
            string topSong = "";

            //частота запросов
            int requestFrequency = 1000;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (true)
            {
                if (sw.ElapsedMilliseconds % requestFrequency != 0)
                    continue;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(methodUrl + offsetString);
                request.Method = "GET";
                //получение списка непрочитанных сообщений
                try
                {
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    Stream stream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(stream);
                    string responseString = reader.ReadToEnd();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Невозможно получить ответ от Телеграма");
                    Console.WriteLine(e.Message);
                    continue;
                }

                int updateId = offset;

                //список всех непрочитанных сообщений в джейсоне
                JObject lastMessages = JObject.Parse(responseString);
                //составляем ответ для каждого сообщения
                foreach (JToken message in lastMessages["result"])
                {
                    //айди сообщений (апдейта), айди чата, имя пользователя и текст сообщения
                    updateId = message["update_id"].Value<int>();
                    int chat = message["message"]["chat"]["id"].Value<int>();
                    string from = message["message"]["from"]["first_name"].Value<string>();
                    string text = message["message"]["text"].Value<string>();

                    //находим наиболее релевантные треки по запросуы
                    var songResults = musicFinder.FindSongByLyrics(text);
                    string answer = "";
                    string link = "";
                    string youtubeAnswer = "";

                    //проверка на наличие найденных треков
                    if (songResults != null)
                    {
                        if (songResults.Count == 0)
                        {
                            answer = string.Format("Hello, {0}.\nNo results. Change your request.", from);
                            SendMessage(chat, answer);
                        }
                        else
                        {
                            //fормируем строку для нахождения на ютубе вида Исполнитель - НазваниеТрека
                            topSong = string.Format("{0} - {1}", songResults[0].Item3, songResults[0].Item1);
                            //получаем строку со списком треков
                            answer = SetAnswer(chat, songResults, from);
                            //находим ссылку на топовый результат поиска
                            youtubeAnswer = youtube.TryYoutube(topSong);
                            //отправляем сообщение со списком
                            SendMessage(chat, answer);

                            //отправка видео или сообщения об ошибке
                            if (youtubeAnswer != null)
                            {
                                link = string.Format("YouTube video for most relevant result:\n{0}", youtubeAnswer);
                                SendMessage(chat, link);
                            }
                            else
                            {
                                link = "Something is broken. Could not load video :(";
                                SendMessage(chat, link);
                            }
                        }
                    }
                    else
                    {
                        answer = string.Format("Hello, {0}.\nCurrently this bot is not working properly. Try again later.", from);
                        SendMessage(chat, answer);
                    }
                }

                //обновление и сохранение оffсета
                offset = updateId + 1;
                SetOffset(offset);
                offsetString = "?offset=" + GetOffset().ToString();
            }
        }

        //метод для fормирования строки со списком треков для отправки
        private string SetAnswer(int chat, IList<Tuple<string, string, string>> songs, string from) 
        {
            StringBuilder sb = new StringBuilder("Hello, ");
            sb.Append(from);
            sb.Append(".\nPossible results are: \n");
            int counter = 0;
            foreach (var item in songs)
            {
                counter++;
                sb.Append(string.Format("{0}) {1} - {2} (album: {3})\n", counter, item.Item3, item.Item1, item.Item2));
            }

            return sb.ToString();
        }

        //метод для отправки сообщения
        public void SendMessage(int chat_id, string text)
        {
            string methodUrl = baseUrl + token + "/sendMessage?chat_id="+chat_id+"&text="+WebUtility.UrlEncode(text);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(methodUrl);
            request.Method = "POST";

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string responseString = reader.ReadToEnd();
            }
            catch (Exception e)
            {
                Console.WriteLine("Невозможно отправить сообщение.");
                Console.WriteLine(e.Message);
            }

        }

        //метод получения оffсета 
        private int GetOffset()
        {
            int offset;
            try
            {
                offset = Properties.Settings.Default.Offset;
            }
            catch (Exception e)
            {
                offset = int.MinValue;
                Console.WriteLine(e.Message);
            }
            return offset;
        }

        //сохранение оffсета
        private int SetOffset(int offset)
        {
            try
            {
                Properties.Settings.Default.Offset = offset;
                Properties.Settings.Default.Save();
                return 1;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 0;
            }          
        }

        //тестовый метод получения инfормации о боте
        public User GetMe()
        {
            string methodUrl = baseUrl + token + "/getMe";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(methodUrl);
            request.Method = "GET";
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string responseString = reader.ReadToEnd();
            }
            catch (Exception e)
            {                                
                Console.WriteLine("Невозможно получить инfормацию о боте.");
                Console.WriteLine(e.Message);
            }            

            JObject botInfo = JObject.Parse(responseString);
            JToken result = botInfo["result"];

            User bot = new User
            {
                id = int.Parse(result["id"].ToString()),
                is_bot = bool.Parse(result["is_bot"].ToString()),
                first_name = result["first_name"].ToString(),
                username = result["username"].ToString()
            };

            return bot;
        }
    }
}
