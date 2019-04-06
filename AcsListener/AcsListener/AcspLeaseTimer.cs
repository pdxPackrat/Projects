using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using System.Net.Sockets;

namespace AcsListener
{
    public class AcspLeaseTimer : System.Timers.Timer
    {
        public NetworkStream Stream;

        /// <summary>
        /// Extends the base class Timers.Timer and provides a NetworkStream (for AcspGetStatus communication) and 
        /// a parameter for number of seconds to trigger
        /// </summary>
        /// <param name="inputStream">Destination NetworkStream for the AcspGetStatus communication</param>
        /// <param name="leaseTimerMsec">Number of milliseconds the timer ticks before triggering the elapsed callback</param>
        public AcspLeaseTimer(NetworkStream inputStream, uint leaseTimerMsec)
        {
            Stream = inputStream;
            base.Interval = leaseTimerMsec;
        }
    }
}
