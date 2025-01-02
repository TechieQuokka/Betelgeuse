namespace AsynchronousServer.StaticMethod
{
    public static class Network
    {
        public async static Task SendInChunksAsync(this Stream stream, byte[] data, int chunkSize)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(data);

            // Send data size first
            var dataSizeBuffer = BitConverter.GetBytes(data.Length);
            await stream.WriteAsync(dataSizeBuffer, 0, dataSizeBuffer.Length);

            for (int index = 0; index < data.Length; index += chunkSize)
            {
                int currentChunkSize = Math.Min(chunkSize, data.Length - index);
                await stream.WriteAsync(data, index, currentChunkSize);
            }

            await stream.FlushAsync();
            return;
        }

        public async static Task<byte[]> ReceiveInChunksAsync(this Stream stream, int chunkSize)
        {
            ArgumentNullException.ThrowIfNull(stream);

            // Read data size
            var dataSizeBuffer = new byte[sizeof(int)];
            _ = await stream.ReadAsync(dataSizeBuffer, 0, dataSizeBuffer.Length);
            int dataSize = BitConverter.ToInt32(dataSizeBuffer, 0);

            var receivedData = new List<byte>();
            var buffer = new byte[chunkSize];
            int bytesRead;
            int totalBytesRead = 0;

            while (totalBytesRead < dataSize && (bytesRead = await stream.ReadAsync(buffer, 0, chunkSize)) > 0)
            {
                receivedData.AddRange(buffer.Take(bytesRead));
                totalBytesRead += bytesRead;
            }

            return receivedData.ToArray();
        }
    }
}
