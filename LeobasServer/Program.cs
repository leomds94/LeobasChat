using LeobasChat.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using LeobasChat.Services;
using System.Collections.Concurrent;

namespace LeobasServer
{
    class Program
    {
        static readonly ConcurrentDictionary<int, TcpClient> list_clients = new ConcurrentDictionary<int, TcpClient>();
        static List<ChatUser> chatUsers = new List<ChatUser>();

        static void Main(string[] args)
        {
            TcpListener ServerSocket = new TcpListener(IPAddress.Any, 10140);
            
            ServerSocket.Start();

            //Listen to the all requests 
            while (true)
            {
                //Handle requests Asynchronously 
                TcpClient client = ServerSocket.AcceptTcpClient();

                Thread t = new Thread(() => HandleClients(client));
                t.Start();
            }
        }

        public static void HandleClients(TcpClient client)
        {

            string publicKey;
            string privateKey;
            int KeySize = 2048;

            RSAEncrypt.GenerateKeys(KeySize, out publicKey, out privateKey);

            while (true)
            {
                if (client.Available > 0)
                {
                    // Setup reader/writer stream 
                    NetworkStream stream = client.GetStream();
                    StreamReader Reader = new StreamReader(stream);
                    StreamWriter Writer = new StreamWriter(stream)
                    {
                        AutoFlush = true
                    };

                    string data = Reader.ReadLine();
                    ChatUser chatUser = JsonConvert.DeserializeObject<ChatUser>(data);
                    if(chatUser.CommandInter == (int)ChatUser.Command.getRSAKey)
                    {
                        chatUser.RsaKey = publicKey;
                        list_clients.TryAdd(chatUser.ChatUserId, client);
                        chatUsers.Add(chatUser);
                        stream = list_clients[chatUser.ChatUserId].GetStream();
                        string Input = JsonConvert.SerializeObject(chatUser);
                        Writer.WriteLine(Input);
                    }
                    else if (chatUser.CommandInter == (int)ChatUser.Command.AddChatUser)
                    {
                        chatUser.DesKey = RSAEncrypt.Decrypt(chatUser.DesKey, KeySize, publicKey);
                        client = list_clients[chatUser.ChatUserId];
                        Console.WriteLine(chatUser.User.UserName + " entrou na sala " + chatUser.Chat.Name + "!");
                        chatUser.Message = Encrypt(chatUser.User.UserName + " entrou na sala " + chatUser.Chat.Name + "!", chatUser.DesKey);
                        Broadcast(chatUser);
                    }
                    else if (chatUser.CommandInter == (int)ChatUser.Command.DeleteChatUser)
                    {
                        client.Client.Shutdown(SocketShutdown.Both);
                        client.Close();
                        TcpClient tcpRemove;
                        list_clients.TryRemove(chatUser.ChatUserId, out tcpRemove);
                        ChatUser userToRemove = chatUsers.Find(c => c.ChatUserId == chatUser.ChatUserId);
                        chatUsers.Remove(userToRemove);
                        Console.WriteLine(chatUser.User.UserName + " saiu da sala " + chatUser.Chat.Name + "!");
                        chatUser.Message = Encrypt(chatUser.User.UserName + " saiu da sala " + chatUser.Chat.Name + "!", chatUser.DesKey);
                        Broadcast(chatUser);
                        break;
                    }
                    else if (chatUser.CommandInter == (int)ChatUser.Command.SendMessage)
                    {
                        // Decrypt the bytes to a string.
                        string messageDecrypted = Decrypt(chatUser.Message, chatUser.DesKey);

                        Console.WriteLine("Sala: " + chatUser.Chat.Name + "-" + chatUser.User.UserName + ": " + messageDecrypted);
                        Broadcast(chatUser);
                    }
                }
            }
        }

        public static void Broadcast(ChatUser chatUser)
        {
            string messageDecrypted = Decrypt(chatUser.Message, chatUser.DesKey);

                NetworkStream stream;

                foreach (KeyValuePair<int, TcpClient> c in list_clients)
                {
                    if (chatUsers.Find(cuser => cuser.ChatUserId == c.Key).ChatRoomId == chatUser.ChatRoomId)
                    {
                        stream = c.Value.GetStream();

                        byte[] encryptedMessage = Encrypt(messageDecrypted, chatUsers.Find(cuser => cuser.ChatUserId == c.Key).DesKey);

                        ChatUser toSend = chatUser;

                        toSend.Message = encryptedMessage;

                        StreamWriter Writer = new StreamWriter(stream)
                        {
                            AutoFlush = true
                        };
                        toSend.DesKey = null;
                        string Input = JsonConvert.SerializeObject(toSend);
                        Writer.WriteLine(Input);
                    }
            }
        }

        public static string ToXmlString(RSA rsa, bool includePrivateParameters)
        {
            RSAParameters parameters = rsa.ExportParameters(includePrivateParameters);

            return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
                  parameters.Modulus != null ? Convert.ToBase64String(parameters.Modulus) : null,
                  parameters.Exponent != null ? Convert.ToBase64String(parameters.Exponent) : null,
                  parameters.P != null ? Convert.ToBase64String(parameters.P) : null,
                  parameters.Q != null ? Convert.ToBase64String(parameters.Q) : null,
                  parameters.DP != null ? Convert.ToBase64String(parameters.DP) : null,
                  parameters.DQ != null ? Convert.ToBase64String(parameters.DQ) : null,
                  parameters.InverseQ != null ? Convert.ToBase64String(parameters.InverseQ) : null,
                  parameters.D != null ? Convert.ToBase64String(parameters.D) : null);
        }

        public static byte[] Encrypt(string source, byte[] key)
        {
            TripleDESCryptoServiceProvider desCryptoProvider = new TripleDESCryptoServiceProvider();
            MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider();

            byte[] byteHash;
            byte[] byteBuff;

            byteHash = key;
            desCryptoProvider.Key = byteHash;
            desCryptoProvider.Mode = CipherMode.ECB; //CBC, CFB
            byteBuff = Encoding.UTF8.GetBytes(source);

            byte[] encoded = desCryptoProvider.CreateEncryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length);
            return encoded;
        }

        public static string Decrypt(byte[] encodedText, byte[] key)
        {
            TripleDESCryptoServiceProvider desCryptoProvider = new TripleDESCryptoServiceProvider();
            MD5CryptoServiceProvider hashMD5Provider = new MD5CryptoServiceProvider();

            byte[] byteHash;
            byte[] byteBuff;

            byteHash = key;
            desCryptoProvider.Key = byteHash;
            desCryptoProvider.Mode = CipherMode.ECB; //CBC, CFB
            byteBuff = encodedText;

            string plaintext = Encoding.UTF8.GetString(desCryptoProvider.CreateDecryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
            return plaintext;
        }
    }
}
