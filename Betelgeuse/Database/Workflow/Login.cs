using System.Text.Json;
using Betelgeuse.Database.Module;
using MySql.Data.MySqlClient;
using Standard.DataType;

namespace Betelgeuse.Database.Workflow
{
    internal static class Login
    {
        public static bool IsLoginSuccessful(this MySqlConnection connection, byte[] buffer, int bufferSize, string commandString, out string identity)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            identity = string.Empty;
            string jsonString = System.Text.Encoding.UTF8.GetString(buffer, 0, bufferSize);
            var header = JsonSerializer.Deserialize<Header>(jsonString);
            if (header == null || header.Request != commandString) return false;

            var information = JsonSerializer.Deserialize<DataType.Login.Information>(header.Data);
            if (information == null) return false;

            object temp = connection.CallDBFunction("CheckUserPassword", information.ID, information.Password);
            bool result = Convert.ToBoolean(temp);
            if (result) identity = information.ID;
            return result;
        }
    }
}
