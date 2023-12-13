using Discord;
using DiscordBotCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot
{
	public class GuildConfig
	{
		public ulong GuildId { get; set; }
		public bool Enabled { get; set; }
		public ulong? TargetUserId { get; set; }
		public int GuildMessageCount { get; set; }
		public int TargetMessageCount { get; set; }
		public double Probability { get; set; }
		public bool AutoUpdateProbability { get; set; }
		public bool AutoUpdateMessages { get; set; }
		public bool CanMention { get; set; }
		public DateTime LastUpdate { get; set; }

		public GuildConfig(ulong guildId)
		{
			GuildId = guildId;
		}

		public GuildConfig(ulong guildId, bool enabled, ulong? targetUserId, int guildMessageCount, int targetMessageCount, double probability, bool autoUpdateProbability, bool canMention, DateTime lastUpdate)
		{
			GuildId = guildId;
			Enabled = enabled;
			TargetUserId = targetUserId;
			GuildMessageCount = guildMessageCount;
			TargetMessageCount = targetMessageCount;
			Probability = probability;
			AutoUpdateProbability = autoUpdateProbability;
			CanMention = canMention;
			LastUpdate = lastUpdate;
		}

		public AllowedMentions AllowedMentions => CanMention ? AllowedMentions.All : AllowedMentions.None;

		public virtual Guild? Guild { get; set; }
		public virtual ICollection<ChannelPermissions> ChannelPermissions { get; set; } = new HashSet<ChannelPermissions>();
		public virtual ICollection<DisabledUser> DisabledUsers { get; set; } = new HashSet<DisabledUser>();
		public virtual ICollection<Message> Messages { get; set; } = new HashSet<Message>();

		public static GuildConfig? Get(ReplicatorContext context, ulong id) => context.GuildConfig.FirstOrDefault(g => g.GuildId == id);
		public static ValueTask<GuildConfig?> GetAsync(ReplicatorContext context, ulong id) => context.GuildConfig.AsAsyncEnumerable().FirstOrDefaultAsync(g => g.GuildId == id);
		public static GuildConfig Add(ReplicatorContext context, GuildConfig config) => context.GuildConfig.Add(config).Entity;
		public static GuildConfig Update(ReplicatorContext context, GuildConfig config) => context.GuildConfig.Update(config).Entity;
		public static void Delete(ReplicatorContext context, ulong id) => Delete(context, Get(context, id)!);
		public static void Delete(ReplicatorContext context, GuildConfig config) => context.GuildConfig.Remove(config);
	}
}
