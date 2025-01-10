﻿namespace AsynchronousServer.StaticMethod
{
    public static class Network
    {
        private static byte[] _buffer = new byte[1024];

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
            int byteRead = await stream.ReadAsync(dataSizeBuffer, 0, dataSizeBuffer.Length);
            if (byteRead != dataSizeBuffer.Length)
            {
                throw new InvalidOperationException("Failed to read data size.");
            }
            int dataSize = BitConverter.ToInt32(dataSizeBuffer, 0);

            if (_buffer.Length < chunkSize) _buffer = new byte[chunkSize];

            var receivedData = new List<byte>();
            var buffer = _buffer;
            int bytesRead = 0;
            int totalBytesRead = 0;

            while (totalBytesRead < dataSize && (bytesRead = await stream.ReadAsync(buffer, 0, chunkSize)) > 0)
            {
                receivedData.AddRange(buffer.Take(bytesRead));
                totalBytesRead += bytesRead;
            }

            if (totalBytesRead != dataSize)
            {
                throw new InvalidOperationException("Failed to read the complete data.");
            }
            return receivedData.ToArray();
        }
    }
}
