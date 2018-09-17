using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RplCreator
{
    public class RplReelDuration
    {
        private UInt64 _editUnits;
        private UInt64 _rateNumerator;
        private UInt64 _rateDenominator;
        private String _editRate;
        private String _reelDuration;
        private uint _hours;
        private uint _minutes;
        private uint _seconds;

        public RplReelDuration(string editRate, string reelDuration)
        {
            ParseEditRate(editRate);
            ParseReelDuration(reelDuration);
            CalculateEditUnits();
        }

        private void CalculateEditUnits()
        {
            ulong TotalSeconds = ((3600 * _hours) + (60 * _minutes) + (_seconds));
            _editUnits = ((TotalSeconds * _rateNumerator) / _rateDenominator);
        }

        private void ParseReelDuration(string reelDuration)
        {
            var DurationSplit = reelDuration.Split(':');

            if (DurationSplit.Length >= 3) // Expected length of 3 in format of HH:MM:SS
            {
                _hours = uint.Parse(DurationSplit[0]);
                _minutes = uint.Parse(DurationSplit[1]);
                _seconds = uint.Parse(DurationSplit[2]);
            }
            else if (DurationSplit.Length == 2) // Presumably only HH:MM was supplied?
            {
                _hours = uint.Parse(DurationSplit[0]);
                _minutes = uint.Parse(DurationSplit[1]);
                _seconds = 0;
            }
            else if (DurationSplit.Length == 1) // Presumably only MM was supplied?
            {
                _hours = 0;
                _minutes = uint.Parse(DurationSplit[0]);
                _seconds = 0;
            }
            else
            {
                string message = "Error: the ReelDuration value was blank";
                throw new FormatException(message);
            }
        }

        private void ParseEditRate(string editRate)
        {
            var RateSplit = editRate.Split(' ');

            if (RateSplit.Length >= 2) // expected length of 2
            {
                _rateNumerator = UInt64.Parse(RateSplit[0]);
                _rateDenominator = UInt64.Parse(RateSplit[1]);
            }
            else if (RateSplit.Length == 1)
            {
                _rateNumerator = UInt64.Parse(RateSplit[0]);
                _rateDenominator = 1;
            }
            else
            {
                String message = "Error: unexpected format for the value of the EditRate: " + editRate;
                throw new FormatException(message);
            }
        }

        public UInt64 EditUnits
        {
            get
            {
                return _editUnits;
            }
        }
    }
}
