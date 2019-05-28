using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.IO;
using Chat_bot.Types;

namespace Chat_bot
{
    class DataBaseWriter
    {
        private static DataBaseWriter instance;


        private readonly string connectionString;
        private SqlConnection sqlConnection;
        private SqlDataReader sqlReader = null;
        private static object locker = new object();

        //Делаем его синглтоном
        protected DataBaseWriter()
        {
            connectionString = string.Format("Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={0}\\MusicBotDB.mdf;Integrated Security=True", Path.GetDirectoryName(Path.GetDirectoryName(System.IO.Directory.GetCurrentDirectory())));
        }

        public static DataBaseWriter GetInstance()
        {
            if (instance == null)
            {
                lock(locker)
                {
                    if (instance == null)
                        instance = new DataBaseWriter();
                }
            }
            return instance;
        }

        //возвращает список треков (если есть в бд)
        public IList<Track> GetRelatableTracks(string stringFromSong)
        {
            //cписок треков для возврата
            List<Track> trackList = new List<Track>();
            //using statment для корректной работы IDisposable объектов
            using (sqlConnection = new SqlConnection(connectionString))
            {
                //открываем соединение 
                sqlConnection.Open();
                //создаем sql команду
                SqlCommand select = new SqlCommand("SELECT * FROM [Table] WHERE [String]=@String ORDER BY [Rating] DESC ", sqlConnection);
                select.Parameters.AddWithValue("String", stringFromSong);

                try
                {
                    //выполняем её
                    sqlReader = select.ExecuteReader();
                    //считываем результат
                    while (sqlReader.Read())
                    {
                        Track currentTrack = new Track(Convert.ToString(sqlReader["SongName"]), Convert.ToString(sqlReader["Album"]), Convert.ToString(sqlReader["Artist"]));
                        trackList.Add(currentTrack);
                    }
                    if (trackList.Count == 0)
                        return null;
                    return trackList;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Не удалось соединиться с бд!");
                    Console.WriteLine(e.Message);
                    return null;
                }
                finally
                {
                    //закрываем reader
                    if (sqlReader != null)
                        sqlReader.Close();
                }
                //if (sqlConnection != null && sqlConnection.State != ConnectionState.Closed)
                //    sqlConnection.Close();
                //return null;
            }
        }

        public void UpdateRating(string artist, string songName, string album, string userString)
        {
            int rating = 0;
            int id = -1;
            using (sqlConnection = new SqlConnection(connectionString))
            {

                sqlConnection.Open();
                SqlCommand select = new SqlCommand("SELECT * FROM [Table] WHERE [Artist]=@Artist AND [SongName]=@SongName AND [Album]=@Album AND [String]=@String", sqlConnection);
                select.Parameters.AddWithValue("Artist", artist);
                select.Parameters.AddWithValue("Songname", songName);
                select.Parameters.AddWithValue("Album", album);
                select.Parameters.AddWithValue("String", userString);
                try
                {
                     sqlReader = select.ExecuteReader();
                    if (sqlReader.Read())
                    {
                        rating = Convert.ToInt32(sqlReader["Rating"]);
                        id = Convert.ToInt32(sqlReader["Id"]);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Не удалось соединиться с бд!");
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    if (sqlReader != null)
                        sqlReader.Close();
                }
                SqlCommand update = new SqlCommand("UPDATE [Table] SET [Rating]=@Rating WHERE [Id]=@Id", sqlConnection);
                update.Parameters.AddWithValue("@Rating", rating + 1);
                update.Parameters.AddWithValue("@Id", id);
                try
                {
                    update.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Не удалось выполнить запрос!");
                }
                //if (sqlConnection != null && sqlConnection.State != ConnectionState.Closed)
                //    sqlConnection.Close();
            }
        }

        public void InsertSong(string artist, string songName, string album, string userString)
        {
            using (sqlConnection = new SqlConnection(connectionString))
            {
                sqlConnection.Open();
                SqlCommand insert = new SqlCommand("INSERT INTO [Table] (Artist, SongName, String, Album)VALUES(@Artist, @SongName, @String, @Album)", sqlConnection);
                insert.Parameters.AddWithValue("Artist", artist);
                insert.Parameters.AddWithValue("Songname", songName);
                insert.Parameters.AddWithValue("String", userString);
                insert.Parameters.AddWithValue("Album", album);
                try
                {
                    insert.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Не удалось выполнить запрос!");
                }
                //if (sqlConnection != null && sqlConnection.State != ConnectionState.Closed)
                //    sqlConnection.Close();
            }
        }
}

}
