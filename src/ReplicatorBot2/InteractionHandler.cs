using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot
{
	public enum AppCommandChangeStatus
	{
		Created,
		Updated,
		Deleted,
	}

	public class InteractionHandler
	{
		protected ILogger<InteractionHandler> Logger { get; init; }
		protected IServiceProvider ServiceProvider { get; init; }
		protected DiscordSocketClient Client { get; init; }

		public InteractionHandler(ILogger<InteractionHandler> logger, IServiceProvider services, DiscordSocketClient client)
		{
			Logger = logger;
			ServiceProvider = services;
			Client = client;
		}
		public async Task ApplicationCommandDeleted(SocketApplicationCommand arg) => await HandleCommandChanges(arg, AppCommandChangeStatus.Deleted);
		public async Task ApplicationCommandUpdated(SocketApplicationCommand arg) => await HandleCommandChanges(arg, AppCommandChangeStatus.Updated);
		public async Task ApplicationCommandCreated(SocketApplicationCommand arg) => await HandleCommandChanges(arg, AppCommandChangeStatus.Created);

		public Task HandleCommandChanges(SocketApplicationCommand command, AppCommandChangeStatus status)
		{
			Logger.LogInformation($"{status} command {command.Name}");
			return Task.CompletedTask;
		}

		public async Task InteractionCreated(SocketInteraction interaction)
		{
			Logger.LogInformation($"Handling interaction {interaction.Id}");
			switch (interaction.Type)
			{
				case InteractionType.ApplicationCommand:
					await SlashCommandHandler(interaction as SocketSlashCommand);
					break;
			}
		}
		private async Task SlashCommandHandler(SocketSlashCommand command)
		{
			using IServiceScope scope = ServiceProvider.CreateScope();
			await command.RespondAsync("Test Response");
			await Task.Delay(1000);
		}
	}
}
