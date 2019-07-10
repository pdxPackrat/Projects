using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS_Proofing
{
    public class AudioMath
    {
        public static Int16 RootMeanSquare(Int16[] x)
        {
            double sum = 0;
            double temp = 0;
            Int16 result = 0;

            for (int i = 0; i < x.Length; i++)
            {
                sum += (x[i] * x[i]);
            }

            temp = Math.Round(Math.Sqrt(sum / x.Length));

            if (temp > Int16.MaxValue)
            {
                temp = Int16.MaxValue;
            }

            result = (Int16)temp;

            return result;
        }

        public static Int32 RootMeanSquare(Int32[] x)
        {
            double sum = 0;
            double temp = 0;
            Int32 result = 0;

            for (int i = 0; i < x.Length; i++)
            {
                sum += (x[i] * x[i]);
            }

            temp = (Int32)(Math.Round(Math.Sqrt(sum / x.Length)));

            if (temp > Int32.MaxValue)
            {
                temp = Int32.MaxValue;
            }

            result = (Int32)temp;

            return result;
        }

        public static float RootMeanSquare(float[] x)
        {
            double sum = 0;
            double temp = 0;
            float result = 0f;

            for (int i = 0; i < x.Length; i++)
            {
                sum += (x[i] * x[i]);
            }

            temp = (float)(Math.Sqrt(sum / x.Length));

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
