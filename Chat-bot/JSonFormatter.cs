using Chat_bot.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_bot
{
    class JSONFormatter
    {
        public List<TelegramMessage> ParseLastMessages (string stringResponse)
        {
            JObject lastMessages = new JObject();
            try
            {
                lastMessages = JObject.Parse(stringResponse);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

            List<TelegramMessage> answer = new List<TelegramMessage>();

            foreach (JToken message in lastMessages["result"])
            {
                TelegramMessage currentMessage = new TelegramMessage()
                {
                    updateId = message["update_id"].Value<int>(),
                    chat = message["message"]["chat"]["id"].Value<int>(),
                    senderName = message["message"]["from"]["first_name"].Value<string>(),
                    text = message["message"]["text"].Value<string>(),
                };
                answer.Add(currentMessage);
            }

            return answer;
        }

        public List<Track> ParseTracks (string stringAnswer, int trackCount)
        {

            JObject jsonAnswer = new JObject();
            try
            {
                //Преобразуем ответ в JSON-объект
                StringBuilder JSONizedAnswer = new StringBuilder(stringAnswer);
                JSONizedAnswer.Remove(0, 9);
                JSONizedAnswer.Remove(JSONizedAnswer.Length - 2, 2);
                jsonAnswer = JObject.Parse(JSONizedAnswer.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Не удалось преобразовать ответ от сервера в JSON-объект!");
                Console.WriteLine(e.Message);
                return null;
            }

            List<Track> answer = new List<Track>();

            List<JToken> jsonChildren = new List<JToken>();
            try
            {
                jsonChildren = jsonAnswer["message"]["body"]["track_list"].Children().ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }

            foreach (JToken result in jsonChildren)
            {
                if (answer.Count < trackCount)
                {
                    Track currentTrack = new Track()
                    {
                        performer = result["track"]["artist_name"].ToString(),
                        name = result["track"]["track_name"].ToString(),
                        album = result["track"]["album_name"].ToString(),
                    };

                    answer.Add(currentTrack);
                }
            }
            return answer;
        }
    }
}
