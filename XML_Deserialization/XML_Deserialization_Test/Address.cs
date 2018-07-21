using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace XML_Deserialization_Test
{
    public class Address
    {
        [XmlAttribute("AddressId")]
        public string AddressId { get; set; }
        [XmlElement("HouseNo")]
        public string HouseNo { get; set; }
        [XmlElement("StreetName")]
        public string StreetName { get; set; }
        [XmlElement("City")]
        public string City { get; set; }
    }
}
