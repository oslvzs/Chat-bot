using System;
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
            //DataBaseWriter dbw = new DataBaseWriter();
            //var a = dbw.gimmeSong("банька парилка");
            //Console.WriteLine(a.Item1.ToString() + a.Item2.ToString() + a.Item3.ToString());
            //dbw.updateRating("Дима Смагин", "Баня", "зима", "банька парилка");
            //dbw.insertSong("Антон", "Иванов", "Пи", "аип");
        }
    }
}
