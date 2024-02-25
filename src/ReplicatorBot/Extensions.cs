using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot
{
	internal static class Extensions
	{
		public static LogLevel ToLogLevel(this LogSeverity log) => (LogLevel)(5 - (int)log);

		public static AllowedMentions GetAllowedMentions(this GuildConfig config)
		{
			if (!config.CanMention)
				return AllowedMentions.None;

			AllowedMentions mentions = new AllowedMentions(AllowedMentionTypes.Roles | AllowedMentionTypes.Users | AllowedMentionTypes.Everyone);

			mentions.UserIds.RemoveAll(id => config.DisabledUsers.Any(u => u.UserId == id));

			return mentions;
		}
	}
}