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

        public VkListener(string token)
        {
            this.vkToken = token;
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

                foreach(var update in longPollHistory.Updates)
                {
                    if (update.Type == GroupUpdateType.MessageNew)
                    {
                        var userID = update.Message.FromId;
                        var from = api.Users.Get(new long[] { (long)userID }).FirstOrDefault().FirstName;
                        var text = update.Message.Text;                       

                        var songResults = musixmatch.FindSongByLyrics(text);
                        var answer = string.Empty;

                        if (songResults != null)
                        {
                            if (songResults.Count == 0)
                            {
                                answer = string.Format("Dear {0}.\nCant find shit. Change your request.", from);
                                SendMessage(userID, answer);
                            }
                            else
                            {
                                var topSong = string.Format("{0} - {1}", songResults[0].Item3, songResults[0].Item1);
                                var youtubeLink = youtube.TryYoutube(topSong);
                                answer = SetAnswer(songResults, from);
                                SendMessage(userID, answer);

                                if (youtubeLink != null)
                                {
                                    answer = string.Format("YouTube video for most relevant result:\n{0}", youtubeLink);
                                    SendMessage(userID, answer);
                                }
                                else
                                {
                                    answer = "Something is broken. Could not load video :(";
                                    SendMessage(userID, answer);
                                }
                            }
                        }
                        else
                        {
                            answer = string.Format("Hello, {0}.\nCurrently this bot is not working properly. Try again later.", from);
                            SendMessage(userID, answer);
                        }
                    }

                }
            }
        }

        private string SetAnswer(IList<Tuple<string, string, string>> songs, string from)
        {
            StringBuilder sb = new StringBuilder("Hello, ");
            sb.Append(from);
            sb.Append(".\nPossible results are: \n");
            int counter = 0;
            foreach (var item in songs)
            {
                counter++;
                sb.Append(string.Format("{0}) {1} - {2} (album: {3})\n", counter, item.Item3, item.Item1, item.Item2));
            }

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
    }
}
