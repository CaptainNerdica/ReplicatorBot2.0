using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBotCore;
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
		protected InteractionHandler InteractionHandler { get; init; }
		protected ILogger<Replicator> Logger { get; init; }
		protected IConfiguration Configuration { get; init; }
		protected IServiceProvider Services { get; init; }

		protected ICollection<ulong> AvailableServers { get; init; }

		public Replicator(DiscordSocketClient client, CommandHandler commands, InteractionHandler interactions, ILogger<Replicator> logger, IConfiguration config, IServiceProvider services)
		{
			Client = client;
			Commands = commands;
			InteractionHandler = interactions;
			Logger = logger;
			Configuration = config;
			Services = services;
			AvailableServers = new HashSet<ulong>();
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			Client.Log += LogAsync;
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
			using ReplicatorContext context = scope.ServiceProvider.GetService<ReplicatorContext>();

			GuildConfig config = context.GuildConfig.AsQueryable().Include(g => g.ChannelPermissions).FirstOrDefault(g => g.GuildId == guild.Id);
			ChannelPermissions permissions = config.ChannelPermissions.FirstOrDefault(c => c.GuildId == guild.Id && c.ChannelId == message.Channel.Id);
			if (permissions is null)
				permissions = context.ChannelPermissions.Add(new ChannelPermissions(guild.Id, message.Channel.Id, ChannelPermission.ReadWrite)).Entity;
			Discord.ChannelPermissions channelPerms = guild.GetUser(Client.CurrentUser.Id).GetPermissions(message.Channel as IGuildChannel);

			if (config.Enabled && (string.IsNullOrEmpty(message.Content) || message.Content[0] != '!'))
			{
				config.LastUpdate = DateTime.UtcNow;
				if (channelPerms.ViewChannel)
				{
					if (!message.Author.IsBot && channelPerms.ReadMessageHistory && permissions.Permissions.HasFlag(ChannelPermission.Read))
					{
						config.GuildMessageCount += 1;
						if (TestMessageStorable(message, config))
							AddNewMessage(context, message, config);
					}
					if (channelPerms.SendMessages && permissions.Permissions.HasFlag(ChannelPermission.Write) && !config.DisabledUsers.Where(d => d.UserId == message.Author.Id).Any())
					{
						Random rand = new Random();
						if (message.MentionedUsers.Select(u => u.Id).Contains(Client.CurrentUser.Id))
							await SendRandomMessageAsync(rand, message.Channel, config);
						if (rand.NextDouble() < config.Probability)
							await SendRandomMessageAsync(rand, message.Channel, config);
					}
				}
				context.GuildConfig.Update(config);
			}
			context.SaveChanges();
		}

		private static void AddNewMessage(ReplicatorContext context, IMessage message, GuildConfig config, bool save = true)
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

			Message m = new Message(config.GuildId, message.Id, config.TargetMessageCount, content);
			config.TargetMessageCount += 1;
			context.Messages.Add(m);
			if (config.AutoUpdateProbability)
				config.Probability = (double)config.TargetMessageCount / config.GuildMessageCount;
			context.GuildConfig.Update(config);
			if (save)
				context.SaveChanges();
		}

		public static async Task SendRandomMessageAsync(Random rand, ISocketMessageChannel channel, GuildConfig config)
		{
			using var typing = channel.EnterTypingState();
			if (config.Messages.Any())
			{
				int next = rand.Next(config.TargetMessageCount);
				Message m = config.Messages.FirstOrDefault(m => m.Index == next);
				await channel.SendMessageAsync(m.Text, allowedMentions: config.CanMention ? AllowedMentions.All : AllowedMentions.None);
			}
			else
				await channel.SendMessageAsync("No stored messages.");
		}

		private static bool TestMessageStorable(IMessage message, GuildConfig config)
		{
			bool author = message.Author.Id == config.TargetUserId;
			bool embeds = config.CanEmbed || !(message.Embeds.Any() || message.Attachments.Any());
			bool substrings = !message.Content.ContainsAny(config.DisabledSubstrings.Select(i => i.Substring));
			return author && embeds && substrings;
		}

		internal static async Task ReadAllMessages(ReplicatorContext context, ILogger logger, DiscordSocketClient client, SocketGuild guild, int maxChannelRead, ISocketMessageChannel reply)
		{
			GuildConfig config = context.GuildConfig.Include(g => g.ChannelPermissions).FirstOrDefault(g => g.GuildId == guild.Id);
			config.GuildMessageCount = 0;
			config.LastUpdate = default;
			config.TargetMessageCount = 0;
			if (config.TargetUserId is null)
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
				ChannelPermissions perms = config.ChannelPermissions.FirstOrDefault(c => c.ChannelId == channel.Id) ?? context.ChannelPermissions.Add(new ChannelPermissions(guild.Id, channel.Id, ChannelPermission.ReadWrite)).Entity;
				if (permissions.ReadMessageHistory && permissions.ViewChannel && perms.Permissions.HasFlag(ChannelPermission.Read))
				{
					var messageCollection = channel.GetMessagesAsync(maxChannelRead).Flatten();
					await foreach (var message in messageCollection)
					{
						if (string.IsNullOrEmpty(message.Content) || message.Content[0] != '!')
						{
							config.GuildMessageCount++;
							if (TestMessageStorable(message, config))
								AddNewMessage(context, message, config, false);
						}
					}
				}
				else
					await reply.SendMessageAsync($"No permission to read in channel {channel.Mention}");
			}
			config.Enabled = true;
			config.LastUpdate = DateTime.UtcNow;
			context.GuildConfig.Update(config);
			await context.SaveChangesAsync();
			logger.LogInformation("Read all messages in guild {name} ({id})", guild.Name, guild.Id);
			await reply.SendMessageAsync($"Read all messages and {currentUser.Mention} is now active");
		}

		internal static async Task ReadSinceTimestamp(ReplicatorContext context, ILogger logger, DiscordSocketClient client, SocketGuild guild, DateTime lastReceivedTime, ISocketMessageChannel reply)
		{
			logger.LogInformation("Read new messages");
			GuildConfig config = context.GuildConfig.Include(g => g.ChannelPermissions).FirstOrDefault(g => g.GuildId == guild.Id);
			HashSet<ulong> messageIds = context.Messages.AsQueryable().Where(m => m.GuildId == guild.Id).Select(m => m.MessageId).ToHashSet();

			IGuildUser currentUser = guild.GetUser(client.CurrentUser.Id);
			foreach (var channel in guild.TextChannels)
			{
				logger.LogInformation($"Reading new messages in channel {channel.Mention}");
				await reply.SendMessageAsync($"Reading new messages in channel {channel.Mention}");
				Discord.ChannelPermissions permissions = currentUser.GetPermissions(channel);
				ChannelPermissions perms = config.ChannelPermissions.FirstOrDefault(c => c.ChannelId == channel.Id) ?? context.ChannelPermissions.Add(new ChannelPermissions(guild.Id, channel.Id, ChannelPermission.ReadWrite)).Entity;
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
									config.GuildMessageCount += 1;
									if (TestMessageStorable(message, config))
										AddNewMessage(context, lastMessage, config, false);
								}
							}
						}
					}
				}
				else
					await reply.SendMessageAsync($"No permission to read in channel {channel.Mention}");
			}
			config.Enabled = true;
			config.LastUpdate = DateTime.UtcNow;
			context.GuildConfig.Update(config);
			await context.SaveChangesAsync();
			logger.LogInformation("Read all new messages in guild {name} ({id})", guild.Name, guild.Id);
			await reply.SendMessageAsync("Read all new messages");
		}
		#region Guild Availability Handlers
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
			using var context = scope.ServiceProvider.GetService<ReplicatorContext>();

			if (!await context.Guild.AsAsyncEnumerable().Select(g => g.GuildId).ContainsAsync(guild.Id))
			{
				context.Guild.Add(new Guild(guild.Id));
				context.GuildConfig.Add(new GuildConfig(guild.Id));
				context.ChannelPermissions.AddRange(guild.Channels.Select(c => new ChannelPermissions(guild.Id, c.Id, ChannelPermission.ReadWrite)));
				await context.SaveChangesAsync();
			}
		}

		private async Task RemoveGuildAsync(SocketGuild guild)
		{
			using var scope = Services.CreateScope();
			using var context = scope.ServiceProvider.GetService<ReplicatorContext>();

			GuildConfig info = await context.GuildConfig.AsAsyncEnumerable().Where(g => g.GuildId == guild.Id).FirstOrDefaultAsync();
			if (info is not null)
			{
				context.Messages.RemoveRange(info.Messages);
				context.ChannelPermissions.RemoveRange(info.ChannelPermissions);
				context.DisabledUsers.RemoveRange(info.DisabledUsers);
				context.DisabledSubstrings.RemoveRange(info.DisabledSubstrings);
				context.GuildConfig.Remove(info);
			}
			Guild g = await context.Guild.AsAsyncEnumerable().FirstOrDefaultAsync(g => g.GuildId == guild.Id);
			context.Guild.Remove(g);
			await context.SaveChangesAsync();
		}
		#endregion

		private async Task LogAsync(LogMessage message) => await Task.Run(() => Logger.Log(message.Severity.ToLogLevel(), "{0}", message));
	}
}
