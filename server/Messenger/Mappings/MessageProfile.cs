using AutoMapper;
using Messenger.Models;
using Messenger.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Messenger.Mappings
{
    public class MessageProfile : Profile
    {
        public MessageProfile()
        {
            CreateMap<Message, MessageView>()
                .ForMember(dst => dst.From, opt => opt.MapFrom(x => x.FromUser.Nickname))
                .ForMember(dst => dst.To, opt => opt.MapFrom(x => x.ToChannel.Name))
                .ForMember(dst => dst.Timestamp, opt => opt.MapFrom(x => x.Timestamp));
            CreateMap<MessageView, Message>();
        }
    }
}
