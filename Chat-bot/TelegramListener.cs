using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Chat_bot
{
    public class TelegramListener : IChatListener
    {

        private readonly string telegramToken;
        private const string baseUrl = "https://api.telegram.org/bot";
        private UserInteraction telegramUserInteraction;

        public TelegramListener(string token)
        {
            this.telegramToken = token;

            telegramUserInteraction = new UserInteraction();
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

                    SendMessages(telegramUserInteraction.PrepareAnswer(chat, from, text));
                }
                //обновление и сохранение оffсета
                offset = updateId + 1;
                SetOffset(offset);
                offsetString = "?offset=" + GetOffset().ToString();
            }
        }

        //метод для отправки сообщения
        public void SendMessages(IList<Tuple<long?, string>> list)
        {
            foreach (Tuple<long?, string> current_message in list)
            {
                string methodUrl = baseUrl + telegramToken + "/sendMessage?chat_id=" + current_message.Item1 + "&text=" + WebUtility.UrlEncode(current_message.Item2);

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
    }
}
