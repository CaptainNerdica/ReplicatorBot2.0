using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot.Modules
{
	[Group("read")]
	public class ReadModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; init; }
		public DiscordSocketClient Client { get; init; }
		public ILogger<ReadModule> Logger { get; init; }
		public IServiceProvider Services { get; init; }

		[Command("update", RunMode = RunMode.Async)]
		[Summary("Reads all new messages in the server")]
		public async Task UpdateAsync()
		{
			using IServiceScope scope = Services.CreateScope();
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();

			GuildConfig config = context.GuildConfig.FirstOrDefault(g => g.GuildId == Context.Guild.Id);

			await Replicator.ReadSinceTimestamp(context, Logger, Client, Context.Guild, config.LastUpdate, Context.Channel);
		}

		[Command("all", RunMode = RunMode.Async)]
		[Summary("Read all messages on the server")]
		public async Task ReadAllAsync(int max = 1000000)
		{
			using IServiceScope scope = Services.CreateScope();
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();

			await Replicator.ReadAllMessages(context, Logger, Client, Context.Guild, max, Context.Channel);
		}

		protected override void AfterExecute(CommandInfo info) => Logger.LogInformation("Executed Command \"{command}\" in {module}", info.Name, nameof(ReadModule));
	}
}
