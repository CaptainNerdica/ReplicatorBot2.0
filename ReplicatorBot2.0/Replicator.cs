using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReplicatorBot
{
	public class Replicator : BackgroundService
	{
		protected DiscordSocketClient Client { get; init; }
		protected CommandHandler Commands { get; init; }
		protected ILogger<Replicator> Logger { get; init; }
		protected IConfiguration Configuration { get; init; }
		protected IServiceProvider Services { get; init; }

		protected ICollection<ulong> AvailableServers { get; init; }

		public Replicator(DiscordSocketClient client, CommandHandler commands, ILogger<Replicator> logger, IConfiguration config, IServiceProvider services)
		{
			Client = client;
			Commands = commands;
			Logger = logger;
			Configuration = config;
			Services = services;
			AvailableServers = new HashSet<ulong>();
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			Client.Log += LogAsync;
			await Commands.InstallCommandsAsync();
			Client.GuildAvailable += GuildAvailableAsync;
			Client.GuildUnavailable += GuildUnavailableAsync;
			Client.JoinedGuild += JoinedGuildAsync;
			Client.LeftGuild += LeftGuildAsync;
			Client.MessageReceived += MessageReceievedAsync;

			stoppingToken.Register(UnregisterCallbacks);
			await Task.Delay(Timeout.Infinite, stoppingToken);
		}

		private void UnregisterCallbacks()
		{
			Client.GuildAvailable -= GuildAvailableAsync;
			Client.GuildUnavailable -= GuildUnavailableAsync;
			Client.JoinedGuild -= JoinedGuildAsync;
			Client.LeftGuild -= LeftGuildAsync;
			Client.MessageReceived -= MessageReceievedAsync;
			Client.Log -= LogAsync;
			Logger.LogInformation("Shutting down Replicator");
		}

		private async Task MessageReceievedAsync(SocketMessage message)
		{
			if ((message.Author as IGuildUser)?.Guild is not SocketGuild guild)
				return;

			using IServiceScope scope = Services.CreateScope();
			using AppDbContext context = scope.ServiceProvider.GetService<AppDbContext>();

			GuildInfo info = context.GuildInfo.AsQueryable().Include(g => g.ChannelPermissions).FirstOrDefault(g => g.GuildId == guild.Id);
			ChannelPermissions permissions = info.ChannelPermissions.FirstOrDefault(c => c.GuildId == guild.Id && c.ChannelId == message.Channel.Id);
			if (permissions is null)
				permissions = context.ChannelPermissions.Add(new ChannelPermissions(guild.Id, message.Channel.Id, ChannelPermission.ReadWrite)).Entity;
			Discord.ChannelPermissions channelPerms = guild.GetUser(Client.CurrentUser.Id).GetPermissions(message.Channel as IGuildChannel);

			if (info.Enabled && (string.IsNullOrEmpty(message.Content) || message.Content[0] != '!'))
			{
				info.LastUpdate = DateTime.UtcNow;
				if (channelPerms.ViewChannel)
				{
					if (!message.Author.IsBot && channelPerms.ReadMessageHistory && permissions.Permissions.HasFlag(ChannelPermission.Read))
					{
						info.GuildMessageCount += 1;
						if (TestMessageStorable(message, info))
							AddNewMessage(context, message, info);
					}
					if (channelPerms.SendMessages && permissions.Permissions.HasFlag(ChannelPermission.Write) && !info.DisabledUsers.Where(d => d.UserId == message.Author.Id).Any())
					{
						Random rand = new Random();
						if (message.MentionedUsers.Select(u => u.Id).Contains(Client.CurrentUser.Id))
							await SendRandomMessageAsync(rand, message.Channel, info);
						if (rand.NextDouble() < info.Probability)
							await SendRandomMessageAsync(rand, message.Channel, info);
					}
				}
				context.GuildInfo.Update(info);
			}
			context.SaveChanges();
		}

		private static void AddNewMessage(AppDbContext context, IMessage message, GuildInfo info, bool save = true)
		{
			string content;
			if (message.Attachments.Any())
			{
				StringBuilder sb = new StringBuilder(message.Content);
				sb.AppendJoin(' ', message.Attachments.Select(m => m.Url));
				content = sb.ToString();
			}
			else
				content = message.Content;

			Message m = new Message(info.GuildId, message.Id, info.TargetMessageCount, content);
			info.TargetMessageCount += 1;
			context.Messages.Add(m);
			if (info.AutoUpdateProbability)
				info.Probability = (double)info.TargetMessageCount / info.GuildMessageCount;
			context.GuildInfo.Update(info);
			if (save)
				context.SaveChanges();
		}

		public static async Task SendRandomMessageAsync(Random rand, ISocketMessageChannel channel, GuildInfo info)
		{
			if (info.Messages.Any())
			{
				int next = rand.Next(info.TargetMessageCount);
				Message m = info.Messages.FirstOrDefault(m => m.Index == next);
				await channel.SendMessageAsync(m.Text, allowedMentions: info.CanMention ? AllowedMentions.All : AllowedMentions.None);
			}
			else
				await channel.SendMessageAsync("No stored messages.");
		}

		private static bool TestMessageStorable(IMessage message, GuildInfo info)
		{
			bool author = message.Author.Id == info.TargetUserId;
			bool embeds = info.CanEmbed || !(message.Embeds.Any() || message.Attachments.Any());
			bool substrings = !message.Content.ContainsAny(info.DisabledSubstrings.Select(i => i.Substring));
			return author && embeds && substrings;
		}

		internal static async Task ReadAllMessages(AppDbContext context, ILogger logger, DiscordSocketClient client, SocketGuild guild, int maxChannelRead, ISocketMessageChannel reply)
		{
			GuildInfo info = context.GuildInfo.Include(g => g.ChannelPermissions).FirstOrDefault(g => g.GuildId == guild.Id);
			info.GuildMessageCount = 0;
			info.LastUpdate = default;
			info.TargetMessageCount = 0;
			if (info.TargetUserId is null)
			{
				await reply.SendMessageAsync("Target user not selected");
				return;
			}
			context.Messages.RemoveRange(context.Messages.AsQueryable().Where(c => c.GuildId == guild.Id));

			IGuildUser currentUser = guild.GetUser(client.CurrentUser.Id);
			foreach (var channel in guild.TextChannels)
			{
				logger.LogInformation($"Attempting to read messages in channel {channel.Name} ({channel.Id})");
				await reply.SendMessageAsync($"Attempting to read messages in channel {channel.Mention}");
				Discord.ChannelPermissions permissions = currentUser.GetPermissions(channel);
				ChannelPermissions perms = info.ChannelPermissions.FirstOrDefault(c => c.ChannelId == channel.Id) ?? context.ChannelPermissions.Add(new ChannelPermissions(guild.Id, channel.Id, ChannelPermission.ReadWrite)).Entity;
				if (permissions.ReadMessageHistory && permissions.ViewChannel && perms.Permissions.HasFlag(ChannelPermission.Read))
				{
					var messageCollection = channel.GetMessagesAsync(maxChannelRead).Flatten();
					await foreach (var message in messageCollection)
					{
						if (string.IsNullOrEmpty(message.Content) || message.Content[0] != '!')
						{
							info.GuildMessageCount++;
							if (TestMessageStorable(message, info))
								AddNewMessage(context, message, info, false);
						}
					}
				}
				else
					await reply.SendMessageAsync($"No permission to read in channel {channel.Mention}");
			}
			info.Enabled = true;
			info.LastUpdate = DateTime.UtcNow;
			context.GuildInfo.Update(info);
			await context.SaveChangesAsync();
			logger.LogInformation("Read all messages in guild {name} ({id})", guild.Name, guild.Id);
			await reply.SendMessageAsync($"Read all messages and {currentUser.Mention} is now active");
		}

		internal static async Task ReadSinceTimestamp(AppDbContext context, ILogger logger, DiscordSocketClient client, SocketGuild guild, DateTime lastReceivedTime, ISocketMessageChannel reply)
		{
			logger.LogInformation("Read new messages");
			GuildInfo info = context.GuildInfo.Include(g => g.ChannelPermissions).FirstOrDefault(g => g.GuildId == guild.Id);
			HashSet<ulong> messageIds = context.Messages.AsQueryable().Where(m => m.GuildId == guild.Id).Select(m => m.MessageId).ToHashSet();

			IGuildUser currentUser = guild.GetUser(client.CurrentUser.Id);
			foreach (var channel in guild.TextChannels)
			{
				logger.LogInformation($"Reading new messages in channel {channel.Mention}");
				await reply.SendMessageAsync($"Reading new messages in channel {channel.Mention}");
				Discord.ChannelPermissions permissions = currentUser.GetPermissions(channel);
				ChannelPermissions perms = info.ChannelPermissions.FirstOrDefault(c => c.ChannelId == channel.Id) ?? context.ChannelPermissions.Add(new ChannelPermissions(guild.Id, channel.Id, ChannelPermission.ReadWrite)).Entity;
				if (permissions.ReadMessageHistory && permissions.ViewChannel && perms.Permissions.HasFlag(ChannelPermission.Read))
				{
					IMessage lastMessage = (await channel.GetMessagesAsync(1).Flatten().FirstAsync());
					DateTime currentTime = lastMessage.Timestamp.UtcDateTime;
					if (lastMessage is not null)
					{
						while (currentTime >= lastReceivedTime)
						{
							var messageCollection = channel.GetMessagesAsync(lastMessage, Direction.Before, 10).Flatten();
							await foreach (var message in messageCollection)
							{
								if (string.IsNullOrEmpty(message.Content) || message.Content[0] != '!')
								{
									lastMessage = message;
									currentTime = lastMessage.Timestamp.UtcDateTime;
									if (currentTime >= lastReceivedTime)
										break;
									if (messageIds.Contains(lastMessage.Id))
										break;
									info.GuildMessageCount += 1;
									if (TestMessageStorable(message, info))
										AddNewMessage(context, lastMessage, info, false);
								}
							}
						}
					}
				}
				else
					await reply.SendMessageAsync($"No permission to read in channel {channel.Mention}");
			}
			info.Enabled = true;
			info.LastUpdate = DateTime.UtcNow;
			context.GuildInfo.Update(info);
			await context.SaveChangesAsync();
			logger.LogInformation("Read all new messages in guild {name} ({id})", guild.Name, guild.Id);
			await reply.SendMessageAsync("Read all new messages");
		}

		private async Task GuildAvailableAsync(SocketGuild guild)
		{
			await AddGuildAsync(guild);
			AvailableServers.Add(guild.Id);
			Logger.LogInformation("Server {name} ({id}) became available", guild.Name, guild.Id);
		}

		private async Task GuildUnavailableAsync(SocketGuild guild)
		{
			await AddGuildAsync(guild);
			AvailableServers.Remove(guild.Id);
			Logger.LogInformation("Server {name} ({id}) became unavailable", guild.Name, guild.Id);
		}

		private async Task JoinedGuildAsync(SocketGuild guild)
		{
			await AddGuildAsync(guild);
			AvailableServers.Add(guild.Id);
			Logger.LogInformation("Joined Server {name} ({id})", guild.Name, guild.Id);
		}

		private async Task LeftGuildAsync(SocketGuild guild)
		{
			AvailableServers.Remove(guild.Id);
			await RemoveGuildAsync(guild);
			Logger.LogInformation("Left Server {name} ({id})", guild.Name, guild.Id);
		}

		private async Task AddGuildAsync(SocketGuild guild)
		{
			using var scope = Services.CreateScope();
			using var context = scope.ServiceProvider.GetService<AppDbContext>();

			if (!await context.GuildInfo.AsAsyncEnumerable().Select(g => g.GuildId).ContainsAsync(guild.Id))
			{
				context.GuildInfo.Add(new GuildInfo(guild.Id));
				context.ChannelPermissions.AddRange(guild.Channels.Select(c => new ChannelPermissions(guild.Id, c.Id, ChannelPermission.ReadWrite)));
				await context.SaveChangesAsync();
			}
		}

		private async Task RemoveGuildAsync(SocketGuild guild)
		{
			using var scope = Services.CreateScope();
			using var context = scope.ServiceProvider.GetService<AppDbContext>();

			GuildInfo info = await context.GuildInfo.AsAsyncEnumerable().Where(g => g.GuildId == guild.Id).FirstOrDefaultAsync();
			if (info is not null)
			{
				context.Messages.RemoveRange(info.Messages);
				context.ChannelPermissions.RemoveRange(info.ChannelPermissions);
				context.DisabledUsers.RemoveRange(info.DisabledUsers);
				context.DisabledSubstrings.RemoveRange(info.DisabledSubstrings);
				context.GuildInfo.Remove(info);
				await context.SaveChangesAsync();
			}
		}

		private async Task LogAsync(LogMessage message) => await Task.Run(() => Logger.Log(message.Severity.ToLogLevel(), "{0}", message));
	}
}
