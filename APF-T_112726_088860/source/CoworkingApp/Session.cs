namespace CoworkingApp
{
    public static class Session
    {
        public static int    UtilizadorId { get; private set; }
        public static string Username     { get; private set; }
        public static string Role         { get; private set; }
        public static int?   ClienteId    { get; private set; }

        public static bool IsAuthenticated => !string.IsNullOrEmpty(Role);
        public static bool IsAdmin   => Role == "Admin";
        public static bool IsStaff   => Role == "Staff"   || IsAdmin;
        public static bool IsCliente => Role == "Cliente";

        public static void Login(int id, string username, string role, int? clienteId)
        {
            UtilizadorId = id;
            Username     = username;
            Role         = role;
            ClienteId    = clienteId;
        }

        public static void Clear()
        {
            UtilizadorId = 0;
            Username     = null;
            Role         = null;
            ClienteId    = null;
        }
    }
}
