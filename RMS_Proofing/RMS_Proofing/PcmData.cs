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

        public static byte[] ConvertFloatToBytes(float[] floatInput)
        {
            var byteOutput = new byte[floatInput.Length * sizeof(float)];

            Buffer.BlockCopy(floatInput, 0, byteOutput, 0, byteOutput.Length);

            return byteOutput;
        }
    }
}
