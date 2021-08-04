using Discord;
using DiscordBotCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot.SlashCommands
{
	[SlashCommand("ping")]
	public class PingCommand : SlashCommandBase, IGlobalSlashCommand
	{
		public override void BuildCommand(SlashCommandBuilder builder)
		{
			builder.WithName("ping")
				.WithDescription("Get the current latency to the bot");
		}

		public async Task ExecuteGlobalCommandAsync()
		{
			DateTimeOffset end = DateTimeOffset.UtcNow;
			DateTimeOffset start = Command.CreatedAt;
			int delay = (end - start).Milliseconds;
			await Command.RespondAsync($"Latency: {delay}ms", ephemeral: true);
		}
	}
}
