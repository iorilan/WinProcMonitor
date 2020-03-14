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
    public static class ProcessRebootService
    {
        public static void Run()
        {
            Init();
            var timer = new Timer(60 * 1000);
            timer.Elapsed += TimerOnElapsed;
            timer.Start();
        }

        private static ILog _log = LogManager.GetLogger(typeof(ProcessRebootService));
        private const string PREFIXKILL = "DailyRestartKillProc_";
        private static Dictionary<string, ServiceRebootInfo> __processes = new Dictionary<string, ServiceRebootInfo>();

        private static void Init()
        {
            try
            {
                var services = ConfigurationManager.AppSettings.GetDictionayWithPrefix(PREFIXKILL);
                if (services.Count > 0)
                {
                    Print($"process kill [2] {services.Count} found");
                }

                foreach (var service in services)
                {
                    var serviceName = service.Key;

                    var arrVal = service.Value.Split('|');
                    var path = arrVal[0];
                    var min = totalMin(arrVal[1]);

                    var restartFrequen = min;

                    Print($"process kill [3] adding Proc {serviceName} {path} in batch");
                    __processes.Add(serviceName, new ServiceRebootInfo()
                    {
                        ProcPath = path,
                        RebootFrequency = restartFrequen,
                        LastStartTime = DateTime.MinValue
                    });
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
        private static int totalMin(string setting)
        {
            Print($"process kill [total Min] parsing :{setting}");
            return int.Parse(setting);
        }

        private static void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {

                var now = DateTime.Now;
                //var nowMin = now.TimeOfDay.TotalMinutes;

                foreach (var service in __processes)
                {
                    var serviceName = service.Key;
                    var serviceInfo = __processes[serviceName];

                    if (serviceInfo.LastStartTime == DateTime.MinValue ||
                        (now - serviceInfo.LastStartTime).TotalMinutes > serviceInfo.RebootFrequency)
                    {
                        Print($"Kill Process :name:'{serviceName}' path:'{serviceInfo.ProcPath}'");
                        __processes[service.Key].LastStartTime = now;
                        ProcessHelper.KillByName(serviceName);
                        Print($"Starting process: {serviceName}");
                        ProcessHelper.Create(serviceInfo.ProcPath);
                        MakesureProcessUp(serviceName, serviceInfo);
                        Print($"Process : Done :{serviceName}");
                    }
                    else
                    {
                        MakesureProcessUp(serviceName, serviceInfo);
                        //Print($"{serviceName} last start : {serviceInfo.LastStartTime}");
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
        }

        private static void MakesureProcessUp(string serviceName, ServiceRebootInfo serviceInfo)
        {
            if (!serviceInfo.ProcPath.Contains(serviceName))
            {
                _log.Error($"[Configuration Error] process path :'{serviceInfo.ProcPath}'. but configured proc name : {serviceName}");
                return;
            }

            var count = 0;
            while (count < 10)
            {
                var procName = Process.GetProcesses()
                    .Where(x => x.ProcessName.Contains(serviceName)).ToList();
                if (procName.Count > 0)
                {
                    //Print($"Process {serviceInfo.ProcPath} restarted successfully .");
                    break;
                }
                else
                {
                    count++;
                    Print($"Process {serviceInfo.ProcPath} didnt start !!!!!! will try again later");
                    Print($"count not find a proc name {serviceName}. is name correct ?");
                    Thread.Sleep(10 * 1000);
                    ProcessHelper.Create(serviceInfo.ProcPath);
                }
            }
        }
    }
}
