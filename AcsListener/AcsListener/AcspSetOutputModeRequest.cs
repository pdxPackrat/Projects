using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    public class AcspSetOutputModeRequest
    {
        private AcspPackKey _key;
        private AcspBerLength _packLength;
        private AcspRequestId _requestId;
        private Byte _outputMode;  // Boolean 0=disable, 1=enabled
        private Byte[] _packArray;

        /// <summary>
        /// Constructs the "SetOutputModeRequest" object for the ACS message
        /// </summary>
        /// <param name="outputMode">True value sets the ACS output mode to "Enabled", a False value to "Disabled"</param>
        public AcspSetOutputModeRequest(bool outputMode)
        {
            InitializeData(outputMode);
            EncodePackArray();
        }

        private void InitializeData(bool outputMode)
        {
            _key = new AcspPackKey(Byte12Data.GoodRequest, Byte13NodeNames.SetOutputModeRequest);
            _packLength = new AcspBerLength(5);  // 4 bytes for RequestId, 1 byte for OutputMode
            _requestId = new AcspRequestId();

            int temp = 0;

            if (outputMode == true)
            {
                temp = 1;
            }
            _outputMode = (Byte)temp;
        }

        private void EncodePackArray()
        {
            _packArray = new Byte[25];  // 16 for PackKey, 4 for BERlength, 4 for RequestId, and 1 for OutputMode

            int i = 0;  // indexer for encoding _packArray

            _key.PackKey.CopyTo(_packArray, i);
            i = i + _key.PackKey.Length;  // where length SHOULD be 16

            _packLength.LengthArray.CopyTo(_packArray, i);
            i = i + _packLength.LengthArray.Length;   // where length SHOULD be 4

            _requestId.IdArray.CopyTo(_packArray, i);
            i = i + _requestId.IdArray.Length;    // where length SHOULD be 4

            _packArray[24] = _outputMode;
            i = i + 1;
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
