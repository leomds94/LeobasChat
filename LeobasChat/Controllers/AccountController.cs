using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LeobasChat.Data;
using Microsoft.EntityFrameworkCore;

namespace LeobasChat.Controllers
{
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _userdbContext;
        private readonly ILogger _logger;

        public enum StatusNorm
        {
            off = 0,
            offline,
            busy,
            online
        }

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext userdbContext, ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userdbContext = userdbContext;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);

            user.Status = "offline";

            var resultUser = await _userManager.UpdateAsync(user);

            try
            {
                await _userdbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {

            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToPage("/Account/Login");
        }
    }
}
