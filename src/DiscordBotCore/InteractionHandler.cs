using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DiscordBotCore
{
	public class InteractionHandler : HandlerService
	{
		protected ILogger<InteractionHandler> Logger { get; init; }
		protected IServiceProvider ServiceProvider { get; init; }
		protected DiscordSocketClient Client { get; init; }

		private readonly IDictionary<SlashCommandData, Type> _handlers = new Dictionary<SlashCommandData, Type>(new SlashCommandData.Comparer());
		private InstallState _state;

		public InteractionHandler(ILogger<InteractionHandler> logger, IServiceProvider services, DiscordSocketClient client)
		{
			Logger = logger;
			ServiceProvider = services;
			Client = client;
		}

		public override Task InstallServiceAsync() => InstallServiceAsync(Assembly.GetEntryAssembly());
		public async Task InstallServiceAsync(Assembly assembly)
		{
			//Check install state so that event handlers are not added multiple times
			if (_state != InstallState.Installed)
			{
				Client.InteractionCreated += InteractionCreated;
				_state = InstallState.Installed;

				Type[] commandTypes = assembly.GetTypes().Where(t => t.CustomAttributes.Select(c => c.AttributeType).Contains(typeof(SlashCommandAttribute))).ToArray();
				IEnumerable<Type> guildTypes = commandTypes.Where(t => t.IsAssignableTo(typeof(IGuildSlashCommand)));
				IEnumerable<Type> globalTypes = commandTypes.Where(t => t.IsAssignableTo(typeof(IGlobalSlashCommand)));

				foreach (ulong guildId in Client.Guilds.Select(g => g.Id))
					await BatchAddGuildCommandsAsync(guildTypes, guildId, true);
				await BatchAddGlobalCommandsAsync(globalTypes, false);
			}
		}
		public async override Task UninstallServiceAsync()
		{
			if (_state != InstallState.Uninstalled)
			{
				Client.InteractionCreated -= InteractionCreated;
				_handlers.Clear();
				_state = InstallState.Uninstalled;
				await Task.CompletedTask;
			}
		}

		public Task AddGlobalCommandAsync<T>(bool buildCommand = false) where T : SlashCommandBase => AddGlobalCommandAsync(typeof(T), buildCommand);
		public async Task AddGlobalCommandAsync(Type type, bool buildCommand = false)
		{
			if (!type.IsAssignableTo(typeof(IGlobalSlashCommand)))
				throw new InvalidOperationException($"Type does not inherit from {nameof(IGlobalSlashCommand)}");
			if (!type.CustomAttributes.Select(a => a.AttributeType).Contains(typeof(SlashCommandAttribute)))
				throw new InvalidOperationException($"Type does not contain attribute {nameof(SlashCommandAttribute)}");
			string name = (type.GetCustomAttribute(typeof(SlashCommandAttribute)) as SlashCommandAttribute).Name;
			if (buildCommand && type.IsAssignableTo(typeof(IBuildableSlashCommand)))
				await Client.Rest.CreateGlobalCommand(BuildCommand(type, name));
			_handlers.Add(new SlashCommandData(name, null), type);
			Logger.LogInformation($"Added slash command '{name}'");
		}
		public async Task BatchAddGlobalCommandsAsync(IEnumerable<Type> types, bool buildCommand = false)
		{
			List<SlashCommandCreationProperties> commands = new List<SlashCommandCreationProperties>();
			foreach (Type type in types)
			{
				if (!type.IsAssignableTo(typeof(IGlobalSlashCommand)))
					throw new InvalidOperationException($"Type does not inherit from {nameof(IGlobalSlashCommand)}");
				if (!type.CustomAttributes.Select(a => a.AttributeType).Contains(typeof(SlashCommandAttribute)))
					throw new InvalidOperationException($"Type does not contain attribute {nameof(SlashCommandAttribute)}");
				string name = (type.GetCustomAttribute(typeof(SlashCommandAttribute)) as SlashCommandAttribute).Name;
				if (buildCommand && type.IsAssignableTo(typeof(IBuildableSlashCommand)))
					commands.Add(BuildCommand(type, name));
				_handlers.Add(new SlashCommandData(name, null), type);
			}
			await Client.Rest.BulkOverwriteGlobalCommands(commands.ToArray());
			Logger.LogInformation($"Added {types.Count()} commands");
		}

		public Task AddGuildCommandAsync<T>(ulong guildId, bool buildCommand = false) => AddGuildCommandAsync(typeof(T), guildId, buildCommand);
		public async Task AddGuildCommandAsync(Type type, ulong guildId, bool buildCommand = false)
		{
			if (!type.IsAssignableTo(typeof(IGuildSlashCommand)))
				throw new InvalidOperationException($"Type does not inherit from {nameof(IGuildSlashCommand)}");
			if (!type.CustomAttributes.Select(a => a.AttributeType).Contains(typeof(SlashCommandAttribute)))
				throw new InvalidOperationException($"Type does not contain attribute {nameof(SlashCommandAttribute)}");
			string name = (type.GetCustomAttribute(typeof(SlashCommandAttribute)) as SlashCommandAttribute).Name;
			if (buildCommand && type.IsAssignableTo(typeof(IBuildableSlashCommand)))
				await Client.Rest.CreateGuildCommand(BuildCommand(type, name), guildId);
			_handlers.Add(new SlashCommandData(name, guildId), type);
		}
		public async Task BatchAddGuildCommandsAsync(IEnumerable<Type> types, ulong guildId, bool buildCommands = false)
		{
			List<SlashCommandCreationProperties> commands = new List<SlashCommandCreationProperties>();
			foreach (Type type in types)
			{
				if (!type.IsAssignableTo(typeof(IGuildSlashCommand)))
					throw new InvalidOperationException($"Type does not inherit from {nameof(IGuildSlashCommand)}");
				if (!type.CustomAttributes.Select(a => a.AttributeType).Contains(typeof(SlashCommandAttribute)))
					throw new InvalidOperationException($"Type does not contain attribute {nameof(SlashCommandAttribute)}");
				string name = (type.GetCustomAttribute(typeof(SlashCommandAttribute)) as SlashCommandAttribute).Name;
				if (buildCommands && type.IsAssignableTo(typeof(IBuildableSlashCommand)))
					commands.Add(BuildCommand(type, name));
				_handlers.TryAdd(new SlashCommandData(name, guildId), type);
			}
			await Client.Rest.BulkOverwriteGuildCommands(commands.ToArray(), guildId);
			Logger.LogInformation($"Added {types.Count()} commands");
		}

		private SlashCommandCreationProperties BuildCommand(Type type, string name)
		{
			using IServiceScope scope = ServiceProvider.CreateScope();
			IBuildableSlashCommand command = ActivatorUtilities.CreateInstance(scope.ServiceProvider, type) as IBuildableSlashCommand;
			SlashCommandBuilder builder = new SlashCommandBuilder();
			command.BuildCommand(builder);
			try
			{
				SlashCommandCreationProperties props = builder.WithName(name).Build();
				Logger.LogInformation($"Built slash command '{name}'");
				return props;
			}
			catch (ApplicationCommandException exception)
			{
				string json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);
				Logger.LogError(exception, json);
				throw;
			}
		}

		public async Task InteractionCreated(SocketInteraction interaction)
		{
			try
			{
				Logger.LogInformation($"Handling interaction {interaction.Id}");
				switch (interaction.Type)
				{
					case InteractionType.ApplicationCommand:
						await SlashCommandHandler(interaction as SocketSlashCommand ?? throw new ArgumentException("Interaction is not of type SlashCommand", nameof(interaction)));
						break;
				}
			}
			catch (Exception e)
			{
				await interaction.RespondAsync($"Interaction failed: {e}");
			}
		}
		private async Task SlashCommandHandler(SocketSlashCommand command)
		{
			using IServiceScope scope = ServiceProvider.CreateScope();
			SlashCommandData data = new SlashCommandData(command.Data.Name, (command.Channel as SocketGuildChannel)?.Guild?.Id);
			if (_handlers.TryGetValue(data, out Type type))
			{
				using SlashCommandBase slashCommand = ActivatorUtilities.CreateInstance(scope.ServiceProvider, type) as SlashCommandBase;
				slashCommand.Command = command;
				if (data.GuildId is null)
					await (slashCommand as IGlobalSlashCommand).ExecuteGlobalCommandAsync();
				else
					await (slashCommand as IGuildSlashCommand).ExecuteGuildCommandAsync((ulong)data.GuildId);
			}
			else
			{
				Logger.LogWarning($"No registered handler for {(data.GuildId is null ? "Global" : "Guild")} command {data.Name}");
				await command.RespondAsync("Interaction failed.");
			}
		}

		~InteractionHandler()
		{
			if (_state != InstallState.Uninstalled)
				Task.Run(async () => await UninstallServiceAsync()).Wait();
		}
	}

	internal readonly struct SlashCommandData : IEquatable<SlashCommandData>
	{
		public string Name { get; init; }
		public ulong? GuildId { get; init; }

		public SlashCommandData(string name, ulong? guildId)
		{
			Name = name;
			GuildId = guildId;
		}

		public override bool Equals([NotNullWhen(true)] object obj) => obj is SlashCommandData d && Equals(d);
		public override int GetHashCode() => HashCode.Combine(Name, GuildId);
		public bool Equals(SlashCommandData other) => this == other;

		public static bool operator ==(SlashCommandData left, SlashCommandData right) => left.Name == right.Name && left.GuildId == right.GuildId;
		public static bool operator !=(SlashCommandData left, SlashCommandData right) => !(left == right);

		internal class Comparer : IEqualityComparer<SlashCommandData>
		{
			public bool Equals(SlashCommandData x, SlashCommandData y) => x == y;
			public int GetHashCode(SlashCommandData value) => value.GetHashCode();
		}
	}
}
