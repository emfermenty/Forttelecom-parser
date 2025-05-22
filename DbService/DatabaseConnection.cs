using System.Configuration;

public class DatabaseConnection
{
    public string ConnectionString { get; }

    public DatabaseConnection() 
    {
        string host = ConfigurationManager.AppSettings["db_host"];
        string port = ConfigurationManager.AppSettings["db_port"];
        string user = ConfigurationManager.AppSettings["db_user"];
        string password = ConfigurationManager.AppSettings["db_password"];
        string dbName = ConfigurationManager.AppSettings["db_name"];

        ConnectionString = $"Host={host};Port={port};Username={user};Password={password};Database={dbName}";
    }
}