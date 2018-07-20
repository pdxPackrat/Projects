using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CinecanvasTest_Console
{
    class Subtitle
    {
        public int SpotNumber;
        TimeSpan FadeUpTime = new TimeSpan(0, 0, 0);
        TimeSpan FadeDownTime = new TimeSpan(0, 0, 0);
        TimeSpan TimeIn = new TimeSpan(0, 0, 0);
        TimeSpan TimeOut = new TimeSpan(0, 0, 0);
    }

    class SubtitleText
    {
        string subtitleText;

        int vPosition;
        int vAlign;
        int hAlign;
        int Direction;
    }
}
