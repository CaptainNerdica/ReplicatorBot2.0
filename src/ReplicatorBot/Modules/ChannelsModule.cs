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
	[Group("channels")]
	public class ChannelsModule : ModuleBase<SocketCommandContext>
	{
		public CommandService Commands { get; init; }
		public DiscordSocketClient Client { get; init; }
		public ILogger<ChannelsModule> Logger { get; init; }
		public IServiceProvider Services { get; init; }

		private static string GetPermsString(ChannelPermission perms)
		{
			return perms switch
			{
				ChannelPermission.Read => "Read",
				ChannelPermission.Write => "Write",
				ChannelPermission.ReadWrite => "Read/Write",
				_ => "None",
			};
		}

		[Command("get")]
		[Summary("Get permissions for current channel")]
		public async Task GetChannelPermsAsync()
		{
			using IServiceScope scope = Services.CreateScope();
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();
			ChannelPermissions channel = context.ChannelPermissions.FirstOrDefault(c => c.ChannelId == Context.Channel.Id && c.GuildId == Context.Guild.Id);
			await ReplyAsync($"Permissions for channel {(Context.Guild.GetChannel(Context.Channel.Id) as ITextChannel).Mention}: {GetPermsString(channel.Permissions)}");
		}

		[Command("get")]
		[Summary("Get permissions for channel")]
		public async Task GetChannelPermsAsync(ITextChannel channel)
		{
			using IServiceScope scope = Services.CreateScope();
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();
			ChannelPermissions perms = context.ChannelPermissions.FirstOrDefault(c => c.ChannelId == channel.Id && c.GuildId == Context.Guild.Id);
			await ReplyAsync($"Permissions for channel {(Context.Guild.GetChannel(channel.Id) as ITextChannel).Mention}: {GetPermsString(perms.Permissions)}");
		}

		[Command("set")]
		[Summary("Set permissions for channel")]
		public async Task SetChannelPermsAsync(string newPerms)
		{
			using IServiceScope scope = Services.CreateScope();
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();
			ChannelPermissions perms = context.ChannelPermissions.FirstOrDefault(c => c.ChannelId == Context.Channel.Id && c.GuildId == Context.Guild.Id);
			if (perms is null)
			{
				perms = context.ChannelPermissions.Add(new ChannelPermissions(Context.Guild.Id, Context.Channel.Id, ChannelPermission.ReadWrite)).Entity;
				context.SaveChanges();
			}
			perms.Permissions = 0;
			if (newPerms.Contains("r", StringComparison.InvariantCultureIgnoreCase))
				perms.Permissions |= ChannelPermission.Read;
			if (newPerms.Contains("w", StringComparison.InvariantCultureIgnoreCase))
				perms.Permissions |= ChannelPermission.Write;
			if (newPerms.Contains("n", StringComparison.InvariantCultureIgnoreCase) || newPerms.Equals("none", StringComparison.InvariantCultureIgnoreCase))
				perms.Permissions = 0;

			context.Update(perms);
			context.SaveChanges();
			await ReplyAsync($"Set new permissions for channel {(Context.Guild.GetChannel(Context.Channel.Id) as ITextChannel).Mention}: {GetPermsString(perms.Permissions)}");
		}

		[Command("set")]
		[Summary("Set permissions for channel")]
		public async Task SetChannelPermsAsync(ITextChannel channel, string newPerms)
		{
			using IServiceScope scope = Services.CreateScope();
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();
			ChannelPermissions perms = context.ChannelPermissions.FirstOrDefault(c => c.ChannelId == channel.Id && c.GuildId == Context.Guild.Id);
			if (perms is null)
			{
				perms = context.ChannelPermissions.Add(new ChannelPermissions(Context.Guild.Id, Context.Channel.Id, ChannelPermission.ReadWrite)).Entity;
				context.SaveChanges();
			}
			perms.Permissions = 0;
			if (newPerms.Contains("r", StringComparison.InvariantCultureIgnoreCase))
				perms.Permissions |= ChannelPermission.Read;
			if (newPerms.Contains("w", StringComparison.InvariantCultureIgnoreCase))
				perms.Permissions |= ChannelPermission.Write;
			if (newPerms.Contains("n", StringComparison.InvariantCultureIgnoreCase) || newPerms.Equals("none", StringComparison.InvariantCultureIgnoreCase))
				perms.Permissions = 0;

			context.Update(perms);
			context.SaveChanges();
			await ReplyAsync($"Set new permissions for channel {(Context.Guild.GetChannel(channel.Id) as ITextChannel).Mention}: {GetPermsString(perms.Permissions)}");
		}

		[Command("list")]
		public async Task ListPermissionsAsync()
		{
			using IServiceScope scope = Services.CreateScope();
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();
			IQueryable<ChannelPermissions> perms = context.ChannelPermissions.AsQueryable().Where(c => c.GuildId == Context.Guild.Id);

			var embedBuilder = new EmbedBuilder
			{
				Title = "Channel Permissions"
			};
			int fieldCount = 0;
			int page = 1;
			foreach (var channel in Context.Guild.TextChannels.OrderBy(t => t.Position))
			{
				embedBuilder.AddField(channel.Name, GetPermsString(perms.FirstOrDefault(p => p.ChannelId == channel.Id).Permissions), true);
				fieldCount++;
				if(fieldCount == 24)
				{
					fieldCount = 0;
					page++;
					await ReplyAsync(embed: embedBuilder.Build());
					embedBuilder = new EmbedBuilder
					{
						Title = $"Channel Permissions pg. {page}"
					};
				}
			}
			await ReplyAsync(embed: embedBuilder.Build());
		}

		protected override void AfterExecute(CommandInfo info) => Logger.LogInformation("Executed Command \"{command}\" in {module}", info.Name, nameof(ChannelsModule));
	}
}
