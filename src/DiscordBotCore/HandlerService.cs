using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DiscordBotCore
{
	public abstract class HandlerService
	{
		public void InstallService() => InstallServiceAsync().GetAwaiter().GetResult();
		public abstract Task InstallServiceAsync();
		public void UninstallService() => UninstallServiceAsync().GetAwaiter().GetResult();
		public abstract Task UninstallServiceAsync();

		public static T Instantiate<T>(IServiceProvider services, bool install = true, params object[] parameters) where T : HandlerService
		{
			T obj = (T)ActivatorUtilities.CreateInstance(services, typeof(T), parameters);
			if (install)
				obj.InstallService();
			return obj;
		}
		public static async Task<T> InstantiateAsync<T>(IServiceProvider services, bool install = true, params object[] parameters) where T : HandlerService
		{
			T obj = (T)ActivatorUtilities.CreateInstance(services, typeof(T), parameters);
			if (install)
				await obj.InstallServiceAsync();
			return obj;
		}
	}
}