using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcsListener;

namespace AcsListener
{
    public class AcspResponseHeader
    {
        private AcspPackKey _key;
        private AcspBerLength _packLength;

        public AcspPackKey Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;
            }
        }

        public AcspBerLength PackLength
        {
            get
            {
                return _packLength;
            }
            set
            {
                _packLength = value;
            }
        }

        /// <summary>
        /// Constructor for a standard Acsp Response Header object.  Takes the standard 20 bytes 
        /// comprised of 16 bytes for the Pack Key, and 4 bytes for the BER length item.
        /// </summary>
        /// <param name="inputArray"> 20 byte array comprising 16 bytes for Pack Key, and 4 bytes for BER length</param>
        /// 
        public AcspResponseHeader(Byte[] inputArray)
        {
            if (inputArray.Length != 20)
            {
                throw new IndexOutOfRangeException("Error: expecting 20-byte input");
            }

            Byte[] keyArray = new Byte[16];
            Byte[] lengthArray = new Byte[4];

            Array.Copy(inputArray, 0, keyArray, 0, keyArray.Length);  // Copy first 16 bytes of inputArray in to keyArray
            Array.Copy(inputArray, keyArray.Length, lengthArray, 0, lengthArray.Length);  // Copy last 4 bytes of inputArray in to lengthArray

            _key = new AcspPackKey(keyArray);
            _packLength = new AcspBerLength(lengthArray);
        }
    }
}
