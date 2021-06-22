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
	[Group("user")]
	public class UsersModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; init; }
		public DiscordSocketClient Client { get; init; }
		public ILogger<EnabledModule> Logger { get; init; }
		public IServiceProvider Services { get; init; }

		[Command("list")]
		[Summary("List all disabled users")]
		public async Task ListDisabledAsync()
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			SocketGuild guild = Context.Guild;
			IEnumerable<DisabledUser> disabledUsers = context.DisabledUsers.AsQueryable().Where(d => d.GuildId == guild.Id).AsEnumerable();

			if (!disabledUsers.Any())
			{
				await ReplyAsync("No disabled users to display");
				return;
			}
			var embedBuilder = new EmbedBuilder { Title = "Disabled Users" };
			foreach (var disabled in disabledUsers)
			{
				IGuildUser user = guild.GetUser(disabled.UserId);
				embedBuilder.AddField(user.Id.ToString(), $"{user.Mention}");
			}
			await ReplyAsync(embed: embedBuilder.Build(), allowedMentions: AllowedMentions.None);
		}

		[Command("add")]
		[Summary("Add a new disabled user")]
		public async Task AddDisabledAsync(IUser user)
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			if (context.DisabledUsers.AsQueryable().Where(d => d.GuildId == Context.Guild.Id && d.UserId == user.Id).Any())
			{
				await ReplyAsync("User already disabled");
				return;
			}
			DisabledUser u = new DisabledUser(Context.Guild.Id, user.Id);
			context.DisabledUsers.Add(u);
			context.SaveChanges();
			await ReplyAsync($"Added {user.Mention} to list of disabled users", allowedMentions: AllowedMentions.None);
		}

		[Command("remove")]
		[Summary("Remove a disabled user")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task RemoveDisabledAsync(IUser user)
		{
			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();
			DisabledUser u = context.DisabledUsers.FirstOrDefault(d => d.GuildId == Context.Guild.Id && d.UserId == user.Id);
			if (u is null)
			{
				await ReplyAsync("User not disabled");
				return;
			}
			context.DisabledUsers.Remove(u);
			context.SaveChanges();
			await ReplyAsync($"Removed {user.Mention} from the list of disabled users", allowedMentions: AllowedMentions.None);
		}

		protected override void AfterExecute(CommandInfo info) => Logger.LogInformation("Executed Command \"{command}\" in {module}", info.Name, nameof(UsersModule));
	}
}
