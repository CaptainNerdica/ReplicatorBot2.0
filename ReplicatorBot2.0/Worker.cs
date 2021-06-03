using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReplicatorBot
{
	public class ReplicatorBot : BackgroundService
	{
		protected ILogger<ReplicatorBot> Logger { get; }
		protected IConfiguration Configuration { get; }

		public ReplicatorBot(ILogger<ReplicatorBot> logger, IConfiguration config)
		{
			Logger = logger;
			Configuration = config;
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken) => throw new NotImplementedException();
	}
}
