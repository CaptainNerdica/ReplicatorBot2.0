using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ReplicatorBot.SlashCommands;

public class ReplyCommand : InteractionModuleBase<SocketInteractionContext>
{
	protected ReplicatorContext ReplicatorContext { get; }
	protected ILogger<ReplyCommand> Logger { get; }

	public ReplyCommand(ReplicatorContext replicatorContext, ILogger<ReplyCommand> logger)
	{
		ReplicatorContext = replicatorContext;
		Logger = logger;
	}

	private async Task<bool> HandleStickersAsync(ulong[] stickerIds, AllowedMentions allowedMentions)
	{
		List<FileAttachment> attachments = [];
		using HttpClient httpClient = new();

		foreach (var stickerId in stickerIds)
		{
			ISticker sticker = await Context.Client.GetStickerAsync(stickerId);
			if (sticker is null)
				continue;

			attachments.Add(await Replicator.StickerToAttachmentAsync(sticker, httpClient));
		}

		if (attachments.Count == 0)
			return false;

		if (attachments.Count > 0)
		{
			await RespondWithFilesAsync(attachments, allowedMentions: allowedMentions);
			attachments.ForEach(attachments => attachments.Dispose());
		}

		return true;
	}

	private async Task<bool> SendMessageAysnc(Message message, GuildConfig config)
	{
		switch (message.Type)
		{
			case MessageType.Raw:
				await RespondAsync(message.Text, allowedMentions: config.GetAllowedMentions());
				return true;
			case MessageType.Sticker:
				ulong[] stickerIds = JsonSerializer.Deserialize<ulong[]>(message.Text) ?? throw new InvalidOperationException("Invalid sticker id");
				return await HandleStickersAsync(stickerIds, config.GetAllowedMentions());
			default:
				return false;
		};
	}

	[SlashCommand("reply", "Get a random response from the bot")]
	public async Task ReplyAsync([Summary("index", "Message index. -1 indicates random")] int index = -1)
	{
		GuildConfig config = GuildConfig.Get(ReplicatorContext, Context.Guild.Id)!;

		if (index < -1 || index >= config.Messages.Count)
		{
			await RespondAsync($"Message index out of bounds (0...{config.Messages.Count - 1})", ephemeral: true);
			return;
		}

		Message m = index == -1 ? Replicator.Default!.RetrieveRandomMessage(config) : config.Messages.First(m => m.Index == index);

		while (!await SendMessageAysnc(m, config).ConfigureAwait(false))
		{
			if (m.Index == -1)
			{
				m = Replicator.Default!.RetrieveRandomMessage(config);
			}
			else
			{
				await RespondAsync("Failed to send message", ephemeral: true);
				break;
			}
		}
	}
}
