using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LeobasChat.Data;
using Microsoft.AspNetCore.Authorization;
using System.Threading;
using System.IO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using LeobasChat.Services;

namespace LeobasChat.Pages.ChatRooms
{
    [Authorize]
    public class EnterModel : PageModel
    {
        public readonly UserManager<ApplicationUser> _userManager;
        public readonly ApplicationDbContext _dbContext;

        public TcpClient Client { get; set; }
        public Stream Stream { get; set; }
        public StreamWriter Writer { get; set; }
        public StreamReader Reader { get; set; }
        public string Input { get; set; }
        public Thread Thread { get; set; }

        public EnterModel(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            if (Startup.userMsgBox.ContainsKey(0))
                MsgHtml = Startup.userMsgBox[0];
        }
        [BindProperty]
        public ChatUser ChatUser { get; set; }
        [BindProperty]
        public ChatRoom CurrentRoom { get; set; }
        [BindProperty]
        public string OnlineUsersHtml { get; set; }
        [BindProperty]
        public string MsgHtml { get; set; }
        [BindProperty]
        public string SendMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            TripleDESCryptoServiceProvider TDES = new TripleDESCryptoServiceProvider();
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();

            OnlineUsersHtml = "";

            var ChatUsers = _dbContext.ChatUsers
                .Include(s => s.User)
                .Include(r => r.Chat);

            foreach (ChatUser user in ChatUsers)
            {
                if (user.UserId == _userManager.GetUserId(User) && user.ChatRoomId == id)
                {
                    ChatUser = user;
                    break;
                }
            }

            CurrentRoom = _dbContext.ChatRooms.Find(id);

            if (CurrentRoom == null)
            {
                RedirectToPage("./Index");
            }

            if (ChatUser == null)
            {
                TDES.GenerateKey();

                ChatUser = new ChatUser()
                {
                    User = await _userManager.GetUserAsync(User),
                    Chat = CurrentRoom,
                    IsAdmin = false,
                    DesKey = TDES.Key,
                    CommandInter = (int)ChatUser.Command.AddChatUser
                };
                _dbContext.ChatUsers.Add(ChatUser);
                await _dbContext.SaveChangesAsync();
            }

            if (!Startup.clients.ContainsKey(ChatUser.ChatUserId))
            {
                Client = new TcpClient();
                await Client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 10140);
                Startup.clients.Add(ChatUser.ChatUserId, Client);
                Startup.userMsgBox.Add(ChatUser.ChatUserId, MsgHtml);

                Stream = Startup.clients[ChatUser.ChatUserId].GetStream();
                Writer = new StreamWriter(Stream)
                {
                    AutoFlush = true
                };
                Reader = new StreamReader(Stream);

                ChatUser.CommandInter = (int)ChatUser.Command.getRSAKey;
                Input = JsonConvert.SerializeObject(ChatUser);
                Writer.WriteLine(Input);
                while(true)
                {
                    if(Client.Available > 0)
                    {
                        string data = Reader.ReadLine();
                        ChatUser = JsonConvert.DeserializeObject<ChatUser>(data);
                        break;
                    }
                }

                ChatUser.CommandInter = (int)ChatUser.Command.AddChatUser;
                ChatUser.DesKey = RSAEncrypt.Encrypt(ChatUser.DesKey, 2048, ChatUser.RsaKey);
                Input = JsonConvert.SerializeObject(ChatUser);
                Writer.WriteLine(Input);
            }
            MsgHtml = Startup.userMsgBox[ChatUser.ChatUserId];

            var chatUsers = _dbContext.ChatUsers
                            .Include(s => s.User)
                            .Include(r => r.Chat)
                            .Where(u => u.ChatRoomId == CurrentRoom.ChatRoomId);

            foreach (ChatUser user in chatUsers)
            {
                OnlineUsersHtml += "<div class='user'>" +
                                        "<div class='avatar'>" +
                                            "<img src ='" + user.User.Avatar + "' alt='User name'>" +
                                            "<div class='status " + user.User.Status + "'></div>" +
                                            "</div>" +
                                        "<div class='name'>" + user.User.UserName + "</div>" +
                                        "<div class='mood'>" + user.User.Mood + "</div>" +
                                    "</div>";
            }

            Startup.ReceiveEvent = new Task(() => ReceiveData(ChatUser));
            Startup.ReceiveEvent.Start();

            return Page();
        }

        public IActionResult OnPost(int id)
        {
            OnlineUsersHtml = "";
            CurrentRoom = _dbContext.ChatRooms.Find(id);
            var chatUsers = _dbContext.ChatUsers
                            .Include(s => s.User)
                            .Include(r => r.Chat)
                            .Where(u => u.ChatRoomId == CurrentRoom.ChatRoomId);

            foreach (ChatUser user in chatUsers)
            {
                OnlineUsersHtml += "<div class='user'>" +
                                        "<div class='avatar'>" +
                                            "<img src ='" + user.User.Avatar + "' alt='User name'>" +
                                            "<div class='status " + user.User.Status + "'></div>" +
                                            "</div>" +
                                        "<div class='name'>" + user.User.UserName + "</div>" +
                                        "<div class='mood'>" + user.User.Mood + "</div>" +
                                    "</div>";

                if (user.UserId == _userManager.GetUserId(User))
                {
                    ChatUser = user;
                    break;
                }
            }

            if (id != 1234)
            {
                Stream = Startup.clients[ChatUser.ChatUserId].GetStream();
                Writer = new StreamWriter(Stream)
                {
                    AutoFlush = true
                };
                Reader = new StreamReader(Stream);

                byte[] messageEncrypted;

                // Encrypt the string to an array of bytes.
                messageEncrypted = Encrypt(SendMessage, ChatUser.DesKey);

                ChatUser.Message = messageEncrypted;
                ChatUser.CommandInter = (int)ChatUser.Command.SendMessage;

                Input = JsonConvert.SerializeObject(ChatUser);
                Writer.WriteLine(Input);
            }
            ReceiveData(ChatUser);

            return Page();
        }

        public IActionResult ReceiveData(ChatUser actualUser)
        {
            Stream = Startup.clients[actualUser.ChatUserId].GetStream();
            Reader = new StreamReader(Stream);

            while (true)
            {
                if (Startup.clients[ChatUser.ChatUserId].Available > 0)
                {
                    Input = Reader.ReadLine();
                    ChatUser userSender = JsonConvert.DeserializeObject<ChatUser>(Input);

                    switch (userSender.CommandInter)
                    {
                        case (int)ChatUser.Command.AddChatUser:
                            Startup.userMsgBox[userSender.ChatUserId] += userSender.User.UserName + " entrou na sala!<br/>";
                            break;

                        case (int)ChatUser.Command.DeleteChatUser:
                            Startup.clients.Remove(userSender.ChatUserId);
                            Startup.userMsgBox.Remove(userSender.ChatUserId);
                            Startup.userMsgBox[userSender.ChatUserId] += userSender.User.UserName + " saiu da sala!<br/>";
                            _dbContext.Remove(userSender);
                            _dbContext.SaveChanges();
                            break;

                        case (int)ChatUser.Command.SendMessage:
                            if (userSender.UserId == actualUser.UserId)
                                Startup.userMsgBox[actualUser.ChatUserId] += "<div class='answer right'>";
                            else
                                Startup.userMsgBox[actualUser.ChatUserId] += "<div class='answer left'>";

                            string messageDecrypted;

                            // Decrypt the bytes to a string.
                            messageDecrypted = Decrypt(userSender.Message, actualUser.DesKey);

                            Startup.userMsgBox[actualUser.ChatUserId] += "<div class='avatar'>" +
                                                        "<img src = '" + userSender.User.Avatar + "' alt='User name'>" +
                                                        "<div class='status " + userSender.User.Status + "'></div>" +
                                                    "</div>" +
                                                        "<div class='text'>" +
                                                            "<div class='media-heading'>" + userSender.User.UserName + "</div>" +
                                                            messageDecrypted +
                                                            "<p class='speech-time'>" +
                                                                "<i class='fa fa-clock-o fa-fw'></i> " + DateTime.Now.ToString("HH:mm:ss") +
                                                            "</p>" +
                                                        "</div>" +
                                                "</div>";

                            break;
                    }
                    MsgHtml = Startup.userMsgBox[actualUser.ChatUserId];

                    return Page();
                }
            }
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