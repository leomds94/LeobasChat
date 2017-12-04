using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LeobasChat.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using System.Text;

namespace LeobasChat.Pages.ChatRooms
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
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

            TripleDESCryptoServiceProvider TDES = new TripleDESCryptoServiceProvider();

            ChatUser chatAdmin = new ChatUser()
            {
                User = await _userManager.GetUserAsync(User),
                Chat = ChatRoom,
                DesKey = TDES.Key,
                IsAdmin = true
            };
            _dbContext.ChatRooms.Add(ChatRoom);
            _dbContext.ChatUsers.Add(chatAdmin);
            await _dbContext.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}