using System.Data.SqlClient;

namespace CoworkingApp
{
    public static class Database
    {
        private const string ConnectionString =
            @"Server=.\SQLEXPRESS;Database=CoworkingDB;Integrated Security=True;";

        public static SqlConnection GetConnection()
        {
            var conn = new SqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }
    }
}
