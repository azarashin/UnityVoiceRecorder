using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Voish.VoiceRecorder
{
    public class VoiceRecorderManager : MonoBehaviour
    {

        int HeaderByteSize = 44;
        int BitsPerSample = 16;
        int AudioFormat = 1;

        private const int Resolution = 8192;
        private const int SampleRate = 48000;

        private AudioSource _micAudioSource;
        private float[] _spectrum = new float[Resolution];
        private float[] _nextSpectrum = new float[Resolution];
        private string _targetDevice;

        private bool _isRecording = false;
        private DateTime _startTime;


        public bool IsRecording { get { return _isRecording; } } 

        private void Awake()
        {
            _micAudioSource = GetComponent<AudioSource>();
        }


        void Start()
        {
            //Setup("https://395c6sel69.execute-api.ap-northeast-1.amazonaws.com/prod/prod/vocalcords1"); 
        }

        private void FixedUpdate()
        {
            if(_isRecording)
            {
                if(!Microphone.IsRecording(_targetDevice))
                {
                    _isRecording = false; 
                }
            }
        }

        public IEnumerator CoSetup()
        {
            _isRecording = false;

            _targetDevice = Microphone.devices[0];

            Debug.Log($"=== Device Set: {_targetDevice} ===");
            _micAudioSource.clip = Microphone.Start(_targetDevice, true, 1, SampleRate);

            yield return CoMicStart();
            //_pitchViewer.Setup(Resolution, SampleRate, maxTrainingTime, correct);
            yield break; 
        }

        public IEnumerator CoStartRecord(int maxDuration)
        {
            _isRecording = true;
            Microphone.End(_targetDevice);
            _startTime = DateTime.Now; 
            _micAudioSource.clip = Microphone.Start(_targetDevice, false, maxDuration, SampleRate);
            yield return CoMicStart();

            //_pitchViewer.Play(false);

            yield break; 
        }


        public void PlayViewer()
        {
            //_pitchViewer.Play(true);
        }

        public void StopViewer()
        {
            //_pitchViewer.Stop();
        }

        public IEnumerator CoFinishRecord(string path)
        {
            _isRecording = false;
            _micAudioSource.Stop();
            //_pitchViewer.Stop();
            Microphone.End(_targetDevice);
            float length = (float)(DateTime.Now - _startTime).TotalSeconds;

            yield return CoWriteSound(_micAudioSource.clip, path, length);

            yield break;
        }

        void Update()
        {
            if (!_micAudioSource.isPlaying)
            {
                return;
            }
            _micAudioSource.GetSpectrumData(_nextSpectrum, 0, FFTWindow.BlackmanHarris);
            for (int i = 0; i < Resolution; i++)
            {
                _spectrum[i] = _spectrum[i] * 0.8f + _nextSpectrum[i] * 0.2f;
            }

            //_pitchViewer.DrawPitch(_spectrum);


        }



        private IEnumerator CoMicStart()
        {
            if (_targetDevice.Equals(""))
            {
                yield break;
            }

            // マイクデバイスの準備ができるまで待つ
            while (Microphone.GetPosition("") <= 0)
            {
                yield return null;
            }

            _micAudioSource.Play();
            yield break;
        }
        private IEnumerator CoWriteSound(AudioClip clip, string path, float lengthSec)
        {
            if(path == null)
            {
                yield break; 
            }
            int samples = Mathf.Min((int)(clip.frequency * lengthSec),  clip.samples); 
            using (MemoryStream currentMemoryStream = new MemoryStream())
            {
                // ChunkID RIFF
                byte[] bufRIFF = Encoding.ASCII.GetBytes("RIFF");
                currentMemoryStream.Write(bufRIFF, 0, bufRIFF.Length);

                // ChunkSize
                byte[] bufChunkSize = BitConverter.GetBytes((UInt32)(HeaderByteSize + samples * clip.channels * BitsPerSample / 8));
                currentMemoryStream.Write(bufChunkSize, 0, bufChunkSize.Length);

                // Format WAVE
                byte[] bufFormatWAVE = Encoding.ASCII.GetBytes("WAVE");
                currentMemoryStream.Write(bufFormatWAVE, 0, bufFormatWAVE.Length);

                // Subchunk1ID fmt
                byte[] bufSubchunk1ID = Encoding.ASCII.GetBytes("fmt ");
                currentMemoryStream.Write(bufSubchunk1ID, 0, bufSubchunk1ID.Length);

                // Subchunk1Size (16 for PCM)
                byte[] bufSubchunk1Size = BitConverter.GetBytes((UInt32)16);
                currentMemoryStream.Write(bufSubchunk1Size, 0, bufSubchunk1Size.Length);

                // AudioFormat (PCM=1)
                byte[] bufAudioFormat = BitConverter.GetBytes((UInt16)AudioFormat);
                currentMemoryStream.Write(bufAudioFormat, 0, bufAudioFormat.Length);

                // NumChannels
                byte[] bufNumChannels = BitConverter.GetBytes((UInt16)clip.channels);
                currentMemoryStream.Write(bufNumChannels, 0, bufNumChannels.Length);

                // SampleRate
                byte[] bufSampleRate = BitConverter.GetBytes((UInt32)clip.frequency);
                currentMemoryStream.Write(bufSampleRate, 0, bufSampleRate.Length);

                // ByteRate (=SampleRate * NumChannels * BitsPerSample/8)
                byte[] bufByteRate = BitConverter.GetBytes((UInt32)(samples * clip.channels * BitsPerSample / 8));
                currentMemoryStream.Write(bufByteRate, 0, bufByteRate.Length);

                // BlockAlign (=NumChannels * BitsPerSample/8)
                byte[] bufBlockAlign = BitConverter.GetBytes((UInt16)(clip.channels * BitsPerSample / 8));
                currentMemoryStream.Write(bufBlockAlign, 0, bufBlockAlign.Length);

                // BitsPerSample
                byte[] bufBitsPerSample = BitConverter.GetBytes((UInt16)BitsPerSample);
                currentMemoryStream.Write(bufBitsPerSample, 0, bufBitsPerSample.Length);

                // Subchunk2ID data
                byte[] bufSubchunk2ID = Encoding.ASCII.GetBytes("data");
                currentMemoryStream.Write(bufSubchunk2ID, 0, bufSubchunk2ID.Length);

                // Subchuk2Size
                byte[] bufSubchuk2Size = BitConverter.GetBytes((UInt32)(samples * clip.channels * BitsPerSample / 8));
                currentMemoryStream.Write(bufSubchuk2Size, 0, bufSubchuk2Size.Length);

                // Data
                float[] floatData = new float[samples * clip.channels];
                clip.GetData(floatData, 0);

                foreach (float f in floatData)
                {
                    byte[] bufData = BitConverter.GetBytes((short)(f * short.MaxValue));
                    currentMemoryStream.Write(bufData, 0, bufData.Length);
                }

                Debug.Log($"WAV データ作成完了");

                byte[] dataWav = currentMemoryStream.ToArray();

                Debug.Log($"dataWav.Length {dataWav.Length}");

                string pathSaveWav = GetFullPath(path);

                //  using を使ってメモリ開放を自動で行う
                using(FileStream currentFileStream = new FileStream(pathSaveWav, FileMode.Create))
                {
                    Debug.Log($"Start writing:  {pathSaveWav}");
                    System.Threading.Tasks.Task task = currentFileStream.WriteAsync(dataWav, 0, dataWav.Length);
                    yield return new WaitUntil(() => task.IsCompleted);
                    Debug.Log($"Completed writing:  {pathSaveWav}");
                    //currentFileStream.Write(dataWav, 0, dataWav.Length);

                    Debug.Log($"保存完了 path : {pathSaveWav}");
                }
                if (System.IO.File.Exists(pathSaveWav))
                {
                    Debug.Log($"Completed save:  {pathSaveWav}");
                }
                else
                {
                    Debug.LogError($"Failed to save:  {pathSaveWav}");
                }
                yield break; 
            }
        }

        public IEnumerator CoLoadSound(string path, AudioSource target)
        {
            string pathLoadWav = GetFullPath(path);
            if (!System.IO.File.Exists(pathLoadWav))
            {
                Debug.LogError($"File not found:  {pathLoadWav}");
                yield break; 
            }

            System.IO.FileInfo fi = new System.IO.FileInfo(pathLoadWav);
            long filesize = fi.Length;
            Debug.Log($"filesize: {filesize}");

            using (StreamReader sr = new StreamReader(pathLoadWav))
            {
                string buf = sr.ReadToEnd();
                Debug.Log($"Buffer length: {buf.Length}");
            }

            var request = UnityWebRequestMultimedia.GetAudioClip("file://" + pathLoadWav, AudioType.WAV);

            ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio = false;
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
                yield break;
            }
            target.clip = DownloadHandlerAudioClip.GetContent(request);
            Debug.Log($"Load Sound: {pathLoadWav}");
            yield break;
        }


        private string GetFullPath(string path)
        {
            return Path.Combine(GetInternalStoragePath(), path); 
        }

        private string GetInternalStoragePath()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var getFilesDir = currentActivity.Call<AndroidJavaObject>("getFilesDir"))
        {
            string secureDataPath = getFilesDir.Call<string>("getCanonicalPath");
            return secureDataPath;
        }
#else
            return Application.persistentDataPath;
#endif
        }
    }
}