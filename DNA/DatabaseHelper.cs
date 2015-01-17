using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Data.Sql;
using System.Data;
using System.Configuration;

namespace DNA
{
    class DatabaseHelper
    {
        private SqlConnection conn;
        private string connectionString;

        public DatabaseHelper()
        {
            try
            {
                this.connectionString = ConfigurationManager.ConnectionStrings["dnaDB"].ConnectionString;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (string.IsNullOrEmpty(this.connectionString))
            {
                Console.WriteLine("Brak connection stringa w app.config.");
            }
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
                String query = string.Format("INSERT INTO [dbo].[User] (Login, Password) VALUES('{0}', '{1}')", login, password);
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
                string query = string.Format("INSERT INTO [dbo].[Friend] (OwnerId, FriendId, FriendLabel) VALUES('{0}', '{1}', '{2}')", ownerId, friendId, friendLogin);
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
                String query = string.Format("DELETE FROM [dbo].[Friend] WHERE OwnerId = '{0}' AND FriendId = '{1}'", ownerId, friendId);
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

        public List<string> GetFriends(string ownerLogin)
        {
            List<string> friendsList = new List<string>();
            int? ownerId;
            ownerId = this.GetUserIdWithLogin(ownerLogin);

            if (ownerId == null)
            {
                return friendsList;
            }

            using (conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                String query = string.Format("SELECT FriendLabel FROM [dbo].[Friend] WHERE OwnerId = '{0}'", ownerId);
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    // TODO: DELETE THIS LINE
                    Console.WriteLine(command.CommandText);

                    using (var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            friendsList.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return friendsList;
        }

        private int? GetUserIdWithLogin(string login)
        {
            using (conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                String query = string.Format("SELECT Id FROM [dbo].[User] WHERE Login = '{0}'", login);
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    // TODO: DELETE THIS LINE
                    Console.WriteLine(command.CommandText);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetInt32(0);
                        }
                    }
                }
                return null;
            }
        }

        private string GetUserLoginWithId(int id)
        {
            using (conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                String query = string.Format("SELECT Login FROM [dbo].[User] WHERE Id = '{0}'", id);
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return reader.GetString(0);
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
                String query = string.Format("SELECT 1 FROM [dbo].[User] WHERE Login = '{0}'", login);
                using (SqlCommand command = new SqlCommand(query, conn))
                {
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
