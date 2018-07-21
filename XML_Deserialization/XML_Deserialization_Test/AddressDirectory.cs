using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace XML_Deserialization_Test
{
    public class AddressDirectory
        {
            [XmlElement("DirectoryOwner")]
            public string DirectoryOwner { get; set; }
            [XmlElement("PinCode")]
            public string PinCode { get; set; }
            [XmlElement("Address")]
            public List<Address> Address { get; set; }
            [XmlElement("Designation")]
            public Designation designation { get; set; }
        }
}
