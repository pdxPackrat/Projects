﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MyTcpListener
{
    class Program
    {
        static void Main(string[] args)
        {

            // var thread1 = new System.Threading.Thread(ListenerProcess);
            // var thread2 = new System.Threading.Thread(ListenerProcess);
            // thread1.Start();
            // thread2.Start();

            // Set the TcpListener on port 13000
            Int32 Port = 13000;
            Int32 AltPort = 4170;
            IPAddress LocalAddr = IPAddress.Parse("127.0.0.1");

            //TcpListener listener = new TcpListener(IPAddress.Any, port);
            TcpListener listener = new TcpListener(LocalAddr, Port);
            TcpListener listener2 = new TcpListener(LocalAddr, AltPort);

            TcpClient client;

            listener.Start();
            Console.WriteLine("Waiting for a connection ... ");

            try
            {
                while (true)
                {
                    // Perform a blocking call to accept requests.
                    // You could also use server.AcceptSocket() here.
                    client = listener.AcceptTcpClient();

                    // Some logic I threw in to get a quick idea of how many worker threads were available (2047)
                    // int WorkerThreadCount;
                    // int PortThreadCount;

                    // ThreadPool.GetAvailableThreads(out WorkerThreadCount, out PortThreadCount);
                    // Console.WriteLine($"There are {WorkerThreadCount} threads available for connection");

                    ThreadPool.QueueUserWorkItem(ListenerProcess, client);
                }
            }
            finally
            {
                Console.WriteLine("\nHit enter to continue...");
                Console.Read();
            }
        }

        static void ListenerProcess(object obj)
        {
            var MyClient = (TcpClient)obj;

            Thread thread = Thread.CurrentThread;

            try
            {
                
                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;    // the data received from the listener
                String command = "";   // the parsed command received from the listener
                bool Listening = true; // boolean that controls the while-loop logic

                // Enter the listening loop.
                while (Listening == true)
                {

                    Console.WriteLine($"Thread #{thread.ManagedThreadId}: Connected!");

                    // Get a stream object for reading and writing.
                    NetworkStream stream = MyClient.GetStream();

                    // Send back a response.
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes("CONNECTED:  WAITING FOR INPUT\r\n");
                    stream.Write(msg, 0, msg.Length);

                    // Prepare for looping
                    data = null;
                    int i;
                    bool CancelCommandReceived = false;  // boolean used to control the while-loop logic

                    // loop to receive all of the data sent by the client.
                    while ((CancelCommandReceived == false) && ((i = stream.Read(bytes, 0, bytes.Length))!= 0))
                    {
                        // Translate data bytes to an ASCII string.
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                        if (data == "\u0003") // checking for a CTRL+C from the connected terminal here
                        {
                            CancelCommandReceived = true;  // set boolean logic to exit the while-loop
                            Console.WriteLine("Received Cancel Command");
                        }
                        else
                        {

                            // Process the data sent by the client.
                            data = data.ToUpper();

                            // If the data segment received is a CRLF, then take whatever parsed command so far and process it
                            if (data == "\r\n")
                            {
                                if (command == "CANCEL" || command == "QUIT")
                                {
                                    Console.WriteLine($"Thread #{thread.ManagedThreadId}: CANCEL/QUIT command received - terminating connection");
                                    CancelCommandReceived = true;
                                }
                                else
                                {
                                    Console.WriteLine($"Thread #{thread.ManagedThreadId}:  Command Received: {command}");
                                }

                                command = "";
                            }
                            else
                            {
                                // Echo the data received and continue
                                // Console.WriteLine($"Received: {data}");
                                command = String.Concat(command, data);
                            }


                            // byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                            // Send back a response.
                            // stream.Write(msg, 0, msg.Length);
                            // Console.WriteLine($"Sent: {data}");

                        }
                    }

                    // Shutdown and end the connection.
                    MyClient.Close();
                    Listening = false;
                }

            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException: {e}");
            }
            finally
            {
                // Stop listening for new clients.
                // server.Stop();

                Console.WriteLine($"Thread #{thread.ManagedThreadId}: Connection Terminated");
            }

        }
    }
}