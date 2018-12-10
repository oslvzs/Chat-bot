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

        private readonly string token = "715661565:AAGZjW3w3WzV2M6o7MM2SxXmetH5X3yYhMA";
        private const string baseUrl = "https://api.telegram.org/bot";

        public void ListenChat()
        {
            MusixmatchFinder musicFinder = new MusixmatchFinder();
            int offset = GetOffset();
            string offsetString = "?offset=" + GetOffset().ToString();
            string methodUrl = baseUrl + token + "/getUpdates";

            int requestFrequency = 3000; 
            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (true)
            {
                if (sw.ElapsedMilliseconds % requestFrequency != 0)
                    continue;

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(methodUrl + offsetString);
                request.Method = "GET";

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string responseString = reader.ReadToEnd();

                int updateId = offset;

                JObject lastMessages = JObject.Parse(responseString);
                foreach (JToken message in lastMessages["result"])
                {
                    updateId = message["update_id"].Value<int>();
                    int chat = message["message"]["chat"]["id"].Value<int>();
                    string from = message["message"]["from"]["first_name"].Value<string>();
                    string text = message["message"]["text"].Value<string>();

                    var songResults = musicFinder.FindSongByLyrics(text);
                    string answer = SetAnswer(chat, songResults, from);

                    SendMessage(chat, answer);
                }

                offset = updateId + 1;
                SetOffset(offset);
                offsetString = "?offset=" + GetOffset().ToString();
            }
        }

        private string SetAnswer(int chat, IList<Tuple<string, string, string>> songs, string from) 
        {
            StringBuilder sb = new StringBuilder("Microchelik ");
            sb.Append(from);
            if (songs.Count == 0)
            {
                sb.Append("\nRezultatov net. Pomenyai zapros ili ya xz. Zhizn pomenyai.");
                return sb.ToString();
            }
            sb.Append("\nChekai: \n");
            int counter = 0;
            foreach (var item in songs)
            {
                counter++;
                sb.Append(counter);
                sb.Append(") ");
                sb.Append(item.Item3);
                sb.Append(" - ");
                sb.Append(item.Item1);
                sb.Append(" iz alboma ");
                sb.Append(item.Item2);
                sb.Append("\n");
            }


            return sb.ToString();
        }

        public void SendMessage(int chat_id, string text)
        {
            string methodUrl = baseUrl + token + "/sendMessage?chat_id="+chat_id+"&text="+WebUtility.UrlEncode(text);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(methodUrl);
            request.Method = "POST";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            string responseString = reader.ReadToEnd();

        }

        public void ListenTemp()
        {
            string methodUrl = baseUrl + token + "/getUpdates";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(methodUrl);
            request.Method = "GET";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            string responseString = reader.ReadToEnd();
            int updateId = GetOffset();

            JObject lastMessages = JObject.Parse(responseString);
            foreach (JToken message in lastMessages["result"])
            {
                updateId = message["update_id"].Value<int>();
                string from = message["message"]["from"]["first_name"].Value<string>();
                int replyTo = message["message"]["message_id"].Value<int>();
                string username = message["message"]["from"]["username"].Value<string>();
                int chat = message["message"]["chat"]["id"].Value<int>();
                string text = message["message"]["text"].Value<string>();
            }

            SetOffset(updateId+1);
        }


        private int GetOffset()
        {
            int offset;
            try
            {
                offset = int.Parse(File.ReadAllText("offset.txt"));
            }
            catch (Exception e)
            {
                offset = int.MinValue; ;
            }
            return offset;
        }
        private int SetOffset(int offset)
        {
            try
            {
                File.WriteAllText("offset.txt", offset.ToString());
                return 1;
            }
            catch
            {
                return 0;
            }          
        }
        public User GetMe()
        {
            string methodUrl = baseUrl + token + "/getMe";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(methodUrl);
            request.Method = "GET";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream stream = response.GetResponseStream();
            StreamReader reader = new StreamReader(stream);
            string responseString = reader.ReadToEnd();

            JObject botInfo = JObject.Parse(responseString);
            JToken result = botInfo["result"];



            User bot = new User();
            bot.id = int.Parse(result["id"].ToString());
            bot.is_bot = bool.Parse(result["is_bot"].ToString());
            bot.first_name = result["first_name"].ToString();
            bot.username = result["username"].ToString();

            return bot;
        }
    }
}
