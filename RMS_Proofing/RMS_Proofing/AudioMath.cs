using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS_Proofing
{
    /// <summary>Utility class that provides some Audio-related mathematical calculations
    /// including RootMeanSquare and ConvertToDbfs (decibel FullScale, for digital audio)</summary>
    public static class AudioMath
    {

        /// <summary>  Calculates an "average" from a set of values, with a slight weighting towards larger values (nullified in this case by rounding performed at the end)</summary>
        /// <param name="pcmData">  An array of type Int16 representing a set of audio PCM values</param>
        /// <returns>Returns the RootMeanSquare value</returns>
        public static Int16 RootMeanSquare(Int16[] pcmData)
        {
            double sum = 0;
            double temp = 0;
            Int16 result = 0;

            for (int i = 0; i < pcmData.Length; i++)
            {
                sum += (pcmData[i] * pcmData[i]);
            }

            temp = Math.Round(Math.Sqrt(sum / pcmData.Length));

            if (temp > Int16.MaxValue)
            {
                temp = Int16.MaxValue;
            }

            result = (Int16)temp;

            return result;
        }

        /// <summary>Calculates an "average" from a set of values, with a slight weighting towards larger values (nullified in this case by rounding performed at the end)</summary>
        /// <param name="pcmData">An array of type Int32 representing a set of audio PCM values</param>
        /// <returns>Returns the RootMeanSquare value</returns>
        public static Int32 RootMeanSquare(Int32[] pcmData)
        {
            double sum = 0;
            double temp = 0;
            Int32 result = 0;

            for (int i = 0; i < pcmData.Length; i++)
            {
                sum += (pcmData[i] * pcmData[i]);
            }

            temp = (Int32)(Math.Round(Math.Sqrt(sum / pcmData.Length)));

            if (temp > Int32.MaxValue)
            {
                temp = Int32.MaxValue;
            }

            result = (Int32)temp;

            return result;
        }

        /// <summary>Calculates an "average" from a set of values, with a slight weighting towards larger values (nullified in this case by rounding performed at the end)</summary>
        /// <param name="ieeeFloatAudioData">An array of type Float that represents a set of audio sample data, converted to ieee Float</param>
        /// <returns>Returns the RootMeanSquare value</returns>
        public static float RootMeanSquare(float[] ieeeFloatAudioData)
        {
            float result = 0f;

            result = RootMeanSquare(ieeeFloatAudioData, ieeeFloatAudioData.Length);

            return result;
        }

        /// <summary>Calculates an "average" from a set of values, with a slight weighting towards larger values (nullified in this case by rounding performed at the end)</summary>
        /// <param name="ieeeFloatAudioData">An array of type Float that represents a set of audio sample data, converted to ieee Float</param>
        /// <param name="totalSamplesToCalculate">Total number of samples (less than or equal to size of the Float array) to perform calculations against</param>
        /// <returns>Returns the RootMeanSquare value</returns>
        public static float RootMeanSquare(float[] ieeeFloatAudioData, int totalSamplesToCalculate)
        {
            double sum = 0;
            double temp = 0;
            float result = 0f;

            for (int i = 0; i < totalSamplesToCalculate; i++)
            {
                sum += (ieeeFloatAudioData[i] * ieeeFloatAudioData[i]);
            }

            temp = (float)(Math.Sqrt(sum / totalSamplesToCalculate));

            // check to see if we're clipping above maximum expected value of 1.0f
            if (temp > 1.0f)
            {
                temp = 1.0f;
            }

            result = (float)temp;

            return result;

        }
        /// <summary>
        /// Calculate dBFS based on previous RMS calculations
        /// </summary>
        /// <param name="input">RMS value of type Int16</param>
        /// <returns>A value between -90 and -0</returns>
        public static int ConvertToDbfs(Int16 input)
        {
            int dbfs;

            dbfs = (int)(Math.Round(20 * Math.Log10(input / (double)Int16.MaxValue)));

            if (dbfs < -90)
            {
                dbfs = -90;
            }

            return dbfs;
        }

        /// <summary>
        /// Calculate dBFS based on previous RMS calculations
        /// </summary>
        /// <param name="input">RMS value of type Int32</param>
        /// <returns>A value between -90 and -0</returns>
        public static int ConvertToDbfs(Int32 input)
        {
            int dbfs;

            dbfs = (int)(Math.Round(20 * Math.Log10(input / (double)Int32.MaxValue)));

            if (dbfs < -90)
            {
                dbfs = -90;
            }

            return dbfs;
        }

        /// <summary>Calculate dBFS based on previous RMS calculations</summary>
        /// <param name="input">RMS value of type Int32</param>
        /// <returns>A value between -90 and -0</returns>
        public static int ConvertToDbfs(float input)
        {
            int dbfs;

            dbfs = (int)(Math.Round(20 * Math.Log10(input / 1f)));

            if (dbfs < -90)
            {
                dbfs = -90;
            }

            return dbfs;
        }

    }
}
