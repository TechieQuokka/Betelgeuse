namespace Betelgeuse.DataType.Login
{
    internal class AuthenticationData
    {
        public required string Name { get; set; }
        public required byte[] Key { get; set; }
    }
}
