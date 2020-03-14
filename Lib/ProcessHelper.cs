using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Net.HealthChecker.Lib
{
	public static class ProcessHelper
	{
		private static ILog _log = LogManager.GetLogger(typeof(ProcessHelper));

		public static Process Create(string path, string parameters = "")
		{
			//log.Info(cmd);
			ProcessStartInfo startInfo = new ProcessStartInfo(path, parameters);
			return Process.Start(startInfo);
		}

		public static void KillByName(string name)
		{
			var procs = Process.GetProcesses().Where(x => x.ProcessName.Contains(name)).ToList();
			if (procs.Count > 0)
			{
				foreach (var process in procs)
				{
					if (process != null)
					{
						_log.Debug($"killing {process.ProcessName}");
						while (true)
						{
							try
							{
								process.Kill();
								break;
							}
							catch (Exception ex)
							{
								_log.Error(ex);
								Thread.Sleep(10 * 1000);
							}
						}
					}
					else
					{
						_log.Error($"didnt find proc with name {name} !!!!!!!!!!!1");
					}
				}
				
			}
			
		}
	}
}
