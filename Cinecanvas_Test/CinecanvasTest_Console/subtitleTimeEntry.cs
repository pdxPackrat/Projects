using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinecanvasTest_Console
{
    public class SubtitleTimeEntry
    {
        // private int hoursField;
        // private int minutesField;
        // private int secondsField;
        // private int millisecondsField;
        
        public SubtitleTimeEntry(string timeEntry, int tickRate)
        {
            // Need some string validation here at some point

            var SplitString = timeEntry.Split(':');

            if (SplitString.Length == 4)
            {
                // This is what we are looking for

                Hours = int.Parse(SplitString[0]);
                Minutes = int.Parse(SplitString[1]);
                Seconds = int.Parse(SplitString[2]);
                Milliseconds = (int.Parse(SplitString[3]) * tickRate);

                Time = new TimeSpan(0, Hours, Minutes, Seconds, Milliseconds);  // the first 0 is for the days value
            }
            else
            {
                string errorMessage = "Invalid format for the TimeEntry object: " + timeEntry;
                throw new FormatException(errorMessage);
            }
        }

        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public int Milliseconds { get; set; }
        public TimeSpan Time { get; set; }

    }
}
