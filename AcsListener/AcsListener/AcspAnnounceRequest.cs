using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    public class AcspAnnounceRequest
    {
        /// <summary>
        /// Per SMPTE 430-10:2010, the DCS-originated "Announce Request" is defined as :
        /// "Announce Request UL Name", of type "Pack Key" (length 16 bytes)
        /// "Length", of type "BER Length" (length 4 bytes)
        /// "Request ID", of type "UInt32" (length 4 bytes)
        /// "Current Time", of type "Int64" (length 8 bytes, measured as currrent system time in seconds from epoch)
        /// "Device Description", of type "Text" (variable length)
        /// </summary>
        /// 

        private AcspPackKey _key;
        private AcspBerLength _packLength;
        private Int32 _valueLength;
        private AcspRequestId _requestId;
        private DateTimeOffset _currentTime;
        private Int64 _currentEpochTime;
        private String _deviceDescription;
        private Byte[] _packArray;

        public AcspAnnounceRequest()
        {
            InitializeAnnounceRequestData();
            EncodePackArray();
        }

        private void EncodePackArray()
        {
            _packArray = new Byte[_valueLength + 20];  // For length of the value data plus BER Length (4) and PackKey (16)

            int i = 0; // Indexer for encoding _packArray

            _key.PackKey.CopyTo(_packArray, i); // Copy the 16 bytes of the AcspPackKey to the byte array _packArray;
            i = i + _key.PackKey.Length;  // Where length SHOULD be 16

            _packLength.LengthArray.CopyTo(_packArray, i);  // Copy the 4 bytes from AcspBerLength to the _packArray byte array. 
            i = i + _packLength.LengthArray.Length;    // Where length SHOULD be 4

            _requestId.IdArray.CopyTo(_packArray, i);  // Copy the expected 4 bytes from RequestID to the _packArray byte array
            i = i + _requestId.IdArray.Length;    // Where length SHOULD be 4

            Byte[] epochTimeArray = BitConverter.GetBytes(_currentEpochTime);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(epochTimeArray);
            }
            epochTimeArray.CopyTo(_packArray, i);  // Copy the expected 8 bytes from _currentEpochTime to the _packArray byte array
            i = i + epochTimeArray.Length;    // Where length SHOULD be 8

            Byte[] deviceDescriptionArray = Encoding.UTF8.GetBytes(_deviceDescription);
            deviceDescriptionArray.CopyTo(_packArray, i);  // Copy variable length device description to _packArray byte array
            i = i + deviceDescriptionArray.Length;
        }

        private void InitializeAnnounceRequestData()
        {
            _key = new AcspPackKey(Byte12Data.GoodRequest, Byte13NodeNames.AnnounceRequest);
            _requestId = new AcspRequestId();
            _currentTime = DateTimeOffset.UtcNow;
            _currentEpochTime = _currentTime.ToUnixTimeSeconds();
            _deviceDescription = "Proludio AcsListener Test Device Description";

            _valueLength = _deviceDescription.Length + 4 + 8;
            _packLength = new AcspBerLength(_valueLength);
        }

        /// <summary>
        /// UpdateTime() is designed to update the "timestamp" in the AnnounceRequest object and re-pack the byte array
        /// </summary>
        public void UpdateTime()
        {
            _currentTime = DateTimeOffset.UtcNow;
            _currentEpochTime = _currentTime.ToUnixTimeSeconds();
            EncodePackArray();
        }

        public Byte[] PackArray
        {
            get
            {
                return _packArray;
            }
        }

        public UInt32 RequestId
        {
            get
            {
                return _requestId.RequestId;
            }
        }
    }
}
