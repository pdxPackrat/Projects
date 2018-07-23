using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinecanvasTest_Console
{
    public class subtitleTimeEntry
    {
        // private int hoursField;
        // private int minutesField;
        // private int secondsField;
        // private int millisecondsField;
        
        public subtitleTimeEntry(string timeEntry, int tickRate)
        {
            // Need some string validation here at some point

            var splitString = timeEntry.Split(':');

            if (splitString.Length == 4)
            {
                // This is what we are looking for

                hours = int.Parse(splitString[0]);
                minutes = int.Parse(splitString[1]);
                seconds = int.Parse(splitString[2]);
                milliseconds = (int.Parse(splitString[3]) * tickRate);

                time = new TimeSpan(0, hours, minutes, seconds, milliseconds);  // the first 0 is for the days value
            }
        }

        public int hours { get; set; }
        public int minutes { get; set; }
        public int seconds { get; set; }
        public int milliseconds { get; set; }
        public TimeSpan time { get; set; }

    }
}
