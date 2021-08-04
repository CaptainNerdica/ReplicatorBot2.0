using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplicatorBot
{
	public class Message
	{
		public ulong GuildId { get; set; }
		public ulong MessageId { get; set; }
		public int Index { get; set; }
		public string Text { get; set; }

		public Message(ulong guildId, ulong messageId, int index, string text)
		{
			GuildId = guildId;
			MessageId = messageId;
			Index = index;
			Text = text;
		}

		public virtual GuildConfig GuildConfig { get; set; }

		public static Message Get(ReplicatorContext context, ulong messageId) => context.Messages.FirstOrDefault(m => m.MessageId == messageId);
		public static Message Get(ReplicatorContext context, ulong guildId, int index) => context.Messages.FirstOrDefault(m => m.GuildId == guildId && m.Index == index);
		public static ValueTask<Message> GetAsync(ReplicatorContext context, ulong messageId) => context.Messages.AsAsyncEnumerable().FirstOrDefaultAsync(m => m.MessageId == messageId);
		public static ValueTask<Message> GetAsync(ReplicatorContext context, ulong guildId, int index) => context.Messages.AsAsyncEnumerable().FirstOrDefaultAsync(m => m.GuildId == guildId && m.Index == index);
		public static Message Add(ReplicatorContext context, Message message) => context.Messages.Add(message).Entity;
		public static Message Update(ReplicatorContext context, Message message) => context.Messages.Update(message).Entity;
		public static void Delete(ReplicatorContext context, Message message) => context.Messages.Remove(message);
		public static void Delete(ReplicatorContext context, ulong messageId) => Delete(context, Get(context, messageId));
	}
}
