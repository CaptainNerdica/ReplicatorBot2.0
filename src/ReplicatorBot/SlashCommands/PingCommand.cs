using Discord;
using Discord.Interactions;
using DiscordBotCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot.SlashCommands;
public class PingCommand : InteractionModuleBase<SocketInteractionContext>
{
	protected ReplicatorContext ReplicatorContext { get; }
	protected ILogger<PingCommand> Logger { get; }

	public PingCommand(ReplicatorContext replicatorContext, ILogger<PingCommand> logger)
	{
		ReplicatorContext = replicatorContext;
		Logger = logger;
	}

	[SlashCommand("ping", "Test the latency to the bot")]
	public async Task PingCommandAsync()
	{
		DateTimeOffset end = DateTimeOffset.UtcNow;
		DateTimeOffset start = Context.Interaction.CreatedAt;
		int delay = (end - start).Milliseconds;

		await Context.Interaction.RespondAsync($"Latency: {delay}ms", ephemeral: true);
	}

	public override void AfterExecute(ICommandInfo command) => Logger.LogInformation("Executed Command '{command}' in {module}", command.Name, GetType().Name);
}