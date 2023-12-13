using Discord;
using Discord.Interactions;
using DiscordBotCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot
{
	[Flags]
	public enum ChannelPermission
	{
		None = 0,
		Read = 1,
		Write = 2,
		[ChoiceDisplay("Read/Write")]
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

		public virtual GuildConfig? GuildConfig { get; set; }

		public static ChannelPermissions CreateDefault(ReplicatorContext context, ulong guildId, ulong channelId)
		{
			ChannelPermissions perms = new ChannelPermissions(guildId, channelId, ChannelPermission.ReadWrite);
			return context.ChannelPermissions.Add(perms).Entity;
		}

		public static ChannelPermissions? Get(ReplicatorContext context, ulong guildId, ulong channelId) => context.ChannelPermissions.FirstOrDefault(c => c.GuildId == guildId && c.ChannelId == channelId);
		public static ValueTask<ChannelPermissions?> GetAsync(ReplicatorContext context, ulong guildId, ulong channelId) => context.ChannelPermissions.AsAsyncEnumerable().FirstOrDefaultAsync(c => c.GuildId == guildId && c.ChannelId == channelId);
		public static IQueryable<ChannelPermissions> GetAll(ReplicatorContext context, ulong guildId) => context.ChannelPermissions.Where(c => c.GuildId == guildId);
		public static IAsyncEnumerable<ChannelPermissions> GetAllAsync(ReplicatorContext context, ulong guildId) => context.ChannelPermissions.AsAsyncEnumerable().Where(c => c.GuildId == guildId);
		public static ChannelPermissions Add(ReplicatorContext context, ChannelPermissions permissions) => context.ChannelPermissions.Add(permissions).Entity;
		public static ChannelPermissions Update(ReplicatorContext context, ChannelPermissions permissions) => context.ChannelPermissions.Update(permissions).Entity;
		public static void Delete(ReplicatorContext context, ChannelPermissions permissions) => context.ChannelPermissions.Remove(permissions);
		public static void Delete(ReplicatorContext context, ulong guildId, ulong channelId) => context.ChannelPermissions.Remove(Get(context,guildId, channelId)!);

	}
}
