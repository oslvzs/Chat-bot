using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_bot.Types
{
    class TelegramMessage
    {
        public int UpdateId { get; set; }
        public int Chat { get; set; }
        public string SenderName { get; set; }
        public string Text { get; set; }

    }
}
