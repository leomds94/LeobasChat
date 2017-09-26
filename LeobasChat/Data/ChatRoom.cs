using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LeobasChat.Data
{
    public class ChatRoom
    {
        [Key]
        public int ChatRoomId { get; set; }
        public string Name { get; set; }
        public bool IsPublic { get; set; }
        public int UserLimit { get; set; }
        public List<ApplicationUser> ChatUsers { get; set; }
    }
}
