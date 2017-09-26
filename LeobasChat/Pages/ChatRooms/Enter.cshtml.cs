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

            MsgHtml = "";
            OnlineUsersHtml = "";
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
            var ChatUsers = _dbContext.ChatUsers
                .Include(s => s.User)
                .Include(r => r.Chat);

            foreach(ChatUser user in ChatUsers)
            {
                if(user.UserId == _userManager.GetUserId(User) && user.ChatRoomId == id)
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
                Thread = new Thread(new ThreadStart(() => ReceiveData(id)));
            }
                
            Stream = Program.clients[ChatUser.ChatUserId].GetStream();
            Writer = new StreamWriter(Stream)
            {
                AutoFlush = true
            };
            Reader = new StreamReader(Stream);

            foreach (ChatUser user in _dbContext.ChatUsers.Where(u => u.ChatRoomId == CurrentRoom.ChatRoomId))
            {
                OnlineUsersHtml += "<div class='user'>" +
                                        "<div class='avatar'>" +
                                            "<img src ='" + user.User.Avatar + "' alt='User name'>" +
                                            "<div class='status " + ChatUser.User.Status + "'></div>" +
                                            "</div>" +
                                        "<div class='name'>" + user.User.UserName + "</div>" +
                                        "<div class='mood'>" + user.User.Mood + "</div>" +
                                    "</div>";
            }

            ChatUser.CommandInter = (int)ChatUser.Command.AddChatUser;
            Input = JsonConvert.SerializeObject(ChatUser);
            Writer.WriteLine(Input);
        }

        public void OnPost(int id)
        {
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

        public void ReceiveData(int id)
        {
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

            Stream = Program.clients[ChatUser.ChatUserId].GetStream();
            Reader = new StreamReader(Stream);
            while (true)
            {
                if (Client.Available > 0)
                {
                    Input = Reader.ReadLine();

                    dynamic objClass = JsonConvert.DeserializeObject<ChatUser>(Input);

                    //if (objClass.Count == 7)
                    //{
                    //    ChatRoom = JsonConvert.DeserializeObject<ChatRoom>(Input);
                    //    switch (ChatRoom.CommandInter)
                    //    {
                    //        case (int)ChatRoom.Command.AddChat:
                    //            break;

                    //        case (int)ChatRoom.Command.DeleteChat:
                    //            break;
                    //    }
                    //}
                    if (objClass is ChatUser)
                    {
                        ChatUser = JsonConvert.DeserializeObject<ChatUser>(Input);
                        switch (ChatUser.CommandInter)
                        {
                            case (int)ChatUser.Command.AddChatUser:
                                break;

                            case (int)ChatUser.Command.DeleteChatUser:
                                Program.clients.Remove(ChatUser.ChatUserId);
                                break;

                            case (int)ChatUser.Command.SendMessage:
                                if (ChatUser.UserId == _userManager.GetUserId(User))
                                    MsgHtml += "<div class='answer right'>";
                                else
                                    MsgHtml += "<div class='answer left'>";

                                MsgHtml += "<div class='avatar'>" +
                                                            "<img src = '" + ChatUser.User.Avatar + "' alt='User name'>" +
                                                            "<div class='status " + ChatUser.User.Status + "'></div>" +
                                                        "</div>" +
                                                            "<div class='text'>" +
                                                                "<div class='media-heading'>" + ChatUser.User.UserName + "</div>" +
                                                                ChatUser.Message +
                                                                "<p class='speech-time'>" +
                                                                    "<i class='fa fa-clock-o fa-fw'></i> 09:27" +
                                                                "</p>" +
                                                            "</div>" +
                                                    "</div>";
                                break;
                        }
                    }
                    //else if (objClass.Count == 6)
                    //{
                    //    loggedUser = Serializer.Deserialize<LoggedUser>(Input);
                    //    switch (loggedUser.CommandInter)
                    //    {
                    //        case (int)LoggedUser.Command.ChangeAvatar:
                    //            break;

                    //        case (int)LoggedUser.Command.ChangeStatus:
                    //            break;

                    //        case (int)LoggedUser.Command.ChangeUserName:
                    //            break;
                    //    }
                    //}
                }
            }
        }
    }
}