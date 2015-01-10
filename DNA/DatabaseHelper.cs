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
            this.connectionString = @"Data Source=.;Initial Catalog=DNA; User Id=dna; Password=dna;";
        }

        public bool Login(string login, string password)
        {
            using(conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                String query = string.Format("SELECT 1 FROM [DNA].[dbo].[User] WHERE Login = '{0}' AND Password = '{1}'", login, password);
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
            using (conn = new SqlConnection(this.connectionString))
            {
                conn.Open();
                String query = string.Format("INSERT INTO [DNA].[dbo].[User] (Login, Password) VALUES('{0}', '{1}')", login, password);
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    // TODO: DELETE THIS LINE
                    Console.WriteLine(command.CommandText);

                    if(command.ExecuteNonQuery() > 0 )
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
