using Discord;
using DiscordBotCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot
{
	public class ReplicatorContext : BotDbBase
	{
		private readonly string _connectionString = "DataSource=/data/application.db";
		private readonly DbProvider _provider = DbProvider.Sqlite;

		public ReplicatorContext() : base() { }
		public ReplicatorContext(string connectionString) : this(connectionString, DbProvider.Sqlite) { }
		public ReplicatorContext(string connectionString, DbProvider provider) : base()
		{
			_connectionString = connectionString;
			_provider = provider;
		}

		public DbSet<GuildConfig> GuildConfig { get; set; }
		public DbSet<ChannelPermissions> ChannelPermissions { get; set; }
		public DbSet<DisabledUser> DisabledUsers { get; set; }
		public DbSet<Message> Messages { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			ValueConverter<ulong, long> converter = new ValueConverter<ulong, long>
				(
				v => unchecked((long)v),
				v => unchecked((ulong)v));

			builder.Entity<GuildConfig>(entity =>
			{
				entity.HasKey(e => e.GuildId);

				entity.Property(e => e.TargetUserId)
					.HasConversion(converter)
					.ValueGeneratedNever();
				entity.HasOne(e => e.Guild)
					.WithOne()
					.HasForeignKey<Guild>(e => e.GuildId);
			});

			builder.Entity<ChannelPermissions>(entity =>
			{
				entity.HasKey(e => new { e.GuildId, e.ChannelId });

				entity.Property(e => e.GuildId)
					.HasConversion(converter)
					.ValueGeneratedNever();

				entity.Property(e => e.ChannelId)
					.HasConversion(converter)
					.ValueGeneratedNever();

				entity.Property(e => e.Permissions)
					.HasConversion(new EnumToNumberConverter<ChannelPermission, int>());

				entity.HasOne(e => e.GuildConfig)
					.WithMany(d => d.ChannelPermissions)
					.HasForeignKey(e => e.GuildId)
					.HasPrincipalKey(d => d.GuildId);

			});

			builder.Entity<DisabledUser>(entity =>
			{
				entity.HasKey(e => new { e.GuildId, e.UserId });

				entity.Property(e => e.GuildId)
					.HasConversion(converter)
					.ValueGeneratedNever();

				entity.Property(e => e.UserId)
					.HasConversion(converter)
					.ValueGeneratedNever();

				entity.HasOne(e => e.GuildConfig)
					.WithMany(d => d.DisabledUsers)
					.HasForeignKey(e => e.GuildId)
					.HasPrincipalKey(d => d.GuildId);
			});

			builder.Entity<Message>(entity =>
			{
				entity.HasKey(e => e.MessageId);

				entity.Property(e => e.MessageId)
					.HasConversion(converter)
					.ValueGeneratedNever();

				entity.Property(e => e.GuildId)
					.HasConversion(converter)
					.ValueGeneratedNever();

				entity.HasIndex(e => new { e.GuildId, e.Index }, "IX_Message_Guild_Index")
					.IsUnique();

				entity.Property(e => e.Text)
					.HasMaxLength(6144)
					.IsRequired();

				entity.Property(e => e.Type)
					.HasConversion(new EnumToNumberConverter<MessageType, int>())
					.IsRequired();

				entity.HasOne(e => e.GuildConfig)
					.WithMany(d => d.Messages)
					.HasForeignKey(e => e.GuildId)
					.HasPrincipalKey(d => d.GuildId);
			});
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				switch (_provider)
				{
					default:
					case DbProvider.Sqlite:
						optionsBuilder.UseSqlite(_connectionString);
						break;
					case DbProvider.SqlServer:
						optionsBuilder.UseSqlServer(_connectionString);
						break;
					case DbProvider.InMemory:
						optionsBuilder.UseInMemoryDatabase("application");
						break;
				}
				optionsBuilder.UseLazyLoadingProxies();
			}
		}
	}
}
