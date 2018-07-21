using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace XML_Deserialization_Test
{
    public class Designation
    {
        [XmlAttribute("place")]
        public string place { get; set; }
        [XmlText]
        public string JobType { get; set; }
    }
}
