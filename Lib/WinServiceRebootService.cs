using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Net.HealthChecker.Lib
{
	public static class WinServiceRebootService
	{
		private const string PREFIX = "DailyRestart_";


		private static ILog _log = LogManager.GetLogger(typeof(WinServiceRebootService));
		public static void Start()
		{
			try
			{
				Print("[1] reading config");

				var services = ConfigurationManager.AppSettings.GetDictionayWithPrefix(PREFIX);
				if (services.Count > 0)
				{
					Print($"[2] {services.Count} found");
				}

				foreach (var service in services)
				{
					var serviceName = service.Key;
					var restartFrequen = totalMin(service.Value);
					Print($"[3] adding service {serviceName} in batch");
					__services.Add(serviceName, new ServiceRebootInfo()
					{
						RebootFrequency = restartFrequen,
						LastStartTime = DateTime.MinValue
					});
				}

				RunBatch();
			}
			catch (Exception ex)
			{
				Print(ex.Message);
				_log.Error(ex);
			}
		}

		private static int totalMin(string setting)
		{
			Print($"[total Min] parsing :{setting}");
			return int.Parse(setting);
		}

		private static Dictionary<string, ServiceRebootInfo> __services = new Dictionary<string, ServiceRebootInfo>();

		private static void RunBatch()
		{
			Task.Run(() =>
			{
				while (true)
				{
					try
					{

						var now = DateTime.Now;
						//var nowMin = now.TimeOfDay.TotalMinutes;

						foreach (var service in __services)
						{
							var serviceName = service.Key;
							var serviceInfo = __services[serviceName];

							if (serviceInfo.LastStartTime == DateTime.MinValue ||
								(now - serviceInfo.LastStartTime).TotalMinutes > serviceInfo.RebootFrequency)
							{
								Print($"Stopping service {serviceName}");
								__services[service.Key].LastStartTime = now;
								WinServiceHelper.StopService(serviceName);

								var counter = 0;
								while (counter <= 100)
								{
									if (WinServiceHelper.ServiceStatus(serviceName) == ServiceControllerStatus.Stopped)
									{
										break;
									}

									counter++;
									Thread.Sleep(10*1000);
								}
								Print($"Starting service {serviceName}");
								while (true)
								{
									WinServiceHelper.StartService(serviceName);
									Thread.Sleep(10 * 1000);
									if (WinServiceHelper.ServiceStatus(serviceName) == ServiceControllerStatus.Running)
									{
										break;
									}
								}


								Print($"Done :{serviceName}");
							}
							else
							{
								//	Print($"{serviceName} last start : {serviceInfo.LastStartTime}");
								//	Print($" {(now-serviceInfo.LastStartTime).TotalMinutes} > {serviceInfo.RebootFrequency} ?");
								//wait
							}
						}
					}
					catch (Exception ex)
					{
						Print(ex.Message);
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
			//Console.WriteLine(str);
			_log.Info(str);
		}
	}

	public class ServiceRebootInfo
	{
		public DateTime LastStartTime { get; set; }
		public int RebootFrequency { get; set; }
		public string ProcPath { get; set; }
	}
}
