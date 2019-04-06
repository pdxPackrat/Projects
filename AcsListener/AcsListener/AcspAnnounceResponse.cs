using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    /// <summary>
    /// Per SMPTE 430-10:2010, an ACS-originated "Announce Response" message will include
    /// the following items:
    /// "Announce Response UL Name", of type "Pack Key", length of 16 bytes
    /// "Length", of type "BER Length", length 4 bytes (to be used to determine length of rest of the byte pack)
    /// "Request ID", of type UInt32, length of 4 bytes
    /// "Current Time", of type Int64, length of 8 bytes
    /// "Device Description Length", of type BER length, length of 4 bytes
    /// "Device Description", of type "var", variable length
    /// "Status Response", of type "KLV", variable length (1 byte key, 4 byte BER length, variable message)
    /// </summary>
    public class AcspAnnounceResponse
    {
        private AcspRequestId _requestId;
        private Int64 _currentTime;
        private AcspBerLength _deviceDescriptionLength;
        private string _deviceDescription;
        private AcspStatusResponse _statusResponse;


        /// <summary>
        /// The AnnounceResponse constructor expects a byte array of at least 21 elements, 
        /// 4 bytes for RequestID, 8 bytes for EpochTime, 4 bytes for DeviceDescriptionLength, and a 
        /// minimum of 5 bytes for the KL (key and length) parts of the StatusResponse KLV.   The
        /// DeviceDescription and StatusMessage (the V in the KLV) are of variable length and will add to the 
        /// 21 byte minimum. 
        /// </summary>
        /// <param name="inputArray">Byte pack that is decoded in to the Announce Response</param>
        public AcspAnnounceResponse(Byte[] inputArray)
        {
            if (inputArray.Length < 21)
            {
                throw new ArgumentOutOfRangeException("Error: was expecting at least a 21-byte array for AnnounceResponse constructor");
            }

            // Take 4-byte slice and convert to UInt32 for RequestId
            int i = 0;
            Byte[] data = new Byte[4];
            Array.Copy(inputArray, 0, data, 0, data.Length);
            i = i + data.Length;
            _requestId = new AcspRequestId(data);

            // Take 8 byte slice and convert to Int64 for CurrentTime
            data = new Byte[8];
            Array.Copy(inputArray, i, data, 0, data.Length);
            i = i + data.Length;
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            _currentTime = BitConverter.ToInt64(data, 0);

            // Take 4 byte slice and convert to AcspBerLength
            data = new Byte[4];
            Array.Copy(inputArray, i, data, 0, data.Length);
            i = i + data.Length;
            _deviceDescriptionLength = new AcspBerLength(data);

            // Take variable-length byte slice and convert UTF-8 string
            int length = _deviceDescriptionLength.Length;
            if (length > 0)
            {
                data = new Byte[length];
                Array.Copy(inputArray, i, data, 0, data.Length);
                i = i + data.Length;
                _deviceDescription = Encoding.UTF8.GetString(data);
            }

            // Take variable-length (but at least 5 bytes) slice and convert to AcspStatusResponse
            int statusLength = inputArray.Length - i;
            data = new Byte[statusLength];
            Array.Copy(inputArray, i, data, 0, data.Length);
            _statusResponse = new AcspStatusResponse(data);
        }

        public Int64 CurrentTime
        {
            get
            {
                return _currentTime;
            }
        }

        public UInt32 RequestId
        {
            get
            {
                return _requestId.RequestId;
            }
        }

        public string DeviceDescription
        {
            get
            {
                return _deviceDescription;
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

        public String StatusResponseKeyString
        {
            get
            {
                return _statusResponse.KeyAsString;
            }
        }
    }
}
