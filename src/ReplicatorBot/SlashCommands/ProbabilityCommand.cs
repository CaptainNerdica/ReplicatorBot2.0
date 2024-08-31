using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBotCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot.SlashCommands;

[Group("probability", "Get or set the probability")]
public class ProbabilityCommand : InteractionModuleBase<SocketInteractionContext>
{
	protected ReplicatorContext ReplicatorContext { get; }
	protected ILogger<ProbabilityCommand> Logger { get; }

	public ProbabilityCommand(ReplicatorContext context, ILogger<ProbabilityCommand> logger)
	{
		ReplicatorContext = context;
		Logger = logger;
	}

	[SlashCommand("get", "Get the current probability")]
	public async Task GetProbabilityAsync()
	{
		GuildConfig? guild = await GuildConfig.GetAsync(ReplicatorContext, Context.Guild.Id);
		if (guild is null)
			await RespondAsync("An error has occurred: Server config does not exist");
		else
			await RespondAsync($"Auto Update: {guild.AutoUpdateProbability}, Probability: {guild.Probability:P1}");
	}

	[SlashCommand("set", "Set the current probability")]
	public async Task SetProbabilityAsync(
		[Summary(description: "Probability to set")] string probability)
	{
		int index = probability.IndexOf('%');
		if (index == -1)
			index = probability.Length;
		if (!double.TryParse(probability[..index], out double value))
			await RespondAsync("Probability is in the wrong format");

		value /= 100;
		value = Math.Clamp(value, 0, 1);

		GuildConfig? guild = await GuildConfig.GetAsync(ReplicatorContext, Context.Guild.Id);
		if (guild is null)
		{
			await RespondAsync("An error has occurred: Server config does not exist");
			return;
		}

		guild.Probability = value;
		guild.AutoUpdateProbability = false;

		GuildConfig.Update(ReplicatorContext, guild);
		await ReplicatorContext.SaveChangesAsync();

		await RespondAsync($"Updated probability to: {guild.Probability:P1}");
	}

	[SlashCommand("auto", "Set probability to auto update or use a fixed value")]
	public async Task AutoProbabilityAsync([Summary(description: "Whether to auto update")] bool value)
	{
		GuildConfig? guild = await GuildConfig.GetAsync(ReplicatorContext, Context.Guild.Id);
		if (guild is null)
		{
			await RespondAsync("An error has occurred: Server config does not exist");
			return;
		}

		guild.AutoUpdateProbability = value;

		if (value)
			guild.Probability = (double)guild.TargetMessageCount / guild.GuildMessageCount;

		if (!double.IsFinite(guild.Probability))
			guild.Probability = 0;

		GuildConfig.Update(ReplicatorContext, guild);
		await ReplicatorContext.SaveChangesAsync();

		await RespondAsync($"Set probability auto update to {value}");
	}

	public override void AfterExecute(ICommandInfo command) => Logger.LogInformation("Executed Command '{command}' in {module}", command.Name, GetType().Name);
}