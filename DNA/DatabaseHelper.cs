using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

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
            using(conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(string.Format("SELECT 1 FROM [DNA].[dbo].[User] WHERE Login = '{0}' AND Password = '{1}'", login, password), conn))
                {
                    // TODO: DELETE THIS LINE
                    Console.WriteLine(command.CommandText);
                    using (var temp = command.ExecuteReader())
                    {
                        if (temp.Read())
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
            using (conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(string.Format("SELECT 1 FROM [DNA].[dbo].[User] WHERE Login = '{0}' AND Password = '{1}'", login, password), conn))
                {
                    // TODO: DELETE THIS LINE
                    Console.WriteLine(command.CommandText);
                    using (var temp = command.ExecuteReader())
                    {
                        if (temp.Read())
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

    }
}
