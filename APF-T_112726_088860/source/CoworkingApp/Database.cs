using System.Configuration;
using System.Data.SqlClient;

namespace CoworkingApp
{
    public static class Database
    {
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["CoworkingDB"].ConnectionString;

        public static SqlConnection GetConnection()
        {
            var conn = new SqlConnection(ConnectionString);
            try   { conn.Open(); return conn; }
            catch { conn.Dispose(); throw; }
        }

        public static string SqlErrorMessage(SqlException ex)
        {
            switch (ex.Number)
            {
                case 2627: case 2601:
                    return "Já existe um registo com estes dados.";
                case 547:
                    return "Não é possível eliminar — registo em uso noutro lado.";
                case 50001: case 50002: case 50003: case 50004:
                case 50005: case 50006: case 50007: case 50008:
                    return ex.Message;
                default:
                    return "Erro ao comunicar com a base de dados.";
            }
        }
    }
}
