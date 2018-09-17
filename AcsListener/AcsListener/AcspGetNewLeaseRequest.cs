using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    public class AcspGetNewLeaseRequest
    {
        private AcspPackKey _key;
        private AcspBerLength _packLength;
        private AcspRequestId _requestId;
        private Byte[] _leaseDuration;
        private Byte[] _packArray;


        /// <summary>
        /// Constructor that takes a UInt32 value (see below) and constructs the PackArray for the message
        /// </summary>
        /// <param name="leaseDurationSeconds">UInt32 representing the number of seconds to request for ACS lease duration</param>
        public AcspGetNewLeaseRequest(UInt32 leaseDurationSeconds)
        {
            InitializeData(leaseDurationSeconds);
            EncodePackArray();

        }

        private void InitializeData(UInt32 leaseDurationSeconds)
        {
            _key = new AcspPackKey(Byte12Data.GoodRequest, Byte13NodeNames.GetNewLeaseRequest);
            _packLength = new AcspBerLength(8); // 4 bytes for RequestId, 4 bytes for LeaseDuration
            _requestId = new AcspRequestId();

            _leaseDuration = BitConverter.GetBytes(leaseDurationSeconds);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_leaseDuration);
            }

        }

        private void EncodePackArray()
        {
            _packArray = new Byte[28];  // 28 bytes, 16 for Key, 4 for BerLength, 4 for RequestId, and 4 for LeaseDuration
            int i = 0; // Indexer for encoding _packArray

            _key.PackKey.CopyTo(_packArray, i);
            i = i + _key.PackKey.Length; // Where length SHOULD be 16, always

            _packLength.LengthArray.CopyTo(_packArray, i);  // Copy the 4 bytes from AcspBerLength to the _packArray byte array.
            i = i + _packLength.LengthArray.Length;  // Where length SHOULD be 4, always

            _requestId.IdArray.CopyTo(_packArray, i); // Copy the 4 bytes from RequestId to the _packArray byte array.
            i = i + _requestId.IdArray.Length;  // Where length SHOULD be 4, always

            _leaseDuration.CopyTo(_packArray, i);  // Copy the 4 bytes from LeaseDuration to the _packArray byte array.
            i = i + _leaseDuration.Length;
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
