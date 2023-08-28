using AutoMapper;
using Messenger.Areas.Identity.Pages.Account;
using Messenger.Data;
using Messenger.Models;
using Messenger.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Messenger.Hubs
{

    public class ChatHub : Hub
    {
        public readonly static List<UserView> Actors = new List<UserView>();
        public readonly static List<ChannelViewModel> Channels = new List<ChannelViewModel>();
        private readonly static Dictionary<string, string> ActorsMap = new Dictionary<string, string>();

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ChatHub(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task Send(string channelName, string message, string username)
        {
            try
            {
                var user = _context.Users.Where(u => u.UserName == username).FirstOrDefault();
                var channel = _context.Channels.Where(r => r.Name == channelName).FirstOrDefault();

                if (!string.IsNullOrEmpty(message.Trim()))
                {
                    var msg = new Message()
                    {
                        Content = Regex.Replace(message, @"(?i)<(?!img|a|/a|/img).*?>", string.Empty),
                        FromUser = user,
                        ToChannel = channel,
                        Timestamp = DateTime.Now
                    };
                    _context.Messages.Add(msg);
                    _context.SaveChanges();

                    var MessageView = _mapper.Map<Message, MessageView>(msg);
                    await Clients.All.SendAsync("newMessage", MessageView, channelName);
                }
            }
            catch (Exception)
            {
                await Clients.Caller.SendAsync("on_error", "Message not send! Message should be 1-500 characters.");
            }
        }

        public async Task Join(string channelName, string username)
        {
            try
            {
                var user = Actors.Where(u => u.Username == username).FirstOrDefault();
                if (user != null && user.CurrentChannel != channelName)
                {
                    if (!string.IsNullOrEmpty(user.CurrentChannel))
                        await Clients.OthersInGroup(user.CurrentChannel).SendAsync("removeUser", user);

                    await Leave(user.CurrentChannel);
                    await Groups.AddToGroupAsync(Context.ConnectionId, channelName);
                    user.CurrentChannel = channelName;

                    await Clients.OthersInGroup(channelName).SendAsync("addUser", user);
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("onError", "You failed to join the chat channel!" + ex.Message);
            }
        }

        public async Task Leave(string channelName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, channelName);
        }

        public async Task CreateChannel(string channelName, string username)
        {
            try
            {

                Match match = Regex.Match(channelName, @"^\w+( \w+)*$");
                if (!match.Success)
                {
                    await Clients.Caller.SendAsync("onError", "Invalid channel name!\nChannel name must contain only letters and numbers.");
                }
                else if (channelName.Length < 5 || channelName.Length > 100)
                {
                    await Clients.Caller.SendAsync("onError", "Channel name must be between 5-100 characters!");
                }
                else if (_context.Channels.Any(r => r.Name == channelName))
                {
                    await Clients.Caller.SendAsync("onError", "Another chat channel with this name exists");
                }
                else
                {
                    var user = _context.Users.Where(u => u.UserName == username).FirstOrDefault();
                    var channel = new Channel()
                    {
                        Name = channelName,
                        Admin = user
                    };
                    _context.Channels.Add(channel);
                    _context.SaveChanges();

                    if (channel != null)
                    {
                        var channelViewModel = _mapper.Map<Channel, ChannelViewModel>(channel);
                        Channels.Add(channelViewModel);
                        await Clients.All.SendAsync("addChannel", channelViewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("onError", "Couldn't create chat channel: " + ex.Message);
            }
        }

        public async Task DeleteChannel(string channelName, string username)
        {
            try
            {
                var channel = _context.Channels.Include(r => r.Admin)
                    .Where(r => r.Name == channelName && r.Admin.UserName == username).FirstOrDefault();
                _context.Channels.Remove(channel);
                _context.SaveChanges();

                var channelViewModel = Channels.First(r => r.Name == channelName);
                Channels.Remove(channelViewModel);

                await Clients.All.SendAsync("removeChannel", channelViewModel, channelName);
            }
            catch (Exception)
            {
                await Clients.Caller.SendAsync("onError", "Can't delete this chat channel. Only owner can delete this channel.");
            }
        }

        public async Task getChannels(string username)
        {
            if (Channels.Count == 0)
            {
                foreach (var channel in _context.Channels)
                {
                    var channelViewModel = _mapper.Map<Channel, ChannelViewModel>(channel);
                    Channels.Add(channelViewModel);
                }
            }

            await Clients.Caller.SendAsync("channelsList", Channels.ToList());
            
        }

        public IEnumerable<UserView> GetUsers(string channelName)
        {
            return Actors.Where(u => u.CurrentChannel == channelName).ToList();
        }

        public async Task GetMessageHistory(string channelName)
        {
            var messageHistory = _context.Messages.Where(m => m.ToChannel.Name == channelName)
                    .Include(m => m.FromUser)
                    .Include(m => m.ToChannel)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(20)
                    .AsEnumerable()
                    .Reverse()
                    .ToList();

            await Clients.Caller.SendAsync("chatHistory", _mapper.Map<IEnumerable<Message>, IEnumerable<MessageView>>(messageHistory).ToList());
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public async Task Login(string username, string password)
        {
            try
            {
                var user = _context.Users.Where(u => u.UserName == username).FirstOrDefault();
                if (user != null)
                {
                    if (user.PasswordHash == password)
                    {
                        var userViewModel = _mapper.Map<Actor, UserView>(user);
                        userViewModel.CurrentChannel = "";
                        if (Actors.Any(u => u.Username == username))
                        {
                            Actors.Add(userViewModel);
                            ActorsMap.Add(username, Context.ConnectionId);
                        }
                        Clients.Caller.SendAsync("loginResult", user.Nickname, true);
                    }
                    else
                    {
                        Clients.Caller.SendAsync("loginResult", "Wrong password!",  false);
                    }
                }
                else
                {
                    Clients.Caller.SendAsync("loginResult", "Wrong login!", false);
                }
            }
            catch (Exception ex)
            {
                Clients.Caller.SendAsync("loginResult", "Wrong login!", false);
            }
        }

        public async Task Register(string username, string password, string nickname)
        {

            var user = _context.Users.Where(u => u.UserName == username).FirstOrDefault();
            if (user == null)
            {
                user = new Actor { UserName = username, Nickname = nickname, PasswordHash = password };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                try
                {
                    var userViewModel = _mapper.Map<Actor, UserView>(user);
                    userViewModel.CurrentChannel = "";

                    if (Actors.Any(u => u.Username == username))
                    {
                        Actors.Add(userViewModel);
                        ActorsMap.Add(username, Context.ConnectionId);
                    }
                    Clients.Caller.SendAsync("registerResult", user.Nickname, true);

                }
                catch (Exception ex)
                {
                    Clients.Caller.SendAsync("registerResult", "Error", false);
                }
            }
            else 
            {
                Clients.Caller.SendAsync("registerResult", "User with the same login is already exist", false);
            }
        }
    }
}
