using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Messenger.ViewModels
{
    public class UploadViewModel
    {
        [Required]
        public int ChannelId { get; set; }
        [Required]
        public IFormFile File { get; set; }
    }
}
