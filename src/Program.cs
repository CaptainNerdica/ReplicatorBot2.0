using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ReplicatorBot
{
	internal static class Program
	{
		public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((context, config) => config.AddCommandLine(args, new Dictionary<string, string>
			{
				{ "-t", "Token" },
				{ "--token", "Token" },
				{ "-c", "ConnectionString" },
				{ "--connection", "ConnectionString" },
				{ "-p", "Provider" },
				{ "--provider", "Provider" }
			}))
			.ConfigureServices((context, services) =>
			{

				string csValue = context.Configuration.GetValue<string>("ConnectionString");
				DbProvider provider = context.Configuration.GetValue("Provider", DbProvider.Sqlite);
				string connection;
				if (string.IsNullOrEmpty(csValue))
				{
					provider = DbProvider.Sqlite;
					connection = "DataSource=/data/application.db";
				}
				else
					connection = csValue;

				
				
				using AppDbContext dbContext = new AppDbContext(connection, provider);
				if (!dbContext.Database.CanConnect())
					dbContext.Database.Migrate();

				services.AddLogging()
					.AddScoped(services => new AppDbContext(connection, provider))
					.AddSingleton(ConfigureClient(context.Configuration).GetAwaiter().GetResult())
					.AddSingleton<CommandService>()
					.AddSingleton<CommandHandler>()
					.AddHostedService<Replicator>();
			});
		private static async Task<DiscordSocketClient> ConfigureClient(IConfiguration config)
		{
			string token;
			string fileConfig = config.GetValue<string>("TokenFile");
			string tokenConfig = config.GetValue<string>("Token");
			if (!string.IsNullOrEmpty(fileConfig) && !string.IsNullOrEmpty(tokenConfig))
				throw new Exception("Configuration contains both Token and TokenFile parameters");
			if (string.IsNullOrEmpty(fileConfig) && string.IsNullOrEmpty(tokenConfig))
				throw new Exception("Configuration does not contain a string or file for the bot token");
			if (!string.IsNullOrEmpty(fileConfig))
				token = System.IO.File.ReadAllText(fileConfig);
			else
				token = tokenConfig;
			DiscordSocketClient client = new DiscordSocketClient();
			await client.LoginAsync(TokenType.Bot, token);
			await client.StartAsync();
			return client;
		}
	}
}
