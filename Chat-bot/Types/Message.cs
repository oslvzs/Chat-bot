using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_bot.Types
{
    class Message
    {
        public int message_id;
        public User from;
        public int date;
        public Chat chat;
        public string text;
    }
}
