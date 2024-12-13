namespace Security
{
    public interface IAdvancedEncryptionStandard : IDisposable
    {
        public byte[] Encrypt(string plainText);
        public string Decrypt(byte[] cipherBytes);
    }
}
