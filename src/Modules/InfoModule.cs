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

	public class InfoModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; init; }
		public DiscordSocketClient Client { get; init; }
		public ILogger<InfoModule> Logger { get; init; }
		public IServiceProvider Services { get; init; }

		[Command("reply")]
		[Summary("Send a random stored message.")]
		public async Task SendRandomMessageAsync()
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			await Replicator.SendRandomMessageAsync(new Random(), Context.Channel, context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id));
		}

		[Command("ping")]
		[Summary("Pings the bot.")]
		public async Task PingAsync()
		{
			await ReplyAsync("pong!");
			Logger.LogInformation("Pinged");
			await Task.CompletedTask;
		}

		[Command("echo")]
		[Summary("Echos a message back.")]
		public async Task EchoAsync([Remainder][Summary("The message to echo back.")] string message)
		{
			Logger.LogInformation($"Echoed {message}");
			await ReplyAsync('\u200B' + message);
		}

		[Command("help")]
		public async Task Help()
		{
			IEnumerable<CommandInfo> commands = Commands.Commands;
			EmbedBuilder embedBuilder = new EmbedBuilder();

			foreach (CommandInfo command in commands)
			{
				// Get the command Summary attribute information
				string embedFieldText = command.Summary ?? "No description available\n";
				string name;
				if (command?.Module?.Group is not null)
					name = $"{command?.Module?.Group} - {command.Name}";
				else
					name = command.Name;
				embedBuilder.AddField(name, embedFieldText);
			}

			await ReplyAsync("Here's a list of commands and their description: ", false, embedBuilder.Build());
		}

		protected override void AfterExecute(CommandInfo info) => Logger.LogInformation("Executed Command \"{command}\" in {module}", info.Name, nameof(InfoModule));
	}
}
