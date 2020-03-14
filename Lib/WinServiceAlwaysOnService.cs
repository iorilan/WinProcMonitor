using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Net.HealthChecker.Lib
{
	public static class WinServiceAlwaysOnService
	{
		private const string PREFIX = "AlwaysOnline_";
		private static ILog _log = LogManager.GetLogger(typeof(WinServiceAlwaysOnService));
		private static List<string> __services =new List<string>();

		//TODO
		//private static IList<string> __processes = new List<string>();

		public static void Start()
		{
			try
			{
				Print("[AlwaysOnlineService. 1] reading config");

				var services = ConfigurationManager.AppSettings.GetDictionayWithPrefix(PREFIX);
				if (services.Count == 0)
				{
					Print($"[AlwaysOnlineService .2] {services.Count} found");
				}

				foreach (var service in services)
				{
					var serviceName = service.Key;
					
					Print($"[AlwaysOnlineService. 3] adding service {serviceName} in batch");
					__services.Add(serviceName);
				}

				RunBatch();
			}
			catch (Exception ex)
			{
				Print(ex.Message);
				_log.Error(ex);
			}
		}

		private static void RunBatch()
		{
			Task.Run(() =>
			{
				while (true)
				{
					try
					{
						if (__services.Count > 0)
						{
							foreach (var service in __services)
							{
								if (!WinServiceHelper.IsServiceInstalled(service))
								{
									_log.Error($"service {service} not installed!");
								}

								if (!WinServiceHelper.IsServiceRunning(service))
								{
									_log.Error($"service {service} is down. now bring is up .");
									WinServiceHelper.StartService(service);
									_log.Info(
										$"service {service} status is : {WinServiceHelper.ServiceStatus(service).ToString()}");
								}
							}
						}

					}
					catch (Exception ex)
					{
						_log.Error(ex);
					}
					finally
					{
						Thread.Sleep(60000);
					}
				}

			});
		}

		private static void Print(string str)
		{
			Console.WriteLine(str);
			_log.Info(str);
		}
	}
}
