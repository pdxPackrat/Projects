using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SharedCommon
{
    /// <summary>Helper extension class for clearing out a NetworkStream so that the next message can be read</summary>
    public static class NetworkStreamCleaner
    {
        /// <summary>Clears the stream for next message.</summary>
        /// <param name="inputStream">The input stream to be cleared</param>
        /// <returns>total number of bytes read while clearing the input stream</returns>
        public static int ClearStreamForNextMessage(this NetworkStream inputStream)
        {
            if (inputStream.DataAvailable)
            {
                int totalBytesRead = 0;
                byte[] tempBuffer = new byte[1024];  // we don't actually care what goes in to the buffer, it is just a temporary holder

                while (inputStream.DataAvailable)
                {
                    totalBytesRead += inputStream.Read(tempBuffer, 0, tempBuffer.Length);
                }

                return totalBytesRead;
            }
            else
            {
                return 0;
            }
        }
    }
}
