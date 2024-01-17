using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot.SlashCommands;
[Group("target", "Set the target user to replicate")]
public class TargetCommand : InteractionModuleBase<SocketInteractionContext>
{
	protected ReplicatorContext ReplicatorContext { get; }
	protected ILogger<ProbabilityCommand> Logger { get; }

	public TargetCommand(ReplicatorContext context, ILogger<ProbabilityCommand> logger)
	{
		ReplicatorContext = context;
		Logger = logger;
	}

	[SlashCommand("get", "Get the current target user")]
	public async Task GetTargetAsync()
	{
		GuildConfig? guild = await GuildConfig.GetAsync(ReplicatorContext, Context.Guild.Id);
		if (guild is null)
		{
			await RespondAsync("An error has occurred: Server config does not exist");
			return;
		}

		ulong? userId = guild.TargetUserId;

		if (userId is null)
			await RespondAsync("Current target: None");
		else
		{
			SocketUser? user = await Context.Client.GetUserAsync((ulong)userId) as SocketUser;
			await RespondAsync($"Current target: {user?.Mention}", allowedMentions: AllowedMentions.None);
		}
	}

	[SlashCommand("set", "Set the current target user")]
	public async Task SetTargetAsync([Summary(description: "The user to set")] IUser? user = null, [Summary(description: "The id of the user to set")] ulong? userId = null)
	{
		if (user is null && userId is null)
		{
			await RespondAsync("At least one parameter is required", ephemeral: true);
			return;
		}

		if (user is not null && userId is not null)
		{
			await RespondAsync("Cannot set both parameters", ephemeral: true);
			return;
		}

		GuildConfig? guild = await GuildConfig.GetAsync(ReplicatorContext, Context.Guild.Id);

		if (guild is null)
		{
			await RespondAsync("An error has occurred: Server config does not exist");
			return;
		}

		if (user is not null)
			guild.TargetUserId = user.Id;

		if (userId is not null)
		{
			guild.TargetUserId = userId;
			user = await Context.Client.GetUserAsync((ulong)userId);
		}

		GuildConfig.Update(ReplicatorContext, guild);
		await ReplicatorContext.SaveChangesAsync();

		await RespondAsync($"Set targeted user to: {user!.Mention}", allowedMentions: AllowedMentions.None);
	}

	public override Task AfterExecuteAsync(ICommandInfo command)
	{
		return base.AfterExecuteAsync(command);
	}
}