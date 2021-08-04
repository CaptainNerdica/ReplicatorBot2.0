using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBotCore
{
	/// <summary>
	/// Interface to use for slash command building and executing.
	/// </summary>
	public abstract class SlashCommandBase : IBuildableSlashCommand, IDisposable
	{
		public SocketSlashCommand Command { get; set; }

		public abstract void BuildCommand(SlashCommandBuilder builder);

		public virtual void Dispose() => GC.SuppressFinalize(this);
	}

	public interface IGlobalSlashCommand
	{
		public SocketSlashCommand Command { get; set; }
		public Task ExecuteGlobalCommandAsync();
	}

	public interface IGuildSlashCommand
	{
		public SocketSlashCommand Command { get; set; }
		public Task ExecuteGuildCommandAsync(ulong guildId);
	}

	public interface IBuildableSlashCommand
	{
		public void BuildCommand(SlashCommandBuilder builder);
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class SlashCommandAttribute : Attribute
	{
		public string Name { get; init; }

		public SlashCommandAttribute(string name) => Name = name;
	}
}
