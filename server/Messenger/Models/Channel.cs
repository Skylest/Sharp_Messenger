using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Messenger.Models
{
    public class Channel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Actor Admin { get; set; }
        public ICollection<Message> Messages { get; set; }
    }
}
