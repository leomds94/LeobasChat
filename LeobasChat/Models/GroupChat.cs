using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace LeobasChat.Models
{
    public class GroupChat
    {
        public string ChatName { get; set; }
        public List<IdentityUser> ChatUsers { get; set; }
        public bool IsPublic { get; set; }
    }
}
