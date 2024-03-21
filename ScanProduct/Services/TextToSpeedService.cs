using Google.Cloud.TextToSpeech.V1;
using NAudio.Wave;
using ScanProduct.TextToSpeedGoogle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanProduct.Services
{
    public class TextToSpeedService : ITextToSpeed
    {

        public void SpeedGoogle(string text)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "../../../service_account.json");
            var client = TextToSpeechClient.Create();
            var input = new SynthesisInput
            {
                Text = text,
            };
            var voiceSelection = new VoiceSelectionParams
            {
                LanguageCode = "vi-VN",
                SsmlGender = SsmlVoiceGender.Neutral,
            };
            var audioConfig = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Mp3
            };
            var response = client.SynthesizeSpeech(input, voiceSelection, audioConfig);
            using (var output = File.Create("output.mp3"))
            {
                response.AudioContent.WriteTo(output);
            }
            PlayMp3("output.mp3");
        }

        public async Task PlayMp3(string filePath)
        {
            using (var audioFile = new AudioFileReader(filePath))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
            await Task.Delay(1000);
        }
    }
}
