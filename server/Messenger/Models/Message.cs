using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Messenger.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public Actor FromUser { get; set; }
        public int ToChannelId { get; set; }
        public Channel ToChannel { get; set; }
    }
}
