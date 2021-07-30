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
	/// Base class to use for slash command building and executing.
	/// </summary>
	public abstract class SlashCommand
	{
		public virtual string Name { get; }

		public abstract SlashCommandCreationProperties BuildGlobalCommand(SlashCommandBuilder builder);
		public abstract Task ExecuteCommandAsync(SocketSlashCommand command);
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class SlashCommandAttribute : Attribute
	{
		public string CommandName { get; init; }

		public SlashCommandAttribute(string commandName)
		{
			CommandName = commandName;
		}
	}
}
