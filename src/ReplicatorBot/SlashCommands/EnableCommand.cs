using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot.SlashCommands;
public class EnableCommand : InteractionModuleBase<SocketInteractionContext>
{
	protected ReplicatorContext ReplicatorContext { get; }
	protected ILogger<EnableCommand> Logger { get; }

	public EnableCommand(ReplicatorContext replicatorContext, ILogger<EnableCommand> logger)
	{
		ReplicatorContext = replicatorContext;
		Logger = logger;
	}

	[SlashCommand("enable", "Enable the bot")]
	public async Task SetEnabledAsync()
	{
		GuildConfig config = ReplicatorContext.GuildConfig.First(g => g.GuildId == Context.Guild.Id);
		config.Enabled = true;
		
		ReplicatorContext.GuildConfig.Update(config);
		await ReplicatorContext.SaveChangesAsync();
		
		await RespondAsync("Enabled Replicator.");
	}

	[SlashCommand("disable", "Disable the bot")]
	public async Task SetDisabledAsync()
	{
		GuildConfig config = ReplicatorContext.GuildConfig.First(g => g.GuildId == Context.Guild.Id);
		config.Enabled = false;

		ReplicatorContext.GuildConfig.Update(config);
		await ReplicatorContext.SaveChangesAsync();

		await RespondAsync("Disabled Replicator.");
	}

	public override void AfterExecute(ICommandInfo command) => Logger.LogInformation("Executed Command '{command}' in {module}", command.Name, GetType().Name);
}
