using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using Messenger.Data;
using Messenger.Hubs;
using Messenger.Models;
using Messenger.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;

namespace Messenger.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly int FileSizeLimit;
        private readonly string[] AllowedExtensions;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment;
        private readonly IHubContext<ChatHub> _hubContext;

        public UploadController(ApplicationDbContext context,
            IMapper mapper,
            IWebHostEnvironment environment,
            IHubContext<ChatHub> hubContext,
            IConfiguration configruation)
        {
            _context = context;
            _mapper = mapper;
            _environment = environment;
            _hubContext = hubContext;

            FileSizeLimit = configruation.GetSection("FileUpload").GetValue<int>("FileSizeLimit");
            AllowedExtensions = configruation.GetSection("FileUpload").GetValue<string>("AllowedExtensions").Split(",");
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] UploadViewModel FileView)
        {
            if (ModelState.IsValid)
            {
                if (!Validate(FileView.File))
                {
                    return BadRequest("Bad file");
                }

                var fileName = DateTime.Now.ToString("yyyymmddMMss") + "_" + Path.GetFileName(FileView.File.FileName);
                var folderPath = Path.Combine(_environment.WebRootPath, "uploads");

                var filePath = Path.Combine(folderPath, fileName);
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await FileView.File.CopyToAsync(fileStream);
                }

                var user = _context.Users.Where(u => u.UserName == User.Identity.Name).FirstOrDefault();
                if (user == null) {
                    return NotFound("Can't found user"); 
                }
                var channel = _context.Channels.Where(r => r.Id == FileView.ChannelId).FirstOrDefault();
                if (channel == null)
                {
                    return NotFound("Can't found channel");
                }

                string htmlImage = string.Format(
                    "<a href=\"/uploads/{0}\" target=\"_blank\">" +
                    "<img src=\"/uploads/{0}\" class=\"post-image\">" +
                    "</a>", fileName);

                var message = new Message()
                {
                    Content = Regex.Replace(htmlImage, @"(?i)<(?!img|a|/a|/img).*?>", string.Empty),
                    Timestamp = DateTime.Now,
                    FromUser = user,
                    ToChannel = channel
                };

                await _context.Messages.AddAsync(message);
                await _context.SaveChangesAsync();

                var MessageView = _mapper.Map<Message, MessageView>(message);
                await _hubContext.Clients.Group(channel.Name).SendAsync("newMessage", MessageView);

                return Ok("Uploaded");
            }

            return BadRequest("Failed to upload");
        }

        private bool Validate(IFormFile file)
        {
            if (file.Length > FileSizeLimit)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Any(s => s.Contains(extension)))
                return false;

            return true;
        }
    }
}
