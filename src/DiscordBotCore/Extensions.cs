using Discord;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBotCore
{
	internal static class Extensions
	{
		public static LogLevel ToLogLevel(this LogSeverity log) => (LogLevel)(5 - (int)log);
	}
}