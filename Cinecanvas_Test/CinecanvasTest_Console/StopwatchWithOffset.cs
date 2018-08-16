using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CinecanvasTest_Console
{
    public class StopwatchWithOffset
    {
        private Stopwatch _stopwatch = null;
        TimeSpan _offsetTimeSpan;

        public StopwatchWithOffset (TimeSpan offsetElapsedTimeSpan)
        {
            _offsetTimeSpan = offsetElapsedTimeSpan;
            _stopwatch = new Stopwatch();
        }

        // public StopwatchWithOffset()
        // {
        //     _offsetTimeSpan = new TimeSpan(0, 0, 0, 0, 0);
        //     _stopwatch = new Stopwatch();
        // }

        public void Start()
        {
            _stopwatch.Start();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }

        public TimeSpan ElapsedTimeSpan
        {
            get
            {
                return _stopwatch.Elapsed + _offsetTimeSpan;
            }

            set
            {
                _offsetTimeSpan = value;
            }
        }
    }
}
