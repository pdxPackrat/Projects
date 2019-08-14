using System;
using System.Configuration;
using Serilog;

namespace AcsListener
{
    /// <summary>Class that represents all configuration items for the AcsListener application</summary>
    class AcsListenerConfigItems
    {
        /// <summary>Initializes a new instance of the <see cref="AcsListenerConfigItems"/> class.</summary>
        public AcsListenerConfigItems()
        {
            ReadConfigItems();
        }

        /// <summary>Reads the configuration items from the application config file.</summary>
        private void ReadConfigItems()
        {
            Log.Debug("Checking the application config file for any default keys");

            // Get the CommandPort value
            try
            {
                var keyValueCommandPort = ConfigurationManager.AppSettings.Get("CommandPort");
                if (keyValueCommandPort != null)
                {
                    CommandPort = Convert.ToInt32(keyValueCommandPort);
                    Log.Debug($"Found CommandPort value: {CommandPort}");
                }
            }
            catch (FormatException ex)
            {
                Log.Error($"Error attempting to read the CommandPort configuration: {ex.Message}");
            }

            // Get the RplUrlPath
            try
            {
                var keyValueRplUrlPath = ConfigurationManager.AppSettings.Get("RplUrlPath");
                if (keyValueRplUrlPath != null)
                {
                    RplUrlPath = keyValueRplUrlPath;
                    Log.Debug($"Found RplUrlPath value: {RplUrlPath}");
                }
            }
            catch (FormatException ex)
            {
                Log.Error($"Error attempting to read the RplUrlPath configuration: {ex.Message}");
            }

            // Get the AutoReload value
            try
            {
                var keyValueAutoReload = ConfigurationManager.AppSettings.Get("AutoReload");
                if (keyValueAutoReload != null)
                {
                    AutoReload = Convert.ToBoolean(keyValueAutoReload);
                    Log.Debug($"Found AutoReload value: {AutoReload}");
                }
            }
            catch (FormatException ex)
            {
                Log.Error($"Error attempting to read the AutoReload configuration: {ex.Message}");
            }
        }

        public int CommandPort { get; set; } = 13000;    // default port of 13000, arbitrary choice on our part
        public int AcsPort { get; } = 4170;              // default port of 4170, per SMPTE 430-10:2010 specifications, not a configurable value at this time
        public string RplUrlPath { get; set; } = "";     // default path to the RPL files that are stored on the system in a website
        public bool AutoReload { get; set; } = false;  // config item whether the "RELOAD" functionality requires manual invoke, or happens automatically

        /// <summary>Converts to string.</summary>
        /// <returns>A <see cref="System.String"/> that represents this instance.</returns>
        public override string ToString()
        {
            return String.Format($"AcsListenerConfigItems: CommandPort: {CommandPort}, AcsPort: {AcsPort}, RplUrlPath: {RplUrlPath}");
        }
    }
}
