using Microsoft.Data.SqlClient;

namespace SmartCafe26.Desktop.Data
{
    public static class DbConnectionFactory
    {
        private static readonly string ConnectionString =
            @"Server=localhost\SQLEXPRESS01;Database=SmartCafe26;Trusted_Connection=True;TrustServerCertificate=True;";

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
    }
}