using System.Threading.Tasks;
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
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;

namespace LeobasChat.Pages.ChatRooms
{
    [Authorize]
    public class EnterModel : PageModel
    {
        public readonly UserManager<ApplicationUser> _userManager;
        public readonly ChatDbContext _dbContext;
        public readonly ApplicationDbContext _userDbContext;

        public TcpClient Client { get; set; }
        public Stream Stream { get; set; }
        public StreamWriter Writer { get; set; }
        public StreamReader Reader { get; set; }
        public string Input { get; set; }
        public Thread Thread { get; set; }

        public EnterModel(ChatDbContext dbContext, ApplicationDbContext userDbContext, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _userDbContext = userDbContext;
            _dbContext = dbContext;
            if(Program.userMsgBox.ContainsKey(0))
                MsgHtml = Program.userMsgBox[0];
        }
        public ChatUser ChatUser { get; set; }

        public ChatRoom CurrentRoom { get; set; }
        [BindProperty]
        public string OnlineUsersHtml { get; set; }

        [BindProperty]
        public string MsgHtml { get; set; }
        [BindProperty]
        public string SendMessage { get; set; }

        public async void OnGetAsync(int id)
        {
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
                ChatUser = new ChatUser()
                {
                    User = await _userManager.GetUserAsync(User),
                    Chat = CurrentRoom,
                    IsAdmin = false,
                    CommandInter = (int)ChatUser.Command.AddChatUser
                };
                _dbContext.ChatUsers.Add(ChatUser);
                await _dbContext.SaveChangesAsync();
            }

            Client = new TcpClient();
            await Client.ConnectAsync(IPAddress.Parse("127.0.0.1"), 10140);
            if (!Program.clients.ContainsKey(ChatUser.ChatUserId))
            {
                Program.clients.Add(ChatUser.ChatUserId, Client);
                Program.userMsgBox.Add(ChatUser.ChatUserId, MsgHtml);
            }
            MsgHtml = Program.userMsgBox[ChatUser.ChatUserId];

            Stream = Program.clients[ChatUser.ChatUserId].GetStream();
            Writer = new StreamWriter(Stream)
            {
                AutoFlush = true
            };
            Reader = new StreamReader(Stream);

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
            MsgHtml = Program.userMsgBox[ChatUser.ChatUserId];

            Program.ReceiveEvent = new Thread(() => ReceiveData(ChatUser));
            Program.ReceiveEvent.Start();

            ChatUser.CommandInter = (int)ChatUser.Command.AddChatUser;
            Input = JsonConvert.SerializeObject(ChatUser);
            Writer.WriteLine(Input);
        }

        public void OnPost(int id)
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

            MsgHtml = Program.userMsgBox[ChatUser.ChatUserId];

            Program.ReceiveEvent = new Thread(() => ReceiveData(ChatUser));
            Program.ReceiveEvent.Start();

            Stream = Program.clients[ChatUser.ChatUserId].GetStream();
            Writer = new StreamWriter(Stream)
            {
                AutoFlush = true
            };
            Reader = new StreamReader(Stream);

            ChatUser.Message = SendMessage;
            ChatUser.CommandInter = (int)ChatUser.Command.SendMessage;

            Input = JsonConvert.SerializeObject(ChatUser);
            Writer.WriteLine(Input);
        }

        public ActionResult ReceiveData(ChatUser actualUser)
        {
            Stream = Program.clients[actualUser.ChatUserId].GetStream();
            Reader = new StreamReader(Stream);

            while (true)
            {
                if (Program.clients[ChatUser.ChatUserId].Available > 0)
                {
                    Input = Reader.ReadLine();

                    dynamic objClass = JsonConvert.DeserializeObject<ChatUser>(Input);

                    if (objClass is ChatUser)
                    {
                        ChatUser userSender = JsonConvert.DeserializeObject<ChatUser>(Input);
                        switch (userSender.CommandInter)
                        {
                            case (int)ChatUser.Command.AddChatUser:
                                Program.userMsgBox[userSender.ChatUserId] += userSender.User.UserName + " entrou na sala!<br/>";
                                break;

                            case (int)ChatUser.Command.DeleteChatUser:
                                Program.clients.Remove(userSender.ChatUserId);
                                Program.userMsgBox.Remove(userSender.ChatUserId);
                                Program.userMsgBox[userSender.ChatUserId] += userSender.User.UserName + " saiu da sala!<br/>";
                                _dbContext.Remove(userSender);
                                _dbContext.SaveChanges();
                                RedirectToPage("./Index");
                                break;

                            case (int)ChatUser.Command.SendMessage:
                                if (userSender.UserId == actualUser.UserId)
                                    Program.userMsgBox[actualUser.ChatUserId] += "<div class='answer right'>";
                                else
                                    Program.userMsgBox[actualUser.ChatUserId] += "<div class='answer left'>";

                                Program.userMsgBox[actualUser.ChatUserId] += "<div class='avatar'>" +
                                                            "<img src = '" + userSender.User.Avatar + "' alt='User name'>" +
                                                            "<div class='status " + userSender.User.Status + "'></div>" +
                                                        "</div>" +
                                                            "<div class='text'>" +
                                                                "<div class='media-heading'>" + userSender.User.UserName + "</div>" +
                                                                userSender.Message +
                                                                "<p class='speech-time'>" +
                                                                    "<i class='fa fa-clock-o fa-fw'></i> " + DateTime.Now.ToString("HH:mm:ss") +
                                                                "</p>" +
                                                            "</div>" +
                                                    "</div>";
                                break;
                        }
                        MsgHtml = Program.userMsgBox[actualUser.ChatUserId];
                        return RedirectToPage();
                    }
                }
            }
        }
    }
}