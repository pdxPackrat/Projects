using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    public class AcspUpdateTimelineRequest
    {
        private AcspPackKey _key;
        private AcspBerLength _packLength;
        private AcspRequestId _requestId;
        private Byte[] _playoutId;              // UInt32 representing unique identifier for the playout
        private Byte[] _timelinePosition;       // UInt64 representing current "edit unit" being displayed
        private Byte[] _editRateNumerator;      // UInt64 representing the edit rate (top number)
        private Byte[] _editRateDenominator;    // UInt64 representing the edit rate (bottom number / divisor)
        private Byte[] _timelineExtensionCount; // UInt32 representing number of timeline extensions (of type AcspTimelineExtension)
        private Byte[] _packArray;

        // Not putting in anything for the possible array of TimeLineExtension objects specified on page 11 of SMPTE 430-11:2010.
        // Right now I'm not sure how to implement the TimelineExtension part of this, so I'm leaving it out and will force extension count of 0 
        // in the default constructor, and no way to set that count. 
            
        /// <summary>
        /// Default constructor for UpdateTimelineRequest.  Takes the bare-minimum necessary parameters (playoutId and timelinePosition)
        /// and assumes default values for all the rest, (edit rate of 24:1) numerator=24, denominator=1, timelineExtensionCount=0.
        /// </summary>
        /// <param name="playoutId">UInt32 representing unique indentifier for this playout</param>
        /// <param name="timelinePosition">UInt64 representing the current "edit unit" to be displayed</param>
        public AcspUpdateTimelineRequest(UInt32 playoutId, UInt64 timelinePosition)
        {
            InitializeData(playoutId, timelinePosition);
            EncodeDataArray();

        }

        private void InitializeData(UInt32 playoutId, UInt64 timelinePosition)
        {
            _key = new AcspPackKey(Byte12Data.GoodRequest, Byte13NodeNames.UpdateTimelineRequest);

            // for now we are going with a FIXED-length schema where we do not have any of the variable-length AcspTimelineExtension involved
            _packLength = new AcspBerLength(36);  // RequestId(4), PlayoutId(4), TimelinePosition(8), RateNumerator(8), RateDenominator(8), ExtensionCount(4)

            _requestId = new AcspRequestId();

            ConvertPlayoutIdToByteArray(playoutId);

            ConvertTimelinePositionToByteArray(timelinePosition);

            // UInt64 numerator = 24;  // assuming a default edit rate of 24:1 for the default constructor
            UInt64 numerator = 25;  // TEMPORARILY overriding the edit unit for a specific test scenario
            ConvertRateNumeratorToByteArray(numerator);

            UInt64 denominator = 1; // assuming a default edit rate of 24:1 for the default constructor
            ConvertRateDenominatorToByteArray(denominator);

            UInt32 extensionCount = 0;  // assuming zero extension counts in this vanilla implementation
            ConvertExtensionCountToByteArray(extensionCount);

        }

        private void ConvertExtensionCountToByteArray(UInt32 extensionCount)
        {
            _timelineExtensionCount = BitConverter.GetBytes(extensionCount);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_timelineExtensionCount);
            }
        }

        private void ConvertRateDenominatorToByteArray(UInt64 denominator)
        {
            _editRateDenominator = BitConverter.GetBytes(denominator);   // expecting 8-byte array
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_editRateDenominator);
            }
        }

        private void ConvertRateNumeratorToByteArray(UInt64 numerator)
        {
            _editRateNumerator = BitConverter.GetBytes(numerator);   // expecting 8-byte array
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_editRateNumerator);
            }
        }

        /// <summary>
        /// Converts the UInt64 TimelinePosition to a 8-byte array, and reverses if LittleEndian
        /// </summary>
        /// <param name="timelinePosition">UInt64 representing the current "edit unit" being displayed</param>
        private void ConvertTimelinePositionToByteArray(UInt64 timelinePosition)
        {
            _timelinePosition = BitConverter.GetBytes(timelinePosition);  // expecting 8-byte array
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_timelinePosition);
            }
        }

        /// <summary>
        /// Converts the UInt32 PlayoutId to a 4-byte array, and reverses if LittleEndian
        /// </summary>
        /// <param name="playoutId">UInt32 representing the unique identifier for this playout</param>
        private void ConvertPlayoutIdToByteArray(UInt32 playoutId)
        {
            _playoutId = BitConverter.GetBytes(playoutId);  // expecting 4-byte array
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_playoutId);
            }
        }

        private void EncodeDataArray()
        {
            _packArray = new Byte[20 + _packLength.Length];  // For "vanilla" implementation this will be total of 56 bytes

            int i = 0;  // indexer for packArray

            _key.PackKey.CopyTo(_packArray, i);
            i = i + _key.PackKey.Length;  // where length SHOULD be 16

            _packLength.LengthArray.CopyTo(_packArray, i);
            i = i + _packLength.LengthArray.Length;  // where length SHOULD be 4

            _requestId.IdArray.CopyTo(_packArray, i);
            i = i + _requestId.IdArray.Length;   // where length SHOULD be 4

            _playoutId.CopyTo(_packArray, i);
            i = i + _playoutId.Length;  // where length SHOULD be 4

            _timelinePosition.CopyTo(_packArray, i);
            i = i + _timelinePosition.Length;  // where length SHOULD be 8

            _editRateNumerator.CopyTo(_packArray, i);
            i = i + _editRateNumerator.Length;  // where length SHOULD be 8

            _editRateDenominator.CopyTo(_packArray, i);
            i = i + _editRateDenominator.Length;  // where length SHOULD be 8

            _timelineExtensionCount.CopyTo(_packArray, i);
            i = i + _timelineExtensionCount.Length;  // where length SHOULD be 4
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

        /// <summary>
        /// UInt32 that represents the unique identifier for this playout.  Setting a 
        /// value for this property will convert it to a 4-byte array and re-encode the PackArray
        /// </summary>
        public UInt32 PlayoutId
        {
            set
            {
                ConvertPlayoutIdToByteArray(value);
                EncodeDataArray();
            }
        }
        
        /// <summary>
        /// UInt64 that represents the current "edit unit" being displayed.  Setting this property
        /// will convert the value to an 8-byte array and re-encode the PackArray
        /// </summary>
        public UInt64 TimelinePosition
        {
            set
            {
                ConvertTimelinePositionToByteArray(value);
                EncodeDataArray();
            }
        }

        /// <summary>
        /// UInt64 that represents the "edit rate" (left/top part of the ratio).
        /// Setting will convert the value to an 8-byte array and re-encode the PackArray
        /// </summary>
        public UInt64 EditRateNumerator
        {
            set
            {
                ConvertRateNumeratorToByteArray(value);
                EncodeDataArray();
            }
        }

        /// <summary>
        /// UInt64 that represents the "edit rate" (right/bottom part of the ratio).
        /// Setting will convert the value to an 8-byte array and re-encode the PackArray
        /// </summary>
        public UInt64 EditRateDenominator
        {
            set
            {
                ConvertRateDenominatorToByteArray(value);
                EncodeDataArray();
            }
        }
    }
}
