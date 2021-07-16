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
	[Group("delay")]
	[Summary("Get or set the message per character delay")]
	public class DelayModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; init; }
		public DiscordSocketClient Client { get; init; }
		public ILogger<PrefixModule> Logger { get; init; }
		public IServiceProvider Services { get; init; }

		[Command("get")]
		[Summary("Get current per character delay")]
		public async Task GetDelayAsync()
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();
			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			await ReplyAsync($"Current Per Character Delay: {info.Delay}ms");
		}

		[Command("set")]
		[Summary("Set the per character delay")]
		public async Task SetDelayAsync(int delay)
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();
			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			info.Delay = delay;
			context.GuildInfo.Update(info);
			context.SaveChanges();
			await ReplyAsync($"Updated per character delay to {info.Prefix}ms");
		}

		protected override void AfterExecute(CommandInfo info) => Logger.LogInformation("Executed Command \"{command}\" in {module}", info.Name, nameof(DelayModule));
	}
}
