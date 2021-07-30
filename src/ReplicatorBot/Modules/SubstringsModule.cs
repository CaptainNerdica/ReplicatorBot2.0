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
	[Group("substring")]
	public class SubstringsModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; init; }
		public DiscordSocketClient Client { get; init; }
		public ILogger<SubstringsModule> Logger { get; init; }
		public IServiceProvider Services { get; init; }

		[Command("list")]
		[Summary("List all disabled substrings for this server")]
		public async Task ListDisabledSubstringsAsync()
		{
			using var scope = Services.CreateScope();
			using var context = scope.ServiceProvider.GetService<ReplicatorContext>();

			IQueryable<DisabledSubstring> substrings = context.DisabledSubstrings.AsQueryable().Where(d => d.GuildId == Context.Guild.Id).OrderBy(d => d.Index);

			if (!substrings.Any())
			{
				await ReplyAsync("No substrings to display.");
				return;
			}
			var embedBuilder = new EmbedBuilder { Title = "Disabled Substrings" };
			foreach (var sub in substrings)
				embedBuilder.AddField($"#{sub.Index}", sub.Substring);

			await ReplyAsync(embed: embedBuilder.Build());
		}

		[Command("add")]
		[Summary("Add a new disabled substring to the list")]
		public async Task AddDisabledSubstringAsync([Remainder] string substring)
		{
			using var scope = Services.CreateScope();
			using var context = scope.ServiceProvider.GetService<ReplicatorContext>();

			int count = context.DisabledSubstrings.AsQueryable().Where(d => d.GuildId == Context.Guild.Id).Count();
			DisabledSubstring d = new DisabledSubstring(Context.Guild.Id, count, substring);
			context.DisabledSubstrings.Add(d);

			context.SaveChanges();
			await ReplyAsync($"Added {substring} to disabled substrings.");

		}

		[Command("remove")]
		[Summary("Remove a disabled substring")]
		public async Task RemoveDisabledSubstringAsync(int index)
		{
			using var scope = Services.CreateScope();
			using var context = scope.ServiceProvider.GetService<ReplicatorContext>();
			DisabledSubstring d = context.DisabledSubstrings.AsQueryable().FirstOrDefault(d => d.GuildId == Context.Guild.Id && d.Index == index);
			if (d is null)
			{
				await ReplyAsync($"Could not find disabled substring with index {index}");
				return;
			}
			context.DisabledSubstrings.Remove(d);
			List<DisabledSubstring> disabledSubstrings = context.DisabledSubstrings.AsQueryable().Where(d => d.Index > index).ToList();
			disabledSubstrings.ForEach(d => d.Index--);
			context.DisabledSubstrings.UpdateRange(disabledSubstrings);
			context.SaveChanges();

			await ReplyAsync($"Removed disabled substring with index {index}");
		}

		protected override void AfterExecute(CommandInfo info) => Logger.LogInformation("Executed Command \"{command}\" in {module}", info.Name, nameof(SubstringsModule));
	}
}
