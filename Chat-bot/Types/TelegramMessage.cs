using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_bot.Types
{
    class TelegramMessage
    {
        public int updateId { get; set; }
        public int chat { get; set; }
        public string senderName { get; set; }
        public string text { get; set; }

    }
}
