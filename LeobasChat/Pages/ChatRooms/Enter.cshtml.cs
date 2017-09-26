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
        }
        public static ChatUser ChatUser { get; set; }

        public static ChatRoom CurrentRoom { get; set; }

        public string MsgHtml { get; set; }


        public async void OnGetAsync(int id)
        {
            ChatUser = await _dbContext.ChatUsers
                .Include(s => s.User)
                .Include(r => r.Chat)
                .SingleOrDefaultAsync(u => u.UserId == _userManager.GetUserId(User) && u.ChatRoomId == id);

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

            CurrentRoom = _dbContext.ChatRooms.Find(id);

            ChatUser.CommandInter = (int)ChatUser.Command.AddChatUser;
            Input = JsonConvert.SerializeObject(ChatUser);
            Writer.WriteLine(Input);
        }

        public void OnPost(int id)
        {
        
            Stream = Program.clients[ChatUser.ChatUserId].GetStream();
            Writer = new StreamWriter(Stream)
            {
                AutoFlush = true
            };
            Reader = new StreamReader(Stream);

            ChatUser.Message = ChatBoxModel.msgToUser;
            ChatUser.CommandInter = (int)ChatUser.Command.SendMessage;

            Input = JsonConvert.SerializeObject(ChatUser);
            Writer.WriteLine(Input);
        }

        public async void ReceiveData(int id)
        {
            ChatUser = await _dbContext.ChatUsers
                .Include(s => s.User)
                .Include(r => r.Chat)
                .SingleOrDefaultAsync(u => u.UserId == _userManager.GetUserId(User) && u.ChatRoomId == id);

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
                                ChatBoxModel.MsgHtml = MsgHtml;
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