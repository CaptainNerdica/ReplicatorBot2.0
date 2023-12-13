using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot.SlashCommands;
[Group("users", "Commands related to disabling replys to users")]
public class UsersCommand : InteractionModuleBase<SocketInteractionContext>
{
	protected ReplicatorContext ReplicatorContext { get; }
	protected ILogger<UsersCommand> Logger { get; }

	public UsersCommand(ReplicatorContext context, ILogger<UsersCommand> logger)
	{
		ReplicatorContext = context;
		Logger = logger;
	}

	[SlashCommand("list", "List all disabled users")]
	public async Task ListDisabledAsync()
	{
		SocketGuild guild = Context.Guild;
		IEnumerable<DisabledUser> disabledUsers = ReplicatorContext.DisabledUsers.AsQueryable().Where(d => d.GuildId == guild.Id).AsEnumerable();

		if (!disabledUsers.Any())
		{
			await RespondAsync("No disabled users to display");
			return;
		}

		var embedBuilder = new EmbedBuilder { Title = "Disabled Users" };
		foreach (var disabled in disabledUsers)
		{
			IGuildUser user = guild.GetUser(disabled.UserId);
			embedBuilder.AddField(user.Id.ToString(), $"{user.Mention}");
		}

		await RespondAsync(embed: embedBuilder.Build(), allowedMentions: AllowedMentions.None);
	}

	[SlashCommand("add", "Add a new disabled user")]
	public async Task AddDisabledUserAsync([Summary(description: "User to add")] IUser user)
	{
		if (ReplicatorContext.DisabledUsers.AsQueryable().Where(d => d.GuildId == Context.Guild.Id && d.UserId == user.Id).Any())
		{
			await RespondAsync("User already disabled");
			return;
		}

		DisabledUser u = new DisabledUser(Context.Guild.Id, user.Id);
		await ReplicatorContext.DisabledUsers.AddAsync(u);
		await ReplicatorContext.SaveChangesAsync();

		await RespondAsync($"Added {user.Mention} to list of disabled users", allowedMentions: AllowedMentions.None);
	}

	[SlashCommand("remove", "Remove a disabled user")]
	public async Task RemoveDisabledUserAsync([Summary(description: "User to remove")] IUser user)
	{
		DisabledUser? u = ReplicatorContext.DisabledUsers.FirstOrDefault(d => d.GuildId == Context.Guild.Id && d.UserId == user.Id);
		if (u is null)
			await RespondAsync("User not disabled");
		else
		{
			ReplicatorContext.DisabledUsers.Remove(u);
			await ReplicatorContext.SaveChangesAsync();

			await RespondAsync($"Removed {user.Mention} from the list of disabled users", allowedMentions: AllowedMentions.None);
		}
	}
}
