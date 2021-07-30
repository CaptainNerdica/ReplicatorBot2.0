using Discord.Commands;
using Discord.WebSocket;
using DiscordBotCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot.Modules
{
	[Group("prefix")]
	[Summary("Get or set the command prefix")]
	public class PrefixModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; init; }
		public DiscordSocketClient Client { get; init; }
		public ILogger<PrefixModule> Logger { get; init; }
		public IServiceProvider Services { get; init; }

		[Command("get")]
		[Summary("Get current command prefix")]
		public async Task GetPrefixAsync()
		{
			using IServiceScope scope = Services.CreateScope();
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();
			Guild guild = context.Guild.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			await ReplyAsync($"Current Prefix: \"{guild.Prefix}\"");
		}

		[Command("set")]
		[Summary("Set the command prefix")]
		public async Task SetPrefixAsync(string prefix)
		{
			using IServiceScope scope = Services.CreateScope();
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();
			Guild guild = context.Guild.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			if (prefix.Length > 10)
			{
				await ReplyAsync("Prefix cannot be longer than 10 characters.");
				return;
			}
			if (string.IsNullOrWhiteSpace(prefix))
			{
				await ReplyAsync("Prefix cannot be empty.");
				return;
			}
			guild.Prefix = prefix;
			context.Guild.Update(guild);
			context.SaveChanges();
			await ReplyAsync($"Updated prefix to \"{guild.Prefix}\"");
		}

		protected override void AfterExecute(CommandInfo info) => Logger.LogInformation("Executed Command \"{command}\" in {module}", info.Name, nameof(PrefixModule));
	}
}
