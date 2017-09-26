using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LeobasChat.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace LeobasChat.Pages.ChatRooms
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ChatDbContext _dbContext;

        public EditModel(ChatDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [BindProperty]
        public ChatRoom ChatRoom { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            ChatRoom = await _dbContext.ChatRooms.FindAsync(id);

            if (ChatRoom == null)
            {
                return RedirectToPage("ChatRoom");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _dbContext.Attach(ChatRoom).State = EntityState.Modified;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {

            }

            return RedirectToPage("./Index");
        }
    }
}