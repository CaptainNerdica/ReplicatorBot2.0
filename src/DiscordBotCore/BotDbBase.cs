using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBotCore
{
	public enum DbProvider
	{
		Sqlite,
		SqlServer,
		InMemory
	}
	public abstract class BotDbBase : DbContext
	{
		private readonly string _connectionString = "DataSource=/data/application.db";
		private readonly DbProvider _provider = DbProvider.Sqlite;

		/// <summary>
		/// Creates a new database context.
		/// </summary>
		public BotDbBase() : base() { }
		/// <summary>
		/// Creates a new database context.
		/// </summary>
		/// <remarks>Database provider defaults to Sqlite</remarks>
		/// <param name="connectionString">Connection string for this context</param>
		public BotDbBase(string connectionString) : this(connectionString, DbProvider.Sqlite) { }
		/// <summary>
		/// Creates a new database context.
		/// </summary>
		/// <param name="connectionString">Connection string for this context.</param>
		/// <param name="provider">Database provider to use.</param>
		public BotDbBase(string connectionString, DbProvider provider) : base()
		{
			_connectionString = connectionString;
			_provider = provider;
		}

		public DbSet<Guild> Guild { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			ValueConverter<ulong, long> converter = new ValueConverter<ulong, long>
				(
				v => unchecked((long)v),
				v => unchecked((ulong)v));

			builder.Entity<Guild>(entity =>
			{
				entity.HasKey(e => e.GuildId);

				entity.Property(e => e.GuildId)
					.HasConversion(converter)
					.ValueGeneratedNever();

				entity.Property(e => e.Prefix)
					.HasMaxLength(10)
					.IsRequired();
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

	public class Guild
	{
		public ulong GuildId { get; set; }
		public string Prefix { get; set; }
		public Guild() { }

		public Guild(ulong guildId)
		{
			GuildId = guildId;
			Prefix = "!";
		}
	}
}
