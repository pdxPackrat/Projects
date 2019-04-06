using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{

    public enum Byte12Data
    {
        BadRequest = 0x01, GoodRequest = 0x02
    }

    public enum Byte13BadRequest
    {
        BadRequest = 0x01
    }

    public enum Byte13NodeNames
    {
        AnnounceRequest = 0x00, AnnounceResponse = 0x01,
        GetNewLeaseRequest = 0x02, GetNewLeaseResponse = 0x03,
        GetStatusRequest = 0x04, GetStatusResponse = 0x05,
        SetRplLocationRequest = 0x06, SetRplLocationResponse = 0x07,
        SetOutputModeRequest = 0x08, SetOutputModeResponse = 0x09,
        UpdateTimelineRequest = 0x0A, UpdateTimelineResponse = 0x0B,
        TerminateLeaseRequest = 0x0C, TerminateLeaseResponse = 0x0D,
        GetLogEventListRequest = 0x10, GetLogEventListResponse = 0x11,
        GetLogEventRequest = 0x12, GetLogEventResponse = 0x13
    }

    public class AcspPackKey
    {

        private Byte[] _packKey = new Byte[16];

        // This should be the "Good Message" constructor formatting for the PackKey
        public AcspPackKey(Byte12Data byte12, Byte13NodeNames byte13)
        {
            SetDefaultPackKeyValues();

            // Here we set bytes "12 and 13" (using 1s-indexing) for the CSP message
            _packKey[11] = (Byte)byte12; // Presumably, if this the "Good Message" then byte12 will have a value of 0x02;
            _packKey[12] = (Byte)byte13; // This can be one of a number of different values depending upon the message type
        }

        public AcspPackKey(Byte12Data byte12, Byte13BadRequest byte13)
        {
            SetDefaultPackKeyValues();

            // Here we set bytes "12 and 13" (using 1s-indexing) for the CSP message
            _packKey[11] = (Byte)byte12; // Presumably, if this the "Bad Message" then byte12 will have a value of 0x01;
            _packKey[12] = (Byte)byte13; // Given that this is the "Bad Message" constructor, we should only have one option for byte13 (0x01);
        }

        public AcspPackKey(Byte[] inputArray)
        {
            if (inputArray.Length != 16)
            {
                throw new IndexOutOfRangeException("Error: expecting a 16-byte array");
            }

            inputArray.CopyTo(_packKey, 0);
        }

        public bool IsBadRequest
        {
            get
            {
                bool result = false;

                if (_packKey[11] == (Byte)Byte12Data.BadRequest)
                {
                    result = true;
                }

                return result;
            }
        }

        public bool IsGoodRequest
        {
            get
            {
                bool result = false;

                if (_packKey[11] == (Byte)Byte12Data.GoodRequest)
                {
                    result = true;
                }

                return result;
            }
        }

        public Byte13NodeNames NodeNames
        {
            get
            {
                return (Byte13NodeNames) _packKey[12];
            }
        }

        private void SetDefaultPackKeyValues()
        {
            _packKey[0] = 0x06;  // Object Identifier, Object ID
            _packKey[1] = 0x0E;  // Label size, Length of UL (Universal Label)
            _packKey[2] = 0x2B;  // Designator, Sub Identifier
            _packKey[3] = 0x34;  // Designator, SMPTE Identifier
            _packKey[4] = 0x02;  // Registry Category Designator, KLV Groups (Sets and Packs)
            _packKey[5] = 0x05;  // Registry Designator, Fixed Length Pack
            _packKey[6] = 0x01;  // Structure Designator, Groups Dictionary
            _packKey[7] = 0x01;  // Version Number, Registry Version: Dictionary version 1
            _packKey[8] = 0x02;  // Item Designator, Administration
            _packKey[9] = 0x07;  // Organization, Access Control
            _packKey[10] = 0x02; // Application, Auxilliar Content Synchronization Protocol (ACSP)

            // CSP Bytes "12 & 13" (using 1s index) are set depending upon the type of message, so are not part of the DEFAULT value settings

            _packKey[13] = 0x00; // Reserved, Not assigned
            _packKey[14] = 0x00; // Reserved, Not assigned
            _packKey[15] = 0x00; // Reserved, Not assigned
        }

        public Byte[] PackKey
        {
            get
            {
                return _packKey;
            }
        }
    }
}
