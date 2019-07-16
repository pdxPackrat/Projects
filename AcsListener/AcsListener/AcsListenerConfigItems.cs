using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcsListener
{
    class AcsListenerConfigItems
    {
        private int _acsPort = 4170;       // default port of 4170
        private int _commandPort = 13000;  // default port of 13000

        public AcsListenerConfigItems()
        {
            ReadConfigItems();
        }

        private void ReadConfigItems()
        {
            string keyValue;

            // Get the CommandPort value
            keyValue = ConfigurationManager.AppSettings.Get("CommandPort");

            if (keyValue != null)
            {
                CommandPort = Convert.ToInt32(keyValue);
            }
        }

        public int CommandPort
        {
            get
            {
                return _commandPort;
            }
            set
            {
                _commandPort = value;
            }

        }

        public int AcsPort
        {
            get
            {
                return _acsPort;
            }
        }
    }
}
