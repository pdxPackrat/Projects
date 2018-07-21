using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO;  // For TextReader

namespace XML_Deserialization_Test
{
    class Program
    {
        static void Main(string[] args)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(AddressDirectory));

            TextReader reader = new StreamReader(@"XML_Test.xml");

            object obj = deserializer.Deserialize(reader);

            AddressDirectory XmlData = (AddressDirectory)obj;

            reader.Close();

            int result = XmlData.Address.Count;

            Console.WriteLine($"Result = {result}");

            for (int i = 0; i < result; i++)
            {
                Address addressData = XmlData.Address[i];

                Console.WriteLine($"AddressID: {addressData.AddressId}");
                Console.WriteLine($"House#:  {addressData.HouseNo}");
                Console.WriteLine($"Street Name: {addressData.StreetName}");
                Console.WriteLine($"City: {addressData.City}");
                Console.WriteLine("------------------------");
            }
        }
    }
}
