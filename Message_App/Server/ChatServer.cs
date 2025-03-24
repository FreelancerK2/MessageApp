using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    class ChatServer
    {
        private static List<TcpClient> clients = new List<TcpClient>();
        private static Dictionary<TcpClient, string> clientNames = new Dictionary<TcpClient, string>();
        private static TcpListener server;

        static void Main()
        {
            server = new TcpListener(IPAddress.Any, 12345);
            server.Start();
            Console.WriteLine("Server started...");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }

        private static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

            try
            {
                string userName = reader.ReadLine();
                if (string.IsNullOrEmpty(userName))
                    return;

                clients.Add(client);
                clientNames[client] = userName;
                Console.WriteLine($"{userName} has connected.");

                // Send updated user list to all clients
               BroadcastUserList();

                // Notify everyone about the new user
                BroadcastMessage($"{userName} has joined the chat.", "Server");

                string message;
                while ((message = reader.ReadLine()) != null)
                {
                    if (message.StartsWith("TYPING:"))
                    {
                        BroadcastTypingStatus(message);
                    }
                    else if (message.StartsWith("STOP_TYPING:"))
                    {
                        BroadcastTypingStatus(message);
                    }
                    else
                    {
                        BroadcastMessage(message, userName);
                    }
                }
            }
            catch { }
            finally
            {
                if (clientNames.ContainsKey(client))
                {
                    Console.WriteLine($"{clientNames[client]} has disconnected.");
                    BroadcastMessage($"{clientNames[client]} has left the chat.", "Server");
                    clients.Remove(client);
                    clientNames.Remove(client);

                    // Broadcast the updated user list
                    BroadcastUserList();
                }
                client.Close();
            }
        }

        private static void BroadcastTypingStatus(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + Environment.NewLine);
            foreach (var client in clients)
            {
                try
                {
                    client.GetStream().Write(data, 0, data.Length);
                }
                catch { }
            }
        }


        private static void BroadcastMessage(string message, string sender)
        {
            string formattedMessage = $"{sender}: {message}";
            byte[] data = Encoding.UTF8.GetBytes(formattedMessage + Environment.NewLine);

            foreach (var client in clients)
            {
                try
                {
                    client.GetStream().Write(data, 0, data.Length);
                }
                catch { }
            }
        }

        /*private static void BroadcastUserList()
        {
            string userList = "Users: " + string.Join(", ", clientNames.Values);
            byte[] data = Encoding.UTF8.GetBytes(userList + Environment.NewLine);

            foreach (var client in clients)
            {
                try
                {
                    client.GetStream().Write(data, 0, data.Length);
                }
                catch { }
            }
        }*/

        private static void BroadcastUserList()
        {
            // Send the list of users to all clients
            string userList = string.Join(",", clientNames.Values);
            foreach (var client in clients)
            {
                StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
                writer.WriteLine("USER_LIST:" + userList);
            }
        }
    }
}
