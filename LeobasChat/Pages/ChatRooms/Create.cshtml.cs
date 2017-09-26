using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LeobasChat.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace LeobasChat.Pages.ChatRooms
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ChatDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ChatDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        [BindProperty]
        public ChatRoom ChatRoom { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            ChatUser chatAdmin = new ChatUser()
            {
                User = await _userManager.GetUserAsync(User),
                Chat = ChatRoom,
                IsAdmin = true
            };
            _dbContext.ChatRooms.Add(ChatRoom);
            _dbContext.ChatUsers.Add(chatAdmin);
            await _dbContext.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}