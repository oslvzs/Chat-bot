using System.Threading;

namespace Chat_bot
{
    class Program
    {
        static void Main(string[] args)
        {
            string vkToken = Properties.Settings.Default.VkKey;
            string telegramToken = Properties.Settings.Default.TelegramKey;

            TelegramListener telegram = new TelegramListener(telegramToken);
            VkListener vk = new VkListener(vkToken);

            Thread telegramThread = new Thread(new ThreadStart(telegram.ListenChat));
            telegramThread.Start();
            Thread vkThread = new Thread(new ThreadStart(vk.ListenChat));
            vkThread.Start();
        }
    }
}
