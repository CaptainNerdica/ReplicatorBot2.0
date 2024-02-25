using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot.SlashCommands;

public class ReplyCommand : InteractionModuleBase<SocketInteractionContext>
{
	protected ReplicatorContext ReplicatorContext { get; }
	protected ILogger<ReplyCommand> Logger { get; }

	public ReplyCommand(ReplicatorContext replicatorContext, ILogger<ReplyCommand> logger)
	{
		ReplicatorContext = replicatorContext;
		Logger = logger;
	}

	[SlashCommand("reply", "Get a random response from the bot")]
	public async Task ReplyAsync()
	{
		GuildConfig config = GuildConfig.Get(ReplicatorContext, Context.Guild.Id)!;
		
		Message m;
		do
		{
			m = Replicator.Default!.RetrieveRandomMessage(config);
		} while (m.Type != MessageType.Raw);
		
		string message = m.Text;
		await RespondAsync(message, allowedMentions: config.GetAllowedMentions());
	}
}
