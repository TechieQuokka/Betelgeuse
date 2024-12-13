using System.Security.Cryptography;
using System.Text;

namespace Security
{
    public class AdvancedEncryptionStandard : IAdvancedEncryptionStandard
    {
        public event EventHandler<string>? BeforeEncrypt;
        public event EventHandler<byte[]>? BeforeDecrypt;

        private bool disposedValue;
        private readonly Aes _aes;

        public AdvancedEncryptionStandard(string key)
        {
            this._aes = Aes.Create();

            var aes = this._aes;
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[16]; // mysql

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return;
        }

        public byte[] Encrypt (string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText)) throw new ArgumentNullException(nameof(plainText));

            this.BeforeEncrypt?.Invoke(this, plainText);
            using (var encryptor = this._aes.CreateEncryptor())
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                return encryptor.TransformFinalBlock (plainBytes, 0, plainBytes.Length);
            }
        }

        public string Decrypt(byte[] cipherBytes)
        {
            if (cipherBytes == null) throw new ArgumentNullException(nameof(cipherBytes));

            this.BeforeDecrypt?.Invoke(this, cipherBytes);
            using (var decryptor = this._aes.CreateDecryptor())
            {
                byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return Encoding.UTF8.GetString(plainBytes);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
                    _aes.Dispose();
                }

                // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                disposedValue = true;
            }
        }

        // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
        // ~AdvancedEncryptionStandard()
        // {
        //     // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
