using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Serilog;

namespace SharedCommon
{
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://www.smpte-ra.org/schemas/428-7/2010/DCST")]
    [System.Xml.Serialization.XmlRootAttribute("SubtitleReel", Namespace = "http://www.smpte-ra.org/schemas/428-7/2010/DCST", IsNullable = false)]
    public class SubtitleReel
    {
        [XmlElement ("Id")]
        public string Id { get; set; }
        
        [XmlElement ("Language")]
        public string Language { get; set; }

        [XmlElement ("EditRate")]
        public string EditRate { get; set; }

        [XmlElement("TimeCodeRate")]
        public int TimeCodeRate { get; set; }

        [XmlElement("StartTime")]
        public string StartTime { get; set; }

        [XmlElement("LoadFont")]
        public string LoadFont { get; set; }

        [XmlElement("SubtitleList")]
        public SubtitleList SubtitleList { get; set; }

        /// <summary>
        /// Prevents a default instance of the <see cref="SubtitleReel"/> class from being created.
        /// </summary>
        private SubtitleReel()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleReel"/> class.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        public SubtitleReel(string inputFile)
        {
            SubtitleReel xmlData;

            try
            {
                // Define the XmlSerializer casting to type SubtitleReel
                XmlSerializer deserializer = new XmlSerializer(typeof(SubtitleReel));

                // Open the input file for reading
                TextReader reader = new StreamReader(inputFile);

                // Deserialize the input file
                object deserializedData = deserializer.Deserialize(reader);

                // Cast the deserialized data to the SubtitleReel type
                xmlData = (SubtitleReel)deserializedData;

                // Close the input file stream
                reader.Close();
            }
            catch (Exception ex)
            {
                Log.Debug($"Error encountered in constructor while attempting to deserialize the SubtitleReel object: {ex.Message}");
                throw;
            }

            this.Id = xmlData.Id;
            this.Language = xmlData.Language;
            this.EditRate = xmlData.EditRate;
            this.LoadFont = xmlData.LoadFont;
            this.StartTime = xmlData.StartTime;
            this.SubtitleList = xmlData.SubtitleList;
            this.TimeCodeRate = xmlData.TimeCodeRate;
        }

        /// <summary>
        /// Another way to solve the problem for us instead of the constructor method, this one parses an XML file and returns the deserialized SubtitleReel.
        /// </summary>
        /// <param name="xml">The XML.</param>
        /// <returns></returns>
        public static SubtitleReel ParseFromXml(string xml)
        {
            SubtitleReel xmlData;

            try
            {
                // Define the XmlSerializer casting to type SubtitleReel
                XmlSerializer deserializer = new XmlSerializer(typeof(SubtitleReel));

                // Open the input file for reading
                TextReader reader = new StreamReader(xml);

                // Deserialize the input file
                object deserializedData = deserializer.Deserialize(reader);

                // Cast the deserialized data to the SubtitleReel type
                xmlData = (SubtitleReel)deserializedData;

                // Close the input file stream
                reader.Close();
            }
            catch (Exception ex)
            {
                Log.Debug($"Error encountered in constructor while attempting to deserialize the SubtitleReel object: {ex.Message}");
                throw;
            }

            return xmlData;
        }
    }

    public class SubtitleList
    {
        [XmlElement("Font")]
        public Font Font { get; set; }
    }

    public class Font
    {
        [XmlAttribute("ID")]
        public string Id { get; set; }

        [XmlAttribute("Size")]
        public string Size { get; set; }

        [XmlElement ("Subtitle")]
        public List<Subtitle> Subtitle { get; set; }
    }

    public class Subtitle
    {
        [XmlAttribute("SpotNumber")]
        public int SpotNumber { get; set; }

        [XmlAttribute("TimeIn")]
        public String TimeIn { get; set; }

        [XmlAttribute("TimeOut")]
        public String TimeOut { get; set; }

        [XmlElement("Text")]
        public List<Text> Text { get; set; }
    }

    public class Text
    {
        [XmlAttribute("Vposition")]
        public int VPosition { get; set; }
        
        [XmlText]
        public string SubtitleText { get; set; }
    }
}
