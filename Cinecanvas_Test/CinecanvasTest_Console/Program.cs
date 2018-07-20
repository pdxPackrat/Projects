using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace CinecanvasTest_Console
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(SubtitleReelType));
            TextReader reader = new StreamReader("Whitney_23.976_Captions_DCinema2010.xml");
            object obj = deserializer.Deserialize(reader);
            SubtitleReelType XmlData = (SubtitleReelType)obj;
            reader.Close();

            // SubtitleReelTypeFont data = XmlData.SubtitleList[0];

            //SubtitleType[] subtitles = (SubtitleType[]) XmlData.SubtitleList.Su
            // SubtitleType data = (SubtitleType) XmlData.SubtitleList;

            SubtitleType mdc = (SubtitleType) XmlData.SubtitleList.

            Console.WriteLine(" All Done ");
        }
        
    }
}
