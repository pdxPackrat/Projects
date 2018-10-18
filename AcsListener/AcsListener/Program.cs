using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using CommandLine;
using SharedCommon;
using System.Diagnostics;
using System.Xml.Serialization;

namespace AcsListener
{
    class Options
    {
        private string _playtimeOffset = "0";

        [Option('d', "debug", Required = false, HelpText = "Use for debugging purposes - provides more verbose output")]
        public bool DebugOutput { get; set; }

        [Option('r', "rplUrl", Required = false, HelpText = "Specify an RPL URL which may include either full URL with IP address (not localhost) or relative location, example: /CaptiView/rpl_test1.xml")]
        public string RplUrl { get; set; }

        [Option('o', "offset", Required = false, HelpText = "Playtime offset in form of -o HH:MM:SS, -o MM:SS, or -o MM")]
        public string PlaytimeOffset
        {
            get
            {
                return _playtimeOffset;
            }
            set
            {
                this._playtimeOffset = value;
            }
        }
    }

    class Program
    {
        #region StaticData
        static readonly Int32 acsPort = 4170;  // Per SMPTE 430-10:2010 specifications
        static readonly Int32 commandPort = 13000;  // Arbitrary port choice

        private static AcspLeaseTimer leaseTimer;
        private static ManualResetEvent CanWriteToStream = new ManualResetEvent(false);
        private static bool ConnectedToAcs = false;
        private static NetworkStream stream;
        private static TcpClient ListenerClient;

        static UInt32 currentRequestId = 0; // Tracks the current RequestID number that has been sent to the ACS
        static bool debugOutput = false;

        // This next section represents static data that is stored at time of an RPL load action.  This should eventually
        // be moved to a static class to give greater control over how the data is set/read

        static UInt32 RplPlayoutId = 0;
        static UInt64 RplTimelineOffset = 0;    // Probably won't be used in our implementation but see 430-10:2010, page 6 section 6.3.2.1 for more information
        static string RplEditRate = "";
        static string RplResourceUrl = "";


    
        
        // Don't need this section anymore as these command line parameters have been obsoleted and shifted over to the command telnet connection
        /*
        static String rplUrl;
        static String timeOffset;
        */

        #endregion StaticData

        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => MainProcess(opts))
                .WithNotParsed<Options>((errs) => HandleParseError(errs));

        }

        static void MainProcess(Options options)
        {
            debugOutput = options.DebugOutput;

            // Don't need this section anymore as these command line parameters have been obsoleted and shifted over to the command telnet connection
            /*
            rplUrl = options.RplUrl;
            timeOffset = options.PlaytimeOffset;
            */

            // Set the TcpListener to listen on port 4170 (per SMPTE 430-10:2010 specifications)

            // IPAddress localAddress = IPAddress.Parse("127.0.0.1");

            IPAddress localAddress = IPAddress.Any;

            TcpListener listenerAcs = new TcpListener(localAddress, acsPort);
            TcpListener listenerCommand = new TcpListener(localAddress, commandPort);

            listenerAcs.Start();
            listenerCommand.Start();
            Console.WriteLine("[MasterThread]: Initial configuration completed - starting network listener");
            Console.WriteLine("[MasterThread]: Waiting for a connection ... ");


            try
            {
                // Perform a non-blocking call to accept requests on both the ACS and Command ports
                listenerAcs.BeginAcceptTcpClient(OnAccept, listenerAcs);
                listenerCommand.BeginAcceptTcpClient(OnAccept, listenerCommand);
            }
            finally
            {
                while (true)
                {
                    // go in to perpetual waiting loop here at the end, doing nothing but hanging out allowing the above try-block to do its thing
                }
            }

            /* Temporary commenting until I can confirm the changes worked
            try
            {
                while (true)
                {
                    // Perform a blocking call to accept requests.
                    client = listener.AcceptTcpClient();

                    Console.WriteLine("[MasterThread]: TcpClient Accepted, assigning child thread");

                    // Set ListenerProcessParams for passing to the downstream process
                    ListenerProcessParams processParams = new ListenerProcessParams(client, options.RplUrl, options.PlaytimeOffset);

                    // 2047 worker threads are available by default
                    ThreadPool.QueueUserWorkItem(ListenerProcess, processParams);
                    Console.WriteLine("[MasterThread]: Waiting for another connection ... ");
                }
            }
            finally
            {
                Console.WriteLine("\nHit enter to continue...");
                Console.Read();
            }
            */
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {

        }

        static private void OnAccept(IAsyncResult res)
        {
            TcpListener listener = (TcpListener)res.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(res);

            IPEndPoint remoteEnd = (IPEndPoint)client.Client.RemoteEndPoint;
            IPEndPoint localEnd = (IPEndPoint)client.Client.LocalEndPoint;
            IPAddress remoteAddress = remoteEnd.Address;
            IPAddress localAddress = localEnd.Address;
            Int32 remoteEndPort = remoteEnd.Port;
            Int32 localEndPort = localEnd.Port;

            Thread thread = Thread.CurrentThread;

            Console.WriteLine($"[Thread #: {thread.ManagedThreadId}] Connection Established! ");
            Console.WriteLine($"   RemoteIP: {remoteAddress}, RemotePort: {remoteEnd.Port}, ");
            Console.WriteLine($"   LocalIP: {localAddress}, LocalPort: {localEnd.Port}");

            ListenerProcessParams processParams = new ListenerProcessParams(client);

            if (localEndPort == acsPort)
            {
                ThreadPool.QueueUserWorkItem(ListenerProcess, processParams);
            }
            
            if (localEndPort == commandPort)
            {
                ThreadPool.QueueUserWorkItem(CommandProcess, processParams);
            }

            // Regardless of which thread we spawn, we need to reset the TcpListener so that it is ready to accept another
            // TCP connection on that port

            listener.BeginAcceptTcpClient(OnAccept, listener);
        }

        static void CommandProcess(object obj)
        {
            var myParams = (ListenerProcessParams)obj;
            TcpClient CommandClient = myParams.Client;
            Thread thread = Thread.CurrentThread;

            try
            {
                // Buffer for reading data
                Byte[] bytes = new byte[512];
                String data = null;       // the data received from the listener
                String CommandInput = ""; // the parsed command received
                bool Listening = true;    // controls the while-loop logic

                // Enter the listening loop.
                while (Listening == true)
                {
                    // Get a stream object for reading and writing

                    NetworkStream commandStream = CommandClient.GetStream();
                    String CommandGreeting = "COMMAND CONNECTION: WAITING FOR COMMAND INPUT\r\n";

                    if (ConnectedToAcs)
                    {
                        CommandGreeting = CommandGreeting + "( ACS connected ): ";
                    }
                    else
                    {
                        CommandGreeting = CommandGreeting + "( ACS disconnected ): ";
                    }

                    // Send back a response 
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(CommandGreeting);
                    commandStream.Write(msg, 0, msg.Length);

                    // Prepare for looping
                    data = null;
                    int i;
                    bool CancelCommandReceived = false;   // used to control the while-loop logic (and when to quit)
                    
                    // loop to receive all of the data sent by the client
                    while ((CancelCommandReceived == false) && ((i = commandStream.Read(bytes, 0, bytes.Length)) != 0))
                    {
                        // Translate the data bytes in to an ASCII string
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                        if (data == "\u0003") // checking for a CTRL+C from the connected terminal
                        {
                            CancelCommandReceived = true;   // set boolean logic to exit the while-loop
                            Console.WriteLine("Received Cancel Command");
                        }
                        else
                        {
                            // Process the data sent by the client

                            // If the data segment received is a CRLF, then take whatever parsed command so far and process it
                            if (data == "\r\n")
                            {
                                // check first if it is one of the CANCEL/QUIT commands
                                if (CommandInput.ToUpper() == "CANCEL" || CommandInput.ToUpper() == "QUIT" || CommandInput.ToUpper() == "EXIT")
                                {
                                    Console.WriteLine($"Thread #{thread.ManagedThreadId}: CANCEL/QUIT command received - terminating connection");
                                    CancelCommandReceived = true;
                                }
                                else  // Begin the section that parses for one of the main commands
                                {
                                    // List of available commands that we can take:
                                    // STATUS - returns output to the connected user indicating whether ACS is connected or not
                                    //          and is the only command that can be used when not connected to the ACS
                                    // LOAD   - in form of LOAD "FullyQualifiedUrlPath", loads an RPL and informs connected ACS
                                    // STOP   - unloads the RPL from the connected ACS (and presumably would terminate the lease?)
                                    // PLAY   - sets the OutputModeRrp to "true"
                                    // PAUSE  - sets the OutputModeRrp to "false"
                                    // TIME   - calls UpdateTimelineRrp with a calculated edit units based on a parameter in HH:MM:SS, MM:SS, or MM format

                                    var CommandSplit = CommandInput.Split(' ');
                                    string CommandBase = CommandSplit[0];
                                    string CommandParameter = ""; 
                                    String commandOutput = "";  // Any output returned from the processed command

                                    if (CommandSplit.Length >= 2) // at least one parameter was passed in with this command - we only accept the first parameter at the moment
                                    {
                                        CommandParameter = CommandSplit[1];
                                    }

                                    // Output the command details to the AcsListener console
                                    Console.WriteLine($"Thread #{thread.ManagedThreadId}:  Command Received:  {CommandBase.ToUpper()} {CommandParameter}");

                                    if (ConnectedToAcs is true)
                                    {
                                        // Start processing based on which command was received
                                        switch (CommandBase.ToUpper())
                                        {
                                            case "STATUS":
                                                commandOutput = DoCommandStatus();
                                                break;

                                            case "LOAD":
                                                if (CommandParameter == "")
                                                {
                                                    commandOutput = "LOAD command requires a parameter in format of LOAD \"FullyQualifiedUrlPath\"";
                                                }
                                                else
                                                {
                                                    string UrlPath = CommandParameter;
                                                    commandOutput = DoCommandLoad(UrlPath);
                                                    // commandOutput = "STUB holder for DoCommandLoad";
                                                }
                                                break;

                                            case "STOP":
                                                commandOutput = DoCommandStop();
                                                commandOutput = "STUB holder for DoCommandStop";
                                                break;

                                            case "PLAY":
                                                commandOutput = DoCommandPlay();
                                                // commandOutput = "STUB holder for DoCommandPlay";
                                                break;

                                            case "PAUSE":
                                                commandOutput = DoCommandPause();
                                                // commandOutput = "STUB holder for DoCommandPause";
                                                break;

                                            case "TIME":
                                                if (CommandParameter == "")
                                                {
                                                    commandOutput = "TIME command requires a parameter in format of TIME <parameter>, where parameter is either HH:MM:SS, MM:SS, or MM";
                                                }
                                                else
                                                {
                                                    // See below - forcing a 25/1 edit rate for now, but eventually we need logic that handles pulling that info from the loaded RPL

                                                    if ((RplPlayoutId != 0) && (RplEditRate != ""))
                                                    {
                                                        // Some basic validation on our part here to make sure that the RPL has been loaded first
                                                        string timeOffsetInput = CommandParameter;
                                                        commandOutput = DoCommandTime(timeOffsetInput, RplEditRate);   // For now, forcing a 25/1 edit rate
                                                    }
                                                    else
                                                    {
                                                        commandOutput = "TIME command can only be used after a successful LOAD command has been issued";
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    else if ((ConnectedToAcs is false) && (CommandBase.ToUpper() == "STATUS"))
                                    {
                                        // the only command allowed when not connected to the ACS is "STATUS"
                                        commandOutput = DoCommandStatus();
                                    }


                                    // If the command resulted in any kind of output, then add CRLF to it for proper formatting
                                    if (commandOutput != "")
                                    {
                                        commandOutput = commandOutput + "\r\n";  // Add a CRLF to the end of the output message so that it is nicely formatted for the other side
                                    }

                                    if (ConnectedToAcs is true)
                                    {
                                        commandOutput = commandOutput + "( ACS connected ): ";
                                    }
                                    else
                                    {
                                        commandOutput = commandOutput + "( ACS disconnected ): ";
                                    }

                                    msg = System.Text.Encoding.ASCII.GetBytes(commandOutput);
                                    commandStream.Write(msg, 0, msg.Length);
                                    commandOutput = "";
                                }

                                CommandInput = "";
                            }
                            else
                            {
                                // Concatenate the input to the command string and continue
                                CommandInput = String.Concat(CommandInput, data);
                            }
                        }
                    }

                    CommandClient.Close();
                    CommandClient.Dispose();
                    Listening = false;
                }

            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException: {e}");
            }
            finally
            {
                // Stop listening for new clients

                if (CommandClient != null)
                {
                    CommandClient.Close();
                    CommandClient.Dispose();
                }

                Console.WriteLine($"Thread #{thread.ManagedThreadId}: Connection Terminated");

            }


        }

        private static string DoCommandStop()
        {
            // Well firstly we need to decide what exactly a STOP command does.  
            // My thought is that it would clear all the static Rpl-related variables, perform a STOP, and then terminate the ACS lease

            ClearRplStatics();
            ProcessSetOutputModeRrp(false);
            ProcessTerminateLease();

            // The moment that the TerminateLease happens, we need to terminate the AcsConnection
            // because it is going to IMMEDIATELY try to re-establish a connection to the DCS

            return "STOP action successfully completed.  Output has been paused, and the ACS lease terminated.";
        }

        private static void ClearRplStatics()
        {
            RplPlayoutId = 0;
            RplEditRate = "";
            RplResourceUrl = "";
            RplTimelineOffset = 0;
        }

        private static string DoCommandTime(string timeOffsetInput, string editRateInput)
        {
            // First, we need to construct an RplReelDuration from the timeOffsetInput that was passed to us.  
            // In this version of DoCommandTime, we are (for now) assuming an editRateInput of "25 1" passed in
            // but eventually we will get this information from the loaded RPL file

            RplReelDuration reelDuration = new RplReelDuration(timeOffsetInput, editRateInput);
            UInt64 updatedEditUnits = reelDuration.EditUnits;
            string outputMessage = "";
            
            if (RplPlayoutId != 0)
            {
                // Some basic validation that we have a successfully loaded RPL
                ProcessUpdateTimelineRrp(RplPlayoutId, updatedEditUnits);
                outputMessage = "TIME command issued with:\r\n" + " Time: " + timeOffsetInput + "\r\n EditRate: " + editRateInput + "\r\n EditUnits: " + updatedEditUnits;
            }
            else
            {
                outputMessage = "Error:  Attempted a DoCommandTime call without a properly loaded RPL file first";
            }

            return outputMessage;
        }

        private static string DoCommandPause()
        {
            String outputMessage = "";
            // Need some additional logic here to handle whether an RPL has already been loaded or not

            // check to make sure that the NetworkStream is already set by the initial connection
            if ((ConnectedToAcs is true) && (stream != null))
            {
                // For now we assume this succeeds, but eventually we need additional logic to detect whether this succeeds or not
                ProcessSetOutputModeRrp(false);
                outputMessage = "ACS instructed to set OutputMode to FALSE";
            }
            else
            {
                outputMessage = "ACS is not currently connected";
            }

            return outputMessage;
        }

        private static string DoCommandPlay()
        {
            String outputMessage = "";
            // Need some additional logic here to handle whether an RPL has already been loaded or not

            // check to make sure that the NetworkStream is already set by the initial connection
            if ((ConnectedToAcs is true) && (stream != null))
            {
                // For now we assume this succeeds, but eventually we need additional logic to detect whether this succeeds or not
                ProcessSetOutputModeRrp(true);
                outputMessage = "ACS instructed to set OutputMode to TRUE";
            }
            else
            {
                outputMessage = "ACS is not currently connected";
            }

            return outputMessage;
        }

        private static string DoCommandStatus()
        {
            string result;

            if (ConnectedToAcs is true)
            {
                result = "ACS is currently connected";
            }
            else
            {
                result = "ACS is NOT connected";
            }

            return result;
        }

        private static string DoCommandLoad(string rplUrlPath)
        {

            // Need some kind of URL / file validation present here prior to the XmlData load

            ResourcePresentationList XmlData = LoadRplFromUrl(rplUrlPath);

            // Set the static data first, that is needed by the other commands

            if (XmlData.PlayoutId > 0)  // basically checking for a valid data load here
            {
                RplPlayoutId = XmlData.PlayoutId;
            }
            else
            {
                return "Error:  RPL file was not loaded correctly.   PlayoutId is not a valid value";
            }

            RplTimelineOffset = XmlData.ReelResources.TimelineOffset;
            RplEditRate = XmlData.ReelResources.EditRate;
            RplResourceUrl = rplUrlPath;

            RplReelDuration startingTimeline = new RplReelDuration("00:00:00", RplEditRate);
            UInt64 timelineEditUnits = startingTimeline.EditUnits;  // Should be 0 in the current iteration

            // Need to set the RPL Location
            ProcessSetRplLocationRrp(RplResourceUrl, RplPlayoutId);
            ProcessGetStatusRrp();

            // Need to set initial timeline here on a LOAD (presumably with a 0 timeline)
            ProcessUpdateTimelineRrp(RplPlayoutId, timelineEditUnits);
            ProcessGetStatusRrp();


            // Really need some error-checking in the above Process{...} stuff so that we have some way to tell if it went wrong

            Console.WriteLine($"LOAD command issued successfully.");
            if (debugOutput is true)
            {
                Console.WriteLine($"PlayoutId:  {RplPlayoutId}");
                Console.WriteLine($"timelineStart:  {RplTimelineOffset}");
                Console.WriteLine($"editRate:  {RplEditRate}");
                Console.WriteLine($"timelineEditUnits:  {timelineEditUnits}");
                Console.WriteLine($"resourceUrl:   {RplResourceUrl}");
            }

            return "LOAD command issued successfully for: " + RplResourceUrl;
        }



        /// <summary>
        /// ListenerProcess is responsible handling most of the work of the AcsListener.  When a TCP connection is made, 
        /// the main thread spawns off a child thread (via the ThreadPool) to ListenerProcess.  
        /// </summary>
        /// <param name="obj">A generic object containing data of type ListenerProcessParams</param>
        static void ListenerProcess(object obj)
        {
            const int timeoutValue = 10000;
            uint leaseSeconds = 60;
            // int numberOfBytes = 0;
            // UInt32 testPlayoutId = 49520318;
            // UInt64 timelineStart = 0;
            // UInt64 timelineEditUnits = 14400;  // 24 (edit units per second) * 60 (seconds) * 10 (minutes)
            // UInt64 timelineEditUnits = 16000;  // 25 (edit units per second) * 60 (seconds) * 10 (minutes) = 15000, and adding another 1000 to make timecode 10:40
            // string testResourceUrl = "http://192.168.9.88/CaptiView/rpl_test1.xml";

            var myParams = (ListenerProcessParams)obj;

            if ((ListenerClient != null) && (ListenerClient.Connected))
            {
                // If the static ListenerClient is already connected in another thread then we need to do the following:
                //    1) Check the signal to make sure that no other thread is trying to write to the stream right now
                //    2) Lock the signal so that no other thread tries to write
                //    3) Close the existing stream
                //    4) Close the existing TCP connection

                CanWriteToStream.WaitOne();   // Wait to make sure no one else is trying to write to the NetworkStream
                CanWriteToStream.Reset();     // Disable writing to the ACS NetworkStream for any other thread

                if (stream != null)
                {
                    stream.Close();               // Close and dispose of the ACS NetworkStream
                    stream = null;                // Null out the static ACS NetworkStream
                }

                ListenerClient.Close();           // Close the existing TCP connection

            }

            ListenerClient = myParams.Client;

            /***  temporarily removed as these command-line options are no longer part of the design.  
            String rplUrlPath = myParams.UrlPath;    // Needs to be fully-qualified URL that can be seen by the ACS system
            String timeOffset = myParams.TimeOffset; // Time offset in the format of HH:MM:SS, MM:SS, or MM
            */

            Thread thread = Thread.CurrentThread;


            // Need some kind of URL / file validation present here prior to the XmlData load
            // This doesn't get done here anymore - it should get processed in DoCommandLoad() instead
            /*
            ResourcePresentationList XmlData = LoadRplFromUrl(rplUrlPath);

            UInt32 PlayoutId = XmlData.PlayoutId;
            UInt64 timelineStart = XmlData.ReelResources.TimelineOffset;
            String editRate = XmlData.ReelResources.EditRate;
            RplReelDuration duration = new RplReelDuration(timeOffset, editRate);
            UInt64 timelineEditUnits = duration.EditUnits;
            string resourceUrl = rplUrlPath;

            if (debugOutput is true)
            {
                Console.WriteLine($"PlayoutId:  {PlayoutId}");
                Console.WriteLine($"timelineStart:  {timelineStart}");
                Console.WriteLine($"editRate:  {editRate}");
                Console.WriteLine($"timelineEditUnits:  {timelineEditUnits}");
                Console.WriteLine($"resourceUrl:   {resourceUrl}");
            }
            */

            try
            {
                IPEndPoint remoteEnd = (IPEndPoint)ListenerClient.Client.RemoteEndPoint;
                IPAddress remoteAddress = remoteEnd.Address;
                
                Console.WriteLine($"[Thread #: {thread.ManagedThreadId}] Connection Established! RemoteIP: {remoteAddress}");
                ConnectedToAcs = true;   // set the static variable to true to let CommandProcess know if connection has occurred

                // Presumably the ACS has establisted 

                if (stream != null)
                {
                    // Check to see if stream hasn't been cleaned up properly since the last connection
                    // If it hasn't, then we need to clean that up first

                    stream.Close();
                }

                stream = ListenerClient.GetStream();
                stream.WriteTimeout = timeoutValue;  // sets the timeout to X milliseconds
                stream.ReadTimeout = timeoutValue;  // sets the  timeout to X milliseconds
                CanWriteToStream.Set();             // Finally, set the signal to indicate that the NetworkStream can be written to by other threads

                // Buffer for reading data
                ProcessAnnounceRrp();
                ProcessGetNewLeaseRrp(leaseSeconds);
                ProcessGetStatusRrp();

                // No longer needed HERE in this implementation design, 
                // will be done from CommandProcess() instead
                // ProcessSetRplLocationRrp(resourceUrl, PlayoutId);
                // ProcessGetStatusRrp();

                // ProcessUpdateTimelineRrp(PlayoutId, timelineStart);
                // ProcessSetOutputModeRrp(true);
                // ProcessUpdateTimelineRrp(PlayoutId, timelineEditUnits);
                // ProcessGetStatusRrp();

                SetLeaseTimer((leaseSeconds * 1000) / 2);  // Convert to milliseconds and then halve the number

                Console.WriteLine($"[Thread #: {thread.ManagedThreadId}] Will wait here until you press a key to exit this thread");
                Console.ReadLine();

                ProcessTerminateLease();
                
                stream = null;   // Null set the NetworkStream object so that it is again available for next connection
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error: Socket timeout of {timeoutValue} reached");
                Console.WriteLine($"Exception message: {ex.Message}");
            }
            finally
            {
                DoAcsConnectionCleanup();

            }

        }

        private static void DoAcsConnectionCleanup()
        {
            Thread thread = Thread.CurrentThread;

            if (stream != null)
            {
                try
                {
                    Console.WriteLine($"[Thread #: {thread.ManagedThreadId}] Attempting to close the NetworkStream and close the overall ACS TCP connection");
                    CanWriteToStream.WaitOne();
                    CanWriteToStream.Reset();    // Block so that nothing else attempts to write to the stream
                    stream.Close();
                }
                finally
                {
                    stream = null;
                }
            }

            if (ListenerClient != null)
            {
                if (ListenerClient.Connected)
                {
                    try
                    {
                        Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: closing network connection");
                        ListenerClient.Close();
                    }
                    finally
                    {
                        ListenerClient = null;
                    }
                }
            }

            if (leaseTimer != null)
            {
                leaseTimer.Stop();
                leaseTimer.Dispose();
                leaseTimer = null;
            }

            ConnectedToAcs = false;   // Set the flag to indicate that connection to/from the ACS has been terminated
        }

        private static ResourcePresentationList LoadRplFromUrl(string rplUrlPath)
        {
            // Define the XmlSerializer casting to be of type ResourcePresentationList
            XmlSerializer Deserializer = new XmlSerializer(typeof(ResourcePresentationList));
            ResourcePresentationList XmlData;

            // Open a new WebClient to get the data from the target URL
            
            WebClient client = new WebClient();

            try
            {
                string data = Encoding.Default.GetString(client.DownloadData(rplUrlPath));

                try
                {
                    Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(data));

                    // Deserialize the input file
                    object DeserializedData = Deserializer.Deserialize(stream);

                    // Cast the deserialized data to the SubtitleReel type
                    XmlData = (ResourcePresentationList)DeserializedData;

                    stream.Close();
                }
                finally
                {
                }
            }
            finally
            {
            }

            // Close the input file stream
            client.Dispose();

            // Send the deserialized data pointer back to the calling routine
            return XmlData;
        }


        /// <summary>
        /// Sets Custom AcspLeaseTimer object
        /// </summary>
        /// <param name="leaseTimerMsec">Number of milliseconds between each Elapsed event</param>
        private static void SetLeaseTimer(uint leaseTimerMsec)
        {
            Thread thread = Thread.CurrentThread;
            leaseTimer = new AcspLeaseTimer(stream, leaseTimerMsec);

            leaseTimer.Elapsed += ProcessLeaseTimer;
            leaseTimer.Start();

            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Setting a recurring GetStatusRequest callback every {leaseTimerMsec} msec");
        }

        private static void ProcessLeaseTimer(object sender, ElapsedEventArgs e)
        {
            NetworkStream leaseStream = ((AcspLeaseTimer)sender).Stream;   // leaseStream isn't used in this current version but not ready to obsolete it quite yet

            ProcessGetStatusRrp();
        }

        private static void ProcessAnnounceRrp()
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field

            if (debugOutput is true)
            {
                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            bool announcePairSuccessful = false;
            while (announcePairSuccessful == false)
            {

                // Send the "Announce request" to the ACS system

                AcspAnnounceRequest announceRequest = new AcspAnnounceRequest();
                currentRequestId = announceRequest.RequestId;

                stream.Write(announceRequest.PackArray, 0, announceRequest.PackArray.Length);
                Console.WriteLine($"[{DateTime.Now}]Announce Request sent to ACS.  RequestID #: {currentRequestId}");

                // Wait for AnnounceResponse from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();


                // Expecting an AnnounceResponse message
                while (announcePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValue))
                {
                    if (stream.DataAvailable == true)
                    {

                        numberOfBytes = stream.Read(header, 0, header.Length);  // Read 20-byte header in from the NetworkStream
                        Console.WriteLine($"[Thread #{thread.ManagedThreadId}] {numberOfBytes}-byte header successfully read from network stream");

                        AcspResponseHeader announceResponseHeader = new AcspResponseHeader(header);
                        if (announceResponseHeader.Key.IsBadRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: AnnounceResponse [Bad Message] received from ACS!");
                            // Need some control logic here to figure out how to handle a Bad Request message
                        }

                        if (announceResponseHeader.Key.IsGoodRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Good AnnounceResponse received from ACS!");
                            int length = announceResponseHeader.PackLength.Length;

                            Byte[] bytes = new Byte[length];
                            numberOfBytes = stream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: {numberOfBytes}-byte AnnounceResponse successfully read from network stream");

                            AcspAnnounceResponse announceResponse = new AcspAnnounceResponse(bytes);
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: RequestId is: {announceResponse.RequestId}");
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: DeviceDescription is: {announceResponse.DeviceDescription}");

                            if (announceResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful)
                            {
                                announcePairSuccessful = true;
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: RrpSuccessful");
                            }
                            else
                            {
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: {announceResponse.StatusResponseKeyString}");
                            }
                        }
                    }  // if (stream.DataAvailable == true)
                }  // end while(stopwatch.ElapsedMilliseconds < timeoutValue)
            }  // end while(annnouncePairSuccessful == false) loop

            CanWriteToStream.Set();  // Signal all other processes that they can write to the stream now
        }

        private static void ProcessGetNewLeaseRrp(uint leaseSeconds)
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field


            if (debugOutput is true)
            {
                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            bool messagePairSuccessful = false;
            while (messagePairSuccessful == false)
            {

                // Send the "Get New Lease request" to the ACS system

                AcspGetNewLeaseRequest leaseRequest = new AcspGetNewLeaseRequest(leaseSeconds);
                currentRequestId = leaseRequest.RequestId;

                stream.Write(leaseRequest.PackArray, 0, leaseRequest.PackArray.Length);
                Console.WriteLine($"[{DateTime.Now}]GetNewLease Request for {leaseSeconds} seconds sent to ACS.  RequestID #: {currentRequestId}");

                // Wait for AnnounceResponse from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Expecting a GetNewLeaseResponse message
                while (messagePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValue))
                {
                    if (stream.DataAvailable == true)
                    {

                        numberOfBytes = stream.Read(header, 0, header.Length);  // Read 20-byte header in from the NetworkStream
                        Console.WriteLine($"[Thread #{thread.ManagedThreadId}] {numberOfBytes}-byte header successfully read from network stream");

                        AcspResponseHeader announceResponseHeader = new AcspResponseHeader(header);
                        if (announceResponseHeader.Key.IsBadRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: GetNewLeaseResponse [Bad Message] received from ACS!");
                            // Need some control logic here to figure out how to handle a Bad Request message
                        }

                        if (announceResponseHeader.Key.IsGoodRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Good GetNewLeaseResponse received from ACS!");

                            if (announceResponseHeader.Key.NodeNames == Byte13NodeNames.GetNewLeaseResponse)
                            {
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Expected GetNewLeaseResponse has been received");

                                int length = announceResponseHeader.PackLength.Length;

                                Byte[] bytes = new Byte[length];
                                numberOfBytes = stream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: {numberOfBytes}-byte GetNewLeaseResponse successfully read from network stream");

                                AcspGetNewLeaseResponse leaseResponse = new AcspGetNewLeaseResponse(bytes);
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: RequestId is: {leaseResponse.RequestId}");

                                if (leaseResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful)
                                {
                                    messagePairSuccessful = true;
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: RrpSuccessful");
                                }
                                else
                                {
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: {leaseResponse.StatusResponseKeyString}");
                                }
                            }
                        }
                    }  // if (stream.DataAvailable == true)
                }  // end while(stopwatch.ElapsedMilliseconds < timeoutValue)

            }  // end while(messagePairSuccessful == false)

            CanWriteToStream.Set();  // Signal that it is okay to write to the NetworkStream

        }  // end ProcessGetNewLeaseRrp()


        private static void ProcessGetStatusRrp()
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field

            if (debugOutput is true)
            {
                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            bool messagePairSuccessful = false;
            while (messagePairSuccessful == false)
            {

                // Send the "GetStatusRequest " to the ACS system

                AcspGetStatusRequest statusRequest = new AcspGetStatusRequest();
                currentRequestId = statusRequest.RequestId;

                stream.Write(statusRequest.PackArray, 0, statusRequest.PackArray.Length);
                Console.WriteLine($"[{DateTime.Now}]GetStatus Request sent to ACS.  RequestID #: {currentRequestId}");

                // Wait for GetStatusResponse from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Expecting a GetStatusResponse message
                while (messagePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValue))
                {
                    if (stream.DataAvailable == true)
                    {
                        numberOfBytes = stream.Read(header, 0, header.Length);  // Read 20-byte header in from the NetworkStream
                        Console.WriteLine($"[Thread #{thread.ManagedThreadId}] {numberOfBytes}-byte header successfully read from network stream");

                        AcspResponseHeader responseHeader = new AcspResponseHeader(header);
                        if (responseHeader.Key.IsBadRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: GetStatusResponse [Bad Message] received from ACS!");
                            // Need some control logic here to figure out how to handle a Bad Request message
                        }

                        if (responseHeader.Key.IsGoodRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Good GetStatusResponse received from ACS!");

                            if (responseHeader.Key.NodeNames == Byte13NodeNames.GetStatusResponse)
                            {
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Expected GetStatusResponse has been received");

                                int length = responseHeader.PackLength.Length;

                                Byte[] bytes = new Byte[length];
                                numberOfBytes = stream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: {numberOfBytes}-byte GetStatusResponse successfully read from network stream");

                                AcspGetStatusResponse statusResponse = new AcspGetStatusResponse(bytes);
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: RequestId is: {statusResponse.RequestId}");

                                if (statusResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful)
                                {
                                    messagePairSuccessful = true;
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: RrpSuccessful");
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Message Received: {statusResponse.StatusResponseMessage}");
                                }
                                else
                                {
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: {statusResponse.StatusResponseKeyString}");
                                }
                            }
                        }
                    }  // if (stream.DataAvailable == true)
                }  // end while(stopwatch.ElapsedMilliseconds < timeoutValue)
            } // end while(messagePairSuccessful == false)

            CanWriteToStream.Set();  // Set signal to indicate that it is safe to write to the NetworkStream

        }  // end ProcessGetStatusRrp()

        private static void ProcessSetRplLocationRrp(string resourceUrl, UInt32 playoutId)
        {

            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field


            if (debugOutput is true)
            {
                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            bool messagePairSuccessful = false;
            while (messagePairSuccessful == false)
            {
                // Send the "SetRplLocationRequest " to the ACS system

                AcspSetRplLocationRequest rplRequest = new AcspSetRplLocationRequest(resourceUrl, playoutId);
                currentRequestId = rplRequest.RequestId;

                stream.Write(rplRequest.PackArray, 0, rplRequest.PackArray.Length);
                Console.WriteLine($"[{DateTime.Now}]SetRplLocation Request sent to ACS.  RequestID #: {currentRequestId}");

                // Wait for SetRplLocationResponse from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Expecting a GetStatusResponse message
                while (messagePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValue))
                {
                    if (stream.DataAvailable == true)
                    {
                        numberOfBytes = stream.Read(header, 0, header.Length);  // Read 20-byte header in from the NetworkStream
                        Console.WriteLine($"[Thread #{thread.ManagedThreadId}] {numberOfBytes}-byte header successfully read from network stream");

                        AcspResponseHeader responseHeader = new AcspResponseHeader(header);
                        if (responseHeader.Key.IsBadRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: SetRplLocation [Bad Message] received from ACS!");
                            // Need some control logic here to figure out how to handle a Bad Request message
                        }

                        if (responseHeader.Key.IsGoodRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Good SetRplLocationResponse received from ACS!");

                            if (responseHeader.Key.NodeNames == Byte13NodeNames.SetRplLocationResponse)
                            {
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Expected SetRplLocationResponse has been received");

                                int length = responseHeader.PackLength.Length;

                                Byte[] bytes = new Byte[length];
                                numberOfBytes = stream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: {numberOfBytes}-byte SetRplLocationResponse successfully read from network stream");

                                AcspSetRplLocationResponse locationResponse = new AcspSetRplLocationResponse(bytes);
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: RequestId is: {locationResponse.RequestId}");

                                if ((locationResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful) || 
                                    (locationResponse.StatusResponseKey == GeneralStatusResponseKey.Processing))
                                {
                                    messagePairSuccessful = true;
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: {locationResponse.StatusResponseKeyString}");
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Message Received: {locationResponse.StatusResponseMessage}");
                                }
                                else
                                {
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: {locationResponse.StatusResponseKeyString}");
                                }
                            }
                        }
                    }  // if (stream.DataAvailable == true)
                }  // end while(stopwatch.ElapsedMilliseconds < timeoutValue)
            } // end while(messagePairSuccessful == false)

            CanWriteToStream.Set();

        }  // end ProcessSetRplLocationRrp()

        private static void ProcessSetOutputModeRrp(bool outputMode)
        {

            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field


            if (debugOutput is true)
            {
                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            bool messagePairSuccessful = false;
            while (messagePairSuccessful == false)
            {
                // Send the "SetOutputModeRequest " to the ACS system

                AcspSetOutputModeRequest outputModeRequest = new AcspSetOutputModeRequest(outputMode);
                currentRequestId = outputModeRequest.RequestId;

                stream.Write(outputModeRequest.PackArray, 0, outputModeRequest.PackArray.Length);
                Console.WriteLine($"[{DateTime.Now}]SetOutputMode Request sent to ACS.  RequestID #: {currentRequestId}");

                // Wait for SetOutputMode Response from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Expecting a GetStatusResponse message
                while (messagePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValue))
                {
                    if (stream.DataAvailable == true)
                    {
                        numberOfBytes = stream.Read(header, 0, header.Length);  // Read 20-byte header in from the NetworkStream
                        Console.WriteLine($"[Thread #{thread.ManagedThreadId}] {numberOfBytes}-byte header successfully read from network stream");

                        AcspResponseHeader responseHeader = new AcspResponseHeader(header);
                        if (responseHeader.Key.IsBadRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: SetOutputMode Response [Bad Message] received from ACS!");
                            // Need some control logic here to figure out how to handle a Bad Request message
                        }

                        if (responseHeader.Key.IsGoodRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Good SetOutputMode Response received from ACS!");

                            if (responseHeader.Key.NodeNames == Byte13NodeNames.SetOutputModeResponse)
                            {
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Expected SetOutputModeResponse has been received");

                                int length = responseHeader.PackLength.Length;

                                Byte[] bytes = new Byte[length];
                                numberOfBytes = stream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: {numberOfBytes}-byte SetRplLocationResponse successfully read from network stream");

                                AcspSetOutputModeResponse outputModeResponse = new AcspSetOutputModeResponse(bytes);
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: RequestId is: {outputModeResponse.RequestId}");

                                if (outputModeResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful)
                                {
                                    messagePairSuccessful = true;
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: {outputModeResponse.StatusResponseKeyString}");
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Message Received: {outputModeResponse.StatusResponseMessage}");
                                }
                                else
                                {
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: {outputModeResponse.StatusResponseKeyString}");
                                }
                            }
                        }
                    }  // if (stream.DataAvailable == true)
                }  // end while(stopwatch.ElapsedMilliseconds < timeoutValue)
            } // end while(messagePairSuccessful == false)

            CanWriteToStream.Set();  // Signal that it is okay to write to the NetworkStream again

        }  // end ProcessSetRplLocationRrp()

        private static void ProcessUpdateTimelineRrp(UInt32 testPlayoutId, UInt64 timelineEditUnits)
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field


            if (debugOutput is true)
            {
                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            bool messagePairSuccessful = false;
            while (messagePairSuccessful == false)
            {
                // Send the "UpdateTimeline Request " to the ACS system

                AcspUpdateTimelineRequest updateTimelineRequest = new AcspUpdateTimelineRequest(testPlayoutId, timelineEditUnits);
                currentRequestId = updateTimelineRequest.RequestId;

                stream.Write(updateTimelineRequest.PackArray, 0, updateTimelineRequest.PackArray.Length);
                Console.WriteLine($"[{DateTime.Now}]UpdateTimeline Request sent to ACS.  RequestID #: {currentRequestId}");

                // Wait for UpdateTimeline Response from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Expecting a GetStatusResponse message
                while (messagePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValue))
                {
                    if (stream.DataAvailable == true)
                    {
                        numberOfBytes = stream.Read(header, 0, header.Length);  // Read 20-byte header in from the NetworkStream
                        Console.WriteLine($"[Thread #{thread.ManagedThreadId}] {numberOfBytes}-byte header successfully read from network stream");

                        AcspResponseHeader responseHeader = new AcspResponseHeader(header);
                        if (responseHeader.Key.IsBadRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: UpdateTimeline Response [Bad Message] received from ACS!");
                            // Need some control logic here to figure out how to handle a Bad Request message
                        }

                        if (responseHeader.Key.IsGoodRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Good UpdateTimeline Response received from ACS!");

                            if (responseHeader.Key.NodeNames == Byte13NodeNames.UpdateTimelineResponse)
                            {
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Expected UpdateTimelineResponse has been received");

                                int length = responseHeader.PackLength.Length;

                                Byte[] bytes = new Byte[length];
                                numberOfBytes = stream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: {numberOfBytes}-byte UpdateTimelineResponse successfully read from network stream");

                                AcspUpdateTimelineResponse updateTimelineResponse = new AcspUpdateTimelineResponse(bytes);
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: RequestId is: {updateTimelineResponse.RequestId}");

                                if (updateTimelineResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful)
                                {
                                    messagePairSuccessful = true;
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: {updateTimelineResponse.StatusResponseKeyString}");
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Message Received: {updateTimelineResponse.StatusResponseMessage}");
                                }
                                else
                                {
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: {updateTimelineResponse.StatusResponseKeyString}");
                                }
                            }
                        }
                    }  // if (stream.DataAvailable == true)
                }  // end while(stopwatch.ElapsedMilliseconds < timeoutValue)
            } // end while(messagePairSuccessful == false)

            CanWriteToStream.Set(); // Signal that it is okay to write to the NetworkStream again

        }   // end ProcessUpdateTimelineRrp()


        private static void ProcessTerminateLease()
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field


            if (debugOutput is true)
            {
                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            bool messagePairSuccessful = false;
            while (messagePairSuccessful == false)
            {
                // Send the "TerminateLease Request " to the ACS system

                AcspTerminateLeaseRequest terminateLeaseRequest = new AcspTerminateLeaseRequest();
                currentRequestId = terminateLeaseRequest.RequestId;

                stream.Write(terminateLeaseRequest.PackArray, 0, terminateLeaseRequest.PackArray.Length);
                Console.WriteLine($"[{DateTime.Now}]TerminateLease Request sent to ACS.  RequestID #: {currentRequestId}");

                // Wait for TerminateLease Response from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                // Expecting a TerminateLeaseResponse message
                while (messagePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValue))
                {
                    if (stream.DataAvailable == true)
                    {
                        numberOfBytes = stream.Read(header, 0, header.Length);  // Read 20-byte header in from the NetworkStream
                        Console.WriteLine($"[Thread #{thread.ManagedThreadId}] {numberOfBytes}-byte header successfully read from network stream");

                        AcspResponseHeader responseHeader = new AcspResponseHeader(header);
                        if (responseHeader.Key.IsBadRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: TerminateLease Response [Bad Message] received from ACS!");
                            // Need some control logic here to figure out how to handle a Bad Request message
                        }

                        if (responseHeader.Key.IsGoodRequest)
                        {
                            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Good TerminateLease Response received from ACS!");

                            if (responseHeader.Key.NodeNames == Byte13NodeNames.TerminateLeaseResponse)
                            {
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Expected TerminateLeaseResponse has been received");

                                int length = responseHeader.PackLength.Length;

                                Byte[] bytes = new Byte[length];
                                numberOfBytes = stream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: {numberOfBytes}-byte TerminateLeaseResponse successfully read from network stream");

                                AcspTerminateLeaseResponse terminateLeaseResponse = new AcspTerminateLeaseResponse(bytes);
                                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: RequestId is: {terminateLeaseResponse.RequestId}");

                                if (terminateLeaseResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful)
                                {
                                    messagePairSuccessful = true;
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: {terminateLeaseResponse.StatusResponseKeyString}");
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Message Received: {terminateLeaseResponse.StatusResponseMessage}");
                                }
                                else
                                {
                                    Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Response Received: {terminateLeaseResponse.StatusResponseKeyString}");
                                }
                            }
                        }
                    }  // if (stream.DataAvailable == true)
                }  // end while(stopwatch.ElapsedMilliseconds < timeoutValue)
            } // end while(messagePairSuccessful == false)

            CanWriteToStream.Set();     // The subsequent clean-up activity needs the signal set (as it safely waits for any other ACS write to finish first)
            DoAcsConnectionCleanup();   // Do all the necessary stuff to clean up the network stream and close the TCP connection

            // We don't need to do a CanWriteToStream.Set() here, since at this point at the end of the TerminateLease, we are assuming that the TCP connection is closed or closing
            
        }  // end ProcessTerminateLease()
    }
}
