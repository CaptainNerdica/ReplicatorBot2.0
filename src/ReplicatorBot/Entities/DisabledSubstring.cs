using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot
{
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

		public virtual GuildConfig GuildConfig { get; set; }

		public static DisabledSubstring Get(ReplicatorContext context, ulong guildId, string substring) => context.DisabledSubstrings.FirstOrDefault(d => d.GuildId == guildId && d.Substring == substring);
		public static DisabledSubstring Get(ReplicatorContext context, ulong guildId, int index) => context.DisabledSubstrings.FirstOrDefault(d => d.GuildId == guildId && d.Index == index);
		public static IQueryable<DisabledSubstring> GetAll(ReplicatorContext context, ulong guildId) => context.DisabledSubstrings.Where(d => d.GuildId == guildId);
		public static DisabledSubstring Add(ReplicatorContext context, DisabledSubstring disabledSubstring) => context.DisabledSubstrings.Add(disabledSubstring).Entity;
		public static DisabledSubstring Update(ReplicatorContext context, DisabledSubstring disabledSubstring) => context.DisabledSubstrings.Update(disabledSubstring).Entity;
		public static void Delete(ReplicatorContext context, DisabledSubstring disabledSubstring) => context.DisabledSubstrings.Remove(disabledSubstring);
		public static void Delete(ReplicatorContext context, ulong guildId, string substring) => context.DisabledSubstrings.Remove(Get(context, guildId, substring));
		public static void Delete(ReplicatorContext context, ulong guildId, int index) => context.DisabledSubstrings.Remove(Get(context, guildId, index));
	}
}
