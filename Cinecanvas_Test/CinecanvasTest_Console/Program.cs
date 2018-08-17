using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Timers;
using System.Diagnostics; // for Stopwatch
using System.Globalization;

using CommandLine;   // For the command-line options/flags that are passed in at launch

namespace CinecanvasTest_Console
{
    class Options
    {
        [Option('f', "file", Required = true, HelpText = "Cinecanvas file to be processed.")]
        public string InputFile { get; set; }

        [Option('t', "timeOffset", Required = false, HelpText = "Time offset to begin with in format of 'HH:MM:SS:FF'")]
        public string TimeOffset { get; set; }

        [Option('d', "debugOutput", Required = false, HelpText = "Only use for debugging purposes")]
        public bool DebugOutput { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {

            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => ProcessCinecanvasFile(opts))
                .WithNotParsed<Options>((errs) => HandleParseError(errs));
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
        }

        private static void ProcessCinecanvasFile(Options options)
        {
            SubtitleReel XmlData = LoadCinecanvasFile(options);

            int TotalSubtitleCount = XmlData.SubtitleList.Font.Subtitle.Count;
            int TimerTickRate = CalculateTimerTickRate(XmlData.TimeCodeRate);

            SubtitleTimeEntry TimeOffset;

            try
            {

                if (options.TimeOffset != null)
                {
                    TimeOffset = new SubtitleTimeEntry(options.TimeOffset, TimerTickRate);
                }
                else
                {
                    TimeOffset = new SubtitleTimeEntry("00:00:00:00", 0);
                }


                Console.WriteLine($"Total Subtitles Entries not counting multiple lines: {TotalSubtitleCount}");


                //Timer elapsedTime = new Timer(timerTickRate);
                //elapsedTime.Elapsed += testMethod;
                //elapsedTime.AutoReset = true;
                //elapsedTime.Enabled = true; 

                // create new Stopwatch
                StopwatchWithOffset Stopwatch = new StopwatchWithOffset(TimeOffset.Time);


                // Begin timing
                Stopwatch.Start();

                // Console.WriteLine(" All Done ");
                // Console.WriteLine("Press the Enter key to exit the program at any time ... ");
                // Console.ReadLine();

                foreach (Subtitle Subtitle in XmlData.SubtitleList.Font.Subtitle)
                {
                    // TimeSpan timeIn = TimeSpan.Parse(subtitle.TimeIn);
                    // TimeSpan timeOut = TimeSpan.Parse(subtitle.TimeOut);
                    // TimeSpan timeIn = TimeSpan.ParseExact(subtitle.TimeIn, "G", CultureInfo.CurrentCulture);
                    // TimeSpan timeOut = TimeSpan.ParseExact(subtitle.TimeOut, "G", CultureInfo.CurrentCulture);

                    SubtitleTimeEntry TimeIn = new SubtitleTimeEntry(Subtitle.TimeIn, TimerTickRate);
                    SubtitleTimeEntry TimeOut = new SubtitleTimeEntry(Subtitle.TimeOut, TimerTickRate);

                    bool SubtitlePrinted = false;
                    bool OutputDebugInfo = false;

                    do
                    {
                        SubtitlePrinted = false;

                        if (options.DebugOutput == true && OutputDebugInfo == false)
                        {
                            Console.Error.WriteLine("");
                            Console.Error.WriteLine($"Waiting @ Spot#: {Subtitle.SpotNumber}");
                            Console.Error.WriteLine($"TimeIn: {TimeIn.Time},  TimeOut: {TimeOut.Time}");
                            Console.Error.WriteLine($"Stopwatch: {Stopwatch.ElapsedTimeSpan}");

                            OutputDebugInfo = true;
                        }

                        if ((Stopwatch.ElapsedTimeSpan > TimeIn.Time)
                            && (Stopwatch.ElapsedTimeSpan < TimeOut.Time))
                        {
                            int Spot = Subtitle.SpotNumber;
                            //Console.WriteLine($"SpotNumber: {spot}");

                            foreach (Text Text in Subtitle.Text)
                            {
                                int Position = Text.VPosition;
                                Console.WriteLine($"{Spot},{Position}: {Text.SubtitleText}");
                            }

                            SubtitlePrinted = true;
                            OutputDebugInfo = false;
                        }

                        if (Stopwatch.ElapsedTimeSpan > TimeOut.Time)
                            break;

                    } while (SubtitlePrinted == false);

                }

                Stopwatch.Stop();
                //Console.WriteLine($"Time elapsed: {stopwatch.Elapsed}");
            }
            catch (FormatException e)
            {
                Console.WriteLine($"{e.HResult}: {e.Message}");
                Environment.Exit(0);
            }
        }

        private static SubtitleReel LoadCinecanvasFile(Options options)
        {
            // Define the XmlSerializer casting to type SubtitleReel
            XmlSerializer Deserializer = new XmlSerializer(typeof(SubtitleReel));

            // Open the input file for reading
            TextReader Reader = new StreamReader(options.InputFile);

            // Deserialize the input file
            object DeserializedData = Deserializer.Deserialize(Reader);

            // Cast the deserialized data to the SubtitleReel type
            SubtitleReel XmlData = (SubtitleReel)DeserializedData;

            // Close the input file stream
            Reader.Close();

            // Send the deserialized data pointer back to the calling routine
            return XmlData;
        }

        static public int CalculateTimerTickRate(int timeCodeRate)
        {
            return (1000 / timeCodeRate);
        }

        /// <summary>
        /// Used this testMethod to test how the System.Timers functionality worked.  At this point
        /// I've determined we don't need this functionality, but saving it for now for reference
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        static public void TestMethod(Object source, System.Timers.ElapsedEventArgs e)
        {
            // Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
        }
        
    }
}
