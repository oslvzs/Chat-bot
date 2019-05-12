using System;
using System.Collections.Generic;
using System.Linq;
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

        private UserInteraction vkUserInteraction;

        public VkListener(string token)
        {
            this.vkToken = token;

            vkUserInteraction = new UserInteraction();
        }
        public void ListenChat()
        {

            api.Authorize(new ApiAuthParams() { AccessToken = vkToken });

            while (true)
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

                foreach (var update in longPollHistory.Updates)
                {
                    if (update.Type == GroupUpdateType.MessageNew)
                    {
                        var userID = update.Message.FromId;
                        var from = api.Users.Get(new long[] { (long)userID }).FirstOrDefault().FirstName;
                        var text = update.Message.Text;

                        SendMessages(vkUserInteraction.PrepareAnswer(userID, from, text));

                    }
                }

            }
        }

        public void SendMessages(IList<Tuple<long?, string>> list)
        {
            foreach (Tuple<long?, string> current_message in list)
            {
                Random rand = new Random();
                api.Messages.Send(
                new MessagesSendParams()
                {
                    RandomId = rand.Next(),
                    UserId = current_message.Item1,
                    Message = current_message.Item2
                });
            }
        }

    }
}