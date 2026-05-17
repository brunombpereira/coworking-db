using System.Configuration;
using Microsoft.Data.SqlClient;

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
                case 50009: case 50011: case 50012:
                case 50016:                                    // lista_espera duplicada
                case 51001: case 51002: case 51010:            // SP validations
                case 51020:                                    // app lock
                case 51030: case 51031:                        // cancelar com reembolso
                case 51040: case 51041:                        // lista espera promoção
                case 51050:                                    // registar pagamento — serviço sem preço
                case 51060: case 51061:                        // self-registration (password curta, username duplicado)
                case 52001: case 52002: case 52003:            // auth
                case 52010: case 52011:                        // admin_create_user (role inválida, password curta)
                case 52012: case 52013:                        // admin_reset_password
                case 52014:                                    // admin_toggle_user_active
                    return ex.Message;
                default:
                    return "Erro ao comunicar com a base de dados.";
            }
        }
    }
}
