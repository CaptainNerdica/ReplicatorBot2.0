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
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			if (info.TargetUserId is null)
			{
				await ReplyAsync("Target user not set");
				return;
			}
			IGuildUser user = Context.Guild.GetUser(info.TargetUserId ?? 0);
			await ReplyAsync($"Current target user: {user.Mention}", allowedMentions: AllowedMentions.None);
		}

		[Command("set")]
		[Summary("Sets the currently targeted user")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task SetTargetAsync(IUser user)
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			info.TargetUserId = user.Id;
			info.Enabled = false;
			info.TargetMessageCount = 0;
			context.Messages.RemoveRange(info.Messages);
			context.GuildInfo.Update(info);
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
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			await Context.Guild.DownloadUsersAsync();
			var user = Context.Guild.GetUser(id);
			if (user is null)
			{
				await ReplyAsync($"Could not find user with id {id}");
				return;
			}
			info.TargetUserId = user.Id;
			info.Enabled = false;
			info.TargetMessageCount = 0;
			context.Messages.RemoveRange(info.Messages);
			context.GuildInfo.Update(info);
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
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			info.TargetUserId = null;
			info.Enabled = false;
			info.TargetMessageCount = 0;
			context.Messages.RemoveRange(info.Messages);
			context.GuildInfo.Update(info);
			await context.SaveChangesAsync();

			await ReplyAsync($"Cleared target user", allowedMentions: AllowedMentions.None);
			await ReplyAsync("Messages have been cleared, re-read all messages to re-enable");
		}

		protected override void AfterExecute(CommandInfo info) => Logger.LogInformation("Executed Command \"{command}\" in {module}", info.Name, nameof(TargetModule));
	}
}
