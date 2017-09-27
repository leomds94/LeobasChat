using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LeobasChat.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace LeobasChat.Pages.ChatRooms
{
    [Authorize]
    public class IndexModel : PageModel
    {
        public readonly ChatDbContext _dbContext;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _userDbContext;

        public IndexModel(ChatDbContext dbContext, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext userdbContext)
        {
            _dbContext = dbContext;
            _signInManager = signInManager;
            _userManager = userManager;
            _userDbContext = userdbContext;

        }

        public List<ChatRoom> ChatRooms { get; private set; }

        [BindProperty]
        public ChatUser UserAdmin { get; set; }

        public ApplicationUser user { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            user = await _userManager.GetUserAsync(User);
            user.Avatar = "https://bootdey.com/img/Content/avatar/avatar6.png";
            user.Status = "online";
            var resultUser = await _userManager.UpdateAsync(user);
            await _userDbContext.SaveChangesAsync();

            ChatRooms = await _dbContext.ChatRooms.ToListAsync();
            UserAdmin = await _dbContext.ChatUsers.Include(s => s.User).SingleOrDefaultAsync(u => u.UserId == _userManager.GetUserId(User));

            //user = await _userManager.GetUserAsync(User);

            //var store = new UserStore<ApplicationUser>(_userDbContext);

            //user.Status = "online";
            //user.Avatar = "https://bootdey.com/img/Content/avatar/avatar1.png";
            //user.Mood = "Olá, estou usando Leobas Chat!";

            //await _userManager.UpdateAsync(user).ConfigureAwait(false);

            //store.Context.SaveChanges();

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var chatRoom = await _dbContext.ChatRooms.FindAsync(id);

            if (chatRoom != null)
            {
                _dbContext.ChatRooms.Remove(chatRoom);
                await _dbContext.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}