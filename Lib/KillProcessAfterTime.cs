using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using log4net;
using Timer = System.Timers.Timer;

namespace Net.HealthChecker.Lib
{
    /// <summary>
    /// This is to kill ffmpeg.exe after running 20mins (not suppose to)
    /// </summary>
	public static class KillProcessAfterTime
    {
		public static void Run()
		{
			Init();
			var timer = new Timer(60 * 1000);
			timer.Elapsed += TimerOnElapsed;
			timer.Start();
		}

		private static ILog _log = LogManager.GetLogger(typeof(KillProcessAfterTime));
		private const string PREFIXKILL = "KillProcess_";
		private static Dictionary<string, int> __processes = new Dictionary<string, int>();

		private static void Init()
		{
			try
			{
				var processes = ConfigurationManager.AppSettings.GetDictionayWithPrefix(PREFIXKILL);
				if (processes.Count > 0)
				{
					Print($"process kill [2] {processes.Count} found");
				}

				foreach (var proc in processes)
				{
					var name = proc.Key;

                    var min = int.Parse(proc.Value);
					

					Print($"process kill [3] adding Proc {name}- {min} in batch");
					__processes.Add(name, min);
				}
			}
			catch (Exception ex)
			{
				_log.Error(ex);
			}
		}

		private static void Print(string str)
		{
			//Console.WriteLine(str);
			_log.Info(str);
		}
		

		private static void TimerOnElapsed(object sender, ElapsedEventArgs e)
		{
			try
			{

				var now = DateTime.Now;
				//var nowMin = now.TimeOfDay.TotalMinutes;

				foreach (var proc in __processes)
				{
					var name = proc.Key;
					var min = __processes[name];

                    var processes = Process.GetProcessesByName(name);
                    _log.Debug($"{processes.Length} processes found . name :{name} min:{min}");
                    foreach (var process in processes)
                    {
                        var processTime = process.StartTime;
                        var minutesAgo = now.AddMinutes(-min);
                        _log.Debug($"{processTime}<{minutesAgo}?");
                        if (processTime < minutesAgo)
                        {
                            ProcessHelper.KillByName(name);
                        }
                        else
                        {
                            // do nothing
                        }
                    }
                }
			}
			catch (Exception ex)
			{
				Print(ex.Message);
				_log.Error(ex);
			}
		}
        
	}
}
