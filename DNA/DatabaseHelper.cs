// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DatabaseHelper.cs" company="DONTNET">
//   
// </copyright>
// <summary>
//   Defines the DatabaseHelper type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNA
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data.SqlClient;

    /// <summary>
    /// The database helper.
    /// </summary>
    public class DatabaseHelper
    {
        /// <summary>
        /// The conn.
        /// </summary>
        private SqlConnection conn;

        /// <summary>
        /// The connection string.
        /// </summary>
        private readonly string connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseHelper"/> class.
        /// </summary>
        public DatabaseHelper()
        {
            try
            {
                this.connectionString = ConfigurationManager.ConnectionStrings["dnaDB"].ConnectionString;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            if (string.IsNullOrEmpty(this.connectionString))
            {
                Console.WriteLine("Brak connection stringa w app.config.");
            }
        }

        /// <summary>
        /// The login method.
        /// </summary>
        /// <param name="login">
        /// The login.
        /// </param>
        /// <param name="password">
        /// The password.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool Login(string login, string password)
        {
            using (this.conn = new SqlConnection(this.connectionString))
            {
               this.conn.Open();
                var query = "SELECT 1 FROM [DNA].[dbo].[User] WHERE Login = @login AND Password COLLATE Polish_CS_AS = @password";
                using (var command = new SqlCommand(query, this.conn))
                {
                    command.Parameters.AddWithValue("login", login);
                    command.Parameters.AddWithValue("password", password);
                    using (var reader = command.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }

        /// <summary>
        /// The register.
        /// </summary>
        /// <param name="login">
        /// The login.
        /// </param>
        /// <param name="password">
        /// The password.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool Register(string login, string password)
        {
            if (this.IsUserExist(login))
            {
                return false;
            }

            using (this.conn = new SqlConnection(this.connectionString))
            {
                this.conn.Open();
                string query = "INSERT INTO [dbo].[User] (Login, Password) VALUES(@login, @password)";
                using (var command = new SqlCommand(query, this.conn))
                {
                    command.Parameters.AddWithValue("login", login);
                    command.Parameters.AddWithValue("password", password);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        /// <summary>
        /// The add friend.
        /// </summary>
        /// <param name="ownerLogin">
        /// The owner login.
        /// </param>
        /// <param name="friendLogin">
        /// The friend login.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool AddFriend(string ownerLogin, string friendLogin)
        {
            var ownerId = this.GetUserIdWithLogin(ownerLogin);
            var friendId = this.GetUserIdWithLogin(friendLogin);

            if (ownerId == null || friendId == null)
            {
                return false;
            }

            using (this.conn = new SqlConnection(this.connectionString))
            {
                this.conn.Open();
                var query = "INSERT INTO [dbo].[Friend] (OwnerId, FriendId, FriendLabel) VALUES(@ownerID, @friendID, @friendLogin)";
                using (var command = new SqlCommand(query, this.conn))
                {
                    command.Parameters.AddWithValue("ownerID", ownerId);
                    command.Parameters.AddWithValue("friendID", friendId);
                    command.Parameters.AddWithValue("friendLogin", friendLogin);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        /// <summary>
        /// The remove friend.
        /// </summary>
        /// <param name="ownerLogin">
        /// The owner login.
        /// </param>
        /// <param name="friendLogin">
        /// The friend login.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool RemoveFriend(string ownerLogin, string friendLogin)
        {
            var ownerId = this.GetUserIdWithLogin(ownerLogin);
            var friendId = this.GetUserIdWithLogin(friendLogin);

            if (ownerId == null || friendId == null)
            {
                return false;
            }

            using (this.conn = new SqlConnection(this.connectionString))
            {
                this.conn.Open();
                var query = "DELETE FROM [dbo].[Friend] WHERE OwnerId = @ownerID AND FriendId = @friendID";
                using (var command = new SqlCommand(query, this.conn))
                {
                    command.Parameters.AddWithValue("ownerID", ownerId);
                    command.Parameters.AddWithValue("friendID", friendId);
                    return command.ExecuteNonQuery() > 0;
                }
            }
        }

        /// <summary>
        /// The get friends.
        /// </summary>
        /// <param name="ownerLogin">
        /// The owner login.
        /// </param>
        /// <returns>
        /// The <see>
        ///         <cref>List</cref>
        ///     </see>
        ///     .
        /// </returns>
        public List<string> GetFriends(string ownerLogin)
        {
            List<string> friendsList;
            var ownerId = this.GetUserIdWithLogin(ownerLogin);

            if (ownerId == null)
            {
                return null;
            }

            using (this.conn = new SqlConnection(this.connectionString))
            {
                friendsList = new List<string>();
                this.conn.Open();
                var query = "SELECT FriendLabel FROM [dbo].[Friend] WHERE OwnerId = @ownerID";
                using (var command = new SqlCommand(query, this.conn))
                {
                    command.Parameters.AddWithValue("ownerID", ownerId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            friendsList.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return friendsList;
        }

        /// <summary>
        /// The get user id with login.
        /// </summary>
        /// <param name="login">
        /// The login.
        /// </param>
        /// <returns>
        /// The <see>
        ///         <cref>int?</cref>
        ///     </see>
        ///     .
        /// </returns>
        private int? GetUserIdWithLogin(string login)
        {
            using (this.conn = new SqlConnection(this.connectionString))
            {
                this.conn.Open();
                var query = "SELECT Id FROM [dbo].[User] WHERE Login = @login";
                using (var command = new SqlCommand(query, this.conn))
                {
                    command.Parameters.AddWithValue("login", login);
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

        /// <summary>
        /// The is user exist.
        /// </summary>
        /// <param name="login">
        /// The login.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool IsUserExist(string login)
        {
            using (this.conn = new SqlConnection(this.connectionString))
            {
                this.conn.Open();
                var query = "SELECT 1 FROM [dbo].[User] WHERE Login = @login";
                using (var command = new SqlCommand(query, this.conn))
                {
                    command.Parameters.AddWithValue("login", login);
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
