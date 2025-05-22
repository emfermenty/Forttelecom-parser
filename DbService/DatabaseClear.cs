using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace testparser.DbService
{
    internal class DatabaseClear
    {
        private readonly string _connectionString;

        public DatabaseClear(DatabaseConnection connection)
        {
            _connectionString = connection.ConnectionString;
        }
        public void DeleteSwitchers()
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(_connectionString)) 
            {
                conn.Open();
                string query = $"CALL prc_delete_all_switches()";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, conn);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
    }
}
