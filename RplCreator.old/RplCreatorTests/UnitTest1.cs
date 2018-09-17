using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RplCreator;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace RplCreatorTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            ResourcePresentationList Rpl = new ResourcePresentationList();

            Rpl.ReelResources.EditRate = "36 1";

            Assert.AreEqual((UInt64)36, Rpl.ReelResources.RateNumerator);
            Assert.AreEqual((UInt64)1, Rpl.ReelResources.RateDenominator);
        }

        [TestMethod]
        public void TestRplReelDurationConstructor()
        {
            RplReelDuration duration = new RplReelDuration("24 1", "01:35:00");

            Assert.AreEqual((UInt64)136800, duration.EditUnits);
        }

        [TestMethod]
        public void TestGetFileName()
        {
            string filename = @"C:\mydir\myfile.txt";
            string result = Path.GetFileName(filename);

            Assert.AreEqual("myfile.txt", result);
        }
    }
}
