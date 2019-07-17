using System;
using System.Linq;  
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RMS_UnitTests
{
    [TestClass]
    public class UnitTest2
    {
        /// <summary>
        /// Proof of concept for something Jon and I are trying to do in C++
        /// Showing how I would do it in C#
        /// </summary>
        [TestMethod]
        public void TestPackingByteArrayToInt64()
        {
            byte[] byteInput = new byte[] { 41, 36, 44, 31, 72, 71, 0, 0 };

            // Array.Reverse(byteInput);  // Testing values from Jon's test run

            byte[] byteOutput; 
            UInt64 testInt = 0;

            testInt = BitConverter.ToUInt64(byteInput, 0);

            byteOutput = BitConverter.GetBytes(testInt);

            Assert.AreEqual(36, byteOutput[1]);   // Test an individual expected element of the array
            Assert.IsTrue(byteInput.SequenceEqual(byteOutput));   // test that both arrays have identical expected values
            CollectionAssert.AreEqual(byteInput, byteOutput);     // Another way to do the same thing
        }

        [TestMethod]
        public void TestPackingByteArrayToUInt64BitShifting()
        {
            byte[] byteInput = new byte[] { 41, 36, 44, 31, 72, 71, 0, 0 };
            UInt64 testOutput = 0;
            UInt64 result;

            for (int i = 0; i < byteInput.Length; i++)
            {
                int bitShifter = ((byteInput.Length - 1) - i) * 8;  // Number of bits to shift, starts high and eventually is 0

                result = (((UInt64)(byteInput[i])) << bitShifter); // 2954361355555045376,10133099161583616,48378511622144,133143986176,
                                                                   // 1207959552,4653056,0,0
                testOutput += result;  // 2954361355555045376,2964494454716628992,2964542833228251136,2964542966372237312,
                                       // 2964542967580196864,2964542967584849920,2964542967584849920,2964542967584849920
                                       // Final value of testOutput: 2964542967584849920
            }

            Assert.AreEqual((UInt64)2964542967584849920, testOutput);
        }

        [TestMethod]
        public void TestUnpackingUInt64ToByteArrayBitShifting()
        {
            byte[] expectedOutput = new byte[] { 41, 36, 44, 31, 72, 71, 0, 0 };
            UInt64 testValue = 2964542967584849920;
            byte[] bytes = new byte[sizeof(UInt64)];
            byte result;

            for (int i = 0; i < bytes.Length; i++)
            {
                // int bitShifter = i * 8;  // Number of bits to shift right, starts at 0 and ends high
                int bitShifter = ((bytes.Length - 1) - i) * 8;  // Number of bits to shift, starts high and eventually is 0

                if (bitShifter > 0)
                {
                    result = (byte)((testValue >> bitShifter) & 0xFF);
                }
                else
                {
                    result = (byte)(testValue & 0xFF);
                }

                bytes[i] = result;
            }

            Assert.IsTrue(expectedOutput.SequenceEqual(bytes));
        }

        [TestMethod]
        public void TestPackingByteArrayIntoInt32BitShifting()
        {
            byte[] byteInput = new byte[] { 90, 18, 18, 36 };
            int testOutput;

            // testOutput = ((byteInput[0] << 24) + (byteInput[1] << 16) + (byteInput[2] << 8) + (byteInput[3]));
            testOutput = (byteInput[0] << 24); // testOutput = 1509949440
            testOutput += (byteInput[1] << 16); // testOutput = 1511129088
            testOutput += (byteInput[2] << 8); // testOutput = 1511133696
            testOutput += byteInput[3];    // testOutput = 1511133732 

            Assert.AreEqual(1511133732, testOutput);
        }

        [TestMethod]
        public void TestUnpackingInt32IntoByteArrayBitShifting()
        {
            int testValue = 1511133732;
            byte[] byteArray = new byte[sizeof(int)];

            byteArray[0] = (byte)(testValue & 0xFF);
            byteArray[1] = (byte)((testValue >> 8) & 0xFF);
            byteArray[2] = (byte)((testValue >> 16) & 0xFF);
            byteArray[3] = (byte)((testValue >> 24) & 0xFF);

            Assert.AreEqual(18, byteArray[1]);
        }
    }
}
