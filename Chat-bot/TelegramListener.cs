using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Chat_bot
{
    public class TelegramListener : IChatListener
    {
        
        private readonly string telegramToken;
        private const string baseUrl = "https://api.telegram.org/bot";

        private readonly string musixMatchToken = Properties.Settings.Default.MusixmatchKey;
        private readonly string youtubeToken = Properties.Settings.Default.YoutubeKey;
        private MusixmatchFinder musicFinder;
        private YoutubeListener youtube;

        //Список пар пользователь-список_песен, чтобы запоминать их, когда пользователь сделал запрос, но еще не ответил, правильная ли песня
        private IList<Tuple<int, IList<Tuple<string, string, string>>>> chatsSongsList = new List<Tuple<int, IList<Tuple<string, string, string>>>>();

        public TelegramListener(string token)
        {
            this.telegramToken = token;

            //сущности для нахождения списка релевантных треков + ссылки на ютуб
            musicFinder = new MusixmatchFinder(musixMatchToken);
            youtube = new YoutubeListener(youtubeToken);
        }
        
        public void ListenChat()
        {
            //получение сдвига
            int offset = GetOffset();
            string offsetString = "?offset=" + GetOffset().ToString();
            string methodUrl = baseUrl + telegramToken + "/getUpdates";

            //частота запросов
            int requestFrequency = 1000;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            string responseString;
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
                    responseString = reader.ReadToEnd();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Невозможно получить ответ от Телеграма");
                    Console.WriteLine(e.Message);
                    break;
                }

                int updateId = offset;
                string answer = "";
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

                    //переменные для ведения списка песен человека
                    int chatListPosition = 0;
                    bool isAnswerPending = false;
                    Tuple<int, IList<Tuple<string, string, string>>> currentChatSongsList = null;

                    //находим список песен, которые были выданы человеку
                    foreach (var chatSongsList in chatsSongsList)
                    {
                        Console.WriteLine(chatSongsList.Item1 + ": " + chatSongsList.Item2.Count);
                        if (chatSongsList.Item1.Equals(chat))
                        {
                            isAnswerPending = true;
                            currentChatSongsList = chatSongsList;
                            chatListPosition = chatsSongsList.IndexOf(currentChatSongsList);
                        }
                    }

                    //Если он еще не ответил, что нашел песню
                    if (!isAnswerPending)
                    {
                        //находим наиболее релевантные треки по запросуы
                        var songResults = musicFinder.FindSongByLyrics(text);                        

                        //проверка на наличие найденных треков
                        if (songResults != null)
                        {
                            
                            if (songResults.Count == 0)
                            {
                                //Если их нет, то отправляем пустой ответ
                                answer = string.Format("Hello, {0}.\nNo results. Change your request.", from);
                                SendMessage(chat, answer);
                            }
                            else
                            {
                                //Если они есть, то формируем новый кортеж списка песен, кидаем его в общий список всех чатов и их песен, отправляем первую
                                var formedTuple = new Tuple<int, IList<Tuple<string, string, string>>>(chat, songResults);
                                chatsSongsList.Add(formedTuple);                             
                                SendSongAnswer(chatsSongsList[chatsSongsList.IndexOf(formedTuple)], from);
                            }
                        }
                        else
                        {
                            answer = string.Format("Hello, {0}.\nCurrently this bot is not working properly. Try again later.", from);
                            SendMessage(chat, answer);
                        }
                    }
                    else
                    {
                       switch (text){
                            case "Yes":
                                //Если это правильная песня, то выкидываем из общего списка чатов и их песен этого пользователя
                                chatsSongsList.Remove(currentChatSongsList);
                                SendMessage(chat, "Great! You can ask me again!");
                                break;
                            case "No":
                                //Если у нас осталась в памяти хотя бы одна песня
                                if (currentChatSongsList.Item2.Count > 1)
                                {
                                    //Выкидываем первую, в прошлый раз она не подошла, отправляем следующую за ней
                                    currentChatSongsList.Item2.RemoveAt(0);
                                    SendSongAnswer(currentChatSongsList, from);
                                }
                                //Иначе говорим, что ничего не нашли
                                else
                                {
                                    Console.WriteLine("Wrong Answer. No more songs.");
                                    answer = string.Format("Dear {0}!\nI can't find your song! Ask me anything else :(", from);
                                    SendMessage(chat, answer);
                                    chatsSongsList.Remove(currentChatSongsList);
                                }
                                break;
                            default:
                                //Если отправлен не ответ на вопрос, то просим повторить запрос
                                SendMessage(chat, "You still haven't answered the question. Answer 'Yes' or 'No'");
                                break;
                        }
                    }
                }
                //обновление и сохранение оffсета
                offset = updateId + 1;
                SetOffset(offset);
                offsetString = "?offset=" + GetOffset().ToString();
            }
        }

        //метод для fормирования строки со списком треков для отправки
        private string SetAnswer(int chat, Tuple<string, string, string> song, string from) 
        {
            StringBuilder sb = new StringBuilder("Hello, ");
            sb.Append(from);
            sb.Append(".\nPossible result is: \n");
            sb.Append(string.Format("{0} - {1} (album: {2})\n", song.Item3, song.Item1, song.Item2));
            return sb.ToString();
        }

        //метод для отправки сообщения
        public void SendMessage(int chat_id, string text)
        {
            string methodUrl = baseUrl + telegramToken + "/sendMessage?chat_id="+chat_id+"&text="+WebUtility.UrlEncode(text);

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

        //Метод для отправки сообщения с песней, ссылкой на ютуб и вопросом про правильнос
        private void SendSongAnswer(Tuple<int, IList<Tuple<string, string, string>>> songsList, string toWhom)
        {
            string answer = "";
            string link = "";
            string youtubeAnswer = "";
            //формируем строку для нахождения на ютубе вида Исполнитель - НазваниеТрека
            var currentSong = string.Format("{0} - {1}", songsList.Item2[0].Item3, songsList.Item2[0].Item1);
            //получаем строку со списком треков
            answer = SetAnswer(songsList.Item1, songsList.Item2[0], toWhom);
            //находим ссылку на топовый результат поиска
            youtubeAnswer = youtube.TryYoutube(currentSong);
            //отправляем сообщение со списком
            SendMessage(songsList.Item1, answer);

            //Если ответ ютуб-сервиса не пустой, то отправляем видео, иначе ошибку
            if (youtubeAnswer != null)
            {
                link = string.Format("YouTube video for most relevant result:\n{0}", youtubeAnswer);
                SendMessage(songsList.Item1, link);
            }
            else
            {
                link = "Something is broken. Could not load video :(";
                SendMessage(songsList.Item1, link);
            }          
            answer = string.Format("Dear {0}!\nIs this the song that you searched? Answer 'Yes' or 'No'", toWhom);
            SendMessage(songsList.Item1, answer);
            }
        }

    }

