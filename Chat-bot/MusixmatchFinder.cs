using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Chat_bot.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Chat_bot
{
    public class MusixmatchFinder
    {
        //Токен и URL для обращения к сервису
        private string musixmatchToken;
        private const string rootURL = "http://api.musixmatch.com/ws/1.1/";

        //Конструктор класса
        public MusixmatchFinder(string token)
        {
            this.musixmatchToken = token;
        }

        //Метод нахождения песни по словам в ней
        public IList<Track> FindSongByLyrics(string lyrics)
        {
            JSONFormatter formatter = new JSONFormatter();

            //Создаем список кортежей, в которых будет храниться только полезная информация: название трека, альбом и исполнитель
            IList<Track> songList = new List<Track>();

            //Формируем строку запроса
            string finalURL = rootURL + "track.search?format=jsonp&callback=callback&q_lyrics="
                + WebUtility.UrlEncode(lyrics) + "&s_track_rating=desc&quorum_factor=1&apikey=" + musixmatchToken;

            //Создаём сам запрос с методом GET
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(finalURL);
            request.Method = "GET";

            string answer = string.Empty;
            try
            {
                //Отправляем запрос и считываем ответ сервиса
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    answer = reader.ReadToEnd();
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("При попытке соединения с API Musixmatch что-то пошло не так!");
                Console.WriteLine(e.Message);
                return null;
            }


            List<Track> tracks = formatter.ParseTracks(answer, 7);
            if (tracks == null)
                return null;

            //Добавляем в список кортеж о каждой найденой песне, если в списке объектов меньше 7
            foreach (Track track in tracks)
            {
                songList.Add(new Track(track.Name, track.Album, track.Performer));
            }

            return songList;
        }
    }
}
