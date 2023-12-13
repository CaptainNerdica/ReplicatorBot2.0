using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Discord.Interactions;
public static class DiscordInteractionServiceExtensions
{
	public static async Task RegisterCommandsAsync(this InteractionService interactions, IServiceProvider provider)
	{
		using var scope = provider.CreateScope();
		var services = scope.ServiceProvider;

		await interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), services);

		IHostEnvironment env = services.GetRequiredService<IHostEnvironment>();
		DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();

		foreach (var globalCommand in await client.GetGlobalApplicationCommandsAsync())
			await globalCommand.DeleteAsync();

		foreach (SocketGuild guild in client.Guilds)
			await interactions.RegisterCommandsToGuildAsync(guild.Id, true);
	}

	public static void InstallService(this InteractionService interactions, IServiceProvider services)
	{
		DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();

		client.InteractionCreated += async (interaction) =>
		{
			if (interaction.CreatedAt.AddSeconds(3) < DateTimeOffset.Now)
			{
				await interaction.Channel.SendMessageAsync($"An error has occurred: Response window has already passed, system time is likely ahead.");
				return;
			}

			if (interaction.CreatedAt > DateTimeOffset.Now.AddSeconds(1))
			{
				await interaction.RespondAsync($"An error has occurred: Deviation detected, system time is likely behind");
				return;
			}

			if (interaction.CreatedAt.AddSeconds(1) < DateTimeOffset.Now)
			{
				await interaction.RespondAsync($"An error has occurred: Deviation detected, system time is likely ahead.");
				return;
			}

			var ctx = new SocketInteractionContext(client, interaction);
			var result = await interactions.ExecuteCommandAsync(ctx, services);
		};
	}
}
