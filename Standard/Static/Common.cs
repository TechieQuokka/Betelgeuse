using System.Text.Json;
using Standard.DataType;

namespace Standard.Static
{
    public static class Common
    {
        public static string ToJson(string data, string request)
        {
            var head = new Header() { Data = data, Request = request };
            return JsonSerializer.Serialize(head);
        }

        public static T GetFirstElement<T>(this T[]? array)
        {
            if (array == null || array.Length == 0)
            {
                throw new ArgumentException("Array is null or empty");
            }

            return array[0];
        }
    }
}
