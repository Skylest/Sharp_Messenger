using Messenger.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Messenger.Data.Configurations
{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Messages");

            builder.Property(s => s.Content).IsRequired().HasMaxLength(500);

            builder.HasOne(s => s.ToChannel)
                .WithMany(m => m.Messages)
                .HasForeignKey(s => s.ToChannelId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
