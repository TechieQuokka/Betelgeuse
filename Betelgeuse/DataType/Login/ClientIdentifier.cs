using System.Collections.Concurrent;

namespace Betelgeuse.DataType.Login
{
    internal class ClientIdentifier : IDisposable
    {
        private bool disposedValue;

        public ISet<string> Names { get; private set; } = new HashSet<string>();
        public IDictionary<string, Guid> ClientNames { get; private set; } = new ConcurrentDictionary<string, Guid>();

        public bool Add (Guid clinetId, string pipeClientName)
        {
            if (string.IsNullOrWhiteSpace(pipeClientName)) return false;
            if (this.Names.Contains(pipeClientName)) return false;

            this.Names.Add(pipeClientName);
            this.ClientNames.Add(pipeClientName, clinetId);
            return true;
        }

        public bool Remove (Guid clientId)
        {
            var pair = this.ClientNames.FirstOrDefault(element => element.Value == clientId);
            if (string.IsNullOrWhiteSpace(pair.Key)) return false;

            this.Names.Remove(pair.Key);
            this.ClientNames.Remove(pair.Key);
            return true;
        }

        public bool Remove (string name)
        {
            if (this.Names.Contains(name) is false) return false;

            this.Names.Remove(name);
            this.ClientNames.Remove(name);
            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리형 상태(관리형 개체)를 삭제합니다.
                    this.Names.Clear();
                    this.ClientNames.Clear();
                }

                // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                disposedValue = true;
            }
        }

        // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
        // ~ClientIdentifier()
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
