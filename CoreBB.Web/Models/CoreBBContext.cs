using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CoreBB.Web.Models
{
    public partial class CoreBBContext : DbContext
    {
        public virtual DbSet<Forum> Forum { get; set; }
        public virtual DbSet<Message> Message { get; set; }
        public virtual DbSet<Topic> Topic { get; set; }
        public virtual DbSet<User> User { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var builder = new ConfigurationBuilder()
                                  .SetBasePath(Directory.GetCurrentDirectory())
                                  .AddJsonFile("appsettings.json");
                var configuration = builder.Build();
                optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
                optionsBuilder.EnableSensitiveDataLogging();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Forum>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.OwnerId).HasColumnName("OwnerID");

                entity.HasOne(d => d.Owner)
                    .WithMany(p => p.Forum)
                    .HasForeignKey(d => d.OwnerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("Fk_OwnerID_User");
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Content).HasMaxLength(1000);

                entity.Property(e => e.FromUserId).HasColumnName("FromUserID");

                entity.Property(e => e.SendDateTime).HasDefaultValueSql("(getdate())");

                entity.Property(e => e.Title).HasMaxLength(100);

                entity.Property(e => e.ToUserId).HasColumnName("ToUserID");

                entity.HasOne(d => d.FromUser)
                    .WithMany(p => p.MessageFromUser)
                    .HasForeignKey(d => d.FromUserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_FromUserID_User");

                entity.HasOne(d => d.ToUser)
                    .WithMany(p => p.MessageToUser)
                    .HasForeignKey(d => d.ToUserId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_ToUserID_User");
            });

            modelBuilder.Entity<Topic>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(e => e.ForumId).HasColumnName("ForumID");

                entity.Property(e => e.ModifiedByUserId).HasColumnName("ModifiedByUserID");

                entity.Property(e => e.OwnerId).HasColumnName("OwnerID");

                entity.Property(e => e.PostDateTime).HasDefaultValueSql("(getdate())");

                entity.Property(e => e.ReplyToTopicId).HasColumnName("ReplyToTopicID");

                entity.Property(e => e.RootTopicId).HasColumnName("RootTopicID");

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(d => d.Forum)
                    .WithMany(p => p.Topic)
                    .HasForeignKey(d => d.ForumId)
                    .HasConstraintName("FK__Topic__ForumID__31EC6D26");

                entity.HasOne(d => d.ModifiedByUser)
                    .WithMany(p => p.TopicModifiedByUser)
                    .HasForeignKey(d => d.ModifiedByUserId)
                    .HasConstraintName("FK__Topic__ModifiedB__35BCFE0A");

                entity.HasOne(d => d.Owner)
                    .WithMany(p => p.TopicOwner)
                    .HasForeignKey(d => d.OwnerId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Topic__OwnerID__30F848ED");

                entity.HasOne(d => d.ReplyToTopic)
                    .WithMany(p => p.InverseReplyToTopic)
                    .HasForeignKey(d => d.ReplyToTopicId)
                    .HasConstraintName("FK__Topic__ReplyToTo__48CFD27E");

                entity.HasOne(d => d.RootTopic)
                    .WithMany(p => p.InverseRootTopic)
                    .HasForeignKey(d => d.RootTopicId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK__Topic__RootTopic__32E0915F");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Description).HasMaxLength(1000);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(255);
            });
        }
    }
}
