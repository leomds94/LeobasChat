using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LeobasChat.Data
{
    public class ChatUser
    {
        [Key]
        public int ChatUserId { get; set; }
        public bool IsAdmin { get; set; }
        public string Message { get; set; }
        public int CommandInter { get; set; }

        public enum Command
        {
            AddChatUser = 0,
            DeleteChatUser,
            SendMessage
        }

        [ForeignKey("User")]
        public string UserId { get; set; }
        public int ChatRoomId { get; set; }

        public ApplicationUser User { get; set; }
        public ChatRoom Chat { get; set; }
    }
}
