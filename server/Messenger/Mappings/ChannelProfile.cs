using AutoMapper;
using Messenger.Models;
using Messenger.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Messenger.Mappings
{
    public class channelProfile : Profile
    {
        public channelProfile()
        {
            CreateMap<Channel, ChannelViewModel>();
            CreateMap<ChannelViewModel, Channel>();
        }
    }
}
