﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using System.Xml.Serialization;
using Serilog;
using SharedCommon;

namespace AcsListener
{
    class AcsListener
    {
        // www.iana.org/assignments/service-names-port-numbers

        // This next section represents static data that is stored at time of an RPL load action.  This should eventually
        // be moved to a static class to give greater control over how the data is set/read

        #region StaticData
        private static Int32 DefaultAcsPort = 4170;       // Per SMPTE 430-10:2010 specifications
        private static Int32 DefaultCommandPort = 13000;  // Arbitrary port choice, but appears to be unassigned at the moment according to
        private static string DefaultRplUrlPath = "";
        private static IPAddress SavedLocalAddress;
        private static AcspLeaseTimer LeaseTimer;
        private static ManualResetEvent CanWriteToStream = new ManualResetEvent(false);
        private static bool ConnectedToAcs = false;
        private static bool KillCommandReceived = false;
        private static NetworkStream ListenerStream;
        private static TcpClient ListenerClient;
        private static Boolean IsAutoReload;
        private static UInt32 CurrentRequestId = 0; // Tracks the current RequestID number that has been sent to the ACS
        private static bool VerboseOutput = false;
        private static RplLoadInformation RplLoadInfo = new RplLoadInformation();
        private static RplLoadInformation RplReloadInfo;      // Set to RplLoadInfo if/when a new RplLoadInfo instance is created
        #endregion StaticData

        public AcsListenerConfigItems Config;

        public AcsListener()
        {
            Config = new AcsListenerConfigItems();
        }

        /// <summary>
        /// Start is responsible for setting up both the ACS and Command TcpListener processes.
        /// Everything is initialized for that, then Start goes in to an infinite 30-second loop, and all else is 
        /// handled by the child threads that are spawned whenever a TCP connection is established on either the ACS or 
        /// Command port. 
        /// </summary>
        /// <param name="options">Command-line parameters passed in from Main</param>
        public void Start()
        {
            DefaultAcsPort = Config.AcsPort;
            DefaultCommandPort = Config.CommandPort;
            DefaultRplUrlPath = Config.RplUrlPath;
            IsAutoReload = Config.AutoReload;

            // Don't need this section anymore as these command line parameters have been obsoleted and shifted over to the command telnet connection
            /*
            rplUrl = options.RplUrl;
            timeOffset = options.PlaytimeOffset;
            */

            // Set the TcpListener to listen on port 4170 (per SMPTE 430-10:2010 specifications)

            // IPAddress localAddress = IPAddress.Parse("127.0.0.1");

            IPAddress localAddress = IPAddress.Any;

            TcpListener listenerAcs = new TcpListener(localAddress, DefaultAcsPort);
            TcpListener listenerCommand = new TcpListener(localAddress, DefaultCommandPort);

            listenerAcs.Start();
            listenerCommand.Start();
            Log.Information("[MasterThread]: Initial configuration completed - starting network listener");
            Log.Information("[MasterThread]: Waiting for a connection ... ");

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
                    Thread.Sleep(30000);
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

        /// <summary>
        /// OnAccept is the method called by MainProcess when one of the two ASYNC TCP connections establishes.
        /// OnAccept handles connections on the default ACS port (always 4170) and the CommandProcess port (default 13000).
        /// The method analyzes whether the connecting port is the ACS or the CommandProcess, and based on that port connection, 
        /// it calls either the ListenerProcess (for ACS connections) or the CommandProcess.  
        /// </summary>
        /// <param name="tcpListenerObject">A TcpListener object passed in from MainProcess, either for ACS or CommandProcess port.</param>
        static private void OnAccept(IAsyncResult tcpListenerObject)
        {
            // Set up the TcpClient to be used by later methods
            TcpListener listener = (TcpListener)tcpListenerObject.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(tcpListenerObject);

            // Initialize some of the TCP-related information that is shown in the AcsListener console/logs
            IPEndPoint remoteEnd = (IPEndPoint)client.Client.RemoteEndPoint;
            IPEndPoint localEnd = (IPEndPoint)client.Client.LocalEndPoint;
            IPAddress remoteAddress = remoteEnd.Address;
            IPAddress localAddress = localEnd.Address;
            Int32 remoteEndPort = remoteEnd.Port;
            Int32 localEndPort = localEnd.Port;

            if (localEndPort == DefaultAcsPort)  // if this is the ACS connecting then ...
            {
                SavedLocalAddress = localAddress;   // Save this address for later use by ValidateSubtitleFromUrlString
            }

            Thread thread = Thread.CurrentThread;

            // Output connection information to the AcsListener process
            Log.Information($"[Thread #: {thread.ManagedThreadId}] Connection Established! ");
            Log.Information($"   RemoteIP: {remoteAddress}, RemotePort: {remoteEnd.Port}, ");
            Log.Information($"   LocalIP: {localAddress}, LocalPort: {localEnd.Port}");

            // Initialize the ListenerProcessParams object for passing to the child thread (the TcpClient info)
            ListenerProcessParams processParams = new ListenerProcessParams(client);

            // Depending on which port connected (ACS or Command) queue a thread for that process
            if (localEndPort == DefaultAcsPort)
            {
                ThreadPool.QueueUserWorkItem(ListenerProcess, processParams);
            }
            
            if (localEndPort == DefaultCommandPort)
            {
                ThreadPool.QueueUserWorkItem(CommandProcess, processParams);
            }

            // Regardless of which thread we spawn, we need to reset the TcpListener so that it is ready to accept another
            // TCP connection on that port

            listener.BeginAcceptTcpClient(OnAccept, listener);
        }

        /// <summary>
        /// CommandProcess is the target of the worker thread that is launched when a connection is made on the Command port (default 13000).
        /// The CommandProcess (sometimes referred to as the CommandProcessor) handles all of the proactive commands that need to be issued
        /// to the ACS, such as setting OutputMode (true or false), specifying a location of an RPL file for Playout, updating Timeline, etc. 
        /// All of these are handled through a variety of commands that are defined in this method (see comments further in the method for that).  
        /// </summary>
        /// <param name="listenerProcessParamsObject">An object of type ListenerProcessParams, the main argument contained within is the TcpClient defined in the parent
        /// process that calls this one.</param>
        private static void CommandProcess(object listenerProcessParamsObject)
        {
            var myParams = (ListenerProcessParams)listenerProcessParamsObject;
            TcpClient CommandClient = myParams.Client;
            Thread thread = Thread.CurrentThread;

            try
            {
                // Buffer for reading data
                Byte[] bytes = new byte[512];
                String data = null; // the data received from the listener
                String commandInput = ""; // the parsed command received
                bool listening = true; // controls the while-loop logic

                // Enter the listening loop.
                while (listening == true)
                {
                    // Get a stream object for reading and writing

                    NetworkStream commandStream = CommandClient.GetStream();
                    String commandGreeting = "COMMAND CONNECTION: WAITING FOR COMMAND INPUT\r\n";

                    // Check the static ConnectedToAcs property to determine whether we have a stable ACS connection or not
                    if (ConnectedToAcs)
                    {
                        commandGreeting = commandGreeting + "( ACS connected ): ";
                    }
                    else
                    {
                        commandGreeting = commandGreeting + "( ACS disconnected ): ";
                    }

                    // Send back a response 
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(commandGreeting);
                    commandStream.Write(msg, 0, msg.Length);

                    // Prepare for looping
                    data = null;
                    int i;
                    bool cancelCommandReceived = false; // used to control the while-loop logic (and when to quit)

                    // loop to receive all of the data sent by the client
                    while ((cancelCommandReceived == false) && ((i = commandStream.Read(bytes, 0, bytes.Length)) != 0))
                    {
                        // Translate the data bytes in to an ASCII string
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                        if (data == "\u0003") // checking for a CTRL+C from the connected terminal
                        {
                            cancelCommandReceived = true; // set boolean logic to exit the while-loop
                            Log.Information("Received Cancel Command");
                        }
                        else if (data == "\b")
                        {
                            // Need to trim the last character in CommandInput, if it has anything in it
                            commandInput = commandInput.TrimLastCharacter();
                        }
                        else
                        {
                            // Process the data sent by the client

                            // If the data segment received is a CRLF, then take whatever parsed command so far and process it
                            if (data == "\r\n")
                            {
                                // check first if it is one of the CANCEL/QUIT commands
                                if (commandInput.ToUpper() == "CANCEL" || commandInput.ToUpper() == "QUIT" || commandInput.ToUpper() == "EXIT")
                                {
                                    Log.Information($"Thread #{thread.ManagedThreadId}: CANCEL/QUIT command received - terminating connection");
                                    cancelCommandReceived = true;
                                }
                                else // Begin the section that parses for one of the main commands
                                {
                                    // List of available commands that we can take:
                                    // HELP   - Shows all of the available commands
                                    // STATUS - returns output to the connected user indicating whether ACS is connected or not
                                    //          and is the only command that can be used when not connected to the ACS
                                    // LOAD   - in form of LOAD "FullyQualifiedUrlPath", loads an RPL and informs connected ACS
                                    // STOP   - unloads the RPL from the connected ACS (and presumably would terminate the lease?)
                                    // PLAY   - sets the OutputModeRrp to "true"
                                    // PAUSE  - sets the OutputModeRrp to "false"
                                    // TIME   - calls UpdateTimelineRrp with a calculated edit units based on a parameter in HH:MM:SS, MM:SS, or MM format
                                    // LIST   - outputs list of RPLs that have been LOADed, along with their index value
                                    // SELECT - choose an RPL to be the "ACTIVE" RPL that is passed the various commands that require PlayoutId 
                                    // UNLOAD - removes an RPL from the selection list.  Note there is no analogue to this on the ACS side.
                                    // RELOAD - if any RPL are available for a RELOAD, it will reload all of them
                                    // KILL   - Performs a STOP, and then terminates the lease

                                    var commandSplit = commandInput.Split(' ');
                                    string commandBase = commandSplit[0];
                                    string commandParameter = "";
                                    String commandOutput = ""; // Any output returned from the processed command

                                    if (commandSplit.Length >= 2) // at least one parameter was passed in with this command - we only accept the first parameter at the moment
                                    {
                                        commandParameter = commandSplit[1];
                                    }

                                    // Output the command details to the AcsListener console
                                    Log.Information($"Thread #{thread.ManagedThreadId}:  Command Received:  {commandBase.ToUpper()} {commandParameter}");

                                    // Confirm that we are connected to the ACS, and if so start processing input from Command connection
                                    if (ConnectedToAcs is true)
                                    {
                                        // Start processing based on which command was received
                                        switch (commandBase.ToUpper())
                                        {
                                            case "STATUS":
                                                commandOutput = DoCommandStatus();
                                                break;

                                            case "LOAD":
                                                if (commandParameter == "")
                                                {
                                                    commandOutput = "LOAD command requires a parameter in format of LOAD \"FullyQualifiedUrlPath\"";
                                                }
                                                else
                                                {
                                                    string UrlPath = commandParameter;
                                                    commandOutput = DoCommandLoad(UrlPath);
                                                }

                                                break;

                                            case "STOP":
                                                commandOutput = DoCommandStop();
                                                break;

                                            case "PLAY":
                                                commandOutput = DoCommandPlay();
                                                break;

                                            case "PAUSE":
                                                commandOutput = DoCommandPause();
                                                break;

                                            case "TIME":
                                                if (commandParameter == "")
                                                {
                                                    commandOutput =
                                                        "TIME command requires a parameter in format of TIME <parameter>, where parameter is either HH:MM:SS, MM:SS, or MM";
                                                }
                                                else
                                                {
                                                    // Some basic validation on our part here to make sure that the RPL has been loaded first
                                                    if (RplLoadInfo.IsPlayoutSelected is true)
                                                    {
                                                        RplPlayoutData playoutData = RplLoadInfo.GetPlayoutData();

                                                        if (playoutData.EditRate != "")
                                                        {
                                                            string timeOffsetInput = commandParameter;
                                                            commandOutput = DoCommandTime(timeOffsetInput, playoutData.EditRate);
                                                        }
                                                        else
                                                        {
                                                            commandOutput = "TIME command can only be used after a successful LOAD command has been issued";
                                                        }
                                                    }
                                                    else
                                                    {
                                                        commandOutput = "TIME command cannot be used until an RPL is chosen by SELECT";
                                                    }
                                                }

                                                break;

                                            case "LIST":
                                                commandOutput = DoCommandList();
                                                break;

                                            // SELECT command requires single parameter (PlayoutId)
                                            // and performs a DoCommandSelect(), checks for success, and then a DoCommandTime() with 0 timeline.
                                            case "SELECT":
                                                if (commandParameter == "")
                                                {
                                                    commandOutput = "SELECT command requires a parameter in format of SELECT <parameter>, where parameter is the PlayoutId";
                                                }
                                                else
                                                {
                                                    UInt32 playoutId;
                                                    try
                                                    {
                                                        playoutId = UInt32.Parse(commandParameter);

                                                        if (RplLoadInfo.IsPlayoutSelected is true)
                                                        {
                                                            if (RplLoadInfo.GetCurrentPlayout() != playoutId)
                                                            {
                                                                // If there is a current playout selected already, and the about-to-be-selected playout is different
                                                                // then before we do any selections of the new playout, we need to pause the current playout.
                                                                commandOutput = DoCommandPause() + "\r\n";
                                                            }
                                                        }

                                                        commandOutput = commandOutput + DoCommandSelect(playoutId);

                                                        // Some basic validation on our part here to make sure that the RPL has been loaded first
                                                        if (RplLoadInfo.IsPlayoutSelected is true)
                                                        {
                                                            RplPlayoutData playoutData = RplLoadInfo.GetPlayoutData();

                                                            if (playoutData.EditRate != "")
                                                            {
                                                                // If we reached this point then we presumably have a good RPL, so send 
                                                                // a UpdateTimeline request per SMPTE 430-10, which states that the RPL must be set first, 
                                                                // then an UpdateTimeline sent, before any OutputMode can be set. 
                                                                commandOutput = commandOutput + "\r\n" + DoCommandTime("00:00", playoutData.EditRate);
                                                            }
                                                            else
                                                            {
                                                                commandOutput = "Error: SELECT succeeded, but something is wrong with the Playout data.  EditRate is blank.";
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (commandOutput == String.Empty) // if by chance we reached this without setting a proper error message already
                                                            {
                                                                commandOutput = String.Format($"Error: SELECT was not successful for PlayoutID: {playoutId}");
                                                            }
                                                        }
                                                    }
                                                    catch (ArgumentException ex) // Most likely the PlayoutId supplied does not exist
                                                    {
                                                        commandOutput = ex.Message;
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        commandOutput = "Error: " + ex.Message;
                                                    }
                                                    finally
                                                    {
                                                    }
                                                }

                                                break;

                                            case "UNLOAD":
                                                // First, confirm that there is even a need for the unload command
                                                if (RplLoadInfo.LoadCount <= 0)
                                                {
                                                    commandOutput = "UNLOAD action skipped, there are no RPLs loaded at this time";
                                                    break;
                                                }

                                                // Next, confirm that the syntax of the command is correct and break early if not
                                                if (commandParameter == "")
                                                {
                                                    commandOutput = "UNLOAD command requires a parameter in format of UNLOAD <parameter>, where parameter is the PlayoutId";
                                                    break;
                                                }
                                                else
                                                {
                                                    // Start the processing for the UNLOAD command
                                                    UInt32 playoutId;
                                                    try
                                                    {
                                                        // Get the Playout ID from the passed parameter
                                                        playoutId = UInt32.Parse(commandParameter);

                                                        // Next check to see if a Playout has been selected
                                                        if (RplLoadInfo.IsPlayoutSelected is true)
                                                        {
                                                            if (RplLoadInfo.GetCurrentPlayout() == playoutId)
                                                            {
                                                                // If there is a current playout selected already, and the about-to-be-unloaded playout is the same
                                                                // then before we do anything else, we need to perform a STOP command first before we UNLOAD the RPL
                                                                commandOutput = DoCommandStop() + "\r\n";
                                                            }
                                                        }

                                                        // Now finally perform the UNLOAD action, remembering this doesn't actually do any unload on the ACS side
                                                        // as there is no such equivalent for the ACS at this time.  This is important for us though because of the 
                                                        // potential for the RELOAD action

                                                        commandOutput = commandOutput + DoCommandUnload(playoutId);

                                                        // commandOutput = "STUB for DoCommandUnload()";
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        commandOutput = "Error: " + ex.Message;
                                                    }
                                                    finally
                                                    {
                                                    }
                                                }

                                                break;

                                            case "RELOAD":
                                                commandOutput = DoCommandReload(true);
                                                break;

                                            case "KILL":
                                                commandOutput = DoCommandStop();
                                                commandOutput = commandOutput + "\r\n" + DoCommandKill();
                                                break;

                                            case "HELP":
                                                commandOutput = DoCommandHelp();
                                                break;
                                        }
                                    }
                                    else if ((ConnectedToAcs is false) && (commandBase.ToUpper() == "STATUS"))
                                    {
                                        // the only functional command allowed when not connected to the ACS is "STATUS" 
                                        commandOutput = DoCommandStatus();
                                    }
                                    else if ((ConnectedToAcs is false) && (commandBase.ToUpper() == "HELP"))
                                    {
                                        // the only other command allowed is "HELP"
                                        commandOutput = DoCommandHelp();
                                    }
                                    else
                                    {
                                        // If we don't meet any of the above criteriaNotify the user that the command is not allowed
                                        commandOutput = "The " + commandBase.ToUpper() + " command is not recognized, or is not allowed in this mode";
                                    }


                                    // If the command resulted in any kind of output, then add CRLF to it for proper formatting
                                    if (commandOutput != "")
                                    {
                                        commandOutput = commandOutput + "\r\n"; // Add a CRLF to the end of the output message so that it is nicely formatted for the other side
                                    }

                                    // Add a mode status output to the return string being sent to the command connection
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

                                commandInput = "";
                            }
                            else
                            {
                                // Concatenate the input to the command string and continue
                                commandInput = String.Concat(commandInput, data);
                            }
                        }
                    }

                    CommandClient.Close();
                    CommandClient.Dispose();
                    listening = false;
                }

            }
            catch (ArgumentNullException ex )  // most likely manually thrown by one of the Process methods after encountering an issue with ListenerStream
            {
                Log.Error($"[Thread #{thread.ManagedThreadId}]: An Argumenth CommandProcessor: {ex.Message}");
            }
            catch (SocketException e)
            {
                Log.Error($"SocketException: {e}");
            }
            catch (IOException ex)
            {
                Log.Error($"[Thread #{thread.ManagedThreadId}]: IOException has occurred in connection with CommandProcessor: {ex.Message}");
            }
            finally
            {
                // Stop listening for new clients

                if (CommandClient != null)
                {
                    CommandClient.Close();
                    CommandClient.Dispose();
                }

                Log.Information($"Thread #{thread.ManagedThreadId}: Connection Terminated");

            }


        }

        /// <summary>  Processes the necessary stuff to perform a RELOAD operation (whether manual or auto)</summary>
        /// <param name="manualReloadMode">Whether this is a RELOAD from CommandProcess (manual) or not (auto)</param>
        /// <returns>String representing the output from the child commands</returns>
        private static string DoCommandReload(Boolean manualReloadMode)
        {
            if (IsAutoReload == true && manualReloadMode == true)
            {
                return "Warning: Manual use of RELOAD command is not needed when AutoReload is configured";
            }

            string outputMessage = "RELOAD not possible as there are no RPLs to reload";

            if (RplReloadInfo?.LoadCount > 0)
            {
                outputMessage = "Processing RELOAD command: \r\n";
                foreach(string urlToReload in RplReloadInfo.GetRplUrlList())
                {
                    outputMessage = outputMessage + DoCommandLoad(urlToReload) + "\r\n";
                }

                // After successful RELOAD, we "ZERO" out the RplReloadInfo  
                // so that another RELOAD with the same data is not possible
                RplReloadInfo = new RplLoadInformation();
            }

            return outputMessage;
        }

        /// <summary>Processes the KILL command.  Sets the KillCommandReceived static variable to TRUE. </summary>
        /// <returns></returns>
        private static string DoCommandKill()
        {
            KillCommandReceived = true;

            return "KILL command issued to ListenerProcess";
        }

        /// <summary>  Performs the UNLOAD command from the CommandProcessor.   It calls RemoveRplData() method, which removes the supplied PlayoutId from the RplLoadInfo Dictionary.</summary>
        /// <param name="playoutId">The playout identifier to be removed from the RplLoadInfo Dictionary.</param>
        /// <returns></returns>
        private static string DoCommandUnload(uint playoutId)
        {
            string outputMessage = "";

            try
            {
                outputMessage = RplLoadInfo.RemoveRplData(playoutId);
            }
            catch (Exception ex)
            {
                outputMessage = "Error: " + ex.Message;
            }
            finally
            {
            }

            return outputMessage;
        }

        /// <summary>
        /// DoCommandSelect is the action method associated with the SELECT command in the CommandProcessor. 
        /// It calls the SetCurrentPlayout method of the static RplLoadInfo object, all else is handled internally
        /// from that object.  
        /// </summary>
        /// <param name="playoutId">A unique number between 10000000 and 99999999, generated for the RPL at time of RPL creation.</param>
        /// <returns></returns>
        private static string DoCommandSelect(UInt32 playoutId)
        {
            string outputMessage;

            outputMessage = RplLoadInfo.SetCurrentPlayout(playoutId);

            return outputMessage;
        }

        /// <summary>
        /// DoCommandList is the action method associated with the LIST command for the CommandProcessor.  
        /// It calls the GetRplLoadList() method of the static RplLoadInfo object, which returns a list of RPLs that are
        /// currently loaded in to memory in the AcsListener. 
        /// </summary>
        /// <returns></returns>
        private static string DoCommandList()
        {
            string outputMessage;

            if (ConnectedToAcs is true)
            {
                if (RplLoadInfo.LoadCount > 0)
                {
                    outputMessage = RplLoadInfo.GetRplLoadList();
                }
                else
                {
                    outputMessage = "LIST command cannot be performed until at least one RPL has been loaded";
                }
            }
            else
            {
                outputMessage = "ACS not connected.  Cannot perform LIST command";
            }

            return outputMessage;
        }

        /// <summary>
        /// DoCommandStop is the action method for the STOP command issued from the CommandProcessor. 
        /// The method pauses any caption playout (setting OutputMode to FALSE), and then clears any selected Playout static data. 
        /// </summary>
        /// <returns></returns>
        private static string DoCommandStop()
        {
            if (RplLoadInfo.IsPlayoutSelected is true)
            {
                ProcessSetOutputModeRrp(false);
                ClearRplSelectedPlayout();
                return "STOP action successfully completed.  ACS output has been paused. Current PlayoutId has been cleared from memory.";
            }
            else
            {
                return "STOP action cannot be executed, as there is no PlayoutId currently selected";
            }
        }

        /// <summary>
        /// ClearRplSelectedPlayout uses the "ClearCurrentPlayout" method of the static RplLoadInfo data object and sets
        /// the current PlayoutId to 0, and the IsPlayoutSelected property to false, all handled internally by the RplLoadInfo object. 
        /// </summary>
        private static void ClearRplSelectedPlayout()
        {
            RplLoadInfo.ClearCurrentPlayout();  // Sets the current PlayoutId to 0 and the IsPlayoutSelected will return false
        }

        /// <summary>  Executes an AcspUpdateTimelineRequest to the ACS</summary>
        /// <param name="timeOffsetInput">The time offset input.</param>
        /// <param name="editRateInput">The edit rate input, as provided from data from the RPL file</param>
        /// <returns>String containing any relevant output message back to the CommandProcess connection.</returns>
        private static string DoCommandTime(string timeOffsetInput, string editRateInput)
        {
            string outputMessage = "";

            // Check to make sure we have a connected ACS
            if (ConnectedToAcs is false)
            {
                outputMessage = "TIME command cannot be issued while ACS is disconnected";
                return outputMessage;
            }

            // First, we need to construct an RplReelDuration from the timeOffsetInput that was passed to us.  
            // In this version of DoCommandTime, we are (for now) assuming an editRateInput of "25 1" passed in
            // but eventually we will get this information from the loaded RPL file

            RplReelDuration reelDuration;
            UInt64 updatedEditUnits;

            try
            {
                reelDuration = new RplReelDuration(timeOffsetInput, editRateInput);
                updatedEditUnits = reelDuration.EditUnits;
            }
            catch (FormatException ex)
            {
                outputMessage = "Error: " + ex.Message;
                return outputMessage;
            }
            finally
            {
            }

            // Some basic validation that we have a successfully loaded RPL
            if (RplLoadInfo.IsPlayoutSelected is true)
            {
                RplPlayoutData playoutData = RplLoadInfo.GetPlayoutData();

                // Just a reminder that once the ACS has an updated timeline, its internal clock starts processing immediately
                ProcessUpdateTimelineRrp(playoutData.PlayoutId, updatedEditUnits);
                outputMessage = "TIME command issued with:\r\n" + " Time: " + timeOffsetInput + "\r\n EditRate: " + editRateInput + "\r\n EditUnits: " + updatedEditUnits;
            }
            else
            {
                outputMessage = "Error:  Attempted a DoCommandTime call without a properly loaded RPL file first";
            }

            return outputMessage;
        }

        /// <summary>
        /// Sends an AcspSetOutputModeRequest FALSE to the ACS.
        /// As a reminder, setting output to FALSE only ensures that the captions are turned off at the CaptiView device.
        /// It doesn't actually "pause" any internal timer.
        /// </summary>
        /// <returns></returns>
        private static string DoCommandPause()
        {
            String outputMessage = "";
            // Need some additional logic here to handle whether an RPL has already been loaded or not

            // check to make sure that the NetworkStream is already set by the initial connection
            if ((ConnectedToAcs is true) && (ListenerStream != null))
            {
                // Check to make sure that we have a PlayoutId chosen by the SELECT command
                // as per SMPTE 430-10, we must have already performed a SetRplLocation and an UpdateTimeline before we can set
                // the OutputMode to TRUE. 

                if (RplLoadInfo.IsPlayoutSelected is true)
                {
                    ProcessSetOutputModeRrp(false);
                    outputMessage = "ACS instructed to set OutputMode to FALSE";
                    // For now we assume this succeeds, but eventually we need additional logic to detect whether this succeeds or not
                }
                else
                {
                    outputMessage = "PAUSE is unsuccessful.  The SELECT command must be used first";
                }

            }
            else
            {
                outputMessage = "ACS is not currently connected";
            }

            return outputMessage;
        }

        /// <summary>
        /// Performs the PLAY command from the CommandProcessor.   Performs some basic validation to make sure that we have an RPL selected for playout, and then calls ProcessSetOutputModeRrp(true)
        /// </summary>
        /// <returns></returns>
        private static string DoCommandPlay()
        {
            String outputMessage = "";
            // Need some additional logic here to handle whether an RPL has already been loaded or not

            // check to make sure that the NetworkStream is already set by the initial connection
            if ((ConnectedToAcs is true) && (ListenerStream != null))
            {
                // Check to make sure that we have a PlayoutId chosen by the SELECT command
                // as per SMPTE 430-10, we must have already performed a SetRplLocation and an UpdateTimeline before we can set
                // the OutputMode to TRUE. 

                if (RplLoadInfo.IsPlayoutSelected is true)
                {
                    ProcessSetOutputModeRrp(true);
                    outputMessage = "ACS instructed to set OutputMode to TRUE";
                    // For now we assume this succeeds, but eventually we need additional logic to detect whether this succeeds or not
                }
                else
                {
                    outputMessage = "PLAY is unsuccessful.  The SELECT command must be used first";
                }
            }
            else
            {
                outputMessage = "ACS is not currently connected";
            }

            return outputMessage;
        }

        /// <summary>  Performs the STATUS command from the CommandProcessor.</summary>
        /// <returns>Whether the ACS is connected, and if so, what the current playout is set to.</returns>
        private static string DoCommandStatus()
        {
            string result;

            if (ConnectedToAcs is true)
            {
                result = "ACS: connected; Current Playout: ";

                if (RplLoadInfo.GetCurrentPlayout() > 0)
                {
                    RplPlayoutData playoutData = RplLoadInfo.GetPlayoutData();
                    result = result + playoutData.PlayoutId + " ( " + playoutData.ResourceUrl + " )";
                }
                else
                {
                    result = result + "None";
                }
            }
            else
            {
                result = "ACS: disconnected; Current Playout: None";
            }

            return result;
        }

        /// <summary>
        /// Performs the "LOAD" command, checking first for a valid URL using the DefaultRplUrlPath (which comes from loading the app.config at startup) and if that doesn't work, then just using the path supplied to the method.
        /// </summary>
        /// <param name="rplUrlPath">  The path to the ResourcePresentationList (RPL) file</param>
        /// <returns>A string containing any of the results of the operation.</returns>
        private static string DoCommandLoad(string rplUrlPath)
        {

            // Need some kind of URL / file validation present here prior to the XmlData load

            string outputMessage = "";

            if (DefaultRplUrlPath != String.Empty)
            {
                Log.Debug($"Detected default RPL path. Testing whether a modified url path is valid: {DefaultRplUrlPath + rplUrlPath}");
                if (IsUrlValid(DefaultRplUrlPath + rplUrlPath))
                {
                    rplUrlPath = DefaultRplUrlPath + rplUrlPath;
                    Log.Debug($"Confirmed that modified path is valid: {rplUrlPath}");
                }
                else
                {
                    Log.Debug($"Modified path is not valid: {DefaultRplUrlPath + rplUrlPath}");
                }
            }

            ResourcePresentationList xmlData;

            try
            {
                xmlData = LoadRplFromUrl(rplUrlPath);
            }
            catch (ArgumentException ex)
            {
                Log.Debug($"ArgumentException received while trying to load RPL file: {ex.Message}");
                outputMessage = "Error performing LOAD command: " + ex.Message;
                return outputMessage;
            }
            catch (WebException ex)
            {
                Log.Debug($"WebException received while trying to load RPL file: {ex.Message}");
                outputMessage = "Error performing LOAD command: " + ex.Message;
                return outputMessage;
            }
            catch (InvalidOperationException ex)
            {
                Log.Debug($"InvalidOperationException received while trying to load RPL file: {ex.Message}");
                outputMessage = "Error performing LOAD command: " + ex.Message;
                return outputMessage;
            }
            finally
            {

            }

            if (xmlData.PlayoutId == 0) // basically checking to make sure we didn't get an invalid load
            {
                return "Error:  RPL file was not loaded correctly.   PlayoutId is not a valid value";
            }

            // Here we need to figure out some kind of validation for the Resource file: 
            // xmlData.ReelResources.ReelResource.ResourceFile.ResourceText
            // We need to figure out first, does a file exist (in much the same way we validate the RPL file location)
            // then we need to figure out if it is a VALID file of type/class SubtitleReel
            // and finally if that loads correctly, then we want to validate that the SubtitleReel.Id field starts with "urn:uuid"

            string resourceFileLocation = xmlData.ReelResources.ReelResource.ResourceFile.ResourceText;
            if (resourceFileLocation != null)
            {
                try
                {
                    if (ValidateSubtitleFromUrlString(resourceFileLocation) == false)
                    {
                        return "Error: RPL is valid, but the resource file does not exist at the specified location";
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Log.Debug($"Error while attempting to validate the Resource/Caption file ({resourceFileLocation}): {ex.Message}");

                    return String.Format($"Validation error in resource file: {ex.Message}");
                }
            }

            RplPlayoutData playoutData = new RplPlayoutData();

            playoutData.PlayoutId = xmlData.PlayoutId;
            playoutData.TimelineOffset = xmlData.ReelResources.TimelineOffset;
            playoutData.EditRate = xmlData.ReelResources.EditRate;
            playoutData.ResourceUrl = rplUrlPath;

            RplReelDuration startingTimeline = new RplReelDuration("00:00:00", playoutData.EditRate);
            UInt64 timelineEditUnits = startingTimeline.EditUnits;  // Should be 0 in the current iteration

            // Need to set the RPL Location
            ProcessSetRplLocationRrp(playoutData.ResourceUrl, playoutData.PlayoutId);

            while (ProcessGetStatusRrp() != "RrpSuccessful")
            {
                Thread.Sleep(500);
            }

            // Special note here - this is just a LOAD action, not a SELECT, so no timeline update at this point

            Log.Information($"LOAD command issued successfully.");
            if (VerboseOutput is true)
            {
                Log.Verbose($"PlayoutId:  {playoutData.PlayoutId}");
                Log.Verbose($"timelineStart:  {playoutData.TimelineOffset}");
                Log.Verbose($"editRate:  {playoutData.EditRate}");
                Log.Verbose($"timelineEditUnits:  {timelineEditUnits}");
                Log.Verbose($"resourceUrl:   {playoutData.ResourceUrl}");
            }

            try
            {
                RplLoadInfo.InsertRplData(playoutData);
                outputMessage = "LOAD command issued successfully for: " + playoutData.ResourceUrl + "\r\n" + " PlayoutId #: " + playoutData.PlayoutId;
            }
            catch (ArgumentException ex)
            {
                outputMessage = ex.Message;
            }

            return outputMessage;

        }

        /// <summary>
        /// Attempts to load a subtitle at the location specified in the resource file location.
        /// </summary>
        /// <param name="resourceFileLocation">The resource file location.</param>
        /// <returns></returns>
        private static Boolean ValidateSubtitleFromUrlString(string resourceFileLocation)
        {
            Log.Debug($"Verifying that the Resource/Caption file exists at: {resourceFileLocation}");

            // Check to see if the resource file location starts with an "http:"
            // If not, then let's assume that the Resource location is specified as a local website that the ACS can handle
            // and as such, we need to prepend the "http://<ipaddress>/" to the resourceFileLocation
            if (resourceFileLocation.StartsWith("http:", StringComparison.OrdinalIgnoreCase) == false)
            {
                if (resourceFileLocation.StartsWith("/") == false)
                {
                    resourceFileLocation = "http://" + SavedLocalAddress.ToString() + "/" + resourceFileLocation;  // Prepend "http://<ipaddress>/" 
                }
                else
                {
                    resourceFileLocation = "http://" + SavedLocalAddress.ToString() + resourceFileLocation;  //  Prepend "http://<ipaddress>" (no trailing "/")
                }
            }

            if (IsUrlValid(resourceFileLocation) == false)
            {
                Log.Debug($"The resource file does not exist at specified location: {resourceFileLocation}");
                return false;  // File does not exist at the specified location
            }

            Log.Debug($"Attempting to load Resource/Caption file: {resourceFileLocation}");

            // Define the XmlSerializer casting to be of type SubtitleReel
            XmlSerializer deserializer = new XmlSerializer(typeof(SubtitleReel));
            SubtitleReel xmlData;

            // Open a new WebClient to get the data from the target URL
            
            using (WebClient client = new WebClient())
            {
                // Note: for issue #59, had to change this to UTF8 encoding to successfully strip the BOM from the byte stream
                string data = Encoding.UTF8.GetString(client.DownloadData(resourceFileLocation));

                using (Stream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
                {
                    // Deserialize the input file
                    object deserializedData = deserializer.Deserialize(memoryStream);

                    // Cast the deserialized data to the SubtitleReel type
                    xmlData = (SubtitleReel)deserializedData;

                    memoryStream.Close();
                }
            }

            Log.Debug("Validation check has successfully loaded SubtitleReel data");

            if (xmlData.Id.StartsWith("urn:uuid:", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// DoCommandHelp() returns a list of commands and their usage to the CommandProcess 
        /// </summary>
        /// <returns>outputMessage as type String</returns>
        private static string DoCommandHelp ()
        {
            string outputMessage = "List of available commands that we can take:\r\n\r\n" + 
                                   "HELP   - Shows all of the available commands\r\n" + 
                                   "STATUS - returns output to the connected user indicating whether ACS is connected or not\r\n" +
                                   "         and is the only command that can be used when not connected to the ACS\r\n" + 
                                   "LOAD   - in form of LOAD 'FullyQualifiedUrlPath', loads an RPL and informs connected ACS\r\n" + 
                                   "STOP   - unloads the RPL from the connected ACS (and presumably would terminate the lease?)\r\n" + 
                                   "PLAY   - sets the OutputModeRrp to 'true'\r\n" + 
                                   "PAUSE  - sets the OutputModeRrp to 'false'\r\n" + 
                                   "TIME   - calls UpdateTimelineRrp with a calculated edit units based on a parameter in HH:MM:SS, MM:SS, or MM format\r\n" + 
                                   "LIST   - outputs list of RPLs that have been LOADed, along with their index value\r\n" + 
                                   "SELECT - choose an RPL to be the 'ACTIVE' RPL that is passed the various commands that require PlayoutId\r\n" +
                                   "UNLOAD - removes an RPL from the selection list.  Note there is no analogue to this on the ACS side\r\n" +
                                   "RELOAD - if any RPL are available for a RELOAD, it will reload all of them\r\n" +
                                   "KILL   - Performs a STOP, and then terminates the lease\r\n";

            return outputMessage;
        }

        /// <summary>
        /// ListenerProcess is responsible handling most of the work of the AcsListener.  When a TCP connection is made, 
        /// the main thread spawns off a child thread (via the ThreadPool) to ListenerProcess.  
        /// </summary>
        /// <param name="listenerProcessParamsObject">A generic object containing data of type ListenerProcessParams</param>
        private static void ListenerProcess(object listenerProcessParamsObject)
        {
            const int timeoutValue = 2000;
            uint leaseSeconds = 60;

            var myParams = (ListenerProcessParams)listenerProcessParamsObject;
            TcpClient tempTcpClient = myParams.Client;

            NetworkStream tempStream = tempTcpClient.GetStream();
            tempStream.WriteTimeout = timeoutValue; // sets the timeout to X milliseconds
            tempStream.ReadTimeout = timeoutValue; // sets the  timeout to X milliseconds

            Thread thread = Thread.CurrentThread;

            try
            {
                IPEndPoint remoteEnd = (IPEndPoint) tempTcpClient.Client.RemoteEndPoint;
                IPAddress remoteAddress = remoteEnd.Address;

                Log.Information($"[Thread #: {thread.ManagedThreadId}] Connection Established! RemoteIP: {remoteAddress}");

                // Presumably the ACS has established, but we need to make sure that it IS an ACS device that is connecting on 4170

                Thread.Sleep(2000); // Brief pause to give any other cleanup activity the chance to finish

                CanWriteToStream.Set(); // Finally, set the signal to indicate that the NetworkStream can be written to by other threads

                // Announce to ACS to prove that this IS an ACS device that is talking to us, throw AcspAnnounceException if not
                ProcessAnnounceRrp(tempStream);

                // If we made it this far, then presumably we ARE connected to an ACS device

                if ((ListenerClient != null) && (ListenerClient.Connected))
                {
                    // If the static ListenerClient is already connected in another thread then we need to do the following:
                    //    1) Check the signal to make sure that no other thread is trying to write to the stream right now
                    //    2) Lock the signal so that no other thread tries to write
                    //    3) Close the existing stream
                    //    4) Close the existing TCP connection


                    if (ListenerStream != null)
                    {
                        try
                        {
                            CanWriteToStream.WaitOne(); // Wait to make sure no one else is trying to write to the NetworkStream
                            CanWriteToStream.Reset(); // Disable writing to the ACS NetworkStream for any other thread

                            // Close and dispose of the ACS NetworkStream
                            ListenerStream?.Close();
                            ListenerStream?.Dispose();
                        }
                        finally
                        {
                            ListenerStream = null; // Null out the static ACS NetworkStream
                        }
                    }

                    ListenerClient?.Close(); // Close the existing TCP connection
                    ListenerClient?.Dispose();
                }

                // Since we have proven it is an ACS talking to us, this will become the new ACS connection
                ListenerClient = tempTcpClient;
                ListenerStream = tempStream;
                ConnectedToAcs = true; // set the static variable to true to let CommandProcess know if connection has occurred

                KillCommandReceived = false; // Initialize the control logic, only set to true by DoCommandKill()

                // set lease with ACS, and finally get status from ACS.  All other actions handled by command process
                ProcessGetNewLeaseRrp(leaseSeconds);
                ProcessGetStatusRrp();

                // As this is a new instance of ACS connection, start up a new instance of the RplLoadInfo static data
                if (RplLoadInfo?.LoadCount > 0)
                {
                    RplLoadInfo = new RplLoadInformation();
                }

                // Check to see if IsAutoReload is set to TRUE - if so, perform a RELOAD operation instead of setting a new RplLoadInfo

                if (IsAutoReload == true)
                {
                    // Now check to see if there is something to RELOAD

                    int numberOfRplsReloaded = GetSavedRplInfo();
                    if (numberOfRplsReloaded > 0)
                    {
                        Log.Information($"[Thread #: {thread.ManagedThreadId}] AutoReloaded {numberOfRplsReloaded} RPL in the ACS");
                    }
                }

                SetLeaseTimer((leaseSeconds * 1000) / 2); // Convert to milliseconds and then halve the number

                // Check for TCP connection 

                while ((KillCommandReceived is false) && (ListenerClient.Connected is true))
                {
                    Thread.Sleep(1000);
                }

                // Determine reason for reaching this point
                if (KillCommandReceived is true)
                {
                    Log.Information($"[Thread #{thread.ManagedThreadId}]  KILL command issued from CommandProcess - terminating connection with ACS");
                    ProcessTerminateLease();
                }
                else
                {
                    Log.Information($"[Thread #{thread.ManagedThreadId}]  Lost TCP connection with ACS!  Cleaning everything else up and waiting for new connection/lease.");
                }

            }
            catch (IOException ex)
            {
                Log.Error($"[Thread #{thread.ManagedThreadId}] Error: Socket timeout of {timeoutValue} reached");
                Log.Error($"[Thread #{thread.ManagedThreadId}] Exception message: {ex.Message}");
                DoAcsConnectionCleanup();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Log.Error($"[Thread #{thread.ManagedThreadId}] Error: An ArgumentException has occurred: {ex.Message}");
                DoAcsConnectionCleanup();
            }
            catch (ObjectDisposedException ex) // likely to occur when the NetworkStream object "ListenerStream" has been cleaned up but another thread is trying to access
            {
                Log.Error($"[Thread #{thread.ManagedThreadId}] Error: An ObjectDisposedException has occurred: {ex.Message}");
                DoAcsConnectionCleanup();
            }
            catch (NullReferenceException ex) // likely to occur when TcpClient "ListenerClient" has been disposed and nulled out, but another thread is trying to access
            {
                Log.Error($"[Thread #{thread.ManagedThreadId}] Error: A NullReferenceException has occurred: {ex.Message}");
                DoAcsConnectionCleanup();
            }
            catch (ArgumentNullException ex) // most likely manually thrown when discovering that the ListenerStream is unexpectedly null
            {
                Log.Error($"[Thread #{thread.ManagedThreadId}] Error: An ArgumentNullException has occurred: {ex.Message}");
                DoAcsConnectionCleanup();
            }
            catch (AcspAnnounceException ex)
            { 
                // Log the error, but we do not want to abort any pre-existing ACS connection
                Log.Error($"[Thread #{thread.ManagedThreadId}] Error: {ex.Message}");

                CanWriteToStream.Set();  // Open up the signal so that processes can write to the ACS again
                tempTcpClient?.Dispose();  // Since we're not assigning anything from the tempTcpClient, get rid of it
                tempStream?.Dispose();
            }
            finally
            {
                Log.Information($"[Thread #{thread.ManagedThreadId}: End of ListenerProcess");
            }

        }

        /// <summary>
        /// Checks whether any Rpl information has been saved in the static RplReloadInfo object, and if so, returns reference to that object.  If not, then returns a new RplLoadInformation() object.
        /// </summary>
        /// <returns> Returns the number of saved Rpl that we attempted to RELOAD</returns>
        private static int GetSavedRplInfo()
        {
            int numberOfRplsToReload = 0;
            string outputMessage;
            Thread thread = Thread.CurrentThread;

            if (RplReloadInfo?.LoadCount > 0)
            {
                numberOfRplsToReload = RplReloadInfo.LoadCount;
                outputMessage = DoCommandReload(false);
                Log.Information($"[Thread #: {thread.ManagedThreadId}] {outputMessage}");
            }

            return numberOfRplsToReload;
        }

        /// <summary>
        /// Stops / closes the AcspLeaseTimer object "LeaseTimer", the TcpClient object "ListenerClient", and the NetworkStream object "ListenerStream",
        /// and then makes sure that any RplLoadInformation object data (if present) is saved to a temporary storage location for later usage by the RELOAD command.
        /// </summary>
        private static void DoAcsConnectionCleanup()
        {
            Thread thread = Thread.CurrentThread;

            if (LeaseTimer != null)
            {
                try
                {
                    Log.Information($"[Thread #{thread.ManagedThreadId}] Attempting to stop the Lease Timer ...");
                    LeaseTimer.Stop();
                    LeaseTimer.Dispose();
                }
                finally
                {
                    LeaseTimer = null;
                    Log.Information($"[Thread #{thread.ManagedThreadId}] Lease Timer has been stopped and closed");
                }
            }

            if (ListenerStream != null)
            {
                try
                {
                    Log.Information($"[Thread #{thread.ManagedThreadId}] Attempting to close the NetworkStream and close the overall ACS TCP connection");
                    CanWriteToStream.WaitOne(1000);
                    CanWriteToStream.Reset();    // Block so that nothing else attempts to write to the stream
                    ListenerStream?.Close();
                }
                finally
                {
                    ListenerStream = null;
                    Log.Information($"[Thread #{thread.ManagedThreadId}] Successfully closed the NetworkStream and closed the overall ACS TCP connection");
                }
            }

            if (ListenerClient != null)
            {
                if (ListenerClient.Connected)
                {
                    try
                    {
                        Log.Information($"[Thread #{thread.ManagedThreadId}]: Attempting to close the network connection");
                        ListenerClient.Close();
                    }
                    finally
                    {
                        ListenerClient = null;
                        Log.Information($"[Thread #{thread.ManagedThreadId}] Successfully closed the network connection");
                    }
                }
                else
                {
                    ListenerClient = null;
                }
            }

            // Prepare to clear out the RplLoadInfo static data, but before we do, if there are any RPLs loaded, we will save that off for possible RELOAD
            if (RplLoadInfo != null)
            {
                if (RplLoadInfo.LoadCount > 0)
                {
                    RplReloadInfo = RplLoadInfo;
                    Log.Information($"[Thread #: {thread.ManagedThreadId}] Storing [{RplLoadInfo.LoadCount}] existing RPL information for RELOAD command use.");
                }
            }
            RplLoadInfo = new RplLoadInformation();   // effectively clear out the static RPL information
            Log.Debug($"[Thread #: {thread.ManagedThreadId}] Cleared out old RPL information");

            ConnectedToAcs = false;   // Set the flag to indicate that connection to/from the ACS has been terminated
        }

        /// <summary>Loads the ResourcePresentationList (RPL) from the supplied URL in to memory and returns a deserialized reference to that object</summary>
        /// <param name="rplUrlPath">  The path to the ResourcePresentationList</param>
        /// <returns>The deserialized</returns>
        private static ResourcePresentationList LoadRplFromUrl(string rplUrlPath)
        {
            Log.Debug($"Attempting to load RPL file: {rplUrlPath}");

            // Define the XmlSerializer casting to be of type ResourcePresentationList
            XmlSerializer deserializer = new XmlSerializer(typeof(ResourcePresentationList));
            ResourcePresentationList xmlData;

            // Open a new WebClient to get the data from the target URL
            
            using (WebClient client = new WebClient())
            {
                string data = Encoding.Default.GetString(client.DownloadData(rplUrlPath));

                using (Stream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
                {
                    // Deserialize the input file
                    object deserializedData = deserializer.Deserialize(memoryStream);

                    // Cast the deserialized data to the SubtitleReel type
                    xmlData = (ResourcePresentationList)deserializedData;

                    memoryStream.Close();
                }
            }

            // Send the deserialized data pointer back to the calling routine
            Log.Debug("Successfully loaded RPL data");
            return xmlData;
        }

        /// <summary>  This method will check a url to see that it does not return server or protocol errors</summary>
        /// <param name="url">  The path to check</param>
        /// <returns>
        ///   <c>true</c> if [is URL valid], otherwise <c>false</c>.</returns>
        private static bool IsUrlValid(string url)
        {
            try
            {
                HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;
                request.Timeout =
                    5000; // set the timeout to 5 seconds to keep the user from getting locked down by slow loading webpage
                request.Method = "HEAD"; // Get only the header information -- no need for full content download

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    int statusCode = (int) response.StatusCode;
                    if (statusCode >= 100 && statusCode < 400) // Good requests
                    {
                        return true;
                    }
                    else if (statusCode >= 500 && statusCode <= 510) // Server errors
                    {
                        Log.Debug($"The remote server has thrown an internal error.  Url is not valid: {url}");
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError) // 400 errors
                {
                    return false;
                }
                else
                {
                    Log.Warning($"Unhandled status {ex.Status} returned for url: {url}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Could not test url: {url}, Exception Message: {ex.Message}");
            }

            return false;
        }

        /// <summary>Sets Custom AcspLeaseTimer object
        /// to send GetStatus message to ACS on a regular frequency. </summary>
        /// <param name="leaseTimerMsec">Number of milliseconds between each Elapsed event</param>
        private static void SetLeaseTimer(uint leaseTimerMsec)
        {
            Thread thread = Thread.CurrentThread;
            LeaseTimer = new AcspLeaseTimer(ListenerStream, leaseTimerMsec);

            LeaseTimer.Elapsed += ProcessLeaseTimer;
            LeaseTimer.Start();

            Log.Information($"[Thread #{thread.ManagedThreadId}]: Setting a recurring GetStatusRequest callback every {leaseTimerMsec} msec");
        }

        /// <summary>
        /// Processes the Lease Timer event occuring (by default) every 30 seconds, sending a "heartbeat" message out to the ACS device to keep the TCP connection alive.   The ACS lease, defaulting to 60 seconds, keeps the TCP connection alive as long as some kind of message comes through before the lease expires.  If the lease expires, then the ACS will terminate any caption playback and attempt to re-establish connection to the playback system (AcsListener in this case), so obviously we want to keep the lease renewing as quickly as possible.  The SMPTE 430-10 specifications, per section 7.2.2 (Get New Lease) recommend sending the Get Status Request at a frequency of &lt;lease duration&gt; / 2 .
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        private static void ProcessLeaseTimer(object sender, ElapsedEventArgs e)
        {
            NetworkStream leaseStream = ((AcspLeaseTimer)sender).Stream;   // leaseStream isn't used in this current version but not ready to obsolete it quite yet

            ProcessGetStatusRrp();
        }

        /// <summary>Processes the Announce RRP (Request and Response Pair)</summary>
        /// <param name="inputStream">NetworkStream object to use for communication to/from the ACS device</param>
        /// <exception cref="AcspAnnounceException">Thrown if the message pairing logic fails to succeed and caught by ListenerProcess</exception>
        private static void ProcessAnnounceRrp(NetworkStream inputStream)
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValueMs = 5000;  // Timeout of 5000ms

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field

            if (VerboseOutput is true)
            {
                Log.Verbose($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            // Send the "Announce request" to the ACS system, and keep trying until a successful response is received
            bool announcePairSuccessful = false;

            // Check to see if there is any unexpected data in the stream, and if so, purge it prior to sending the request
            if (inputStream?.DataAvailable is true)
            {
                int clearedBytes = inputStream.ClearStreamForNextMessage();
                Log.Debug($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: {clearedBytes} bytes of additional data cleared from the stream");
            }

            // Prepare a new AnnounceRequest data packet
            AcspAnnounceRequest announceRequest = new AcspAnnounceRequest();
            CurrentRequestId = announceRequest.RequestId;

            // Send the AnnounceRequest data packet to the ACS
            inputStream.Write(announceRequest.PackArray, 0, announceRequest.PackArray.Length);
            Log.Information($"[Thread #{thread.ManagedThreadId}, {DateTime.Now}]Announce Request sent to ACS.  RequestID #: {CurrentRequestId}");

            // Wait for AnnounceResponse from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Expecting an AnnounceResponse message
            while (announcePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValueMs))
            {
                if (inputStream.DataAvailable == true)
                {
                    var numberOfBytes = inputStream.Read(header, 0, header.Length);
                    Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}] {numberOfBytes}-byte header successfully read from network stream");

                    AcspResponseHeader announceResponseHeader = new AcspResponseHeader(header);
                    if (announceResponseHeader.Key.IsBadRequest)
                    {
                        Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: AnnounceResponse [Bad Message] received from ACS!");
                        // Need some control logic here to figure out how to handle a Bad Request message
                    }

                    if (announceResponseHeader.Key.IsGoodRequest)
                    {
                        Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Good AnnounceResponse received from ACS!");
                        int length = announceResponseHeader.PackLength.Length;

                        Byte[] bytes = new Byte[length];
                        numberOfBytes = inputStream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                        Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: {numberOfBytes}-byte AnnounceResponse successfully read from network stream");

                        AcspAnnounceResponse announceResponse = new AcspAnnounceResponse(bytes);
                        Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: RequestId is: {announceResponse.RequestId}");
                        Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: DeviceDescription is: {announceResponse.DeviceDescription}");

                        // Check for RrpSuccesful code and matching RequestId values
                        if ((announceResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful) && (CurrentRequestId == announceResponse.RequestId))
                        {
                            announcePairSuccessful = true;
                            Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Response Received: RrpSuccessful");
                        }
                        else
                        {
                            Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Response Received: {announceResponse.StatusResponseKeyString}");
                        }
                    }
                } // end if (inputStream.DataAvailable == true) 
                else
                {
                    Thread.Sleep(100);  // Since there was no data yet, wait 100ms and try again
                } 
            }  // end while(stopwatch.ElapsedMilliseconds < timeoutValue)

            CanWriteToStream.Set();  // Open up the signal so that other processes can write to the ACS again

            if (announcePairSuccessful == false) // that means we didn't get a successful message pairing before the time limit ran out
            {
                throw new AcspAnnounceException($"Some kind of non-ACS device attempted to connect on the AcsPort");
            }
        }

        /// <summary>  Sends GetNewLease message to the ACS.</summary>
        /// <param name="leaseSeconds">The lease seconds.</param>
        private static void ProcessGetNewLeaseRrp(uint leaseSeconds)
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field


            if (VerboseOutput is true)
            {
                Log.Verbose($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            // Send the "Get New Lease request" to the ACS system, and keep trying until a successful response is received
            bool messagePairSuccessful = false;
            while (messagePairSuccessful == false)
            {
                // Check to see if there is any unexpected data in the stream, and if so, purge it prior to sending the request
                if (ListenerStream?.DataAvailable is true)
                {
                    int clearedBytes = ListenerStream.ClearStreamForNextMessage();
                    Log.Debug($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: {clearedBytes} bytes of additional data cleared from the stream");
                }

                // Prepare the GetNewLeaseRequest data packet
                AcspGetNewLeaseRequest leaseRequest = new AcspGetNewLeaseRequest(leaseSeconds);
                CurrentRequestId = leaseRequest.RequestId;

                // Send the GetNewLeaseRequest data packet to the ACS

                if (ListenerStream != null)
                {
                    ListenerStream.Write(leaseRequest.PackArray, 0, leaseRequest.PackArray.Length);
                    Log.Information(
                        $"[Thread #{thread.ManagedThreadId}, {DateTime.Now}]GetNewLease Request for {leaseSeconds} seconds sent to ACS.  RequestID #: {CurrentRequestId}");

                    // Wait for AnnounceResponse from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    // Expecting a GetNewLeaseResponse message
                    while (messagePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValue))
                    {
                        if (ListenerStream?.DataAvailable == true)
                        {
                            numberOfBytes = ListenerStream.Read(header, 0, header.Length); // Read 20-byte header in from the NetworkStream
                            Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}] {numberOfBytes}-byte header successfully read from network stream");

                            AcspResponseHeader announceResponseHeader = new AcspResponseHeader(header);
                            if (announceResponseHeader.Key.IsBadRequest)
                            {
                                Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: GetNewLeaseResponse [Bad Message] received from ACS!");
                                // Need some control logic here to figure out how to handle a Bad Request message
                            }

                            if (announceResponseHeader.Key.IsGoodRequest)
                            {
                                Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Good GetNewLeaseResponse received from ACS!");

                                if (announceResponseHeader.Key.NodeNames == Byte13NodeNames.GetNewLeaseResponse)
                                {
                                    Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Expected GetNewLeaseResponse has been received");

                                    int length = announceResponseHeader.PackLength.Length;

                                    Byte[] bytes = new Byte[length];
                                    numberOfBytes = ListenerStream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                                    Log.Information(
                                        $"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: {numberOfBytes}-byte GetNewLeaseResponse successfully read from network stream");

                                    AcspGetNewLeaseResponse leaseResponse = new AcspGetNewLeaseResponse(bytes);
                                    Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: RequestId is: {leaseResponse.RequestId}");

                                    if (leaseResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful)
                                    {
                                        messagePairSuccessful = true;
                                        Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Response Received: RrpSuccessful");
                                    }
                                    else
                                    {
                                        Log.Information(
                                            $"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Response Received: {leaseResponse.StatusResponseKeyString}");
                                    }
                                }
                            }
                        } // if (ListenerStream.DataAvailable == true)
                    } // end while(stopwatch.ElapsedMilliseconds < timeoutValue)
                }  // if (ListenerStream != null)
                else  // if we reach this point then we need to force a NullReferenceException to be caught further up the stack
                {
                    Log.Error($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Error encountered with the ACS network stream");
                    throw new ArgumentNullException(nameof(ListenerStream), $"The ListenerStream object is null when it should be set");
                }   
            }  // end while(messagePairSuccessful == false)

            CanWriteToStream.Set();  // Signal that it is okay to write to the NetworkStream
        } // end ProcessGetNewLeaseRrp()

        /// <summary>
        /// Sends the GetStatus message to the ACS device.   This message type is used immediately following a LoadRpl,
        /// and is also the message type used for the periodic "heartbeat" message that is sent every 30 seconds by default.
        /// </summary>
        /// <returns></returns>
        private static string ProcessGetStatusRrp()
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 3000;
            int numberOfBytes;
            string outputMessage = "";

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field

            if (VerboseOutput is true)
            {
                Log.Verbose($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            // Send the "GetStatusRequest " to the ACS system, and keep trying until a successful response is received
            bool messagePairSuccessful = false;
            while (messagePairSuccessful == false)
            {
                // Check to see if there is any unexpected data in the stream, and if so, purge it prior to sending the request
                if (ListenerStream?.DataAvailable is true)
                {
                    int clearedBytes = ListenerStream.ClearStreamForNextMessage();
                    Log.Debug($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: {clearedBytes} bytes of additional data cleared from the stream");
                }

                // Prepare a new GetStatusRequest data packet
                AcspGetStatusRequest statusRequest = new AcspGetStatusRequest();
                CurrentRequestId = statusRequest.RequestId;

                // Send the GetStatusRequest data packet
                if (ListenerStream != null)
                {
                    ListenerStream.Write(statusRequest.PackArray, 0, statusRequest.PackArray.Length);
                    Log.Information($"[Thread #{thread.ManagedThreadId}, {DateTime.Now}]GetStatus Request sent to ACS.  RequestID #: {CurrentRequestId}");

                    // Wait for GetStatusResponse from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    string debugMessage = "";
                    int messageCounter = 0;

                    // Expecting a GetStatusResponse message
                    while (messagePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValue))
                    {
                        if (VerboseOutput is true)
                        {
                            string thisMessage = "Debug Info: stopwatch elapsedMs is " + stopwatch.ElapsedMilliseconds + "ms";
                            if (thisMessage == debugMessage)
                            {
                                messageCounter++;
                            }
                            else
                            {
                                if (messageCounter > 0)
                                {
                                    Log.Verbose($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}] Debug Info: <previous message repeated {messageCounter} times");
                                }

                                messageCounter = 0;
                                Log.Verbose($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}] {thisMessage}");
                                debugMessage = thisMessage;
                            }
                        }

                        if (ListenerStream?.DataAvailable == true)
                        {
                            numberOfBytes = ListenerStream.Read(header, 0, header.Length); // Read 20-byte header in from the NetworkStream
                            Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}] {numberOfBytes}-byte header successfully read from network stream");

                            AcspResponseHeader responseHeader = new AcspResponseHeader(header);
                            if (responseHeader.Key.IsBadRequest)
                            {
                                Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: GetStatusResponse [Bad Message] received from ACS!");
                                // Need some control logic here to figure out how to handle a Bad Request message
                            }

                            if (responseHeader.Key.IsGoodRequest)
                            {
                                Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Good GetStatusResponse received from ACS!");

                                if (responseHeader.Key.NodeNames == Byte13NodeNames.GetStatusResponse)
                                {
                                    Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Expected GetStatusResponse has been received");

                                    int length = responseHeader.PackLength.Length;

                                    Byte[] bytes = new Byte[length];
                                    numberOfBytes = ListenerStream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                                    Log.Information(
                                        $"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: {numberOfBytes}-byte GetStatusResponse successfully read from network stream");

                                    AcspGetStatusResponse statusResponse = new AcspGetStatusResponse(bytes);
                                    Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: RequestId is: {statusResponse.RequestId}");

                                    if (statusResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful)
                                    {
                                        Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Response Received: RrpSuccessful");
                                        Log.Information(
                                            $"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Message Received [{statusResponse.StatusResponseMessage.Length} byte]: {statusResponse.StatusResponseMessage}");
                                    }
                                    else
                                    {
                                        Log.Information(
                                            $"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Response Received: {statusResponse.StatusResponseKeyString}");
                                    }

                                    messagePairSuccessful = true;
                                    outputMessage = statusResponse.StatusResponseKeyString;
                                }
                            }
                        } // if (ListenerStream.DataAvailable == true)
                    } // end while(stopwatch.ElapsedMilliseconds < timeoutValue)
                } // if (ListenerStream != null)
                else  // if we reach this point then we need to force a NullReferenceException to be caught further up the stack
                {
                    Log.Error($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Error encountered with the ACS network stream");
                    throw new ArgumentNullException(nameof(ListenerStream), $"The ListenerStream object is null when it should be set");
                }
            } // end while(messagePairSuccessful == false)

            CanWriteToStream.Set();  // Set signal to indicate that it is safe to write to the NetworkStream
            return outputMessage;
        }  // end ProcessGetStatusRrp()

        /// <summary>  Sends the SetRplLocation message to the ACS</summary>
        /// <param name="resourceUrl">  The URL to the RPL file to be loaded</param>
        /// <param name="playoutId">  PlayoutId of the RPL file to be loaded</param>
        private static void ProcessSetRplLocationRrp(string resourceUrl, UInt32 playoutId)
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field


            if (VerboseOutput is true)
            {
                Log.Verbose($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            // Send the "SetRplLocationRequest " to the ACS system, and keep trying until a successful response is received
            bool messagePairSuccessful = false;
            while (messagePairSuccessful == false)
            {
                // Check to see if there is any unexpected data in the stream, and if so, purge it prior to sending the request
                if (ListenerStream?.DataAvailable is true)
                {
                    int clearedBytes = ListenerStream.ClearStreamForNextMessage();
                    Log.Debug($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: {clearedBytes} bytes of additional data cleared from the stream");
                }

                // Prepare the SetRplLocationRequest data packet
                AcspSetRplLocationRequest rplRequest = new AcspSetRplLocationRequest(resourceUrl, playoutId);
                CurrentRequestId = rplRequest.RequestId;

                // Send the SetRplLocationRequest data packet to the ACS
                if (ListenerStream != null)
                {
                    ListenerStream.Write(rplRequest.PackArray, 0, rplRequest.PackArray.Length);
                    Log.Information($"[{DateTime.Now}]SetRplLocation Request sent to ACS.  RequestID #: {CurrentRequestId}");

                    // Wait for SetRplLocationResponse from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    // Expecting a GetStatusResponse message
                    while (messagePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValue))
                    {
                        if (ListenerStream?.DataAvailable == true)
                        {
                            numberOfBytes = ListenerStream.Read(header, 0, header.Length); // Read 20-byte header in from the NetworkStream
                            Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}] {numberOfBytes}-byte header successfully read from network stream");

                            AcspResponseHeader responseHeader = new AcspResponseHeader(header);
                            if (responseHeader.Key.IsBadRequest)
                            {
                                Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: SetRplLocation [Bad Message] received from ACS!");
                                // Need some control logic here to figure out how to handle a Bad Request message
                            }

                            if (responseHeader.Key.IsGoodRequest)
                            {
                                Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Good SetRplLocationResponse received from ACS!");

                                if (responseHeader.Key.NodeNames == Byte13NodeNames.SetRplLocationResponse)
                                {
                                    Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Expected SetRplLocationResponse has been received");

                                    int length = responseHeader.PackLength.Length;

                                    Byte[] bytes = new Byte[length];
                                    numberOfBytes = ListenerStream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                                    Log.Information(
                                        $"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: {numberOfBytes}-byte SetRplLocationResponse successfully read from network stream");

                                    AcspSetRplLocationResponse locationResponse = new AcspSetRplLocationResponse(bytes);
                                    Log.Information($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: RequestId is: {locationResponse.RequestId}");

                                    if ((locationResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful) ||
                                        (locationResponse.StatusResponseKey == GeneralStatusResponseKey.Processing))
                                    {
                                        messagePairSuccessful = true;
                                        Log.Information(
                                            $"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Response Received: {locationResponse.StatusResponseKeyString}");
                                        Log.Information(
                                            $"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Message Received: {locationResponse.StatusResponseMessage}");
                                    }
                                    else
                                    {
                                        Log.Information(
                                            $"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Response Received: {locationResponse.StatusResponseKeyString}");
                                    }
                                }
                            }
                        } // if (ListenerStream.DataAvailable == true)
                    } // end while(stopwatch.ElapsedMilliseconds < timeoutValue)
                } // if (ListenerStream != null)
                else  // if we reach this point then we need to force a NullReferenceException to be caught further up the stack
                {
                    Log.Error($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Error encountered with the ACS network stream");
                    throw new ArgumentNullException(nameof(ListenerStream), $"The ListenerStream object is null when it should be set");
                }
            } // end while(messagePairSuccessful == false)

            CanWriteToStream.Set();
        }  // end ProcessSetRplLocationRrp()

        /// <summary>  Sends the SetOutputMode message to the ACS</summary>
        /// <param name="outputMode">  OutputMode for the ACS to set.   If set to TRUE, then CaptiView device will play captions.  If set to FALSE, it will not play captions.</param>
        private static void ProcessSetOutputModeRrp(bool outputMode)
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field


            if (VerboseOutput is true)
            {
                Log.Verbose($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            // Send the "SetOutputModeRequest " to the ACS system, and keep trying until a successful response is received
            bool messagePairSuccessful = false;
            while (messagePairSuccessful == false)
            {
                // Check to see if there is any unexpected data in the stream, and if so, purge it prior to sending the request
                if (ListenerStream?.DataAvailable is true)
                {
                    int clearedBytes = ListenerStream.ClearStreamForNextMessage();
                    Log.Debug($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: {clearedBytes} bytes of additional data cleared from the stream");
                }

                // Prepare the SetOutputModeRequest data packet
                AcspSetOutputModeRequest outputModeRequest = new AcspSetOutputModeRequest(outputMode);
                CurrentRequestId = outputModeRequest.RequestId;

                // Send the SetOutputModeRequest data packet to the ACS
                if (ListenerStream != null)
                {
                    ListenerStream.Write(outputModeRequest.PackArray, 0, outputModeRequest.PackArray.Length);
                    Log.Information($"[{DateTime.Now}]SetOutputMode Request sent to ACS.  RequestID #: {CurrentRequestId}");

                    // Wait for SetOutputMode Response from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    // Expecting a GetStatusResponse message
                    while (messagePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValue))
                    {
                        if (ListenerStream?.DataAvailable == true)
                        {
                            numberOfBytes = ListenerStream.Read(header, 0, header.Length); // Read 20-byte header in from the NetworkStream
                            Log.Information($"[Thread #{thread.ManagedThreadId}] {numberOfBytes}-byte header successfully read from network stream");

                            AcspResponseHeader responseHeader = new AcspResponseHeader(header);
                            if (responseHeader.Key.IsBadRequest)
                            {
                                Log.Information($"[Thread #{thread.ManagedThreadId}]: SetOutputMode Response [Bad Message] received from ACS!");
                                // Need some control logic here to figure out how to handle a Bad Request message
                            }

                            if (responseHeader.Key.IsGoodRequest)
                            {
                                Log.Information($"[Thread #{thread.ManagedThreadId}]: Good SetOutputMode Response received from ACS!");

                                if (responseHeader.Key.NodeNames == Byte13NodeNames.SetOutputModeResponse)
                                {
                                    Log.Information($"[Thread #{thread.ManagedThreadId}]: Expected SetOutputModeResponse has been received");

                                    int length = responseHeader.PackLength.Length;

                                    Byte[] bytes = new Byte[length];
                                    numberOfBytes = ListenerStream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                                    Log.Information($"[Thread #{thread.ManagedThreadId}]: {numberOfBytes}-byte SetRplLocationResponse successfully read from network stream");

                                    AcspSetOutputModeResponse outputModeResponse = new AcspSetOutputModeResponse(bytes);
                                    Log.Information($"[Thread #{thread.ManagedThreadId}]: RequestId is: {outputModeResponse.RequestId}");

                                    if (outputModeResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful)
                                    {
                                        messagePairSuccessful = true;
                                        Log.Information($"[Thread #{thread.ManagedThreadId}]: Response Received: {outputModeResponse.StatusResponseKeyString}");
                                        Log.Information($"[Thread #{thread.ManagedThreadId}]: Message Received: {outputModeResponse.StatusResponseMessage}");
                                    }
                                    else
                                    {
                                        Log.Information($"[Thread #{thread.ManagedThreadId}]: Response Received: {outputModeResponse.StatusResponseKeyString}");
                                    }
                                }
                            }
                        } // if (ListenerStream.DataAvailable == true)
                    } // end while(stopwatch.ElapsedMilliseconds < timeoutValue)
                } // if (ListenerStream != null)
                else  // if we reach this point then we need to force a NullReferenceException to be caught further up the stack
                {
                    Log.Error($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Error encountered with the ACS network stream");
                    throw new ArgumentNullException(nameof(ListenerStream), $"The ListenerStream object is null when it should be set");
                }
            } // end while(messagePairSuccessful == false)

            CanWriteToStream.Set();  // Signal that it is okay to write to the NetworkStream again
        } // end ProcessSetRplLocationRrp()

        /// <summary>  Sends an UpdateTimeline message to the ACS</summary>
        /// <param name="testPlayoutId">The test playout identifier.</param>
        /// <param name="timelineEditUnits">The timeline edit units.</param>
        private static void ProcessUpdateTimelineRrp(UInt32 testPlayoutId, UInt64 timelineEditUnits)
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field

            if (VerboseOutput is true)
            {
                Log.Verbose($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            // Send the "UpdateTimeline Request " to the ACS system, keep trying until we receive a successful response
            bool messagePairSuccessful = false;
            while (messagePairSuccessful == false)
            {
                // Check to see if there is any unexpected data in the stream, and if so, purge it prior to sending the request
                if (ListenerStream?.DataAvailable is true)
                {
                    int clearedBytes = ListenerStream.ClearStreamForNextMessage();
                    Log.Debug($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: {clearedBytes} bytes of additional data cleared from the stream");
                }

                // Prepare the UpdateTimelineRequest data packet
                AcspUpdateTimelineRequest updateTimelineRequest = new AcspUpdateTimelineRequest(testPlayoutId, timelineEditUnits);
                CurrentRequestId = updateTimelineRequest.RequestId;

                // Send the UpdateTimelineRequest data packet to the ACS
                if (ListenerStream != null)
                {
                    ListenerStream.Write(updateTimelineRequest.PackArray, 0, updateTimelineRequest.PackArray.Length);
                    Log.Information($"[{DateTime.Now}]UpdateTimeline Request sent to ACS.  RequestID #: {CurrentRequestId}");

                    // Wait for UpdateTimeline Response from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    // Expecting a GetStatusResponse message
                    while (messagePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValue))
                    {
                        if (ListenerStream?.DataAvailable == true)
                        {
                            numberOfBytes = ListenerStream.Read(header, 0, header.Length); // Read 20-byte header in from the NetworkStream
                            Log.Information($"[Thread #{thread.ManagedThreadId}] {numberOfBytes}-byte header successfully read from network stream");

                            AcspResponseHeader responseHeader = new AcspResponseHeader(header);
                            if (responseHeader.Key.IsBadRequest)
                            {
                                Log.Information($"[Thread #{thread.ManagedThreadId}]: UpdateTimeline Response [Bad Message] received from ACS!");
                                // Need some control logic here to figure out how to handle a Bad Request message
                            }

                            if (responseHeader.Key.IsGoodRequest)
                            {
                                Log.Information($"[Thread #{thread.ManagedThreadId}]: Good UpdateTimeline Response received from ACS!");

                                if (responseHeader.Key.NodeNames == Byte13NodeNames.UpdateTimelineResponse)
                                {
                                    Log.Information($"[Thread #{thread.ManagedThreadId}]: Expected UpdateTimelineResponse has been received");

                                    int length = responseHeader.PackLength.Length;

                                    Byte[] bytes = new Byte[length];
                                    numberOfBytes = ListenerStream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                                    Log.Information($"[Thread #{thread.ManagedThreadId}]: {numberOfBytes}-byte UpdateTimelineResponse successfully read from network stream");

                                    AcspUpdateTimelineResponse updateTimelineResponse = new AcspUpdateTimelineResponse(bytes);
                                    Log.Information($"[Thread #{thread.ManagedThreadId}]: RequestId is: {updateTimelineResponse.RequestId}");

                                    if (updateTimelineResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful)
                                    {
                                        messagePairSuccessful = true;
                                        Log.Information($"[Thread #{thread.ManagedThreadId}]: Response Received: {updateTimelineResponse.StatusResponseKeyString}");
                                        Log.Information($"[Thread #{thread.ManagedThreadId}]: Message Received: {updateTimelineResponse.StatusResponseMessage}");
                                    }
                                    else
                                    {
                                        Log.Information($"[Thread #{thread.ManagedThreadId}]: Response Received: {updateTimelineResponse.StatusResponseKeyString}");
                                    }
                                }
                            }
                        } // if (ListenerStream.DataAvailable == true)
                    } // end while(stopwatch.ElapsedMilliseconds < timeoutValue)
                } // if (ListenerStream != null)
                else  // if we reach this point then we need to force a NullReferenceException to be caught further up the stack
                {
                    Log.Error($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Error encountered with the ACS network stream");
                    throw new ArgumentNullException(nameof(ListenerStream), $"The ListenerStream object is null when it should be set");
                }
            } // end while(messagePairSuccessful == false)

            CanWriteToStream.Set(); // Signal that it is okay to write to the NetworkStream again
        } // end ProcessUpdateTimelineRrp()

        /// <summary>  Sends a TerminateLease message to the ACS</summary>
        private static void ProcessTerminateLease()
        {
            Thread thread = Thread.CurrentThread;
            const int timeoutValue = 10000;
            int numberOfBytes;

            Byte[] header = new Byte[20];  // 20 bytes - 16 for the PackKey, and 4 for the BER Length field


            if (VerboseOutput is true)
            {
                Log.Verbose($"[Thread #{thread.ManagedThreadId}]: Waiting on signal to allow write to stream");
            }

            CanWriteToStream.WaitOne();
            CanWriteToStream.Reset();  // Block the signal so that no other processes try to write to the stream at same time

            // Send the "TerminateLease Request " to the ACS system, keep trying until a successful response is received
            bool messagePairSuccessful = false;
            while (messagePairSuccessful == false)
            {
                // Check to see if there is any unexpected data in the stream, and if so, purge it prior to sending the request
                if (ListenerStream?.DataAvailable is true)
                {
                    int clearedBytes = ListenerStream.ClearStreamForNextMessage();
                    Log.Debug($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: {clearedBytes} bytes of additional data cleared from the stream");
                }

                // Prepare the TerminateLeaseRequest data packet
                AcspTerminateLeaseRequest terminateLeaseRequest = new AcspTerminateLeaseRequest();
                CurrentRequestId = terminateLeaseRequest.RequestId;

                // Send the TerminateLeaseRequest data packet to the ACS
                if (ListenerStream != null)
                {
                    ListenerStream.Write(terminateLeaseRequest.PackArray, 0, terminateLeaseRequest.PackArray.Length);
                    Log.Information($"[{DateTime.Now}]TerminateLease Request sent to ACS.  RequestID #: {CurrentRequestId}");

                    // Wait for TerminateLease Response from ACS (Per SMPTE 430-10:2010, must wait at least 2 seconds before allowing timeout)

                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    // Expecting a TerminateLeaseResponse message
                    while (messagePairSuccessful == false && (stopwatch.ElapsedMilliseconds < timeoutValue))
                    {
                        if (ListenerStream?.DataAvailable == true)
                        {
                            numberOfBytes = ListenerStream.Read(header, 0, header.Length); // Read 20-byte header in from the NetworkStream
                            Log.Information($"[Thread #{thread.ManagedThreadId}] {numberOfBytes}-byte header successfully read from network stream");

                            AcspResponseHeader responseHeader = new AcspResponseHeader(header);
                            if (responseHeader.Key.IsBadRequest)
                            {
                                Log.Information($"[Thread #{thread.ManagedThreadId}]: TerminateLease Response [Bad Message] received from ACS!");
                                // Need some control logic here to figure out how to handle a Bad Request message
                            }

                            if (responseHeader.Key.IsGoodRequest)
                            {
                                Log.Information($"[Thread #{thread.ManagedThreadId}]: Good TerminateLease Response received from ACS!");

                                if (responseHeader.Key.NodeNames == Byte13NodeNames.TerminateLeaseResponse)
                                {
                                    Log.Information($"[Thread #{thread.ManagedThreadId}]: Expected TerminateLeaseResponse has been received");

                                    int length = responseHeader.PackLength.Length;

                                    Byte[] bytes = new Byte[length];
                                    numberOfBytes = ListenerStream.Read(bytes, 0, bytes.Length); // Read variable-length header in from NetworkStream
                                    Log.Information($"[Thread #{thread.ManagedThreadId}]: {numberOfBytes}-byte TerminateLeaseResponse successfully read from network stream");

                                    AcspTerminateLeaseResponse terminateLeaseResponse = new AcspTerminateLeaseResponse(bytes);
                                    Log.Information($"[Thread #{thread.ManagedThreadId}]: RequestId is: {terminateLeaseResponse.RequestId}");

                                    if (terminateLeaseResponse.StatusResponseKey == GeneralStatusResponseKey.RrpSuccessful)
                                    {
                                        messagePairSuccessful = true;
                                        Log.Information($"[Thread #{thread.ManagedThreadId}]: Response Received: {terminateLeaseResponse.StatusResponseKeyString}");
                                        Log.Information($"[Thread #{thread.ManagedThreadId}]: Message Received: {terminateLeaseResponse.StatusResponseMessage}");
                                    }
                                    else
                                    {
                                        Log.Information($"[Thread #{thread.ManagedThreadId}]: Response Received: {terminateLeaseResponse.StatusResponseKeyString}");
                                    }
                                }
                            }
                        } // if (ListenerStream.DataAvailable == true)
                    } // end while(stopwatch.ElapsedMilliseconds < timeoutValue)
                } // if (ListenerStream != null)
                else  // if we reach this point then we need to force a NullReferenceException to be caught further up the stack
                {
                    Log.Error($"[Thread #{thread.ManagedThreadId}, RequestID #{CurrentRequestId}]: Error encountered with the ACS network stream");
                    throw new ArgumentNullException(nameof(ListenerStream), $"The ListenerStream object is null when it should be set");
                }
            } // end while(messagePairSuccessful == false)

            CanWriteToStream.Set();     // The subsequent clean-up activity needs the signal set (as it safely waits for any other ACS write to finish first)
            DoAcsConnectionCleanup();   // Do all the necessary stuff to clean up the network stream and close the TCP connection

            // We don't need to do a CanWriteToStream.Set() here, since at this point at the end of the TerminateLease, we are assuming that the TCP connection is closed or closing
        } // end ProcessTerminateLease()
    }
}