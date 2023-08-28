using AutoMapper;
using Messenger.Models;
using Messenger.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Messenger.Mappings
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<Actor, UserView>()
                .ForMember(dst => dst.Username, opt => opt.MapFrom(x => x.UserName));
            CreateMap<UserView, Actor>();
        }
    }
}
