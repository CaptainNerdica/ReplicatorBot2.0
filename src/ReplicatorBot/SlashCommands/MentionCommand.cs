using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot.SlashCommands;

[Group("mention", "Commands related to mentions")]
public class MentionCommand : InteractionModuleBase<SocketInteractionContext>
{
	protected ReplicatorContext ReplicatorContext { get; }
	protected ILogger<MentionCommand> Logger { get; }

	public MentionCommand(ReplicatorContext replicatorContext, ILogger<MentionCommand> logger)
	{
		ReplicatorContext = replicatorContext;
		Logger = logger;
	}

	[SlashCommand("get", "Get whether mentions are enabled")]
	public async Task GetCanMentionAsync()
	{
		GuildConfig config = ReplicatorContext.GuildConfig.First(g => g.GuildId == Context.Guild.Id);

		await RespondAsync($"The bot is {(config.CanMention ? "able" : "unable")} to send mentions");
	}

	[SlashCommand("enable", "Enable the bot to send mentions")]
	public async Task SetEnableAsync()
	{
		GuildConfig config = ReplicatorContext.GuildConfig.First(g => g.GuildId == Context.Guild.Id);
		config.CanMention = true;

		ReplicatorContext.GuildConfig.Update(config);
		await ReplicatorContext.SaveChangesAsync();
		
		await RespondAsync("Enabled the bot to send mentions");
	}

	[SlashCommand("disable", "Disable the bot from sending mentions")]
	public async Task SetDisabledAsync()
	{
		GuildConfig config = ReplicatorContext.GuildConfig.First(g => g.GuildId == Context.Guild.Id);
		config.CanMention = false;

		ReplicatorContext.GuildConfig.Update(config);
		await ReplicatorContext.SaveChangesAsync();

		await RespondAsync("Disabled the bot from sending mentions");
	}

	public override void AfterExecute(ICommandInfo command) => Logger.LogInformation("Executed Command '{command}' in {module}", command.Name, GetType().Name);
}
