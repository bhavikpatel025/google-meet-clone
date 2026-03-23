using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VideoCallApp.Domain.Entities;

namespace VideoCallApp.Persistence.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Meeting> Meetings { get; set; }
    public DbSet<MeetingParticipant> MeetingParticipants { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Meeting configuration
        builder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MeetingCode)
                .IsRequired()
                .HasMaxLength(8);

            entity.HasIndex(e => e.MeetingCode)
                .IsUnique();

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.HasOne(e => e.Host)
                .WithMany(u => u.HostedMeetings)
                .HasForeignKey(e => e.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Participants)
                .WithOne(p => p.Meeting)
                .HasForeignKey(p => p.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ChatMessages)
                .WithOne(c => c.Meeting)
                .HasForeignKey(c => c.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MeetingParticipant configuration
        builder.Entity<MeetingParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.User)
                .WithMany(u => u.MeetingParticipants)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(e => e.ConnectionId)
                .HasMaxLength(100);
        });

        // ChatMessage configuration
        builder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(2000);

            entity.HasOne(e => e.Sender)
                .WithMany(u => u.ChatMessages)
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // User configuration
        builder.Entity<User>(entity =>
        {
            entity.Property(e => e.FirstName)
                .HasMaxLength(50);

            entity.Property(e => e.LastName)
                .HasMaxLength(50);

            entity.Property(e => e.ProfilePictureUrl)
                .HasMaxLength(500);
        });
    }
}