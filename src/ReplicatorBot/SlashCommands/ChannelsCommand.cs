using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBotCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot.SlashCommands;

[Group("channel", "Gets or sets permissions for channels.")]
public class ChannelCommand : InteractionModuleBase<SocketInteractionContext>
{
	protected ReplicatorContext ReplicatorContext { get; }
	protected ILogger<ChannelCommand> Logger { get; }

	public ChannelCommand(ReplicatorContext replicatorContext, ILogger<ChannelCommand> logger)
	{
		ReplicatorContext = replicatorContext;
		Logger = logger;
	}

	internal static IReadOnlyDictionary<ChannelPermission, string> PermissionStrings { get; } = new Dictionary<ChannelPermission, string>
	{
		[ChannelPermission.None] = "None",
		[ChannelPermission.Read] = "Read",
		[ChannelPermission.Write] = "Write",
		[ChannelPermission.ReadWrite] = "Read/Write"
	};

	[Discord.Interactions.SlashCommand("get", "Get the permissions for a channel")]
	public async Task GetChannelPermissions([Summary(description: "Channel to get")] IMessageChannel? channel = null)
	{
		if (Context.Guild is null)
		{
			await RespondAsync("Channel is not part of a server", ephemeral: true);
			return;
		}

		channel ??= Context.Channel;
		SocketGuildChannel guildChannel = Context.Guild.GetChannel(channel.Id);

		ChannelPermissions? perms = ReplicatorContext.ChannelPermissions.FirstOrDefault(c => c.ChannelId == Context.Channel.Id && c.GuildId == Context.Guild.Id);
		string mention = (guildChannel as IMentionable)?.Mention ?? string.Empty;

		if (perms is null)
			await RespondAsync($"{mention} has no permissions set.");
		else
			await RespondAsync($"{mention}: {PermissionStrings[perms.Permissions]}");
	}

	[Discord.Interactions.SlashCommand("set", "Set the permissions for a channel.")]
	public async Task SetChannelPermissions(
		[Summary(description: "Permissions to set")] ChannelPermission permissions,
		[Summary(description: "Channel to modify")] IMessageChannel? channel = null)
	{
		if (Context.Guild is null)
		{
			await RespondAsync("Channel is not part of a server", ephemeral: true);
			return;
		}

		ChannelPermissions? perms = ReplicatorContext.ChannelPermissions.FirstOrDefault(c => c.ChannelId == Context.Channel.Id && c.GuildId == Context.Guild.Id);
		if (perms is null)
		{
			perms = new ChannelPermissions(Context.Guild.Id, Context.Channel.Id, ChannelPermission.ReadWrite);
			perms = ReplicatorContext.ChannelPermissions.Add(perms).Entity;
			ReplicatorContext.SaveChanges();
		}

		perms.Permissions = permissions;

		ReplicatorContext.Update(perms);
		ReplicatorContext.SaveChanges();

		channel ??= Context.Channel;

		SocketGuildChannel guildChannel = Context.Guild.GetChannel(channel.Id);
		string mention = (guildChannel as IMentionable)?.Mention ?? string.Empty;

		await RespondAsync($"Set new permissions for channel {mention}: {PermissionStrings[permissions]}");
	}

	public override void AfterExecute(ICommandInfo command) => Logger.LogInformation("Executed Command '{command}' in {module}", command.Name, GetType().Name);
}
