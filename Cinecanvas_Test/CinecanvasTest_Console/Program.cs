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
            XmlSerializer deserializer = new XmlSerializer(typeof(SubtitleReel));

            TextReader reader = new StreamReader(options.InputFile);

            object obj = deserializer.Deserialize(reader);

            SubtitleReel XmlData = (SubtitleReel)obj;

            reader.Close();

            int TotalSubtitleCount = XmlData.subtitleList.Font.Subtitle.Count;
            int TimerTickRate = CalculateTimerTickRate(XmlData.timeCodeRate);

            subtitleTimeEntry TimeOffset;

            try
            {

                if (options.TimeOffset != null)
                {
                    TimeOffset = new subtitleTimeEntry(options.TimeOffset, TimerTickRate);
                }
                else
                {
                    TimeOffset = new subtitleTimeEntry("00:00:00:00", 0);
                }


                Console.WriteLine($"Total Subtitles Entries not counting multiple lines: {TotalSubtitleCount}");


                //Timer elapsedTime = new Timer(timerTickRate);
                //elapsedTime.Elapsed += testMethod;
                //elapsedTime.AutoReset = true;
                //elapsedTime.Enabled = true; 

                // create new Stopwatch
                StopwatchWithOffset stopwatch = new StopwatchWithOffset(TimeOffset.time);


                // Begin timing
                stopwatch.Start();

                // Console.WriteLine(" All Done ");
                // Console.WriteLine("Press the Enter key to exit the program at any time ... ");
                // Console.ReadLine();

                foreach (Subtitle subtitle in XmlData.subtitleList.Font.Subtitle)
                {
                    // TimeSpan timeIn = TimeSpan.Parse(subtitle.TimeIn);
                    // TimeSpan timeOut = TimeSpan.Parse(subtitle.TimeOut);
                    // TimeSpan timeIn = TimeSpan.ParseExact(subtitle.TimeIn, "G", CultureInfo.CurrentCulture);
                    // TimeSpan timeOut = TimeSpan.ParseExact(subtitle.TimeOut, "G", CultureInfo.CurrentCulture);

                    subtitleTimeEntry timeIn = new subtitleTimeEntry(subtitle.TimeIn, TimerTickRate);
                    subtitleTimeEntry timeOut = new subtitleTimeEntry(subtitle.TimeOut, TimerTickRate);

                    bool subtitlePrinted = false;
                    bool outputDebugInfo = false;

                    do
                    {
                        subtitlePrinted = false;

                        if (options.DebugOutput == true && outputDebugInfo == false)
                        {
                            Console.Error.WriteLine("");
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

                            foreach (Text text in subtitle.Text)
                            {
                                int pos = text.VPosition;
                                Console.WriteLine($"{spot},{pos}: {text.SubtitleText}");
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
