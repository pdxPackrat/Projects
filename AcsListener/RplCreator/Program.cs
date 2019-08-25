using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Serilog;
using Serilog.Events;

using CommandLine;
using SharedCommon;

namespace RplCreator
{
    class Options
    {
        [Option('f', "file", Required = true, HelpText = "CineCanvas file to be processed.")]
        public string InputFile { get; set; }

        [Option('d', "debug", Required = false, HelpText = "For debug purposes only")]
        public bool DebugOutput { get; set; }

        [Option('r', "ReelDuration", Required = false, HelpText = "Specify duration of the reel in the format of HH:MM:SS, enclosed by quotes.  If no duration is specified, then RplCreator will look at last subtitle time and add one minute to it.")]
        public string ReelDuration { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Error)
                .WriteTo.File("RplCreatorLog-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
             CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => MainProcessing(opts))
                .WithNotParsed<Options>((errs) => HandleParseError(errs));
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            Console.WriteLine("Note: RplCreator.exe must be ran from within a command prompt -- press any key to exit");
            Console.Read();
        }

        private static void MainProcessing(Options options)
        {
            string fileBasename = Path.GetFileName(options.InputFile);

            ResourcePresentationList Rpl = new ResourcePresentationList(fileBasename);  // string argument creates PlayoutId based on hash of the filename

            SubtitleReel XmlData;

            try
            {
                XmlData = SubtitleReel.ParseFromXml(options.InputFile);

                // Reminder:  PlayoutId is auto-generated in the ResourcePresentationList constructor

                Rpl.ReelResources.EditRate = XmlData.EditRate;

                Rpl.ReelResources.TimelineOffset = 0;  // we're always going to default to 0 for now for simplicity

                if (options.ReelDuration != null)
                {
                    RplReelDuration duration = new RplReelDuration(options.ReelDuration, Rpl.ReelResources.EditRate);
                    Rpl.ReelResources.ReelResource.Duration = duration.EditUnits;
                }
                else
                {
                    int LastSubtitleElementNumber = XmlData.SubtitleList.Font.Subtitle.Count - 1;
                    string TimeOutString = XmlData.SubtitleList.Font.Subtitle[LastSubtitleElementNumber].TimeOut;

                    var TimeOutSplit = TimeOutString.Split(':');
                    if (TimeOutSplit.Length >= 2)
                    {
                        uint hours = uint.Parse(TimeOutSplit[0]);
                        uint minutes = uint.Parse(TimeOutSplit[1]);
                        minutes += 1;
                        if (minutes >= 60)
                        {
                            minutes -= 60;
                            hours++;
                        }

                        string output = hours.ToString() + ":" + minutes.ToString() + ":00";  // Needs to be in format of HH:MM:SS
                        RplReelDuration lastSubtitleDuration = new RplReelDuration(output, Rpl.ReelResources.EditRate);
                        RplReelDuration startTimelineDuration = new RplReelDuration(XmlData.StartTime, Rpl.ReelResources.EditRate);
                        Rpl.ReelResources.ReelResource.Duration = lastSubtitleDuration.EditUnits - startTimelineDuration.EditUnits;
                    }
                    else
                    {
                        Rpl.ReelResources.ReelResource.Duration = 0;
                    }
                }

                Rpl.ReelResources.ReelResource.EntryPoint = 0;  // For simplicity sake we are going to default to EntryPoint of 0 for now

                if (XmlData.Language == "en" || XmlData.Language == "en-us")
                {
                    Rpl.ReelResources.ReelResource.Language = "en-us";
                }
                else
                {
                    Rpl.ReelResources.ReelResource.Language = XmlData.Language;
                }

                Rpl.ReelResources.ReelResource.ResourceType = "ClosedCaption";

                Rpl.ReelResources.ReelResource.Id = XmlData.Id;
                Rpl.ReelResources.ReelResource.IntrinsicDuration = Rpl.ReelResources.ReelResource.Duration;

                string result = Path.GetFileName(options.InputFile);
                Rpl.ReelResources.ReelResource.ResourceFile.ResourceText = "/CaptiView/" + result;

                SerializeRplFile(Rpl);
            }
            catch (FileNotFoundException ex)
            {
                Log.Error($"Error: {ex.Message}");
            }
            finally
            {
            }
        }

        private static void SerializeRplFile(ResourcePresentationList inputRpl)
        {
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("rpl", "http://www.smptera.org/schemas/430-11/2010/RPL");


            StringWriter writer = new Utf8StringWriter();


            XmlSerializer xser = new XmlSerializer(typeof(ResourcePresentationList));
            XmlWriterSettings settings = new XmlWriterSettings() { Encoding = new System.Text.UTF8Encoding(false) };
            // XmlWriter writer = XmlWriter.Create("test.xml", settings);
            xser.Serialize(writer, inputRpl, ns);
            Console.WriteLine(writer);
        }
    }
}
