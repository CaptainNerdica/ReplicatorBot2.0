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
	[Group("probability")]
	public class ProbabilityModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; init; }
		public DiscordSocketClient Client { get; init; }
		public ILogger<ProbabilityModule> Logger { get; init; }
		public IServiceProvider Services { get; init; }

		[Command("get")]
		public async Task GetProbabilityAsync()
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);

			await ReplyAsync($"Auto Update: {info.AutoUpdateProbability}, Probability: {info.Probability:P1}");
		}

		[Command("set")]
		public async Task SetProbabilityAsync(string input)
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			GuildInfo info = context.GuildInfo.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
			if (input.Equals("auto", StringComparison.InvariantCultureIgnoreCase))
			{
				info.AutoUpdateProbability = true;
				double d = (double)info.TargetMessageCount / info.GuildMessageCount;
				info.Probability = double.IsNaN(d) ? 0 : d;
				context.GuildInfo.Update(info);
				await ReplyAsync("Set probability to auto update");
			}
			else if (double.TryParse(input, out double d))
			{
				info.AutoUpdateProbability = false;
				info.Probability = double.IsNaN(d) ? 0 : d; ;
				context.GuildInfo.Update(info);
				await ReplyAsync($"Set probability to {d:P1}");
			}
			else
				await ReplyAsync("Probability in incorrect format");
			context.SaveChanges();
		}

		protected override void AfterExecute(CommandInfo info) => Logger.LogInformation("Executed Command \"{command}\" in {module}", info.Name, nameof(EnabledModule));
	}
}
