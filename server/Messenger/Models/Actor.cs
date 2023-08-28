using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Messenger.Models
{
    public class Actor : IdentityUser
    {
        public string Nickname { get; set; }
        public ICollection<Channel> Channels { get; set; }
        public ICollection<Message> Messages { get; set; }
    }
}
