using System;
using System.Collections.Generic;

namespace Chat_bot
{
    interface IChatListener
    {
        void ListenChat();
        void SendMessages(IList<Tuple<long?, string>> list);
    }
}
