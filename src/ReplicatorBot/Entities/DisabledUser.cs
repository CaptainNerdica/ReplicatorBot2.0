using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot
{
	public class DisabledUser
	{
		public ulong GuildId { get; set; }
		public ulong UserId { get; set; }

		public DisabledUser(ulong guildId, ulong userId)
		{
			GuildId = guildId;
			UserId = userId;
		}

		public virtual GuildConfig? GuildConfig { get; set; }

		public static DisabledUser? Get(ReplicatorContext context, ulong guildId, ulong userId) => context.DisabledUsers.FirstOrDefault(d => d.GuildId == guildId && d.UserId == userId);
		public static ValueTask<DisabledUser?> GetAsync(ReplicatorContext context, ulong guildId, ulong userId) => context.DisabledUsers.AsAsyncEnumerable().FirstOrDefaultAsync(d => d.GuildId == guildId && d.UserId == userId);
		public static IQueryable<DisabledUser> GetAll(ReplicatorContext context, ulong guildId) => context.DisabledUsers.Where(d => d.GuildId == guildId);
		public static IAsyncEnumerable<DisabledUser> GetAllAsync(ReplicatorContext context, ulong guildId) => context.DisabledUsers.AsAsyncEnumerable().Where(d => d.GuildId == guildId);
		public static DisabledUser Add(ReplicatorContext context, DisabledUser user) => context.DisabledUsers.Add(user).Entity;
		public static DisabledUser Update(ReplicatorContext context, DisabledUser user) => context.DisabledUsers.Update(user).Entity;
		public static void Delete(ReplicatorContext context, ulong guildId, ulong userId) => context.DisabledUsers.Remove(Get(context, guildId, userId)!);
		public static void Delete(ReplicatorContext context, DisabledUser user) => context.DisabledUsers.Remove(user);
	}
}
