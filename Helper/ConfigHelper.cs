using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIPCallHandler.Helper
{
    public static class ConfigHelper
    {
        public static void SetSetting(string key, string value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings[key].Value = value;
            config.Save(ConfigurationSaveMode.Full, true);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
