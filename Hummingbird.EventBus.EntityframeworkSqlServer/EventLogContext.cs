using Hummingbird.EventBus.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hummingbird.EventBus.EntityframeworkSqlServer
{
    public class EventLogContext : DbContext
    {       
        public EventLogContext(DbContextOptions<EventLogContext> options) : base(options)
        {
        }

        public DbSet<EventLogEntry> EventLogs { get; set; }

        public DbSet<EventConsumeLogEntry> EventConsumeLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {          
            builder.Entity<EventLogEntry>(ConfigureEventLogEntry);
            builder.Entity<EventConsumeLogEntry>(ConfigureEventConsumeLogs);
        }

        void ConfigureEventLogEntry(EntityTypeBuilder<EventLogEntry> builder)
        {
            builder.ToTable("EventLogs");

            builder.HasKey(e => e.EventId);

            builder.Property(e => e.EventId)
                .IsRequired();

            builder.Property(e => e.Content)
                .IsRequired();

            builder.Property(e => e.CreationTime)
                .IsRequired();

            builder.Property(e => e.State)
                .IsRequired();

            builder.Property(e => e.TimesSent)
                .IsRequired();

            builder.Property(e => e.EventTypeName)
                .IsRequired();

        }

        void ConfigureEventConsumeLogs(EntityTypeBuilder<EventConsumeLogEntry> builder)
        {
            builder.ToTable("EventConsumeLogs");

            builder.HasKey(e => e.EventConsumeLogId);

            builder.Property(e => e.EventConsumeLogId)
                .IsRequired();

            builder.Property(e => e.EventId)
            .IsRequired();

            builder.Property(e => e.QueueName)
                .IsRequired();

            builder.Property(e => e.TimesConsume)
                .IsRequired();

            builder.Property(e => e.CreationTime)
                .IsRequired();

            builder.Property(e => e.State)
                .IsRequired();

            builder.Property(e => e.TimesConsume)
                .IsRequired();
        }

    }

    public class EventLogContextDesignFactory : IDesignTimeDbContextFactory<EventLogContext>
    {
        public EventLogContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<EventLogContext>()
                .UseSqlServer("Server=10.2.29.234;Database=HealthCloud.Services.PharmacyService;User Id=sa;Password=kmdb@2016");

            return new EventBus.EntityframeworkSqlServer.EventLogContext(optionsBuilder.Options);
        }
    }

}
