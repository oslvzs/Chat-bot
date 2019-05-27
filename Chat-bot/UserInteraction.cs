using Chat_bot.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Chat_bot
{
    class UserInteraction
    {
        private readonly string musixMatchToken = Properties.Settings.Default.MusixmatchKey;
        private readonly string youtubeToken = Properties.Settings.Default.YoutubeKey;
        private MusixmatchFinder musicFinder;
        private YoutubeListener youtube;
        private IList<Tuple<long?, string>> listOfMessages;

        //ссылка на наш объект взаимодействия с базой
        private DataBaseWriter dbw;

        //флаг для проверки, закончился ли список песен из базы
        private bool isDBListEmpty = false;

        //Список пар пользователь-списокпесен, чтобы запоминать их, когда пользователь сделал запрос, но еще не ответил, правильная ли песня
        private IList<Tuple<long?, string, IList<Track>>> chatsSongsList = new List<Tuple<long?, string, IList<Track>>>();


        public UserInteraction()
        {
            //сущности для нахождения списка релевантных треков + ссылки на ютуб
            musicFinder = new MusixmatchFinder(musixMatchToken);
            youtube = new YoutubeListener(youtubeToken);
            dbw  = DataBaseWriter.GetInstance();
        }
  


        public IList<Tuple<long?, string>> PrepareAnswer(long? chat, string from, string text)
        {
            listOfMessages = new List<Tuple<long?, string>>();
            string answer = "";
            //переменные для ведения списка песен человека
            int chatListPosition = 0;
            bool isAnswerPending = false;
            Tuple<long?, string, IList<Track>> currentChatSongsList = null;

            //находим список песен, которые были выданы человеку
            foreach (var chatSongsList in chatsSongsList)
            {
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
                var songResults = dbw.GetRelatableTracks(text);

                //проверка на наличие найденных треков
                if (songResults != null)
                {
                    //если в базе их нет
                    if (songResults.Count == 0)
                    {
                        //находим наиболее релевантные треки по запросу в сервисе
                        songResults = musicFinder.FindSongByLyrics(text);
                        isDBListEmpty = true;

                        //Если их и там нет, то отправляем пустой ответ
                        if (songResults.Count == 0)
                        { 
                            answer = string.Format("Hello, {0}.\nNo results. Change your request.", from);
                            listOfMessages.Add(new Tuple<long?, string>(chat, answer));
                            return listOfMessages;
                        }
                    }

                    //Если они есть, то формируем новый кортеж списка песен, кидаем его в общий список всех чатов и их песен, отправляем первую
                    SetNewChatSongList(chat, from, text, songResults);
                    return listOfMessages;  
                }
                else
                {
                    answer = string.Format("Hello, {0}.\nCurrently this bot is not working properly. Try again later.", from);
                    listOfMessages.Add(new Tuple<long?, string>(chat, answer));
                    return listOfMessages;
                }
            }
            else
            {
                switch (text.ToLower())
                {
                    case "yes":
                    case "y":

                        Track track = chatsSongsList[0].Item3[0];
                        if (isDBListEmpty)
                        {
                            dbw.InsertSong(track.Performer, track.Name, track.Album, currentChatSongsList.Item2);
                        }
                        else
                        {
                            dbw.UpdateRating(track.Performer, track.Name, track.Album, currentChatSongsList.Item2);
                        }

                        //Если это правильная песня, то выкидываем из общего списка чатов и их песен этого пользователя
                        chatsSongsList.Remove(currentChatSongsList);                
                        listOfMessages.Add(new Tuple<long?, string>(chat, "Great! You can ask me again!"));
                        return listOfMessages;
                    case "no":
                    case "n":
                        //Если у нас осталась в памяти хотя бы одна песня
                        if (currentChatSongsList.Item3.Count > 1)
                        {
                            //Выкидываем первую, в прошлый раз она не подошла, отправляем следующую за ней
                            currentChatSongsList.Item3.RemoveAt(0);
                            PrepareSongAnswer(currentChatSongsList, from);
                            return listOfMessages;
                        }
                        else
                        {
                            //Если мы уже прошли список песен из базы, и список песен из сервиса закончился
                            if (isDBListEmpty)
                            {
                                PrepareMessageWithNoResults(chat, from, currentChatSongsList);
                                return listOfMessages;
                            }
                            //Если это был только список из базы
                            else
                            {
                                isDBListEmpty = true;
                                var songResults = musicFinder.FindSongByLyrics(text);

                                //Если их и там нет, то отправляем ответ о пустом результате
                                if (songResults.Count == 0)
                                {
                                    PrepareMessageWithNoResults(chat, from, currentChatSongsList);
                                    return listOfMessages;
                                }
                                else
                                {

                                    //перенести первый запрос в новый кортеж
                                    text = currentChatSongsList.Item2;
                                    chatsSongsList.Remove(currentChatSongsList);

                                    //Если они есть, то формируем новый кортеж списка песен, кидаем его в общий список всех чатов и их песен, отправляем первую
                                    SetNewChatSongList(chat, from, text, songResults);
                                    return listOfMessages;
                                }
                            }
                        }
                    default:
                        //Если отправлен не ответ на вопрос, то просим повторить запрос
                        listOfMessages.Add(new Tuple<long?, string>(chat, "You still haven't answered the question. Answer 'Yes' or 'No'"));
                        return listOfMessages;
                }
            }
        }

        //Метод создания сообщения о том, что ничего не нашли
        private void PrepareMessageWithNoResults(long? chat, string from, Tuple<long?, string, IList<Track>> currentChatSongsList)
        {
            var answer = string.Format("Dear {0}!\nI can't find your song! Ask me anything else :(", from);
            chatsSongsList.Remove(currentChatSongsList);
            listOfMessages.Add(new Tuple<long?, string>(chat, answer));
        }

        //Метод создания нового кортежа пользователь-список
        private void SetNewChatSongList(long? chat, string from, string text, IList<Track> songResults)
        {           
            var formedTuple = new Tuple<long?, string, IList<Track>>(chat, text, songResults);
            chatsSongsList.Add(formedTuple);
            PrepareSongAnswer(chatsSongsList[chatsSongsList.IndexOf(formedTuple)], from);
        }

        //метод для fормирования строки со треком для отправки
        private string SetAnswer(long? chat, Track song, string from)
        {
            StringBuilder sb = new StringBuilder("Hello, ");
            sb.Append(from);
            sb.Append(".\nPossible result is: \n");
            sb.Append(string.Format("{0} - {1} (album: {2})\n", song.Performer, song.Name, song.Album));
            return sb.ToString();
        }

        //Метод для отправки сообщения с песней, ссылкой на ютуб и вопросом про правильность
        private void PrepareSongAnswer(Tuple<long?, string, IList<Track>> songsList, string toWhom)
        {
            string answerInternal = string.Empty;
            string link = "";
            string youtubeAnswer = "";
            //формируем строку для нахождения на ютубе вида Исполнитель - НазваниеТрека
            var currentSong = string.Format("{0} - {1}", songsList.Item3[0].Performer, songsList.Item3[0].Name);
            //получаем строку с треком
            answerInternal = SetAnswer(songsList.Item1, songsList.Item3[0], toWhom);
            //находим ссылку на топовый результат поиска
            youtubeAnswer = youtube.TryYoutube(currentSong);
            //отправляем сообщение со списком
            listOfMessages.Add(new Tuple<long?, string>(songsList.Item1, answerInternal));

            //Если ответ ютуб-сервиса не пустой, то отправляем видео, иначе ошибку
            if (youtubeAnswer != null)
            {
                link = string.Format("YouTube video for most relevant result:\n{0}", youtubeAnswer);
                listOfMessages.Add(new Tuple<long?, string>(songsList.Item1, link));
            }
            else
            {
                link = "Something is broken. Could not load video :(";
                listOfMessages.Add(new Tuple<long?, string>(songsList.Item1, link));
            }
            answerInternal = string.Format("Dear {0}!\nIs this the song that you searched? Answer 'Yes' or 'No'", toWhom);
            listOfMessages.Add(new Tuple<long?, string>(songsList.Item1, answerInternal));
        }
    }
}

