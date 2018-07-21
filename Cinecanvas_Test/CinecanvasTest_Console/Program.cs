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
            // XmlSerializer deserializer = new XmlSerializer(typeof(SubtitleList));
            // XmlSerializer deserializer = new XmlSerializer(typeof(SubtitleReelType));
            XmlSerializer deserializer = new XmlSerializer(typeof(SubtitleReel));

            TextReader reader = new StreamReader("Whitney_23.976_Captions_DCinema2010.xml");

            object obj = deserializer.Deserialize(reader);

            // SubtitleList XmlData = (SubtitleList)obj;
            // SubtitleReelType XmlData = (SubtitleReelType)obj;
            SubtitleReel XmlData = (SubtitleReel)obj;

            reader.Close();

            // SubtitleReelTypeFont data = XmlData.SubtitleList[0];

            //SubtitleType[] subtitles = (SubtitleType[]) XmlData.SubtitleList.Su
            // SubtitleType data = (SubtitleType) XmlData.SubtitleList;
            // FontType = (FontType) XmlData.SubtitleList.

            int result = XmlData.subtitleList.font.subtitle.Count;

            Console.WriteLine($"Total Subtitles Entries not counting multiple lines: {result}");

            foreach(Subtitle subtitle in XmlData.subtitleList.font.subtitle)
            {
                int spot = subtitle.SpotNumber;
                Console.WriteLine($"SpotNumber: {spot}");

                foreach (Text text in subtitle.text)
                {
                    int pos = text.vPosition;
                    Console.WriteLine($"{pos}: {text.subtitleText}");
                }
            }

            Console.WriteLine(" All Done ");
        }
        
    }
}
