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
            conn.Open();
            return conn;
        }

        public static string SqlErrorMessage(SqlException ex)
        {
            if (ex.Number == 2627 || ex.Number == 2601)
                return "Já existe um registo com estes dados.";
            if (ex.Number == 547)
                return "Não é possível eliminar — registo em uso noutro lado.";
            if (ex.Message.Contains("sobreposta") || ex.Message.Contains("Sobreposição") ||
                ex.Message.Contains("sobreposição") || ex.Message.Contains("horário"))
                return ex.Message;
            if (ex.Message.Contains("capacidade") || ex.Message.Contains("participantes"))
                return ex.Message;
            if (ex.Message.Contains("disponível") || ex.Message.Contains("disponivel"))
                return ex.Message;
            return "Erro ao comunicar com a base de dados.";
        }
    }
}
