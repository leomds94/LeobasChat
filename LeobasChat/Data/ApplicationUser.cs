using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace LeobasChat.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public string Status { get; set; }

        public string Avatar { get; set; }

        public string Mood { get; set; }

        public List<ApplicationUser> ChatUsers { get; set; }
    }
}
