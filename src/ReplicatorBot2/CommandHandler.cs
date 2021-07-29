using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot
{
	public class CommandHandler
	{
		protected DiscordSocketClient Client { get; }
		protected CommandService Commands { get; }
		protected IServiceProvider Services { get; }
		protected ILogger<CommandHandler> Logger { get; }

		public CommandHandler(IServiceProvider services, DiscordSocketClient client, CommandService commands, ILogger<CommandHandler> logger)
		{
			Services = services;
			Client = client;
			Commands = commands;
			commands.Log += LogAsync;
			Logger = logger;
		}

		private async Task LogAsync(LogMessage message) => await Task.Run(() => Logger.Log(message.Severity.ToLogLevel(), "{0}", message));

		public async Task InstallCommandsAsync()
		{
			Client.MessageReceived += HandleCommandsAsync;
			await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), Services);
		}

		private async Task HandleCommandsAsync(SocketMessage messageParam)
		{
			using var scope = Services.CreateScope();
			using var appDb = scope.ServiceProvider.GetService<AppDbContext>();
			// Don't process the command if it was a system message
			if (messageParam is not SocketUserMessage message) return;
			if ((message.Author as IGuildUser)?.Guild is not SocketGuild guild)
				return;
			// Create a number to track where the prefix ends and the command begins
			int argPos = 0;
			GuildInfo info = appDb.GuildInfo.FirstOrDefault(g => g.GuildId == guild.Id);
			if (info is null)
				return;
			// Determine if the message is a command based on the prefix and make sure no bots trigger commands
			if (!(message.HasStringPrefix(info.Prefix, ref argPos) || message.Author.IsBot))
				return;

			// Create a WebSocket-based command context based on the message
			var context = new SocketCommandContext(Client, message);

			// Execute the command with the command context we just
			// created, along with the service provider for precondition checks.
			await Commands.ExecuteAsync(context, argPos, services: Services);
		}
	}
}
