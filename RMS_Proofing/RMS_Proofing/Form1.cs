using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace RMS_Proofing
{
    public partial class Form1 : Form
    {
        private string selectedFile = "";
        private int samplesTotal;
        private int bytesPerSample;
        private int bitsPerSample;
        private int channelCount;
        private int sampleRate;
        private string encodingType;
        private int audioFrames;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // int[] x = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };               // RMS = 6.20483682299543
            // int[] x = new int[] { 10, 9, 8, 9, 10, 9, 8, 9, 10, 9, 8, 9, 10 };   // RMS = 9.10621089823187
            // int[] x = new int[] { 10, 9, 8, 10, 9, 8, 10, 9, 8, 10, 9, 8 };   //   RMS = 9.03696114115064 
            Int16[] x = new Int16[] { 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9 };   //   RMS = 9

            TextboxRmsValue.Text = (AudioMath.RootMeanSquare(x)).ToString();
        }

        private void ButtonLoadFile_Click(object sender, EventArgs e)
        {
            // Load the file asynchronously
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "What audio file do you want to load?";
            string allExtensions = "*.wav;*.mp3;";
            openFileDialog.Filter = String.Format("All Supported Files|{0}|All Files (*.*)|*.*", allExtensions);
            openFileDialog.FilterIndex = 1;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ButtonPlayFile.Enabled = false;  // Diable the playback choice while we are in the middle of loading the file async
                selectedFile = openFileDialog.FileName;
                TextboxFileName.Text = selectedFile;
                ExtractPcmInfoFromAudioFile();
            }
        }

        private void ExtractPcmInfoFromAudioFile()
        {
            var reader = new AudioFileReader(selectedFile);
            bitsPerSample = reader.WaveFormat.BitsPerSample;
            channelCount = reader.WaveFormat.Channels;
            sampleRate = reader.WaveFormat.SampleRate;
            encodingType = reader.WaveFormat.Encoding.ToString(); ;

            bytesPerSample = (reader.WaveFormat.BitsPerSample / 8);
            var samples = reader.Length / (bytesPerSample);
            samplesTotal = (int)samples;
            audioFrames = samplesTotal / channelCount;


            UpdateAudioInfoOnForm();
            ButtonPlayFile.Enabled = true;
            // PlaybackPcmAudio(reader);
        }

        private void DecodePcmAudio()
        {
            int bytesReadFromBuffer;
            int samplesReadFromBuffer;
            int totalSamplesReadFromBuffer = 0;
            int framesReadFromBuffer = 0;

            int totalBytesToRead = bytesPerSample * samplesTotal;
            int frameCount = samplesTotal / channelCount;
            int frameSizeBytes = bytesPerSample * channelCount;
            int frameWindowSize = 1024;
            int frameWindowIndex = 0;
            int framesRemaining;
            int dataIndex = 0;
            int numberOfSamplesToRead = frameWindowSize * channelCount;

            float rmsValue = 0f;
            List<float> rmsList = new List<float>();

            ISampleProvider decoder = new AudioFileReader(selectedFile);
            float[] sampleBuffer = new float[1024];

            while (decoder.Read(sampleBuffer, 0, sampleBuffer.Length) > 0)
            {
                rmsValue = AudioMath.RootMeanSquare(sampleBuffer);
                TextboxRmsValue.Text = rmsValue.ToString();

                rmsList.Add(rmsValue);
                ListboxRmsList.Items.Add(rmsValue);

                PlaybackAudioFromBuffer(sampleBuffer);
            }
        }

        private void PlaybackAudioFromBuffer(float[] x)
        {
            byte[] byteStream = PcmData.ConvertFloatToBytes(x);

            var ms = new MemoryStream(byteStream);
            var rs = new RawSourceWaveStream(ms, new WaveFormat(sampleRate, bitsPerSample, channelCount));
            var wo = new WaveOutEvent();
            wo.Init(rs);
            wo.Play();
        }

        /// <summary>
        /// This method implementation is buggy as hell right now
        /// </summary>
        /// <param name="reader"></param>
        private void PlaybackPcmAudio(AudioFileReader reader)
        {
            int bytesReadFromBuffer;
            int samplesReadFromBuffer;
            int totalSamplesReadFromBuffer = 0;
            int framesReadFromBuffer = 0;

            int totalBytesToRead = bytesPerSample * samplesTotal;
            int frameCount = samplesTotal / channelCount;
            int frameSizeBytes = bytesPerSample * channelCount;
            int frameWindowSize = 1024;
            int frameWindowIndex = 0;
            int framesRemaining;
            int dataIndex = 0;
            int numberOfSamplesToRead = frameWindowSize * channelCount;

            float[] frameWindowBuffer = new float[frameWindowSize * (frameSizeBytes / 4)];

            byte[] PcmData = new byte[samplesTotal * bytesPerSample];
            float[] ReadBuffer = new float[channelCount];

            if (reader.WaveFormat.Encoding == WaveFormatEncoding.IeeeFloat) // We are assuming IeeeFloat encoding for now
            {
                for (int i = 0; i < frameCount; i += frameWindowSize)
                {
                    framesRemaining = frameCount - framesReadFromBuffer; 

                    if (framesRemaining < frameWindowSize)
                    {
                        // Normally numberOfSamplesToRead is equal to frameWindowSize * channelCount
                        // This check is to make sure that there is enough data to read, and if not, to reduce the number of samples to read
                        numberOfSamplesToRead = framesRemaining * channelCount;
                    }

                    samplesReadFromBuffer = reader.Read(frameWindowBuffer, totalSamplesReadFromBuffer, numberOfSamplesToRead);
                    totalSamplesReadFromBuffer += samplesReadFromBuffer;
                    framesReadFromBuffer = totalSamplesReadFromBuffer / channelCount;
                }
            }
            else
            {
                return;
            }

            /* 
            for (int i = 0; i < frameCount; i++)  // spin through each sample, 1 for each channel 
            {
                dataIndex = i * frameSizeBytes;

                // Apparently the Read() function requires to read a full sample from ALL channels
                // bytesReadFromBuffer = reader.Read(PcmData, dataIndex, frameSizeBytes);

                // if (reader.CanRead)
                // {
                //     bytesReadFromBuffer = reader.Read(ReadBuffer, 0, channelCount);
                //     float TempLeft = ReadBuffer[0];
                //     float TempRight = ReadBuffer[1];

                //     Buffer.BlockCopy(ReadBuffer, 0, PcmData, i * bytesPerSample * channelCount, ReadBuffer.Length);
                // }
            }
            */

            // Something is obviously wrong right now with my data extraction, as this next segment's playback sounds 
            // corrupted somehow. 

            /* 
            var ms = new MemoryStream(PcmData);
            var rs = new RawSourceWaveStream(ms, new WaveFormat(sampleRate, bitsPerSample, channelCount));
            var wo = new WaveOutEvent();
            wo.Init(rs);
            wo.Play();

            while (wo.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(100);
            }

            wo.Dispose();
            */
        }

        private void UpdateAudioInfoOnForm()
        {
            TextboxBitrate.Text = bitsPerSample.ToString() + "-bit";
            TextboxSampleRate.Text = sampleRate.ToString() + " Hz";

            string channelType; 

            if (channelCount == 1)
            {
                channelType = "Mono (single channel) audio";
            }
            else if (channelCount == 2)
            {
                channelType = "Stereo audio";
            }
            else if (channelCount >= 3)
            {
                channelType = String.Format("{0}-channel audio", channelCount); 
            }
            else
            {
                channelType = "Unknown";
            }
            TextboxChannelCount.Text = channelType;

            TextboxEncodingType.Text = encodingType;

            TextboxAudioFrames.Text = audioFrames.ToString() + " frames";
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void TextboxFileName_TextChanged(object sender, EventArgs e)
        {

        }

        private void ButtonPlayFile_Click(object sender, EventArgs e)
        {
            var reader = new AudioFileReader(selectedFile);

            // PlaybackPcmAudio(reader);
            DecodePcmAudio();
        }
    }
}
