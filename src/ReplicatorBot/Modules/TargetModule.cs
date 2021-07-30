using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot.Modules
{
	[Group("target")]
	public class TargetModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; init; }
		public DiscordSocketClient Client { get; init; }
		public ILogger<TargetModule> Logger { get; init; }
		public IServiceProvider Services { get; init; }

		[Command("get")]
		[Summary("Gets the currently targeted user")]
		public async Task GetTargetAsync()
		{
			using IServiceScope scope = Services.CreateScope();
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();

			GuildConfig config = context.GuildConfig.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			if (config.TargetUserId is null)
			{
				await ReplyAsync("Target user not set");
				return;
			}
			IGuildUser user = Context.Guild.GetUser(config.TargetUserId ?? 0);
			await ReplyAsync($"Current target user: {user.Mention}", allowedMentions: AllowedMentions.None);
		}

		[Command("set")]
		[Summary("Sets the currently targeted user")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task SetTargetAsync(IUser user)
		{
			using IServiceScope scope = Services.CreateScope();
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();

			GuildConfig config = context.GuildConfig.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			config.TargetUserId = user.Id;
			config.Enabled = false;
			config.TargetMessageCount = 0;
			context.Messages.RemoveRange(config.Messages);
			context.GuildConfig.Update(config);
			await context.SaveChangesAsync();

			await ReplyAsync($"Set current target user to: {user.Mention}", allowedMentions: AllowedMentions.None);
			await ReplyAsync("Messages have been cleared, re-read all messages to re-enable");
		}

		[Command("set", RunMode = RunMode.Async)]
		[Summary("Sets the currently targeted user")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task SetTargetAsync(ulong id)
		{
			using IServiceScope scope = Services.CreateScope();
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();

			GuildConfig config = context.GuildConfig.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			await Context.Guild.DownloadUsersAsync();
			var user = Context.Guild.GetUser(id);
			if (user is null)
			{
				await ReplyAsync($"Could not find user with id {id}");
				return;
			}
			config.TargetUserId = user.Id;
			config.Enabled = false;
			config.TargetMessageCount = 0;
			context.Messages.RemoveRange(config.Messages);
			context.GuildConfig.Update(config);
			await context.SaveChangesAsync();

			await ReplyAsync($"Set current target user to: {user.Mention}", allowedMentions: AllowedMentions.None);
			await ReplyAsync("Messages have been cleared, re-read all messages to re-enable");
		}

		[Command("clear")]
		[Summary("Clear currently targeted user")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task ClearTargetAsync()
		{
			using IServiceScope scope = Services.CreateScope();
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();

			GuildConfig config = context.GuildConfig.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			config.TargetUserId = null;
			config.Enabled = false;
			config.TargetMessageCount = 0;
			context.Messages.RemoveRange(config.Messages);
			context.GuildConfig.Update(config);
			await context.SaveChangesAsync();

			await ReplyAsync($"Cleared target user", allowedMentions: AllowedMentions.None);
			await ReplyAsync("Messages have been cleared, re-read all messages to re-enable");
		}

		protected override void AfterExecute(CommandInfo info) => Logger.LogInformation("Executed Command \"{command}\" in {module}", info.Name, nameof(TargetModule));
	}
}
