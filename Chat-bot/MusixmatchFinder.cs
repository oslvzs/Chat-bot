using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Chat_bot
{
    class MusixmatchFinder
    {
        private const string musixmatchToken = "a9d4b1bbe71864653c7008ec4abf773e";
        private const string rootURL = "http://api.musixmatch.com/ws/1.1/";

        public string FindSongByLyrics(string lyrics)
        {
            string answer = string.Empty; 
            string finalURL = rootURL + "track.search?format=jsonp&callback=callback&q_lyrics="
                + WebUtility.UrlEncode(lyrics) + "&s_track_rating=desc&quorum_factor=1&apikey=" + musixmatchToken;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(finalURL);
            request.Method = "GET";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
               answer = reader.ReadToEnd();
            }

            Console.WriteLine(answer);

            return answer;
        }
    }
}
