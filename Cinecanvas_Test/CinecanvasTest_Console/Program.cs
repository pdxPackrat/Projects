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

using CommandLine;

namespace CinecanvasTest_Console
{
    class Options
    {
        [Option('f', "file", Required = true, HelpText = "Cinecanvas file to be processed.")]
        public string inputFile { get; set; }

        [Option('t', "timeOffset", Required = false, HelpText = "Time offset to begin with in format of 'HH:MM:SS:FF'")]
        public string timeOffset { get; set; }

        [Option('d', "debugOutput", Required = false, HelpText = "Only use for debugging purposes")]
        public bool debugOutput { get; set; }
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
            XmlSerializer deserializer = new XmlSerializer(typeof(SubtitleReel));

            TextReader reader = new StreamReader(options.inputFile);

            object obj = deserializer.Deserialize(reader);

            SubtitleReel XmlData = (SubtitleReel)obj;

            reader.Close();

            int totalSubtitleCount = XmlData.subtitleList.font.subtitle.Count;
            int timerTickRate = calculateTimerTickRate(XmlData.timeCodeRate);

            subtitleTimeEntry timeOffset;

            try
            {

                if (options.timeOffset != null)
                {
                    timeOffset = new subtitleTimeEntry(options.timeOffset, timerTickRate);
                }
                else
                {
                    timeOffset = new subtitleTimeEntry("00:00:00:00", 0);
                }


                Console.WriteLine($"Total Subtitles Entries not counting multiple lines: {totalSubtitleCount}");


                //Timer elapsedTime = new Timer(timerTickRate);
                //elapsedTime.Elapsed += testMethod;
                //elapsedTime.AutoReset = true;
                //elapsedTime.Enabled = true; 

                // create new Stopwatch
                StopwatchWithOffset stopwatch = new StopwatchWithOffset(timeOffset.time);


                // Begin timing
                stopwatch.Start();

                // Console.WriteLine(" All Done ");
                // Console.WriteLine("Press the Enter key to exit the program at any time ... ");
                // Console.ReadLine();

                foreach (Subtitle subtitle in XmlData.subtitleList.font.subtitle)
                {
                    // TimeSpan timeIn = TimeSpan.Parse(subtitle.TimeIn);
                    // TimeSpan timeOut = TimeSpan.Parse(subtitle.TimeOut);
                    // TimeSpan timeIn = TimeSpan.ParseExact(subtitle.TimeIn, "G", CultureInfo.CurrentCulture);
                    // TimeSpan timeOut = TimeSpan.ParseExact(subtitle.TimeOut, "G", CultureInfo.CurrentCulture);

                    subtitleTimeEntry timeIn = new subtitleTimeEntry(subtitle.TimeIn, timerTickRate);
                    subtitleTimeEntry timeOut = new subtitleTimeEntry(subtitle.TimeOut, timerTickRate);

                    bool subtitlePrinted = false;
                    bool outputDebugInfo = false;

                    do
                    {
                        subtitlePrinted = false;

                        if (options.debugOutput == true && outputDebugInfo == false)
                        {
                            Console.Error.WriteLine($"Waiting @ Spot#: {subtitle.SpotNumber}");
                            Console.Error.WriteLine($"TimeIn: {timeIn.time},  TimeOut: {timeOut.time}");
                            Console.Error.WriteLine($"Stopwatch: {stopwatch.ElapsedTimeSpan}");

                            outputDebugInfo = true;
                        }

                        if ((stopwatch.ElapsedTimeSpan > timeIn.time) 
                            && (stopwatch.ElapsedTimeSpan < timeOut.time))
                        {
                            int spot = subtitle.SpotNumber;
                            //Console.WriteLine($"SpotNumber: {spot}");

                            foreach (Text text in subtitle.text)
                            {
                                int pos = text.vPosition;
                                Console.WriteLine($"{spot},{pos}: {text.subtitleText}");
                            }

                            subtitlePrinted = true;
                            outputDebugInfo = false;
                        }

                        if (stopwatch.ElapsedTimeSpan > timeOut.time)
                            break;

                    } while (subtitlePrinted == false);

                }

                stopwatch.Stop();
                //Console.WriteLine($"Time elapsed: {stopwatch.Elapsed}");
            }
            catch (FormatException e)
            {
                Console.WriteLine($"{e.HResult}: {e.Message}");
                Environment.Exit(0);
            }
        }

        static public int calculateTimerTickRate(int timeCodeRate)
        {
            return (1000 / timeCodeRate);
        }

        /// <summary>
        /// Used this testMethod to test how the System.Timers functionality worked.  At this point
        /// I've determined we don't need this functionality, but saving it for now for reference
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        static public void testMethod(Object source, System.Timers.ElapsedEventArgs e)
        {
            // Console.WriteLine("The Elapsed event was raised at {0}", e.SignalTime);
        }
        
    }
}
