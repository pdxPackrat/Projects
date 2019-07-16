#define DSP_USES_SIGNALS
// #define DSP_PLAYS_AUDIO
// #define DSP_FORCES_FORM_UPDATE

using System;
using System.Configuration;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
using Serilog;

namespace RMS_Proofing
{

    public partial class Form1 : Form
    {
        private string selectedFile = "";
        private int samplesTotal;
        private int bytesPerSample;
        private int bitsPerSample;
        private int bitRateOut = 16;
        private int channelCount;
        private int sampleRate;
        private string encodingType;
        private int audioFrames;
        private static AutoResetEvent CanPlayAudio = new AutoResetEvent(true);
        private float currentRmsValue = 0f;
        private WaveOutEvent outputDevice;

        public Form1()
        {
            InitializeComponent();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logfile.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            CheckConfigurationValues();
        }

        private void CheckConfigurationValues()
        {
            string defaultFile;

            defaultFile = ConfigurationManager.AppSettings.Get("DefaultFile");

            if (defaultFile != null && defaultFile != String.Empty)
            {
                selectedFile = defaultFile;
                LoadSelectedFile(selectedFile);
            }
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
                selectedFile = openFileDialog.FileName;
            }

            LoadSelectedFile(selectedFile);
        }

        private void LoadSelectedFile(string inputFilename)
        {
            // Should probably have something in here that at least checks to make sure that the file exists

            if (File.Exists(inputFilename) == true)
            {
                Log.Debug("(LoadSelectedFile) Loading file: " + inputFilename);
                ButtonPlayFile.Enabled = false;  // Diable the playback choice while we are in the middle of loading the file async
                TextboxFileName.Text = selectedFile;

                ExtractPcmInfoFromAudioFile();
            }
            else
            {
                Log.Debug("(LoadSelectedFile) " + inputFilename + " does not exist - skipping this step");
            }

        }

        private void ExtractPcmInfoFromAudioFile()
        {
            var reader = new AudioFileReader(selectedFile);
            Log.Debug("Beginning extraction of audio data from " + reader.ToString());

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

            string audioDataToText = String.Format("BitsPerSample: {0}, ChannelCount: {1}, SampleRate: {2}, EncodingType{3}, BytesPerSample: {4}, AudioFrames: {5}",
                                                    bitsPerSample, channelCount, sampleRate, encodingType, bytesPerSample, audioFrames);
            Log.Debug(audioDataToText);
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
            int dbfsValue;
            ISampleProvider decoder = new AudioFileReader(selectedFile);

            Log.Debug("Beginning decode of PCM audio");

            if (CheckboxShowDsp.Checked == true)
            {
                float rmsValue = 0f;
                List<string> rmsList = new List<string>();

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                float[] sampleBuffer = new float[frameWindowSize * channelCount];

                while ((totalSamplesReadFromBuffer = decoder.Read(sampleBuffer, 0, sampleBuffer.Length)) > 0)
                {
                    if (totalSamplesReadFromBuffer < sampleBuffer.Length)  // We must be at the end of the file then
                    {
                        rmsValue = AudioMath.RootMeanSquare(sampleBuffer, totalSamplesReadFromBuffer);
                    }
                    else
                    {
                        rmsValue = AudioMath.RootMeanSquare(sampleBuffer);
                    }

                    dbfsValue = AudioMath.ConvertToDbfs(rmsValue);

#if DSP_USES_SIGNALS
                    CanPlayAudio.WaitOne();
                    ThreadPool.QueueUserWorkItem(PlaybackAudioFromBuffer, sampleBuffer);
#else

                    PlaybackAudioFromBuffer(sampleBuffer);
#endif

                    long elapsedTime = stopWatch.ElapsedMilliseconds;
                    string dbfsString = String.Format("FrameWindow: {0}, Time: {1}ms, dBFS: {2}", frameWindowIndex.ToString(), elapsedTime.ToString(), dbfsValue.ToString());
                    TextboxRmsValue.Text = dbfsString;

                    rmsList.Add(dbfsString);
                    if (CheckboxReverseList.Checked == true)
                    {
                        ListboxRmsList.Items.Insert(0, dbfsString);
                    }
                    else
                    {
                        ListboxRmsList.Items.Add(dbfsString);
                    }

                    frameWindowIndex++;

#if DSP_FORCES_FORM_UPDATE
                    this.Update();

#endif
                }

                stopWatch.Stop();
            }
            else
            {
                if (outputDevice == null)
                {
                    var trimmed = new OffsetSampleProvider(decoder)
                    {
                        // DelayBy = TimeSpan.FromSeconds(1),
                        // LeadOut = TimeSpan.FromSeconds(1),
                        // SkipOverSamples = 4096 * 4
                    };

                    outputDevice = new WaveOutEvent();
                    outputDevice.DesiredLatency = 600;

                    outputDevice.PlaybackStopped += OnPlaybackStopped;
                    outputDevice.Init(trimmed);
                    outputDevice.Play();
                }

            }

            Log.Debug("Finished decode of PCM audio");
        }

        private void OnPlaybackStopped(object sender, EventArgs args)
        {
            if (outputDevice != null)
            {
                outputDevice.Dispose();
                outputDevice = null;
            }

            CanPlayAudio.Set();
        }

        private void PlaybackAudioFromBuffer(object obj)
        {
            var floats = (float[])obj;

            byte[] byteStream = PcmData.ConvertFloatToPcmBytes(floats);

            var ms = new MemoryStream(byteStream);
            var rs = new RawSourceWaveStream(ms, new WaveFormat(sampleRate, bitRateOut, channelCount));

#if DSP_PLAYS_AUDIO
            if (outputDevice == null)
            {
                outputDevice = new WaveOutEvent();
                outputDevice.DesiredLatency = 50;
                outputDevice.PlaybackStopped += OnPlaybackStopped;
            }

            outputDevice.Init(rs);
            outputDevice.Play();
#elif DSP_USES_SIGNALS
            CanPlayAudio.Set();  // Only set the signal here if we aren't playing back audio 
                                 // since normally the PlaybackStopped event would handle setting the signal
#endif

        }

        /// <summary>
        /// This method implementation is buggy as hell right now and not used
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

        private void CheckboxShowDsp_Click(object sender, EventArgs e)
        {
            bool visibility = false;

            if (CheckboxShowDsp.Checked)
            {
                visibility = true;
            }

            label1.Visible = visibility;
            label7.Visible = visibility;
            TextboxRmsValue.Visible = visibility;
            ListboxRmsList.Visible = visibility;
            ButtonClearRmsData.Visible = visibility;
            CheckboxReverseList.Visible = visibility;
            CheckboxReverseList.Enabled = visibility;
        }

        private void ButtonClearRmsData_Click(object sender, EventArgs e)
        {
            TextboxRmsValue.Text = "";
            ListboxRmsList.Items.Clear();
            this.Update();
        }
    }
}
