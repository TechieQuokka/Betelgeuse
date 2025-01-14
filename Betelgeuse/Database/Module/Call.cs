using MySql.Data.MySqlClient;

namespace Betelgeuse.Database.Module
{
    internal static class Call
    {
        public static object CallDBProcedure(this MySqlConnection connection, string name, (string name, MySqlDbType type) result, params (string parameter_name, object parameter)[] arguments)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            using (var command = new MySqlCommand(name, connection))
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;

                // 입력 파라미터 설정
                foreach (var element in arguments)
                {
                    command.Parameters.AddWithValue(element.parameter_name, element.parameter);
                }

                // 출력 파라미터 설정
                var isValidParam = new MySqlParameter(result.name, result.type);
                isValidParam.Direction = System.Data.ParameterDirection.Output;
                command.Parameters.Add(isValidParam);

                // 저장 프로시저 실행
                command.ExecuteNonQuery();

                // 출력 파라미터 값 확인
                return isValidParam.Value;
            }
        }

        public static IList<object> CallDBProcedure(this MySqlConnection connection, string name, params (string parameter_name, object parameter)[] arguments)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            IList<object> result = new List<object>(128);

            using (var command = new MySqlCommand(name, connection))
            {
                command.CommandType = System.Data.CommandType.StoredProcedure;

                // 입력 파라미터 설정
                foreach (var element in arguments)
                {
                    command.Parameters.AddWithValue(element.parameter_name, element.parameter);
                }

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new object[reader.FieldCount];
                        reader.GetValues(row);
                        result.Add(row);
                    }
                }
            }

            return result;
        }

        private static string ConcatenateArguments(int count)
        {
            var result = string.Empty;
            for (int index = 0; index < count; index++)
            {
                if (index + 1 == count) result += $"@item{index}";
                else result += $"@item{index}, ";
            }
            return result;
        }

        public static object? CallDBFunction(this MySqlConnection connection, string name, params object[] arguments)
        {
            if (connection == null) throw new ArgumentNullException(nameof(connection));

            string parameters = ConcatenateArguments(arguments.Length);
            using (MySqlCommand command = new MySqlCommand($"SELECT {name}({parameters})", connection))
            {
                for (int index = 0; index < arguments.Length; index++)
                {
                    command.Parameters.AddWithValue($"@item{index}", arguments[index]);
                }

                var result = command.ExecuteScalar();
                return result;
            }
        }
    }
}
