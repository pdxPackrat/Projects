using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace CinecanvasTest_Console
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
        public string id { get; set; }

        [XmlElement("TimeCodeRate")]
        public string timeCodeRate { get; set; }

        [XmlElement("LoadFont")]
        public string loadFont { get; set; }

        [XmlElement("SubtitleList")]
        public SubtitleList subtitleList { get; set; }
    }
    public class SubtitleList
    {
        [XmlElement("Font")]
        public Font font { get; set; }
    }

    public class Font
    {
        [XmlAttribute("ID")]
        public string id { get; set; }

        [XmlAttribute("Size")]
        public string size { get; set; }

        [XmlElement ("Subtitle")]
        public List<Subtitle> subtitle { get; set; }
    }

    public class Subtitle
    {
        [XmlAttribute("SpotNumber")]
        public int SpotNumber { get; set; }

        [XmlAttribute("TimeIn")]
        public string TimeIn { get; set; }

        [XmlAttribute("TimeOut")]
        public string TimeOut { get; set; }

        [XmlElement("Text")]
        public List<Text> text { get; set; }
    }

    public class Text
    {
        [XmlAttribute("Vposition")]
        public int vPosition { get; set; }
        
        [XmlText]
        public string subtitleText { get; set; }
    }
}
