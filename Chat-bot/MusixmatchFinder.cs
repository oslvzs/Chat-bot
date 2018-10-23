using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Chat_bot
{
    class MusixmatchFinder
    {
        //Токен и URL для обращения к сервису
        private const string musixmatchToken = "a9d4b1bbe71864653c7008ec4abf773e";
        private const string rootURL = "http://api.musixmatch.com/ws/1.1/";

        //Метод нахождения песни по словам в ней
        public IList<Tuple<string, string, string>> FindSongByLyrics(string lyrics)
        {
            string answer = string.Empty; 

            //Формируем строку запроса
            string finalURL = rootURL + "track.search?format=jsonp&callback=callback&q_lyrics="
                + WebUtility.UrlEncode(lyrics) + "&s_track_rating=desc&quorum_factor=1&apikey=" + musixmatchToken;

            //Создаём сам запрос с методом GET
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(finalURL);
            request.Method = "GET";

            //Отправляем запрос и считываем ответ сервиса
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
               answer = reader.ReadToEnd();
            }

            //Преобразуем ответ в JSON-объект
            StringBuilder JSONizedAnswer = new StringBuilder(answer);
            JSONizedAnswer.Remove(0, 9);
            JSONizedAnswer.Remove(JSONizedAnswer.Length-2, 2);
            JObject answerJSON = JObject.Parse(JSONizedAnswer.ToString());

            //Вытаскиваем из объекта его "детей" с полезной информацией
            List<JToken> results = answerJSON["message"]["body"]["track_list"].Children().ToList();

            //Создаем список кортежей, в которых будет храниться только полезная информация: название трека, альбом и артист
            IList<Tuple<string, string, string>> songList = new List<Tuple<string, string, string>>();

            //Добавляем в список кортеж о каждой найденой песне
            foreach (JToken result in results)
            {
                Tuple<string, string, string> songInfo = new Tuple<string, string, string>(result["track"]["track_name"].ToString(), result["track"]["album_name"].ToString(), result["track"]["artist_name"].ToString());
                songList.Add(songInfo);

            }

            foreach(Tuple<string, string, string> thing in songList)
            {
                Console.WriteLine(thing.Item1 + ", " + thing.Item2 + ", " + thing.Item3 + ".");
            }

            return songList;
        }
    }
}
