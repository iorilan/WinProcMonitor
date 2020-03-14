
using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using log4net;

public static class WinServiceHelper
{
	private static ILog log = LogManager.GetLogger(typeof(WinServiceHelper));

	public static ServiceController GetService(string serviceName)
	{
		ServiceController[] services = ServiceController.GetServices();
		return services.FirstOrDefault(_ => _.ServiceName.ToUpper().Contains(serviceName.ToUpper()));
	}
	public static bool IsServiceRunning(string serviceName)
	{
		const int retryTimes = 30;
		ServiceControllerStatus status;
		uint counter = 0;
		do
		{
			ServiceController service = GetService(serviceName);
			if (service == null)
			{
				return false;
			}

			Thread.Sleep(100);
			status = service.Status;
		} while (!(status == ServiceControllerStatus.Stopped ||
				   status == ServiceControllerStatus.Running) &&
				 (++counter < retryTimes));
		return status == ServiceControllerStatus.Running;
	}

	public static ServiceControllerStatus ServiceStatus(string serviceName)
	{
		ServiceController service = GetService(serviceName);
		if (service == null)
		{
			throw new ApplicationException(string.Format("Service with name '{0}' not found", serviceName));
		}

		return service.Status;
	}



	public static bool IsServiceInstalled(string serviceName)
	{
		return GetService(serviceName) != null;
	}

	public static void StartService(string serviceName)
	{
		ServiceController controller = GetService(serviceName);
		if (controller == null)
		{
			return;
		}

		controller.Start();
		controller.WaitForStatus(ServiceControllerStatus.Running);
	}

	public static void StopService(string serviceName)
	{
		ServiceController controller = GetService(serviceName);
		if (controller == null)
		{
			return;
		}

		controller.Stop();
		controller.WaitForStatus(ServiceControllerStatus.Stopped);
	}

	public static void KillIt(string serviceName)
	{
		System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcesses();
		if (processes.Length > 0)
		{
			foreach (System.Diagnostics.Process p in processes)
			{
				if (p.ProcessName.ToUpper().Contains(serviceName.ToUpper()))
				{
					p.Kill();
				}
			}
		}
	}


	public static void TryStopTooLongThenKillIt(string serviceName, int moreThanSecondsThenKill)
	{
		var second = 0;
		while (second < moreThanSecondsThenKill)
		{
			var status = ServiceStatus(serviceName);
			if (status == ServiceControllerStatus.Stopped ||
				status == ServiceControllerStatus.Paused)
			{
				log.InfoFormat("[TryStopIfMoreThanSecondsThenKillIt] : Service stopped.");
				return;
			}

			if (status == ServiceControllerStatus.Running)
			{
				try
				{
					log.InfoFormat("[TryStopIfMoreThanSecondsThenKillIt] : |+++++++++++Stopping Service Start+++++++++++|");
					StopService(serviceName);
					log.InfoFormat("[TryStopIfMoreThanSecondsThenKillIt] : |+++++++++++Stopping Service End+++++++++++|");
				}
				catch (Exception ex)
				{
					log.Error(ex);
				}
			}
			else
			{
				log.InfoFormat("[TryStopIfMoreThanSecondsThenKillIt] : service status is {0}", status.ToString());
			}



			second += 1;
			Thread.Sleep(1000);
		}

		try
		{
			log.InfoFormat("|+++++++++++[TryStopIfMoreThanSecondsThenKillIt] :Killing Service Start+++++++++++|");
			KillIt(serviceName);
			log.InfoFormat("|+++++++++++[TryStopIfMoreThanSecondsThenKillIt] :Killing Service End+++++++++++|");
		}
		catch (Exception ex)
		{
			log.InfoFormat("[TryStopIfMoreThanSecondsThenKillIt] :Kill Service Exception");
			log.Error(ex);
		}
	}
}
