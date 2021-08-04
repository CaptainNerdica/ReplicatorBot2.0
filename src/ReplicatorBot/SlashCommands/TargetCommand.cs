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
	[SlashCommand("target")]
	public class TargetCommand : SlashCommandBase, IGuildSlashCommand
	{
		protected DiscordSocketClient Client { get; init; }
		protected ReplicatorContext ReplicatorContext { get; init; }

		public TargetCommand(DiscordSocketClient client, ReplicatorContext context)
		{
			Client = client;
			ReplicatorContext = context;
		}

		public override void BuildCommand(SlashCommandBuilder builder)
		{
			builder.WithName("target")
				.WithDescription("Set the target user to replicate");

			builder.AddOption("get", ApplicationCommandOptionType.SubCommand, "Get the current target user");

			SlashCommandOptionBuilder setOptions = new SlashCommandOptionBuilder().AddOption("user", ApplicationCommandOptionType.User, "The user to set", true);
			builder.AddOption("set", ApplicationCommandOptionType.SubCommand, "Set the current target user", options: setOptions.Options);
		}

		public async Task ExecuteGuildCommandAsync(ulong guildId)
		{
			string sub = Command.Data.Options.First().Name;
			await (sub switch
			{
				"get" => ExecuteGetAsync(guildId),
				"set" => ExecuteSetAsync(guildId, Command.Data.Options.Skip(1)),
				_ => Task.CompletedTask
			});
		}

		private async Task ExecuteGetAsync(ulong guildId)
		{
			GuildConfig config = await GuildConfig.GetAsync(ReplicatorContext, guildId);
			ulong? userId = config.TargetUserId;
			if (userId is null)
				await Command.RespondAsync("Current target: None");
			else
			{
				SocketUser user = await Client.GetUserAsync((ulong)userId) as SocketUser;
				await Command.RespondAsync($"Current target: {user.Mention}", allowedMentions: AllowedMentions.None);
			}
		}
		private async Task ExecuteSetAsync(ulong guildId, IEnumerable<SocketSlashCommandDataOption> options)
		{
			GuildConfig config = await GuildConfig.GetAsync(ReplicatorContext, guildId);
			IUser user = options.First().Value as IUser;
			config.TargetUserId = user.Id;
			GuildConfig.Update(ReplicatorContext, config);
			await ReplicatorContext.SaveChangesAsync();
			await Command.RespondAsync($"Set targeted user to: {user.Mention}", allowedMentions: AllowedMentions.None);
		}
	}
}
