using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.RequestParams;

namespace Chat_bot
{
    class VkListener : IChatListener
    {
        private readonly string vkToken;
        private readonly ulong groupID = 182209141;
        private static VkApi api = new VkApi();

        private readonly string musixMatchToken = Properties.Settings.Default.MusixmatchKey;
        private readonly string youtubeToken = Properties.Settings.Default.YoutubeKey;

        private MusixmatchFinder musicFinder;
        private YoutubeListener youtube;

        //Список пар пользователь-список_песен, чтобы запоминать их, когда пользователь сделал запрос, но еще не ответил, правильная ли песня
        private IList<Tuple<long?, IList<Tuple<string, string, string>>>> chatsSongsList = new List<Tuple<long?, IList<Tuple<string, string, string>>>>();

        public VkListener(string token)
        {
            this.vkToken = token;

            //сущности для нахождения списка релевантных треков + ссылки на ютуб
            musicFinder = new MusixmatchFinder(musixMatchToken);
            youtube = new YoutubeListener(youtubeToken);
        }
        public void ListenChat()
        {
            MusixmatchFinder musixmatch = new MusixmatchFinder(musixMatchToken);
            YoutubeListener youtube = new YoutubeListener(youtubeToken);

            api.Authorize(new ApiAuthParams() { AccessToken = vkToken });

            while(true)
            {
                var longPollServer = api.Groups.GetLongPollServer(groupID);
                var longPollHistory = api.Groups.GetBotsLongPollHistory(
                    new BotsLongPollHistoryParams()
                    {
                        Server = longPollServer.Server,
                        Key = longPollServer.Key,
                        Ts = longPollServer.Ts,
                        Wait = 25
                    });

                if (longPollHistory.Updates.Count() == 0)
                    continue;

                string answer = "";
                foreach (var update in longPollHistory.Updates)
                {
                    if (update.Type == GroupUpdateType.MessageNew)
                    {
                        var userID = update.Message.FromId;
                        var from = api.Users.Get(new long[] { (long)userID }).FirstOrDefault().FirstName;
                        var text = update.Message.Text;


                        //переменные для ведения списка песен человека
                        int chatListPosition = 0;
                        bool isAnswerPending = false;
                        Tuple<long?, IList<Tuple<string, string, string>>> currentChatSongsList = null;

                        //находим список песен, которые были выданы человеку
                        foreach (var chatSongsList in chatsSongsList)
                        {
                            if (chatSongsList.Item1.Equals(userID))
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
                                    SendMessage(userID, answer);
                                }
                                else
                                {
                                    //Если они есть, то формируем новый кортеж списка песен, кидаем его в общий список всех чатов и их песен, отправляем первую
                                    var formedTuple = new Tuple<long?, IList<Tuple<string, string, string>>>(userID, songResults);
                                    chatsSongsList.Add(formedTuple);
                                    SendSongAnswer(chatsSongsList[chatsSongsList.IndexOf(formedTuple)], from);
                                }
                            }
                            else
                            {
                                answer = string.Format("Hello, {0}.\nCurrently this bot is not working properly. Try again later.", from);
                                SendMessage(userID, answer);
                            }
                        }
                        else
                        {
                            switch (text)
                            {
                                case "Yes":
                                    //Если это правильная песня, то выкидываем из общего списка чатов и их песен этого пользователя
                                    chatsSongsList.Remove(currentChatSongsList);
                                    SendMessage(userID, "Great! You can ask me again!");
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
                                        answer = string.Format("Dear {0}!\nI can't find your song! Ask me anything else :(", from);
                                        SendMessage(userID, answer);
                                        chatsSongsList.Remove(currentChatSongsList);
                                    }
                                    break;
                                default:
                                    //Если отправлен не ответ на вопрос, то просим повторить запрос
                                    SendMessage(userID, "You still haven't answered the question. Answer 'Yes' or 'No'");
                                    break;
                            }
                        }
                    }
                }

                }
            }
        //метод для fормирования строки со списком треков для отправки
        private string SetAnswer(long? chat, Tuple<string, string, string> song, string from)
        {
            StringBuilder sb = new StringBuilder("Hello, ");
            sb.Append(from);
            sb.Append(".\nPossible result is: \n");
            sb.Append(string.Format("{0} - {1} (album: {2})\n", song.Item3, song.Item1, song.Item2));
            return sb.ToString();
        }

        private void SendMessage(long? userID, string text)
        {
            Random rand = new Random();
            api.Messages.Send(
                new MessagesSendParams()
                {
                    RandomId = rand.Next(),
                    UserId = userID,
                    Message = text
                });
        }

        //Метод для отправки сообщения с песней, ссылкой на ютуб и вопросом про правильнос
        private void SendSongAnswer(Tuple<long?, IList<Tuple<string, string, string>>> songsList, string toWhom)
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

