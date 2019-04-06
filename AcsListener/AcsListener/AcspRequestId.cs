using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    /// <summary>
    /// Per SMPTE 430-10:2010, a "Request ID" is defined as an application-level tag for the Request,
    /// which shall be echoed in the corresponding Response.  A non-zero Request_ID value shall be set by the 
    /// DCS, which should select unique values (e.g. a sequencing counter) for each connection it manages. 
    /// (Request_ID generation means is left to implementers). 
    /// </summary>
    public class AcspRequestId
    {
        private static UInt32 _nextId = 1;
        private UInt32 _requestId; 
        private Byte[] _idArray;

        /// <summary>
        /// AcspRequestId constructor that increments a static UInt32 field by 1, converts that number
        /// to a 4-byte array, reverses (for big-endian).  IdArray is exposed for the encoded byte array. 
        /// </summary>
        public AcspRequestId()
        {
            AssignNextIdNumber();

            EncodeRequestId();
        }

        /// <summary>
        /// AcspRequestId constructor that accepts a (big-endian) 4-byte array, decodes that back to a UInt32, 
        /// and confirms that it matches the current RequestId (or throws an exception). 
        /// </summary>
        /// <param name="inputArray">A 4-byte big-endian array that gets converted to UInt32</param>
        public AcspRequestId(Byte[] inputArray)
        {
            DecodeRequestId(inputArray);
            EncodeRequestId();  // After decoding the byte array passed to the constructor, re-encode for the exposed IdArray
        }

        private void DecodeRequestId(byte[] inputArray)
        {
            if (inputArray.Length != 4)
            {
                throw new ArgumentOutOfRangeException("Error: was excpecting a 4-byte array passed to the RequestId constructor");
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(inputArray);
            }

            _requestId = BitConverter.ToUInt32(inputArray, 0);
        }

        private void EncodeRequestId()
        {
            _idArray = BitConverter.GetBytes(_requestId);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_idArray);
            }
        }

        private void AssignNextIdNumber()
        {
            _requestId = _nextId;
            _nextId++;
        }

        public Byte[] IdArray
        {
            get
            {
                return _idArray;
            }
        }

        /// <summary>
        /// Asks the RequestId instance to update the RequestId to the next available Id number 
        /// Typically used when an existing Request message object needs to be re-sent. 
        /// </summary>
        public void UpdateId()
        {
            AssignNextIdNumber();
            EncodeRequestId();
        }

        public UInt32 RequestId
        {
            get
            {
                return _requestId;
            }
        }
    }
}
