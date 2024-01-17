using DiscordBotCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ReplicatorBot;
using System;
using System.Collections.Generic;

var builder = Host.CreateApplicationBuilder(args);

if (builder.Environment.IsDevelopment())
	builder.Configuration.AddJsonFile("secrets.json", true);

IDictionary<string, string> commandLineArgs = new Dictionary<string, string>
{
	{ "-t", "Token" },
	{ "--token", "Token" },
	{ "-c", "ConnectionString" },
	{ "--connection", "ConnectionString" },
	{ "-p", "Provider" },
	{ "--provider", "Provider" }
};
builder.Configuration.AddCommandLine(args, commandLineArgs);

string? connectionStringValue = builder.Configuration.GetValue<string>("ConnectionString");
DbProvider provider = builder.Configuration.GetValue("Provider", DbProvider.Sqlite);

string connection;
if (string.IsNullOrEmpty(connectionStringValue))
{
	provider = DbProvider.Sqlite;
	connection = OperatingSystem.IsWindows() ? "DataSource=application.db" : "DataSource=/data/application.db";
}
else
	connection = connectionStringValue;

using (ReplicatorContext dbContext = new ReplicatorContext(connection, provider))
{
	if (!dbContext.Database.CanConnect())
		dbContext.Database.EnsureDeleted();

	dbContext.Database.Migrate();
}

builder.Services
	.AddLogging()
	.AddScoped(services => new ReplicatorContext(connection, provider))
	.AddDiscordSocketClient()
	.AddInteractionService()
	.AddHostedService<Replicator>();

builder.Build().Run();