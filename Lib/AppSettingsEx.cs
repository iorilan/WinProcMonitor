using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using log4net;

namespace Net.HealthChecker.Lib
{
	public static class AppSettingsEx
	{
		private static ILog _log = LogManager.GetLogger(typeof(AppSettingsEx));

		/// <summary>
		/// input :
		/// <key="prefix_k1", value="v1"></key>
		/// <key="prefix_k2", value="v2"></key>
		/// ...
		/// output:
		/// Dictionary ({k1:v1},{k2:v2})...
		/// </summary>
		/// <param name="appSettings"></param>
		/// <param name="key"></param>
		/// <param name="prefix"></param>
		/// <returns></returns>
		public static Dictionary<string, string> GetDictionayWithPrefix(this NameValueCollection appSettings, string prefix)
		{
			var keys = ConfigurationManager.AppSettings.AllKeys.Where(x => x.Contains(prefix)).ToList();
			if (keys.Count == 0)
			{
				_log.Warn($"no prefix '{prefix}' found in appsettings . check your config-appsettings");
				return new Dictionary<string, string>();
			}

			var dict = new Dictionary<string, string>();
			foreach (var k in keys)
			{
				var keyName = k.Split('_')[1];
				var value = ConfigurationManager.AppSettings[k];
				dict.Add(keyName, value);
			}

			return dict;
		}
	}
}
