using System;

public class Class1
{
        static void MainOld(string[] args)
        {
            // XmlReader r = XmlReader.Create("Whitney_23.976_Captions_DCinema2010.xml");
            // while (r.NodeType != XmlNodeType.Element)
            // {
            //     r.Read();
            // }

            // XElement e = XElement.Load(r);
            // Console.WriteLine(e);

            XmlDocument file = new XmlDocument();

            file.Load("Whitney_23.976_Captions_DCinema2010.xml");

            XmlNodeList tree = file.GetElementsByTagName("dcst:Subtitle");
            Console.WriteLine("Number of subtitles: " + tree.Count);

            Subtitle[] arrayOfSubtitles = new Subtitle[tree.Count];

            Console.WriteLine($"New array of Subtitles created with {arrayOfSubtitles.Count()} entries");

            int subtitleIndex = 1;

            foreach (XmlNode node in tree)
            {
                XmlAttributeCollection attributes = node.Attributes;
                XmlAttribute Spot = attributes["SpotNumber"];
                XmlAttribute TimeIn = attributes["TimeIn"];

                arrayOfSubtitles[subtitleIndex].SpotNumber = Int32.Parse(Spot.Value);

                if (Spot.Value != null)
                {
                    Console.WriteLine($"Currently viewing SpotNumber #{Spot.Value}, {Spot.Value.GetType()}");
                }
                else
                {
                    Console.WriteLine("Something bad happened");
                }

                subtitleIndex++;
            }
        }
}
