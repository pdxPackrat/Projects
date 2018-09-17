using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AcsListener;

namespace AcsListener.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Byte[] testBytes = new Byte[4];

            testBytes[0] = 0x83;
            testBytes[1] = 0x00;
            testBytes[2] = 0x00;
            testBytes[3] = 0x0C;

            AcspBerLength berLength = new AcspBerLength(testBytes);

            // berLength.LengthArray = testBytes;

            testBytes[3] = 0x55;

            testBytes = berLength.LengthArray;

            Assert.AreEqual(testBytes[3], 0x0C);
        }

        [TestMethod]
        public void ConfirmEndianConversion()
        {
            int number = 256;

            // 256, 0x00000100 (in big-endian) is converted to 0x00010000 in little-endian architectures.  
            // Bascially, the entirety of 3rd index is moved to 0 index (and vice-versa) and 1st/2nd index swap as well
            // In an example of 0x1234ABCD (big-endian), the BitConverter for little-endian system would yield the result of
            // 0xCDAB3412 - yes, very confusing :) 
            Byte[] numArray = BitConverter.GetBytes(number);
            Assert.AreEqual(0x00, numArray[0]);
            Assert.AreEqual(0x01, numArray[1]);
            Assert.AreEqual(0x00, numArray[2]);
            Assert.AreEqual(0x00, numArray[3]);
            Assert.AreEqual(true, BitConverter.IsLittleEndian);

            if(BitConverter.IsLittleEndian)
                Array.Reverse(numArray);

            Assert.AreEqual(0x00, numArray[0]);
            Assert.AreEqual(0x00, numArray[1]);
            Assert.AreEqual(0x01, numArray[2]);
            Assert.AreEqual(0x00, numArray[3]);

        }

        [TestMethod]
        public void TestAcspBerLengthGetLogic()
        {
            Byte[] testBytes = new Byte[4];

            testBytes[0] = 0x83;
            testBytes[1] = 0x00;
            testBytes[2] = 0x00;
            testBytes[3] = 0x0C;

            AcspBerLength test = new AcspBerLength(testBytes);

            // test.LengthArray = testBytes;

            int result = test.Length;

            Assert.AreEqual(12, result);
        }

        [TestMethod]
        public void TestBerLengthSetLogic()
        {
            AcspBerLength test = new AcspBerLength(12);

            Byte[] testBytes = new Byte[4];

            // test.Length = 12;

            test.LengthArray.CopyTo(testBytes, 0);

            Assert.AreEqual(0x83, testBytes[0]);
            Assert.AreEqual(0x0C, testBytes[3]);
        }

        [TestMethod]
        public void TestAcspPackKeyConstructorByByteArray()
        {

            byte[] testArray = new byte[16];

            testArray[0] = 0x06;  // Object Identifier, Object ID
            testArray[1] = 0x0E;  // Label size, Length of UL (Universal Label)
            testArray[2] = 0x2B;  // Designator, Sub Identifier
            testArray[3] = 0x34;  // Designator, SMPTE Identifier
            testArray[4] = 0x02;  // Registry Category Designator, KLV Groups (Sets and Packs)
            testArray[5] = 0x05;  // Registry Designator, Fixed Length Pack
            testArray[6] = 0x01;  // Structure Designator, Groups Dictionary
            testArray[7] = 0x01;  // Version Number, Registry Version: Dictionary version 1
            testArray[8] = 0x02;  // Item Designator, Administration
            testArray[9] = 0x07;  // Organization, Access Control
            testArray[10] = 0x02; // Application, Auxilliar Content Synchronization Protocol (ACSP)
            testArray[11] = 0x02; // 0x02 *should* equal a "Good Message"
            testArray[12] = 0x03; // I believe this is an "Get New Lease Response"
            testArray[13] = 0x00; // Reserved, Not assigned
            testArray[14] = 0x00; // Reserved, Not assigned
            testArray[15] = 0x00; // Reserved, Not assigned

            AcspPackKey testPack = new AcspPackKey(testArray);

            Assert.IsTrue(testPack.IsGoodRequest);

            Byte13NodeNames byte13 = testPack.NodeNames;

            Assert.AreEqual(Byte13NodeNames.GetNewLeaseResponse, byte13);
        }

        [TestMethod]
        public void TestRequestIdFunctionality()
        {
            AcspRequestId testId;

            testId = new AcspRequestId();
            Assert.AreEqual((UInt32)1, testId.RequestId);

            testId = new AcspRequestId();
            Assert.AreEqual((UInt32)2, testId.RequestId);
            Assert.AreEqual(0x02, testId.IdArray[3]);   // Making sure the conversion to big-endian is working correctly

            testId.UpdateId();
            Assert.AreEqual((UInt32)3, testId.RequestId);
        }

        [TestMethod]
        public void TestAnnounceRequestConstructorLogic()
        {
            AcspAnnounceRequest testAnnouncer = new AcspAnnounceRequest();

            Assert.AreEqual(76, testAnnouncer.PackArray.Length);
            Assert.AreEqual(0x38, testAnnouncer.PackArray[19]); // The 20th byte should be the actual value length encoded in the BER length value
            Assert.AreEqual(0x83, testAnnouncer.PackArray[16]); // The 17th byte would be the leading byte of the BER length, and should always be 0x83 for us
        }

        [TestMethod]
        public void TestMessageHeaderConstructor()
        {
            AcspResponseHeader testMessageHeader;

            byte[] testArray = GetTestArrayPackKey(20);
            testArray[16] = 0x83; // Beginning of BER length block
            testArray[17] = 0x00; // 
            testArray[18] = 0x00; //
            testArray[19] = 0x0C; // 17, 18, and 19 combined for a final number of 00000C, or 12 decimal


            testMessageHeader = new AcspResponseHeader(testArray);

            Assert.IsTrue(testMessageHeader.Key.IsGoodRequest);
        }

        private static byte[] GetTestArrayPackKey(int length)
        {
            byte[] testArray = new byte[length];

            testArray[0] = 0x06;  // Object Identifier, Object ID
            testArray[1] = 0x0E;  // Label size, Length of UL (Universal Label)
            testArray[2] = 0x2B;  // Designator, Sub Identifier
            testArray[3] = 0x34;  // Designator, SMPTE Identifier
            testArray[4] = 0x02;  // Registry Category Designator, KLV Groups (Sets and Packs)
            testArray[5] = 0x05;  // Registry Designator, Fixed Length Pack
            testArray[6] = 0x01;  // Structure Designator, Groups Dictionary
            testArray[7] = 0x01;  // Version Number, Registry Version: Dictionary version 1
            testArray[8] = 0x02;  // Item Designator, Administration
            testArray[9] = 0x07;  // Organization, Access Control
            testArray[10] = 0x02; // Application, Auxilliar Content Synchronization Protocol (ACSP)
            testArray[11] = 0x02; // 0x02 *should* equal a "Good Message"
            testArray[12] = 0x03; // I believe this is an "Get New Lease Response"
            testArray[13] = 0x00; // Reserved, Not assigned
            testArray[14] = 0x00; // Reserved, Not assigned
            testArray[15] = 0x00; // Reserved, Not assigned
            return testArray;
        }

        [TestMethod]
        public void TestRequestIdConstructorByByteArray()
        {
            Byte[] testArray = new Byte[4];
            
            // Creating 4-byte (big-endian) representation of 6000
            testArray[3] = 0x70; // 112 decimal
            testArray[2] = 0x17; // 23 x 256 = 5888
            testArray[1] = 0x00;
            testArray[0] = 0x00;

            AcspRequestId testId = new AcspRequestId(testArray);

            Assert.AreEqual((UInt32)6000, testId.RequestId);
            Assert.AreEqual(0x70, testId.IdArray[3]);  // checking to make sure that the encoded array is back to big-endian
        }

        [TestMethod]
        public void TestAnnounceResponseConstructor()
        {
            Byte[] testArray = new Byte[48];

            testArray[0] = 0x00;
            testArray[1] = 0x00;
            testArray[2] = 0x17;  // decimal 23 * 256 = 5888
            testArray[3] = 0x70;  // decimal 112 * 1 = 112, total of 6000

            // Next comes current time in Epoch seconds, an 8-byte slice that gets turned in to an Int64
            testArray[4] = 0x00;
            testArray[5] = 0x00;
            testArray[6] = 0x00;
            testArray[7] = 0x00;
            testArray[8] = 0x5B;
            testArray[9] = 0x87;
            testArray[10] = 0x4C;
            testArray[11] = 0xF9;  // for a grand total of 1535593721 seconds, or approximate 6:49pm PT on August 29th, 2018o

            // Next is the device description length field
            testArray[12] = 0x83;
            testArray[13] = 0x00;
            testArray[14] = 0x00;
            testArray[15] = 0x16;

            // Next is the device description, in this case the string "Proludio ACS Simulator", 22 characters long
            testArray[16] = 0x50; // "P"
            testArray[17] = 0x72; // "r"
            testArray[18] = 0x6F; // "o"
            testArray[19] = 0x6C; // "l"
            testArray[20] = 0x75; // "u"
            testArray[21] = 0x64; // "d"
            testArray[22] = 0x69; // "i"
            testArray[23] = 0x6F; // "o"
            testArray[24] = 0x20; // blank space
            testArray[25] = 0x41; // "A"
            testArray[26] = 0x43; // "C"
            testArray[27] = 0x53; // "S"
            testArray[28] = 0x20; // blank space
            testArray[29] = 0x53; // "S"
            testArray[30] = 0x69; // "i"
            testArray[31] = 0x6D; // "m"
            testArray[32] = 0x75; // "u"
            testArray[33] = 0x6C; // "l"
            testArray[34] = 0x61; // "a"
            testArray[35] = 0x74; // "t"
            testArray[36] = 0x6F; // "o"
            testArray[37] = 0x72; // "r"

            // Next comes the variable-length KLV Status Response, 1 byte for the Key, 4 bytes for length, and variable bytes for the message
            testArray[38] = 0x07; // Recoverable error message
            testArray[39] = 0x83;
            testArray[40] = 0x00;
            testArray[41] = 0x00;
            testArray[42] = 0x05;
            testArray[43] = 0x48; // "H"
            testArray[44] = 0x65; // "e"
            testArray[45] = 0x6C; // "l"
            testArray[46] = 0x6C; // "l"
            testArray[47] = 0x6F; // "o"

            AcspAnnounceResponse testResponse = new AcspAnnounceResponse(testArray);

            Assert.AreEqual(1535593721, testResponse.CurrentTime);
            Assert.AreEqual("Hello", testResponse.StatusResponseMessage);
            Assert.AreEqual("Proludio ACS Simulator", testResponse.DeviceDescription);
            Assert.AreEqual(GeneralStatusResponseKey.RecoverableError, testResponse.StatusResponseKey);
        }

        [TestMethod]
        public void TestGetNewLeaseRequestConstructorLogic()
        {
            AcspGetNewLeaseRequest newLeaseRequest = new AcspGetNewLeaseRequest(120);

            // Each New Lease Request is expected 28-bytes in size, so in this case our last 
            Assert.AreEqual((UInt32)0x05, newLeaseRequest.RequestId);
            Assert.AreEqual(0x78, newLeaseRequest.PackArray[27]);

            newLeaseRequest = new AcspGetNewLeaseRequest(6000);
            Assert.AreEqual((UInt32)0x06, newLeaseRequest.RequestId);
            Assert.AreEqual(0x17, newLeaseRequest.PackArray[26]);
            Assert.AreEqual(0x70, newLeaseRequest.PackArray[27]);
        }

        [TestMethod]
        public void TestGetNewLeaseResponseConstructor()
        {
            Byte[] testArray = new Byte[14];

            // First 4 bytes are for RequestId;
            testArray[0] = 0x00;
            testArray[1] = 0x00;
            testArray[2] = 0x17;  // decimal 23 * 256 = 5888
            testArray[3] = 0x70;  // decimal 112 * 1 = 112, total of 6000

            // Next comes the variable-length KLV Status Response, 1 byte for the Key, 4 bytes for length, and variable bytes for the message
            testArray[4] = 0x07; // Recoverable error message
            testArray[5] = 0x83;
            testArray[6] = 0x00;
            testArray[7] = 0x00;
            testArray[8] = 0x05;
            testArray[9] = 0x48; // "H"
            testArray[10] = 0x65; // "e"
            testArray[11] = 0x6C; // "l"
            testArray[12] = 0x6C; // "l"
            testArray[13] = 0x6F; // "o"

            AcspGetNewLeaseResponse leaseResponse = new AcspGetNewLeaseResponse(testArray);

            Assert.AreEqual((UInt32)6000, leaseResponse.RequestId);
            Assert.AreEqual(GeneralStatusResponseKey.RecoverableError, leaseResponse.StatusResponseKey);
            Assert.AreEqual("RecoverableError", leaseResponse.StatusResponseKeyString);
            Assert.AreEqual("Hello", leaseResponse.StatusResponseMessage);
        }

        [TestMethod]
        public void TestGetStatusRequestConstructor()
        {
            AcspGetStatusRequest statusRequest = new AcspGetStatusRequest();

            Assert.AreEqual(0x02, statusRequest.PackArray[11]);
            Assert.AreEqual(0x04, statusRequest.PackArray[12]);
            Assert.AreEqual(Byte13NodeNames.GetStatusRequest, (Byte13NodeNames)statusRequest.PackArray[12]);
        }

        [TestMethod]
        public void TestGetStatusResponseConstructor()
        {
            byte[] testArray = GetTestArrayForGenericResponse();

            AcspGetNewLeaseResponse statusResponse = new AcspGetNewLeaseResponse(testArray);

            Assert.AreEqual((UInt32)6000, statusResponse.RequestId);
            Assert.AreEqual(GeneralStatusResponseKey.RecoverableError, statusResponse.StatusResponseKey);
            Assert.AreEqual("RecoverableError", statusResponse.StatusResponseKeyString);
            Assert.AreEqual("Hello", statusResponse.StatusResponseMessage);
        }

        [TestMethod]
        public void TestSetRplLocationRequestConstructor()
        {
            AcspSetRplLocationRequest rplLocationRequest = new AcspSetRplLocationRequest("http://192.68.9.88/CaptiView/rpl_test1.xml", 49520318);

            Assert.AreEqual(0x32, rplLocationRequest.PackArray[19]);  // Byte 19 should represent the byte-length in the remainder of the packArray

            // Testing for Bytes 24-27 as hex 0x02F39EBE (49520318 decimal)
            Assert.AreEqual(0x02, rplLocationRequest.PackArray[24]);
            Assert.AreEqual(0xF3, rplLocationRequest.PackArray[25]);
            Assert.AreEqual(0x9E, rplLocationRequest.PackArray[26]);
            Assert.AreEqual(0xBE, rplLocationRequest.PackArray[27]);
        }

        [TestMethod]
        public void TestSetOutputModeRequestConstructor()
        {
            AcspSetOutputModeRequest outputModeRequest = new AcspSetOutputModeRequest(true);

            Assert.AreEqual(0x05, outputModeRequest.PackArray[19]);   // Expecting a length value of 5 (4 for RequestId and 1 for OutputMode)
            Assert.AreEqual(0x01, outputModeRequest.PackArray[24]);   // Expecting a value of 0x01 to represent the "1 = enabled" (conversion from boolean)
        }

        [TestMethod]
        public void TestSetOutputModeResponseConstructor()
        {
            byte[] testArray = GetTestArrayForGenericResponse();

            AcspSetOutputModeResponse outputModeResponse = new AcspSetOutputModeResponse(testArray);

            Assert.AreEqual((UInt32)6000, outputModeResponse.RequestId);
            Assert.AreEqual(GeneralStatusResponseKey.RecoverableError, outputModeResponse.StatusResponseKey);
            Assert.AreEqual("RecoverableError", outputModeResponse.StatusResponseKeyString);
            Assert.AreEqual("Hello", outputModeResponse.StatusResponseMessage);

        }

        private static byte[] GetTestArrayForGenericResponse()
        {
            Byte[] testArray = new Byte[14];

            // First 4 bytes are for RequestId;
            testArray[0] = 0x00;
            testArray[1] = 0x00;
            testArray[2] = 0x17;  // decimal 23 * 256 = 5888
            testArray[3] = 0x70;  // decimal 112 * 1 = 112, total of 6000

            // Next comes the variable-length KLV Status Response, 1 byte for the Key, 4 bytes for length, and variable bytes for the message
            testArray[4] = 0x07; // Recoverable error message
            testArray[5] = 0x83;
            testArray[6] = 0x00;
            testArray[7] = 0x00;
            testArray[8] = 0x05;
            testArray[9] = 0x48; // "H"
            testArray[10] = 0x65; // "e"
            testArray[11] = 0x6C; // "l"
            testArray[12] = 0x6C; // "l"
            testArray[13] = 0x6F; // "o"
            return testArray;
        }
    }
}
