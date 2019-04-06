using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    public class AcspTerminateLeaseRequest
    {
        private AcspPackKey _key;
        private AcspBerLength _packLength;
        private AcspRequestId _requestId;
        private Byte[] _packArray;

        /// <summary>
        /// TerminateLease Request message pack that is sent to ACS to give up existing lease
        /// </summary>
        public AcspTerminateLeaseRequest()
        {
            InitializeData();
            EncodePackArray();
        }

        private void InitializeData()
        {
            _key = new AcspPackKey(Byte12Data.GoodRequest, Byte13NodeNames.TerminateLeaseRequest);
            _packLength = new AcspBerLength(4);
            _requestId = new AcspRequestId();
        }

        private void EncodePackArray()
        {
            _packArray = new Byte[24];  // PackKey(16), BERlength(4), RequestId(4)

            int i = 0;  // indexer for packArray

            _key.PackKey.CopyTo(_packArray, i);
            i = i + _key.PackKey.Length;   // where Length should be 16

            _packLength.LengthArray.CopyTo(_packArray, i);
            i = i + _packLength.LengthArray.Length;  // where Length should be 4

            _requestId.IdArray.CopyTo(_packArray, i);
            i = i + _requestId.IdArray.Length;  // where Length should be 4
        }

        public UInt32 RequestId
        {
            get
            {
                return _requestId.RequestId;
            }
        }

        public Byte[] PackArray
        {
            get
            {
                return _packArray;
            }
        }
    }
}
