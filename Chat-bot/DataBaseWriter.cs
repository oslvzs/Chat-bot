using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Chat_bot
{
    class DataBaseWriter
    {
        private string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\тпр\Source\Repos\Chat-bot4\Chat-bot\MusicBotDB.mdf;Integrated Security=True";
        private SqlConnection sqlConnection;
        private SqlDataReader sqlReader = null;
        private int rating;
        private int id;

        //возвращает исполнителя, трек и альбом, если они есть в бд
        public Tuple<string, string, string> gimmeSong(string stringFromSong)
        {
            //кортеж для возврата
            Tuple<string, string, string> artSong = null;
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
                    if (sqlReader.Read())
                        return artSong = new Tuple<string, string, string>(Convert.ToString(sqlReader["Artist"]), Convert.ToString(sqlReader["SongName"]), Convert.ToString(sqlReader["Album"]));
                    else
                        return null;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Не удалось соединиться с бд!");
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    //закрываем reader
                    if (sqlReader != null)
                        sqlReader.Close();
                }
                //if (sqlConnection != null && sqlConnection.State != ConnectionState.Closed)
                //    sqlConnection.Close();
                return null;
            }
        }

        public void updateRating(string artist, string songName, string album, string userString)
        {
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
                    Console.WriteLine(e);
                    Console.WriteLine("Не удалось выполнить запрос!");
                }
                //if (sqlConnection != null && sqlConnection.State != ConnectionState.Closed)
                //    sqlConnection.Close();
            }
        }

        public void insertSong(string artist, string songName, string album, string userString)
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
                    Console.WriteLine(e);
                    Console.WriteLine("Не удалось выполнить запрос!");
                }
                //if (sqlConnection != null && sqlConnection.State != ConnectionState.Closed)
                //    sqlConnection.Close();
            }
        }
}

}
