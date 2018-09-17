using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    public class AcspGetNewLeaseResponse
    {
        private AcspRequestId _requestId;
        private AcspStatusResponse _statusResponse;

        public AcspGetNewLeaseResponse(Byte[] inputArray)
        {
            InitializeData(inputArray);
        }

        private void InitializeData(byte[] inputArray)
        {
            if (inputArray.Length < 9) // Expect at least a 9-byte array for RequestId + 5 bytes minimum for StatusResponse KLV
            {
                throw new ArgumentOutOfRangeException("Error: was expecting at least a 9-byte array for NewLeaseResponse constructor");
            }

            // Take a 4-byte slice and convert to UInt32 for RequestId
            int i = 0; // indexer for the inputArray

            Byte[] data = new Byte[4];
            Array.Copy(inputArray, 0, data, 0, data.Length);
            i = i + data.Length;
            _requestId = new AcspRequestId(data);

            // Take variable-length (but at least 5 bytes) slice and convert to AcspStatusResponse
            int statusLength = inputArray.Length - i;
            data = new Byte[statusLength];
            Array.Copy(inputArray, i, data, 0, data.Length);
            i = i + data.Length;
            _statusResponse = new AcspStatusResponse(data);
        }

        public UInt32 RequestId
        {
            get
            {
                return _requestId.RequestId;
            }
        }

        public string StatusResponseMessage
        {
            get
            {
                return _statusResponse.Message;
            }
        }

        public GeneralStatusResponseKey StatusResponseKey
        {
            get
            {
                return _statusResponse.Key;
            }
        }

        public string StatusResponseKeyString
        {
            get
            {
                return _statusResponse.KeyAsString;
            }
        }
    }
}
