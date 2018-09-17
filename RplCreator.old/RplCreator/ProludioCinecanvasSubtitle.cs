using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace RplCreator
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

        [XmlElement("LoadFont")]
        public string LoadFont { get; set; }

        [XmlElement("SubtitleList")]
        public SubtitleList SubtitleList { get; set; }
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
