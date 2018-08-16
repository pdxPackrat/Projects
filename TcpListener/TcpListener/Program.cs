using System;
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
            Int32 port = 13000;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");

            TcpListener listener = new TcpListener(localAddr, port);
            TcpClient client;

            listener.Start();

            while (true)
            {
                client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(ListenerProcess, client);
            }
        }

        static void ListenerProcess(object obj)
        {
            var MyClient = (TcpClient)obj;

            TcpListener server = null;

            try
            {

                // TcpListener server = new TcpListener(port);
                server = new TcpListener(localAddr, port);

                // Start listening for client requests
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;
                String command = "";

                // Enter the listening loop.
                while (true)
                {
                    Console.Write("Waiting for a connection ... ");

                    // Perform a blocking call to accept requests.
                    // You could also use server.AcceptSocket() here.
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    // Get a stream object for reading and writing.
                    NetworkStream stream = client.GetStream();

                    // Send back a response.
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes("CONNECTED:  WAITING FOR INPUT\n");
                    stream.Write(msg, 0, msg.Length);

                    // Prepare for looping
                    data = null;
                    int i;
                    bool CancelCommandReceived = false;

                    // loop to receive all of the data sent by the client.
                    while ((CancelCommandReceived == false) && ((i = stream.Read(bytes, 0, bytes.Length))!= 0))
                    {
                        // Translate data bytes to an ASCII string.
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                        if (data == "\u0003")
                        {
                            CancelCommandReceived = true;
                            Console.WriteLine("Received Cancel Command");
                        }
                        else
                        {

                            // Process the data sent by the client.
                            data = data.ToUpper();

                            if (data == "\r\n")
                            {
                                if (command == "CANCEL" || command == "QUIT")
                                {
                                    Console.WriteLine("CANCEL/QUIT command received - terminating connection");
                                    CancelCommandReceived = true;
                                }
                                else
                                {
                                    Console.WriteLine($"Command Received: {command}");
                                }

                                command = "";
                            }
                            else
                            {
                                Console.WriteLine($"Received: {data}");
                                command = String.Concat(command, data);
                            }


                            // byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                            // Send back a response.
                            // stream.Write(msg, 0, msg.Length);
                            // Console.WriteLine($"Sent: {data}");

                        }
                    }

                    // Shutdown and end the connection.
                    client.Close();
                }

            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException: {e}");
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }
    }
}
