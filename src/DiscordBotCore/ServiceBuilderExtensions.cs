using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection;
public static class ServiceBuilderExtensions
{
	/// <summary>
	/// Adds a <see cref="DiscordSocketClient"/> to the service collection
	/// </summary>
	/// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add the service to.</param>
	/// <param name="lifetime">The lifetime of the service to add.</param>
	/// <returns>A reference to this instance after the operation has completed.</returns>
	/// <exception cref="InvalidOperationException">The configuration contains both Token and TokenFile parameters.</exception>
	/// <exception cref="InvalidOperationException">The configuration does not contain a string or file for the bot token.</exception>
	public static IServiceCollection AddDiscordSocketClient(this IServiceCollection serviceCollection, ServiceLifetime lifetime = ServiceLifetime.Singleton)
	{
		static async Task<DiscordSocketClient> Builder(IServiceProvider services)
		{
			IConfiguration config = services.GetRequiredService<IConfiguration>();

			string token;
			string? fileConfig = config.GetValue<string>("TokenFile");
			string? tokenConfig = config.GetValue<string>("Token");

			if (!string.IsNullOrEmpty(fileConfig) && !string.IsNullOrEmpty(tokenConfig))
				throw new InvalidOperationException("Configuration contains both Token and TokenFile parameters");

			if (string.IsNullOrEmpty(fileConfig) && string.IsNullOrEmpty(tokenConfig))
				throw new InvalidOperationException("Configuration does not contain a string or file for the bot token");

			if (!string.IsNullOrEmpty(fileConfig))
				token = File.ReadAllText(fileConfig);
			else
				token = tokenConfig!;

			GatewayIntents intents =
				GatewayIntents.GuildMembers |
				GatewayIntents.GuildMessages |
				GatewayIntents.GuildMessageTyping |
				GatewayIntents.Guilds;

			DiscordSocketClient client = new DiscordSocketClient(new DiscordSocketConfig
			{
				GatewayIntents = intents,
				AlwaysDownloadDefaultStickers = true,
				AlwaysResolveStickers = true,
				MessageCacheSize = 1000,
				AlwaysDownloadUsers = true
			});
			await client.LoginAsync(TokenType.Bot, token);
			await client.StartAsync();

			return client;
		}

		serviceCollection.Add(new ServiceDescriptor(typeof(DiscordSocketClient), services => Builder(services).GetAwaiter().GetResult(), lifetime));

		return serviceCollection;
	}

	/// <summary>
	/// Adds an <see cref="InteractionService"/> to the service collection.
	/// </summary>
	/// <param name="serviceCollection">The <see cref="IServiceCollection"/> to add the service to.</param>
	/// <param name="lifetime">The lifetime of the service to add.</param>
	/// <returns>A reference to this instance after the operation has completed.</returns>
	public static IServiceCollection AddInteractionService(this IServiceCollection serviceCollection, ServiceLifetime lifetime = ServiceLifetime.Singleton)
	{
		static InteractionService Builder(IServiceProvider services)
		{
			DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();

			return new InteractionService(client, new InteractionServiceConfig
			{
				DefaultRunMode = RunMode.Async,
				AutoServiceScopes = true,
				UseCompiledLambda = false
			});
		}

		serviceCollection.Add(new ServiceDescriptor(typeof(InteractionService), Builder, lifetime));

		return serviceCollection;
	}
}
