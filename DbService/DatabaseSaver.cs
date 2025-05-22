using Npgsql;
using ParserFortTelecom.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DatabaseSaver
{
    private readonly string _connectionString;

    public DatabaseSaver(DatabaseConnection connection)
    {
        _connectionString = connection.ConnectionString;
    }

    public void SaveSwitches(List<SwitchData> switches)
    {
        using (NpgsqlConnection conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();

            foreach (var sw in switches)
            {
                string query = $"CALL updateswitch(" +
                    $"'{sw.Company}', " +
                    $"'{sw.Name.Replace("'", "''")}', " +
                    $"{sw.Price}, " +
                    $"{(sw.PoEports.HasValue ? sw.PoEports.Value.ToString() : "NULL")}, " +
                    $"{(sw.SFPports.HasValue ? sw.SFPports.Value.ToString() : "NULL")}, " +
                    $"{(sw.UPS.HasValue ? sw.UPS.Value.ToString().ToLower() : "NULL")}, " +
                    $"'{sw.dateload}', " +
                //Console.Write(sw.dateload);
                   $"{sw.controllable.ToString().ToLower()});";
                //$"{("2025-03-31")}";
                using NpgsqlCommand cmd = new NpgsqlCommand(query, conn);
                cmd.ExecuteNonQuery();
            }

            conn.Close();
        }
    }
    public void falseall()
    {
        using (NpgsqlConnection conn = new NpgsqlConnection(_connectionString))
        {
            conn.Open();
            string query = $"CALL allfalse();";
            conn.Close();
        }
    }
}

