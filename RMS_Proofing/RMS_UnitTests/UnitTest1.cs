using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMS_Proofing;

namespace AudioMath_Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestRms1()
        {
            Int16 result = 0;
            Int16[] data = new Int16[] { 9, 9, 9, 9, 9, 9, 9, 9, 9 };   // RMS = 9

            result = AudioMath.RootMeanSquare(data);
            Assert.AreEqual((Int16)9, result);
        }


        [TestMethod]
        public void TestRms2()
        {
            Int16 result = 0;
            Int16[] data = new Int16[] { 10, 9, 8, 10, 9, 8, 10, 9, 8 };   // RMS = 9.03696114115064

            result = AudioMath.RootMeanSquare(data);
            Assert.AreEqual((Int16)9, result);
        }

        [TestMethod]
        public void TestRmsHandlesMinValueOverflow()
        {
            Int16 result = 0;
            Int16[] data = new Int16[] { Int16.MinValue, Int16.MinValue, Int16.MinValue, Int16.MinValue, Int16.MinValue, Int16.MinValue, Int16.MinValue };

            // Int16.MinValue = -32768 and Int16.Max value = 32767.  If only -32768 are sent through a regular RMS calculation, it will effectively ABS the number and result
            // in 32768, which exceeds the max value that can be put in a (short) / (Int16).   I've modified the rootMeanSquare calculation to try to account for that and if it sees the overflow 
            // situation, it will set the value to Int16.MaxValue instead. 

            result = AudioMath.RootMeanSquare(data);
            Assert.AreEqual((Int16)32767, result);   
        }

        [TestMethod]
        public void TestRmsToDbConversion01()
        {
            Int16 result = 0;
            int dbFS;
            Int16[] data = new Int16[] { Int16.MaxValue, Int16.MaxValue, Int16.MaxValue, Int16.MinValue, Int16.MinValue };

            result = AudioMath.RootMeanSquare(data);

            // Now, convert that to dBFS

            dbFS = AudioMath.ConvertToDbfs(result);
            Assert.AreEqual(0, dbFS);
        }

        [TestMethod]
        public void TestRmsToDbConversion02()
        {
            Int16 result = 0;
            double dbFS;
            Int16[] data = new Int16[] { 30000, 20000, 10000, 0, -5000, -10000, 0, 5000, 10000, 15000, 20000, 25000, 30000, 31000, 31500, 26000 };

            result = AudioMath.RootMeanSquare(data);
            dbFS = AudioMath.ConvertToDbfs(result);

            // If you aren't getting the expected -4 dB, instead getting -90, then you probably have an overflow condition in RMSR. 
            Assert.AreEqual(-4, dbFS);
        }

        [TestMethod]
        public void TestDbConversionOfZero()
        {
            Int16 result = 0;
            int dbFS;

            dbFS = AudioMath.ConvertToDbfs(result);
            Assert.AreEqual(-90, dbFS);
        }
    }
}
