using System;
using System.Collections.Generic;

namespace VentyTime.Shared.Models
{
    public class Conversation
    {
        public int Id { get; set; }
        public User? OtherUser { get; set; }
        public Message? LastMessage { get; set; }
        public int UnreadCount { get; set; }
        public List<Message> Messages { get; set; } = new();
    }

    public class Message
    {
        public int Id { get; set; }
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public bool IsFromCurrentUser { get; set; }
        public int SenderId { get; set; }
        public int ConversationId { get; set; }
    }
}
