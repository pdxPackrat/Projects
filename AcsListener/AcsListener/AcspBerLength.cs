using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    public class AcspBerLength
    {
        private Byte[] _lengthArray;

        public AcspBerLength(Int32 length)
        {
            _lengthArray = new Byte[4];
            this.Length = length;
        }

        public AcspBerLength(Byte[] inputArray)
        {
            if (inputArray.Length != 4)
            {
                throw new ArrayTypeMismatchException("Expected a 4-byte array");
            }

            _lengthArray = new Byte[4];

            inputArray.CopyTo(_lengthArray, 0);
        }

        public Byte[] LengthArray
        {
            //set
            //{
            //    if (value.Length == 4)
            //    {
            //        _lengthArray = value.ToArray();
            //    }
            //}
            get
            {
                return _lengthArray;
            }

        }

        public Int32 Length
        {
            get
            {
                // SMPTE 430-10 specifications apparently FORCE a 4-byte length array every time
                // meaning that we EXPECT that the first byte of the length array will ALWAYS be 
                // 0x83 (high-bit set and the number 3 indicating 3 trailing bytes that together make
                // an integer value representing the total length of the value object in the KLV.  
                // Therefore we are performing a check for 0x83, and if it isn't set to that, something is
                // definitely wrong. 
                if (_lengthArray[0] == 0x83) 
                {
                    Byte[] lengthValue = new Byte[4];
                    _lengthArray.CopyTo(lengthValue, 0);

                    lengthValue[0] = 0x00; // "Zero out" the expected 0x83 header and leave only the length

                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(lengthValue);
                    }
                    int i = BitConverter.ToInt32(lengthValue, 0);

                    return i;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                // See above Get method for SMPTE notes.  
                Int32 i = value;

                Byte[] lengthValue = BitConverter.GetBytes(i);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(lengthValue);
                }

                if (lengthValue.Length != 4)
                {
                    throw new ArrayTypeMismatchException("Error: Expected Int32 conversion to 4-byte array");
                }

                lengthValue.CopyTo(_lengthArray, 0);
                _lengthArray[0] = 0x83;
            }
        }

        public bool CheckBerLengthHighBit()
        {
            Byte firstByte = _lengthArray[0];

            // Compares the first byte in the length array (a 4-byte structure) and checks to see if
            // the Most Significant Bit is set, which indicates that the rest of the value in the byte
            // is an integer number that is the number of trailing bytes to read and then convert
            // in to an integer for a total length number.   For example, a 4-byte structure of 
            // 0x83 0x00 0x00 0x0C would be a length value of 12.   The 0x83 indicates high-bit is set,
            // and the "3" part of 0x83 is the number of trailing bytes (3) that need to be turned in
            // to a single integer.  0x00 0x00 0x0C as one full number is 00000000 00000000 00001100, 
            // or "12" in this case.  

            if ((firstByte & 0x80) == 0x80) 
            {
                return true;
            }

            else
            {
                return false;
            }
        }

    }
}
