using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Serilog;
using Serilog.Events;

using CommandLine;
using SharedCommon;
using System.Diagnostics;
using System.Xml.Serialization;

namespace AcsListener
{
    class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Use for specific debugging purposes - provides more verbose output in certain scenarios")]
        public bool VerboseOutput { get; set; } = false;
    }

    class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        ///  In this case we are setting the Log configuration, and parsing for command line arguments (of which the only one we use is the -d option anymore).
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
        {
            // Configure the Serilog logger to write to console and file
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information)
                .WriteTo.File("AcsListenerLog-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Check if we are passing command line arguments in the allowed format
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => MainProcess(opts))
                .WithNotParsed<Options>((errs) => HandleParseError(errs));

        }

        private static void MainProcess(Options opts)
        {
            // For now we are going to ignore usage of the "debug" option and just proceed to opening up an instance of the AcsListener class

            AcsListener listener = new AcsListener();

            // listener.Options.VerboseMode = opts.VerboseOutput;  // override whatever is in the configuration file

            listener.Start();

        }

        /// <summary>
        /// HandleParseError is responsible for any logic associated with errors in the command line parameters.
        /// Currently we don't do anything special with that, but we could if we wanted. 
        /// </summary>
        /// <param name="errs">A list of errors that were passed in from Main</param>
        static void HandleParseError(IEnumerable<Error> errs)
        {

        }
    }
}
