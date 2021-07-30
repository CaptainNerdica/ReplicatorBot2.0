using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBotCore
{
	public class InteractionHandler : HandlerService
	{
		protected ILogger<InteractionHandler> Logger { get; init; }
		protected IServiceProvider ServiceProvider { get; init; }
		protected DiscordSocketClient Client { get; init; }

		protected IDictionary<string, Type> SlashCommandHandlers { get; } = new Dictionary<string, Type>();
		private InstallState _state;

		public InteractionHandler(ILogger<InteractionHandler> logger, IServiceProvider services, DiscordSocketClient client)
		{
			Logger = logger;
			ServiceProvider = services;
			Client = client;
		}

		public async override Task InstallServiceAsync()
		{
			//Check install state so that event handlers are not added multiple times
			if (_state != InstallState.Installed)
			{
				Client.InteractionCreated += InteractionCreated;
				_state = InstallState.Installed;
				await Task.CompletedTask;
			}
		}

		public async override Task UninstallServiceAsync()
		{
			if (_state != InstallState.Uninstalled)
			{
				Client.InteractionCreated -= InteractionCreated;
				SlashCommandHandlers.Clear();
				_state = InstallState.Uninstalled;
				await Task.CompletedTask;
			}
		}

		public async Task InteractionCreated(SocketInteraction interaction)
		{
			Logger.LogInformation($"Handling interaction {interaction.Id}");
			switch (interaction.Type)
			{
				case InteractionType.ApplicationCommand:
					await SlashCommandHandler(interaction as SocketSlashCommand ?? throw new ArgumentException("Interaction is not of type SlashCommand", nameof(interaction)));
					break;
			}
		}
		private async Task SlashCommandHandler(SocketSlashCommand command)
		{
			using IServiceScope scope = ServiceProvider.CreateScope();
			await command.RespondAsync("Test Response");
			await Task.Delay(1000);
		}

		~InteractionHandler()
		{
			if (_state != InstallState.Uninstalled)
				Task.Run(async () => await UninstallServiceAsync()).GetAwaiter().GetResult();
		}
	}
}
