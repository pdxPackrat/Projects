using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    public class AcspGetStatusRequest
    {
        private AcspPackKey _key;
        private AcspBerLength _packLength;
        private AcspRequestId _requestId;
        private Byte[] _packArray;

        /// <summary>
        /// Constructs new AcspGetStatusRequest object, initializes the data, encodes the PackArray
        /// </summary>
        public AcspGetStatusRequest()
        {
            InitializeData();
            EncodePackArray();
        }

        private void InitializeData()
        {
            _key = new AcspPackKey(Byte12Data.GoodRequest, Byte13NodeNames.GetStatusRequest);
            _packLength = new AcspBerLength(4);  // 4 bytes for the RequestId
            _requestId = new AcspRequestId();
        }

        private void EncodePackArray()
        {
            _packArray = new Byte[24];

            int i = 0;  // Indexer to PackArray

            _key.PackKey.CopyTo(_packArray, i);
            i = i + _key.PackKey.Length;  // Where length SHOULD be 16

            _packLength.LengthArray.CopyTo(_packArray, i);
            i = i + _packLength.LengthArray.Length;   // Where length SHOULD be 4

            _requestId.IdArray.CopyTo(_packArray, i);
            i = i + _requestId.IdArray.Length;  // Where length SHOULD be 4
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
