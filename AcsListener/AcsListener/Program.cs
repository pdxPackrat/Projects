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
using System.Diagnostics;

namespace AcsListener
{
    class Options
    {
        [Option('d', "debug", Required = false, HelpText = "Use for debugging purposes - provides more verbose output")]
        public bool DebugOutput { get; set; }

        [Option('f', "file", Required = false, HelpText = "Specify an RPL file which must be in format of *.xml")]
        public string RplFile { get; set; }
    }

    class Program
    {
        private static AcspLeaseTimer leaseTimer;

        static UInt32 currentRequestId = 0; // Tracks the current RequestID number that has been sent to the ACS
        static bool debugOutput = false;

        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => MainProcess(opts))
                .WithNotParsed<Options>((errs) => HandleParseError(errs));

        }

        static void MainProcess(Options options)
        {
            debugOutput = options.DebugOutput;

            // Set the TcpListener to listen on port 4170 (per SMPTE 430-10:2010 specifications)
            Int32 port = 4170;
            // IPAddress localAddress = IPAddress.Parse("127.0.0.1");

            IPAddress localAddress = IPAddress.Any;

            // TcpListener = new TcpListener(IPAddress.Any, Port);
            TcpListener listener = new TcpListener(localAddress, port);

            TcpClient client;

            listener.Start();
            Console.WriteLine("[MasterThread]: Initial configuration completed - starting network listener");
            Console.WriteLine("[MasterThread]: Waiting for a connection ... ");

            try
            {
                while (true)
                {
                    // Perform a blocking call to accept requests.
                    client = listener.AcceptTcpClient();
                    Console.WriteLine("[MasterThread]: TcpClient Accepted, assigning child thread");

                    // 2047 worker threads are available by default
                    ThreadPool.QueueUserWorkItem(ListenerProcess, client);
                    Console.WriteLine("[MasterThread]: Waiting for another connection ... ");
                }
            }
            finally
            {
                Console.WriteLine("\nHit enter to continue...");
                Console.Read();
            }
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {

        }

        static void ListenerProcess(object obj)
        {
            const int timeoutValue = 10000;
            // int numberOfBytes = 0;
            uint leaseSeconds = 60;
            UInt32 testPlayoutId = 49520318;
            UInt64 timelineStart = 0;
            // UInt64 timelineEditUnits = 14400;  // 24 (edit units per second) * 60 (seconds) * 10 (minutes)
            UInt64 timelineEditUnits = 16000;  // 25 (edit units per second) * 60 (seconds) * 10 (minutes) = 15000, and adding another 1000 to make timecode 10:40
            string testResourceUrl = "http://192.168.9.88/CaptiView/rpl_test1.xml";
            var myClient = (TcpClient)obj;
            Thread thread = Thread.CurrentThread;

            try
            {
                IPEndPoint remoteEnd = (IPEndPoint)myClient.Client.RemoteEndPoint;
                IPAddress remoteAddress = remoteEnd.Address;
                Console.WriteLine($"[Thread #: {thread.ManagedThreadId}] Connection Established! RemoteIP: {remoteAddress}");

                // Presumably the ACS has establisted 

                NetworkStream stream = myClient.GetStream();
                stream.WriteTimeout = timeoutValue;  // sets the timeout to X seconds
                stream.ReadTimeout = timeoutValue;  // sets the  timeout to X seconds

                // Buffer for reading data
                ProcessAnnounceRrp(stream);
                ProcessGetNewLeaseRrp(stream, leaseSeconds);
                ProcessGetStatusRrp(stream);
                ProcessSetRplLocationRrp(stream, testResourceUrl, testPlayoutId);
                ProcessGetStatusRrp(stream);
                ProcessUpdateTimelineRrp(stream, testPlayoutId, timelineStart);
                ProcessSetOutputModeRrp(stream, true);
                ProcessUpdateTimelineRrp(stream, testPlayoutId, timelineEditUnits);
                ProcessGetStatusRrp(stream);

                SetLeaseTimer(stream, ((leaseSeconds * 1000) / 2));  // Convert to milliseconds and then halve the number

                Console.WriteLine($"[Thread #: {thread.ManagedThreadId}] Will wait here until you press a key to exit this thread");
                Console.ReadLine();

                ProcessTerminateLease(stream);

            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error: Socket timeout of {timeoutValue} reached");
                Console.WriteLine($"Exception message: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: closing network connection");
                myClient.Close();

                if (leaseTimer != null)
                {
                    leaseTimer.Stop();
                    leaseTimer.Dispose();
                }
            }

        }


        /// <summary>
        /// Sets Custom AcspLeaseTimer object
        /// </summary>
        /// <param name="leaseTimerMsec">Number of milliseconds between each Elapsed event</param>
        private static void SetLeaseTimer(NetworkStream stream, uint leaseTimerMsec)
        {
            Thread thread = Thread.CurrentThread;
            leaseTimer = new AcspLeaseTimer(stream, leaseTimerMsec);

            leaseTimer.Elapsed += ProcessLeaseTimer;
            leaseTimer.Start();

            Console.WriteLine($"[Thread #{thread.ManagedThreadId}]: Setting a recurring GetStatusRequest callback every {leaseTimerMsec} msec");
        }

        private static void ProcessLeaseTimer(object sender, ElapsedEventArgs e)
        {
            NetworkStream leaseStream = ((AcspLeaseTimer)sender).Stream;

            ProcessGetStatusRrp(leaseStream);
        }

        private static void ProcessAnnounceRrp(NetworkStream stream)
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field
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
        }

        private static void ProcessGetNewLeaseRrp(NetworkStream stream, uint leaseSeconds)
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field
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

            }

        }  // end ProcessGetNewLeaseRrp()


        private static void ProcessGetStatusRrp(NetworkStream stream)
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field
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

        }  // end ProcessGetStatusRrp()

        private static void ProcessSetRplLocationRrp(NetworkStream stream, string resourceUrl, UInt32 playoutId)
        {

            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field
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


        }  // end ProcessSetRplLocationRrp()

        private static void ProcessSetOutputModeRrp(NetworkStream stream, bool outputMode)
        {

            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field
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


        }  // end ProcessSetRplLocationRrp(

        private static void ProcessUpdateTimelineRrp(NetworkStream stream, UInt32 testPlayoutId, UInt64 timelineEditUnits)
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field
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
        }   // end ProcessUpdateTimelineRrp()


        private static void ProcessTerminateLease(NetworkStream stream)
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field
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
        }  // end ProcessTerminateLease()
    }
}
