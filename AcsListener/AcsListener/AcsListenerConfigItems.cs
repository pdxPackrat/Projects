using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace AcsListener
{
    class AcsListenerConfigItems
    {
        public AcsListenerConfigItems()
        {
            ReadConfigItems();
        }

        private void ReadConfigItems()
        {
            Log.Debug("Checking the application config file for any default keys");

            // Get the CommandPort value
            var keyValueCommandPort = ConfigurationManager.AppSettings.Get("CommandPort");
            if (keyValueCommandPort != null)
            {
                CommandPort = Convert.ToInt32(keyValueCommandPort);
                Log.Debug($"Found CommandPort value: {CommandPort}");
            }

            // Get the RplUrlPath
            var keyValueRplUrlPath = ConfigurationManager.AppSettings.Get("RplUrlPath");
            if (keyValueRplUrlPath != null)
            {
                RplUrlPath = keyValueRplUrlPath;
                Log.Debug($"Found RplUrlPath value: {RplUrlPath}");
            }
        }

        public int CommandPort { get; set; } = 13000;  // default port of 13000, arbitrary choice on our part
        public int AcsPort { get; } = 4170;            // default port of 4170, per SMPTE 430-10:2010 specifications
        public string RplUrlPath { get; set; } = "";   // default path to the RPL files that are stored on the system in a website
    }
}
