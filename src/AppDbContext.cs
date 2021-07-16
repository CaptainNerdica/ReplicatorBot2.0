using Discord;
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
	public enum DbProvider
	{
		Sqlite,
		SqlServer,
		InMemory
	}
	public class AppDbContext : DbContext
	{
		private readonly string _connectionString = "DataSource=/data/application.db";
		private readonly DbProvider _provider = DbProvider.Sqlite;

		/// <summary>
		/// Creates a new database context.
		/// </summary>
		public AppDbContext() : base() { }
		/// <summary>
		/// Creates a new database context.
		/// </summary>
		/// <remarks>Database provider defaults to Sqlite</remarks>
		/// <param name="connectionString">Connection string for this context</param>
		public AppDbContext(string connectionString) : this(connectionString, DbProvider.Sqlite) { }
		/// <summary>
		/// Creates a new database context.
		/// </summary>
		/// <param name="connectionString">Connection string for this context.</param>
		/// <param name="provider">Database provider to use.</param>
		public AppDbContext(string connectionString, DbProvider provider) : base()
		{
			_connectionString = connectionString;
			_provider = provider;
		}

		public DbSet<GuildInfo> GuildInfo { get; set; }
		public DbSet<ChannelPermissions> ChannelPermissions { get; set; }
		public DbSet<DisabledSubstring> DisabledSubstrings { get; set; }
		public DbSet<DisabledUser> DisabledUsers { get; set; }
		public DbSet<Message> Messages { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			ValueConverter<ulong, long> converter = new ValueConverter<ulong, long>
				(
				v => unchecked((long)v),
				v => unchecked((ulong)v));

			builder.Entity<GuildInfo>(entity =>
			{
				entity.HasKey(e => e.GuildId);

				entity.Property(e => e.GuildId)
					.HasConversion(converter)
					.ValueGeneratedNever();

				entity.Property(e => e.Prefix)
					.HasMaxLength(10)
					.IsRequired();

				entity.Property(e => e.TargetUserId)
					.HasConversion(converter)
					.ValueGeneratedNever();
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

				entity.HasOne(e => e.GuildInfo)
					.WithMany(d => d.ChannelPermissions)
					.HasForeignKey(e => e.GuildId)
					.HasPrincipalKey(d => d.GuildId);

			});

			builder.Entity<DisabledSubstring>(entity =>
			{
				entity.HasKey(e => new { e.GuildId, e.Index });

				entity.Property(e => e.GuildId)
					.HasConversion(converter)
					.ValueGeneratedNever();

				entity.Property(e => e.Substring)
					.HasMaxLength(200)
					.IsRequired();

				entity.HasOne(e => e.GuildInfo)
					.WithMany(d => d.DisabledSubstrings)
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

				entity.HasOne(e => e.GuildInfo)
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
					.HasMaxLength(2048)
					.IsRequired();

				entity.HasOne(e => e.GuildInfo)
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

	public class GuildInfo
	{
		public ulong GuildId { get; set; }
		public bool Enabled { get; set; }
		public string Prefix { get; set; }
		public ulong? TargetUserId { get; set; }
		public int GuildMessageCount { get; set; }
		public int TargetMessageCount { get; set; }
		public double Probability { get; set; }
		public bool AutoUpdateProbability { get; set; }
		public bool AutoUpdateMessages { get; set; }
		public bool CanMention { get; set; }
		public bool CanEmbed { get; set; }
		public int Delay { get; set; }
		public bool FixedDelay { get; set; }
		public DateTime LastUpdate { get; set; }

		public GuildInfo() { }

		public GuildInfo(ulong guildId)
		{
			GuildId = guildId;
			Prefix = "!";
		}

		public GuildInfo(ulong guildId, bool enabled, string prefix, ulong? targetUserId, int guildMessageCount, int targetMessageCount, double probability, bool autoUpdateProbability, bool canMention, bool canEmbed, int delay, bool fixedDelay, DateTime lastUpdate)
		{
			GuildId = guildId;
			Enabled = enabled;
			Prefix = prefix;
			TargetUserId = targetUserId;
			GuildMessageCount = guildMessageCount;
			TargetMessageCount = targetMessageCount;
			Probability = probability;
			AutoUpdateProbability = autoUpdateProbability;
			CanMention = canMention;
			CanEmbed = canEmbed;
			Delay = delay;
			FixedDelay = fixedDelay;
			LastUpdate = lastUpdate;
		}

		public virtual ICollection<ChannelPermissions> ChannelPermissions { get; set; } = new HashSet<ChannelPermissions>();
		public virtual ICollection<DisabledUser> DisabledUsers { get; set; } = new HashSet<DisabledUser>();
		public virtual ICollection<DisabledSubstring> DisabledSubstrings { get; set; } = new HashSet<DisabledSubstring>();
		public virtual ICollection<Message> Messages { get; set; } = new HashSet<Message>();
	}

	[Flags]
	public enum ChannelPermission
	{
		Read = 1,
		Write = 2,
		ReadWrite = Read | Write
	}

	public class ChannelPermissions
	{
		public ulong GuildId { get; set; }
		public ulong ChannelId { get; set; }
		public ChannelPermission Permissions { get; set; }

		public ChannelPermissions(ulong guildId, ulong channelId, ChannelPermission permissions)
		{
			GuildId = guildId;
			ChannelId = channelId;
			Permissions = permissions;
		}

		public virtual GuildInfo GuildInfo { get; set; }
	}

	public class DisabledUser
	{
		public ulong GuildId { get; set; }
		public ulong UserId { get; set; }

		public DisabledUser(ulong guildId, ulong userId)
		{
			GuildId = guildId;
			UserId = userId;
		}

		public virtual GuildInfo GuildInfo { get; set; }
	}

	public class DisabledSubstring
	{
		public ulong GuildId { get; set; }
		public int Index { get; set; }
		public string Substring { get; set; }

		public DisabledSubstring(ulong guildId, int index, string substring)
		{
			GuildId = guildId;
			Index = index;
			Substring = substring;
		}

		public virtual GuildInfo GuildInfo { get; set; }
	}

	public class Message
	{
		public ulong GuildId { get; set; }
		public ulong MessageId { get; set; }
		public int Index { get; set; }
		public string Text { get; set; }

		public Message(ulong guildId, ulong messageId, int index, string text)
		{
			GuildId = guildId;
			MessageId = messageId;
			Index = index;
			Text = text;
		}

		public virtual GuildInfo GuildInfo { get; set; }
	}
}
