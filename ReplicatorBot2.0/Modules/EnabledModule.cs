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
	public class EnabledModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; init; }
		public DiscordSocketClient Client { get; init; }
		public ILogger<EnabledModule> Logger { get; init; }
		public IServiceProvider Services { get; init; }

		[Command("enable")]
		[Summary("Enable the bot")]
		public async Task SetEnabled()
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();
			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			info.Enabled = true;
			context.GuildInfo.Update(info);
			context.SaveChanges();
			await ReplyAsync("Enabled Replicator.");
		}

		[Command("disable")]
		[Summary("Disable the bot")]
		public async Task SetDisabled()
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();
			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			info.Enabled = false;
			context.GuildInfo.Update(info);
			context.SaveChanges();
			await ReplyAsync("Enabled Replicator.");
		}

		protected override void AfterExecute(CommandInfo info) => Logger.LogInformation("Executed Command \"{command}\" in {module}", info.Name, nameof(EnabledModule));
	}

	[Group("mentions")]
	[Alias("mention")]
	public class MentionsModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; init; }
		public DiscordSocketClient Client { get; init; }
		public ILogger<MentionsModule> Logger { get; init; }
		public IServiceProvider Services { get; init; }

		[Command("get")]
		[Summary("Gets whether the bot can send mentions")]
		public async Task GetCanMention()
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			await ReplyAsync($"Can mention: {info.CanMention}");
		}

		[Command("set")]
		[Summary("Sets whether the bot can send mentions")]
		public async Task SetCanMention(bool canMention)
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			info.CanMention = canMention;
			context.GuildInfo.Update(info);
			context.SaveChanges();
			await ReplyAsync($"Updated can mention to: {info.CanMention}");
		}

		protected override void AfterExecute(CommandInfo info) => Logger.LogInformation("Executed Command \"{command}\" in {module}", info.Name, nameof(MentionsModule));
	}

	[Group("embeds")]
	[Alias("embed")]
	public class EmbedsModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; init; }
		public DiscordSocketClient Client { get; init; }
		public ILogger<EmbedsModule> Logger { get; init; }
		public IServiceProvider Services { get; init; }

		[Command("get")]
		[Summary("Gets whether the bot can send embeds")]
		public async Task GetCanEmbed()
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			await ReplyAsync($"Can embed: {info.CanEmbed}");
		}

		[Command("set")]
		[Summary("Sets whether the bot can send embeds")]
		public async Task SetCanEmbed(bool canEmbed)
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			info.CanEmbed = canEmbed;
			context.GuildInfo.Update(info);
			context.SaveChanges();
			await ReplyAsync($"Updated can embed to: {info.CanEmbed}");
		}

		protected override void AfterExecute(CommandInfo info) => Logger.LogInformation("Executed Command \"{command}\" in {module}", info.Name, nameof(EmbedsModule));
	}
}
