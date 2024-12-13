using System.Text.Json;
using MySql.Data.MySqlClient;
using Standard.DataType;
using Standard.Static;

namespace Betelgeuse
{
    public partial class MainProgram
    {
        private static bool IsLoginSuccessful(MySqlConnection connection, byte[] buffer, int bufferSize, out string identity)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            identity = string.Empty;
            string jsonString = System.Text.Encoding.UTF8.GetString(buffer, 0, bufferSize);
            var header = JsonSerializer.Deserialize<Header>(jsonString);
            if (header == null) return false;

            var information = JsonSerializer.Deserialize<DataType.Login.Information>(header.Data);
            if (information == null) return false;

            object temp = connection.CallDBProcedure("CheckUserPassword", ("p_is_valid", MySqlDbType.Bit), ("p_user_id", information.ID), ("p_password", information.Password));
            bool result = Convert.ToBoolean(temp);
            if (result) identity = information.ID;
            return result;
        }
    }
}
