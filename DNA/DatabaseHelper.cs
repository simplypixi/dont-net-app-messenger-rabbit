using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data.Sql;
using System.Data;

namespace DNA
{
    class DatabaseHelper
    {
        private SqlConnection conn;
        private string connectionString;

        public DatabaseHelper()
        {
            this.connectionString = @"Data Source=montwulfpc\Baza;Initial Catalog=DNA; User Id=dna; Password=dna;";
        }

        public bool Login(string login, string password)
        {
            using (conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                String query = string.Format("SELECT 1 FROM [DNA].[dbo].[User] WHERE Login = '{0}' AND Password COLLATE Polish_CS_AS = '{1}'", login, password);
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    // TODO: DELETE THIS LINE
                    Console.WriteLine(command.CommandText);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }

        public bool Register(string login, string password)
        {
            if (this.IsUserExist(login))
            {
                return false;
            }

            using (conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                String query = string.Format("INSERT INTO [DNA].[dbo].[User] (Login, Password) VALUES('{0}', '{1}')", login, password);
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    // TODO: DELETE THIS LINE
                    Console.WriteLine(command.CommandText);

                    if (command.ExecuteNonQuery() > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public bool AddFriend(string ownerLogin, string friendLogin)
        {
            int? ownerId, friendId;
            ownerId = this.GetUserIdWithLogin(ownerLogin);
            friendId = this.GetUserIdWithLogin(friendLogin);

            if (ownerId == null || friendId == null)
            {
                return false;
            }

            using (conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                String query = string.Format("INSERT INTO [DNA].[dbo].[Friend] (OwnerId, FriendId) VALUES('{0}', '{1}')", ownerId, friendId);
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    // TODO: DELETE THIS LINE
                    Console.WriteLine(command.CommandText);

                    if (command.ExecuteNonQuery() > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public bool RemoveFriend(string ownerLogin, string friendLogin)
        {
            int? ownerId, friendId;
            ownerId = this.GetUserIdWithLogin(ownerLogin);
            friendId = this.GetUserIdWithLogin(friendLogin);

            if (ownerId == null || friendId == null)
            {
                return false;
            }

            using (conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                String query = string.Format("DELETE FROM [DNA].[dbo].[Friend] WHERE OwnerId = '{0}' AND FriendId = '{1}'", ownerId, friendId);
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    // TODO: DELETE THIS LINE
                    Console.WriteLine(command.CommandText);

                    if (command.ExecuteNonQuery() > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        private int? GetUserIdWithLogin(string login)
        {
            using (conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                String query = string.Format("SELECT Id FROM User WHERE Login = '{0}'", login);
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    // TODO: DELETE THIS LINE
                    Console.WriteLine(command.CommandText);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetInt32(1);
                        }
                    }
                }
                return null;
            }
        }

        private bool IsUserExist(string login)
        {
            using (conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                String query = string.Format("SELECT 1 FROM User WHERE Login = '{0}'", login);
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    // TODO: DELETE THIS LINE
                    Console.WriteLine(command.CommandText);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

    }
}
