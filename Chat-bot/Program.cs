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
            string telegramToken = Properties.Settings.Default.TelegramKey;
            string vkToken = Properties.Settings.Default.VkKey;

            //TelegramListener telegram = new TelegramListener(telegramToken);      
            //telegram.ListenChat();
            VkListener vk = new VkListener(vkToken);
            vk.ListenChat();
        }
    }
}
