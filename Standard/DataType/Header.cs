namespace Standard.DataType
{
    /// <summary>
    /// To receive various types of data from the client.
    /// </summary>
    public class Header
    {
        public required string Request { get; set; } // command
        public required string Data { get; set; } // Json data
    }
}
