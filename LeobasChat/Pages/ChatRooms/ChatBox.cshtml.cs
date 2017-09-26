using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LeobasChat.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LeobasChat.Pages.ChatRooms
{
    public class ChatBoxModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ChatDbContext _dbContext;
        private readonly ApplicationDbContext _userDbContext;

        [BindProperty]
        public static string MsgHtml { get; set; }

        [BindProperty]
        public string SendMessage { get; set; }

        [BindProperty]
        public string OnlineUsersHtml { get; set; }

        public ChatUser ChatUser { get; set; }
        [BindProperty]
        public ChatRoom CurrentRoom { get; set; }

        public static string msgToUser { get; set; }

        public ChatBoxModel() { }

        public ChatBoxModel(ChatDbContext dbContext, ApplicationDbContext userDbContext, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _userDbContext = userDbContext;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            CurrentRoom = EnterModel.CurrentRoom;

            if (CurrentRoom == null)
            {
                return RedirectToPage("./Index");
            }

            if (EnterModel.ChatUser == null)
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

            foreach (ChatUser user in _dbContext.ChatUsers.FromSql($@"
                                                                    SELECT *
                                                                    FROM ChatUsers
                                                                    WHERE ChatRoomId = {CurrentRoom.ChatRoomId}"))
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
            return Page();
        }
        public IActionResult OnPost(int id)
        {
            msgToUser = SendMessage;
            return Page();
        }
    }
}