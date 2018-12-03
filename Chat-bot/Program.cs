using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chat_bot
{
    class Program
    {
        static void Main(string[] args)
        {
            //TelegramListener telegram = new TelegramListener();
            //telegram.ListenChat();
            YoutubeListener ytl = new YoutubeListener();
            ytl.TryYoutube("банька парилка");
        }
    }
}
