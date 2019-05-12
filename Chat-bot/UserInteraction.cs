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

        //Список пар пользователь-списокпесен, чтобы запоминать их, когда пользователь сделал запрос, но еще не ответил, правильная ли песня
        private IList<Tuple<long?, IList<Tuple<string, string, string>>>> chatsSongsList = new List<Tuple<long?, IList<Tuple<string, string, string>>>>();


        public UserInteraction()
        {
            //сущности для нахождения списка релевантных треков + ссылки на ютуб
            musicFinder = new MusixmatchFinder(musixMatchToken);
            youtube = new YoutubeListener(youtubeToken);
        }
  


        public IList<Tuple<long?, string>> PrepareAnswer(long? chat, string from, string text)
        {
            listOfMessages = new List<Tuple<long?, string>>();
            string answer = "";
            //переменные для ведения списка песен человека
            int chatListPosition = 0;
            bool isAnswerPending = false;
            Tuple<long?, IList<Tuple<string, string, string>>> currentChatSongsList = null;

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
                //находим наиболее релевантные треки по запросуы
                var songResults = musicFinder.FindSongByLyrics(text);

                //проверка на наличие найденных треков
                if (songResults != null)
                {

                    if (songResults.Count == 0)
                    {
                        //Если их нет, то отправляем пустой ответ
                        answer = string.Format("Hello, {0}.\nNo results. Change your request.", from);

                        listOfMessages.Add(new Tuple<long?, string>(chat, answer));
                        return listOfMessages;
                    }
                    else
                    {
                        //Если они есть, то формируем новый кортеж списка песен, кидаем его в общий список всех чатов и их песен, отправляем первую
                        var formedTuple = new Tuple<long?, IList<Tuple<string, string, string>>>(chat, songResults);
                        chatsSongsList.Add(formedTuple);
                        PrepareSongAnswer(chatsSongsList[chatsSongsList.IndexOf(formedTuple)], from);
                        return listOfMessages;
                    }
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
                switch (text)
                {
                    case "Yes":
                        //Если это правильная песня, то выкидываем из общего списка чатов и их песен этого пользователя
                        chatsSongsList.Remove(currentChatSongsList);
                        listOfMessages.Add(new Tuple<long?, string>(chat, "Great! You can ask me again!"));
                        return listOfMessages;
                    case "No":
                        //Если у нас осталась в памяти хотя бы одна песня
                        if (currentChatSongsList.Item2.Count > 1)
                        {
                            //Выкидываем первую, в прошлый раз она не подошла, отправляем следующую за ней
                            currentChatSongsList.Item2.RemoveAt(0);
                            PrepareSongAnswer(currentChatSongsList, from);
                            return listOfMessages;
                        }
                        //Иначе говорим, что ничего не нашли
                        else
                        {
                            answer = string.Format("Dear {0}!\nI can't find your song! Ask me anything else :(", from);
                            chatsSongsList.Remove(currentChatSongsList);
                            listOfMessages.Add(new Tuple<long?, string>(chat, answer));
                            return listOfMessages;
                        }
                    default:
                        //Если отправлен не ответ на вопрос, то просим повторить запрос
                        listOfMessages.Add(new Tuple<long?, string>(chat, "You still haven't answered the question. Answer 'Yes' or 'No'"));
                        return listOfMessages;
                }
            }
        }


        //метод для fормирования строки со треком для отправки
        private string SetAnswer(long? chat, Tuple<string, string, string> song, string from)
        {
            StringBuilder sb = new StringBuilder("Hello, ");
            sb.Append(from);
            sb.Append(".\nPossible result is: \n");
            sb.Append(string.Format("{0} - {1} (album: {2})\n", song.Item3, song.Item1, song.Item2));
            return sb.ToString();
        }

        //Метод для отправки сообщения с песней, ссылкой на ютуб и вопросом про правильность
        private void PrepareSongAnswer(Tuple<long?, IList<Tuple<string, string, string>>> songsList, string toWhom)
        {
            string answerInternal = string.Empty;
            string link = "";
            string youtubeAnswer = "";
            //формируем строку для нахождения на ютубе вида Исполнитель - НазваниеТрека
            var currentSong = string.Format("{0} - {1}", songsList.Item2[0].Item3, songsList.Item2[0].Item1);
            //получаем строку с треком
            answerInternal = SetAnswer(songsList.Item1, songsList.Item2[0], toWhom);
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

