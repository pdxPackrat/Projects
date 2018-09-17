using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    public class AcspSetRplLocationRequest
    {
        private AcspPackKey _key;
        private AcspBerLength _packLength;
        private AcspRequestId _requestId;
        private Byte[] _playoutId;
        private Byte[] _resourceUrl;
        private Byte[] _packArray;

        public AcspSetRplLocationRequest(String inputUrl, UInt32 inputId)
        {
            InitializeData(inputUrl, inputId);
            EncodePackArray();
        }

        private void EncodePackArray()
        {
            _packArray = new Byte[20 + _packLength.Length];  // 20 bytes for PackKey & BERlength, and then variable for the rest

            int i = 0; // index for encoding _packArray

            _key.PackKey.CopyTo(_packArray, i);
            i = i + _key.PackKey.Length;  // where length SHOULD be 16

            _packLength.LengthArray.CopyTo(_packArray, i);
            i = i + _packLength.LengthArray.Length;  // where length SHOULD be 4

            _requestId.IdArray.CopyTo(_packArray, i);
            i = i + _requestId.IdArray.Length;  // where length SHOULD be 4

            _playoutId.CopyTo(_packArray, i);
            i = i + _playoutId.Length;  // where length SHOULD be 4

            _resourceUrl.CopyTo(_packArray, i);
            i = i + _resourceUrl.Length;  // variable length 
        }

        private void InitializeData(String inputUrl, UInt32 inputId)
        {
            _key = new AcspPackKey(Byte12Data.GoodRequest, Byte13NodeNames.SetRplLocationRequest);

            // Calculate required length of the value part of the packArray
            // which is 4 bytes for RequestId, 4 bytes for PlayoutId, and a 
            // variable amount for the length of the ResourceUrl

            int length = 4 + 4 + inputUrl.Length;
            _packLength = new AcspBerLength(length);

            // Generate the new RequestId
            _requestId = new AcspRequestId();

            // Set the Playout ID
            _playoutId = BitConverter.GetBytes(inputId);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_playoutId);
            }

            // Set the Resource URL
            // Note that per SMPTE 430-10:2010, this field is supposed to be of type "URL", which may be different than expected UTF8
            _resourceUrl = Encoding.UTF8.GetBytes(inputUrl);
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
