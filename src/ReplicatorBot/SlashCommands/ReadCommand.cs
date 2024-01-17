using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReplicatorBot.SlashCommands;

[Group("read", "Commands related to reading messages")]
public class ReadCommand : InteractionModuleBase<SocketInteractionContext>
{
	protected ReplicatorContext ReplicatorContext { get; }
	protected ILogger<ReadCommand> Logger { get; }
	protected IServiceProvider Services { get; }

	public ReadCommand(ReplicatorContext replicatorContext, ILogger<ReadCommand> logger, IServiceProvider services)
	{
		ReplicatorContext = replicatorContext;
		Logger = logger;
		Services = services;
	}

	[SlashCommand("update", "Read all new messages in the server", runMode: RunMode.Async)]
	public async Task UpdateMessagesAsync()
	{
		await RespondAsync("Reading new messages.");
		Replicator.Default!.EnqueueReadUpdateOperation(Context.Guild.Id, Context.Channel.Id);
	}

	[SlashCommand("all", "Read all messages on the server")]
	public async Task ReadAllMessagesAsync([Summary(description: "The max number of messages to read")] int maxMessages = 1000000)
	{
		await RespondAsync("Reading all messages in server.");
		Replicator.Default!.EnqueueReadAllOperation(Context.Guild.Id, Context.Channel.Id, maxMessages);
	}
}
