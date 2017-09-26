using LeobasChat.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LeobasServer
{
    class Program
    {
        static readonly object _lock = new object();
        static readonly Dictionary<ChatUser, TcpClient> list_clients = new Dictionary<ChatUser, TcpClient>();

        static void Main(string[] args)
        {
            TcpListener ServerSocket = new TcpListener(IPAddress.Any, 10140);
            ServerSocket.Start();

            //Listen to the all requests 
            while (true)
            {
                //Handle requests Asynchronously 
                TcpClient client = ServerSocket.AcceptTcpClient();
                Console.WriteLine("Someone connected!!");

                Thread t = new Thread(HandleClients);
                t.Start();
            }
        }

        public static void HandleClients()
        {
            // Accept connection 
            TcpClient client = new TcpClient();

            while (true)
            {
                if (client.Available > 0)
                {
                    // Setup reader/writer stream 
                    NetworkStream stream = client.GetStream();
                    StreamReader Reader = new StreamReader(stream);

                    string data = Reader.ReadLine();
                    ChatUser chatUser = (ChatUser)JsonConvert.DeserializeObject(data);
                    if (chatUser.CommandInter == (int)ChatUser.Command.AddChatUser)
                    {
                        lock (_lock) list_clients.Add(chatUser, client);
                        lock (_lock) client = list_clients[chatUser];
                    }
                    else if (chatUser.CommandInter == (int)ChatUser.Command.DeleteChatUser)
                    {
                        lock (_lock) list_clients.Remove(chatUser);
                        client.Client.Shutdown(SocketShutdown.Both);
                        client.Close();
                    }
                    else if (chatUser.CommandInter == (int)ChatUser.Command.SendMessage)
                    {
                        Console.WriteLine(chatUser.User.UserName + ": " + chatUser.Message);
                        Broadcast(data, chatUser.ChatRoomId);
                    }
                }
            }
        }

        public static void Broadcast(string data, int chatRoomId)
        {
            lock (_lock)
            {
                NetworkStream stream;
                foreach (KeyValuePair<ChatUser, TcpClient> c in list_clients)
                {
                    if (c.Key.ChatRoomId == chatRoomId)
                    {
                        stream = c.Value.GetStream();
                        StreamWriter Writer = new StreamWriter(stream)
                        {
                            AutoFlush = true
                        };
                        Writer.WriteLine(data);
                    }
                }
            }
        }
    }
}
