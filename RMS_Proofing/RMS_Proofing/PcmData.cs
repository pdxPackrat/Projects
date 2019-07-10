using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS_Proofing
{
    class PcmData
    {
        public PcmData()
        {

        }

        public PcmData(float[] floatInput)
        {

        }

        /// <summary>
        /// </summary>
        /// <param name="floatInput"></param>
        /// <returns></returns>
        public static byte[] ConvertFloatToPcmBytes(float[] floatInput)
        {
            var byteOutput = new byte[floatInput.Length * sizeof(Int32)];
            Int16 tempValue;

            for (int i = 0; i < floatInput.Length; i++)
            {
                tempValue = (Int16)(floatInput[i] * Int16.MaxValue);

                var bytes = BitConverter.GetBytes(tempValue);
                /*  Don't think we need to reverse, but I'll double check
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(bytes);
                }
                */

                Array.Copy(bytes, 0, byteOutput, i * bytes.Length, bytes.Length);

                /*
                for (int n = 0; n < bytes.Length; n++)
                {
                    byteOutput[i * bytes.Length + n] = bytes[n];
                }
                */
            }

            // Buffer.BlockCopy(floatInput, 0, byteOutput, 0, byteOutput.Length);

            return byteOutput;
        }
    }
}
