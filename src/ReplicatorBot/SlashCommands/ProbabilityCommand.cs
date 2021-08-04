using Discord;
using Discord.WebSocket;
using DiscordBotCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot.SlashCommands
{
	[SlashCommand("probability")]
	public class ProbabilityCommand : SlashCommandBase, IGuildSlashCommand
	{
		protected ReplicatorContext ReplicatorContext { get; }
		protected DiscordSocketClient Client { get; }
		public ProbabilityCommand(ReplicatorContext context, DiscordSocketClient client)
		{
			ReplicatorContext = context;
			Client = client;
		}
		public override void BuildCommand(SlashCommandBuilder builder)
		{
			builder.WithName("probability")
				.WithDescription("Get or set the probability");

			builder.AddOption("get", ApplicationCommandOptionType.SubCommand, "Get the current probability", required: false);

			var setOptions = new SlashCommandOptionBuilder()
				.AddOption("probability", ApplicationCommandOptionType.String, "Value to set the probability to (eg. 50%)", required: true);
			builder.AddOption("set", ApplicationCommandOptionType.SubCommand, "Set the current probability", required: false, options: setOptions.Options);

			var autoOptions = new SlashCommandOptionBuilder()
				.AddOption("value", ApplicationCommandOptionType.Boolean, "Value to set auto update to", required: true);
			builder.AddOption("auto", ApplicationCommandOptionType.SubCommand, "Set probability to auto update or use a fixed value", required: false, options: autoOptions.Options);
		}

		public async Task ExecuteGuildCommandAsync(ulong guildId)
		{
			string sub = Command.Data.Options.First().Name;
			switch (sub)
			{
				case "get":
					await ExecuteGetAsync(guildId);
					break;
				case "set":
					await ExecuteSetAsync(guildId, Command.Data.Options.First().Options);
					break;
				case "auto":
					await ExecuteAutoAsync(guildId, Command.Data.Options.First().Options);
					break;
			}
		}

		private async Task ExecuteGetAsync(ulong guildId)
		{
			GuildConfig guild = await GuildConfig.GetAsync(ReplicatorContext, guildId);
			await Command.RespondAsync($"Auto Update: {guild.AutoUpdateProbability}, Probability: {guild.Probability:P1}");
		}
		private async Task ExecuteSetAsync(ulong guildId, IEnumerable<SocketSlashCommandDataOption> options)
		{
			string value = options.First().Value as string;
			if (double.TryParse(value.Substring(0, value.IndexOf('%')), out double v))
			{
				v /= 100;
				v = Math.Clamp(v, 0, 1);
				GuildConfig guild = await GuildConfig.GetAsync(ReplicatorContext, guildId);
				guild.Probability = v;
				guild.AutoUpdateProbability = false;
				GuildConfig.Update(ReplicatorContext, guild);
				await ReplicatorContext.SaveChangesAsync();
				await Command.RespondAsync($"Updated probability to: {guild.Probability:P1}");
			}
			else
				await Command.RespondAsync("Input was not in the correct format");
		}
		private async Task ExecuteAutoAsync(ulong guildId, IEnumerable<SocketSlashCommandDataOption> options)
		{
			bool value = (bool)options.First().Value;
			GuildConfig guild = await GuildConfig.GetAsync(ReplicatorContext, guildId);
			guild.AutoUpdateProbability = value;
			if (value)
				guild.Probability = (double)guild.TargetMessageCount / guild.GuildMessageCount;
			if (!double.IsFinite(guild.Probability))
				guild.Probability = 0;
			GuildConfig.Update(ReplicatorContext, guild);
			await ReplicatorContext.SaveChangesAsync();
			await Command.RespondAsync($"Set probability auto update to {value}");
		}
	}
}
