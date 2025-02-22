using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;


namespace TextToSpeech
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class TextToSpeechPlugin : BaseUnityPlugin
    {
        internal const string ModName = "TextToSpeech";
        internal const string ModVersion = "1.0.0";
        internal const string Author = "Azumatt";
        private const string ModGUID = $"{Author}.{ModName}";
        private static string ConfigFileName = $"{ModGUID}.cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource TextToSpeechLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        private FileSystemWatcher _watcher = null!;
        private readonly object _reloadLock = new();
        private DateTime _lastConfigReloadTime;
        private const long RELOAD_DELAY = 10000000; // One second

        private static readonly HttpClient httpClient = new HttpClient();
#if RunAsServer
        private readonly Process _piperProcess = null!;
#endif

        // URLs for Piper zip release and voice model
        private const string PiperZipUrl = "https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip";
        private string _voiceModelConfigUrl = string.Empty;

        private static string _voiceModelConfigPath = string.Empty;


        // Local paths for storing Piper and voice model
        private string _modDirectory = string.Empty;
        private string _piperFolder = string.Empty;
        private string _piperZipPath = string.Empty;
        private static string _piperExePath = string.Empty;
        private static string _voiceModelPath = string.Empty;

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public async void Awake()
        {
            bool saveOnSet = Config.SaveOnConfigSet;
            Config.SaveOnConfigSet = false;

            VoiceModelUrl = config("1 - General", "Voice Model URL", "https://huggingface.co/rhasspy/piper-voices/resolve/v1.0.0/en/en_GB/alba/medium/en_GB-alba-medium.onnx", "URL for the voice model file.");
            SkipSelf = config("1 - Preferences", "Skip Self In Chat", Toggle.On, "Do not TTS your own messages in chat.");
            
            _voiceModelConfigUrl = VoiceModelUrl.Value.Replace("?download=true", string.Empty) + ".json";

            
            _modDirectory = !string.IsNullOrEmpty(Info.Location) ? Path.GetDirectoryName(Info.Location) : Environment.CurrentDirectory;
            _piperFolder = Path.Combine(_modDirectory, "Piper");
            Directory.CreateDirectory(_piperFolder);

            UpdateVoiceModelPaths(); // Now piperFolder is available

            VoiceModelUrl.SettingChanged += (sender, args) =>
            {
                UpdateVoiceModelPaths();
                _ = EnsureVoiceModel();
                _ = EnsureVoiceModelConfig();
            };

            _piperZipPath = Path.Combine(_piperFolder, "piper_windows_amd64.zip");
            _piperExePath = Path.Combine(_piperFolder, "piper", "piper.exe");

            // Continue with your other initialization tasks...
            await EnsurePiperFiles();
            await EnsureVoiceModel();
            await EnsureVoiceModelConfig();

#if RunAsServer
            StartPiperProcess();
#endif

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();

            Config.Save();
            if (saveOnSet)
            {
                Config.SaveOnConfigSet = saveOnSet;
            }
        }


        /// <summary>
        /// Checks for the Piper executable and voice model file, and downloads them if missing.
        /// </summary>
        private async Task EnsurePiperFiles()
        {
            if (!File.Exists(_piperExePath))
            {
                TextToSpeechLogger.LogInfo("Piper executable not found. Downloading Piper zip...");
                await DownloadFile(PiperZipUrl, _piperZipPath);
                TextToSpeechLogger.LogInfo("Extracting Piper zip...");
                try
                {
                    ZipFile.ExtractToDirectory(_piperZipPath, _piperFolder);
                    TextToSpeechLogger.LogInfo("Piper extracted successfully.");
                    // Delete the zip to save space
                    File.Delete(_piperZipPath);
                }
                catch (Exception ex)
                {
                    TextToSpeechLogger.LogError("Failed to extract Piper: " + ex.Message);
                }
            }
            else
            {
                TextToSpeechLogger.LogInfo("Piper already exists.");
            }
        }

        /// <summary>
        /// Checks for the voice model file and downloads it if missing.
        /// </summary>
        private async Task EnsureVoiceModel()
        {
            if (!File.Exists(_voiceModelPath))
            {
                TextToSpeechLogger.LogInfo("Voice model not found. Downloading voice model...");
                await DownloadFile(VoiceModelUrl.Value, _voiceModelPath);
            }
            else
            {
                TextToSpeechLogger.LogInfo("Voice model already exists.");
            }
        }

        private async Task EnsureVoiceModelConfig()
        {
            if (!File.Exists(_voiceModelConfigPath))
            {
                TextToSpeechLogger.LogInfo("Voice model config not found. Downloading voice model config...");
                await DownloadFile(_voiceModelConfigUrl, _voiceModelConfigPath);
            }
            else
            {
                TextToSpeechLogger.LogInfo("Voice model config already exists.");
            }
        }


        /// <summary>
        /// Downloads a file from the specified URL to the destination path.
        /// </summary>
        private async Task DownloadFile(string url, string destinationPath)
        {
            try
            {
                byte[] data = await httpClient.GetByteArrayAsync(url);
                File.WriteAllBytes(destinationPath, data);
                TextToSpeechLogger.LogInfo($"Downloaded file from {url} to {destinationPath}");
            }
            catch (Exception ex)
            {
                TextToSpeechLogger.LogError($"Failed to download file from {url}: {ex.Message}");
            }
        }

        private void UpdateVoiceModelPaths()
        {
            // Use Uri to safely parse the URL and extract the filename
            if (Uri.TryCreate(VoiceModelUrl.Value, UriKind.Absolute, out Uri? uri))
            {
                string fileName = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrEmpty(fileName))
                {
                    TextToSpeechLogger.LogError("Failed to extract file name from URL: " + VoiceModelUrl.Value);
                    return;
                }

                _voiceModelPath = Path.Combine(_piperFolder, fileName);
                _voiceModelConfigPath = Path.Combine(_piperFolder, $"{fileName}.json");
                _voiceModelConfigUrl = VoiceModelUrl.Value.Replace("?download=true", string.Empty) + ".json";
                TextToSpeechLogger.LogDebug($"Updated voice model path to: {_voiceModelPath} and config path to: {_voiceModelConfigPath}");
            }
            else
            {
                TextToSpeechLogger.LogError("Invalid voice model URL: " + VoiceModelUrl.Value);
            }
        }

#if RunAsServer
        /// <summary>
        /// Launches the Piper executable with the proper arguments.
        /// </summary>
        private void StartPiperProcess()
        {
            if (IsPiperRunning())
            {
                TextToSpeechLogger.LogInfo("Piper is already running.");
                return;
            }

            try
            {
                ProcessStartInfo psi = new()
                {
                    FileName = piperExePath,
                    Arguments = $"--model \"{voiceModelPath}\" --server 127.0.0.1:5002",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };
                piperProcess = Process.Start(psi);
                TextToSpeechLogger.LogInfo("Started Piper process.");
            }
            catch (Exception ex)
            {
                TextToSpeechLogger.LogError("Failed to start Piper: " + ex.Message);
            }
        }
        /// <summary>
        /// Checks if Piper is already running on 127.0.0.1:5002.
        /// </summary>
        private bool IsPiperRunning()
        {
            try
            {
                using TcpClient tcpClient = new System.Net.Sockets.TcpClient();
                Task result = tcpClient.ConnectAsync("127.0.0.1", 5002);
                return result.Wait(100);
            }
            catch
            {
                return false;
            }
        }
#endif
        public static async Task Speak(string text, AudioSource? targetSource = null!, bool playAtPoint = false, Vector3 position = default)
        {
            try
            {
                string tempWavPath = Path.Combine(Path.GetTempPath(), $"piper_tts_{Guid.NewGuid()}.wav");

                ProcessStartInfo psi = new()
                {
                    FileName = _piperExePath,
                    Arguments = $"--model \"{_voiceModelPath}\" --model_config \"{_voiceModelConfigPath}\" --output_file \"{tempWavPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process p = new() { StartInfo = psi })
                {
                    p.Start();
                    // Send the text to Piper via standard input.
                    await p.StandardInput.WriteLineAsync(text);
                    p.StandardInput.Close();

                    // Capture and log any error output from Piper.
                    string errorOutput = await p.StandardError.ReadToEndAsync();
                    p.WaitForExit();
                    if (!string.IsNullOrEmpty(errorOutput))
                    {
                        if (!errorOutput.Contains("[info]"))
                        {
                            TextToSpeechLogger.LogError("Piper error: " + errorOutput);
                        }
                        else
                        {
                            TextToSpeechLogger.LogDebug("Piper info: " + errorOutput);
                        }
                    }
                }

                if (File.Exists(tempWavPath))
                {
                    byte[] wavData = File.ReadAllBytes(tempWavPath);
                    PlayAudio(wavData, targetSource, playAtPoint, position);
                    File.Delete(tempWavPath);
                }
                else
                {
                    TextToSpeechLogger.LogError("Temporary WAV file was not created.");
                }
            }
            catch (Exception ex)
            {
                TextToSpeechLogger.LogError("Error in Piper TTS: " + ex.Message);
            }
        }

        private static void PlayAudio(byte[] audioData, AudioSource? targetSource = null!, bool playAtPoint = false, Vector3 position = default)
        {
            AudioClip? clip = WavUtility.ToAudioClip(audioData);
            if (clip == null) return;
            if (targetSource != null)
            {
                targetSource.clip = clip;
                if (playAtPoint)
                {
                    UtilityMethods.PlayOneShotAtPointWithBypass(targetSource.clip, position == default ? targetSource.transform.position : position, targetSource);
                }
                else
                    targetSource.Play();
            }
            else
            {
                // Fallback: create a temporary GameObject to play the audio.
                GameObject audioObject = new GameObject("PiperTTS");
                AudioSource source = audioObject.AddComponent<AudioSource>();
                source.clip = clip;
                source.Play();
                Destroy(audioObject, clip.length);
            }
        }


        private static void PlayRawAudio(byte[] rawData)
        {
            const int sampleRate = 22050;
            const int channels = 1;
            int samplesCount = rawData.Length / 2; // 2 bytes per 16-bit sample
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                short sample = BitConverter.ToInt16(rawData, i * 2);
                samples[i] = sample / 32768f;
            }

            AudioClip clip = AudioClip.Create("PiperTTS", samplesCount, channels, sampleRate, false);
            clip.SetData(samples, 0);

            GameObject audioObject = new GameObject("PiperTTS");
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.Play();
            Destroy(audioObject, clip.length);
        }


        /// <summary>
        /// Converts WAV byte data to an AudioClip and plays it.
        /// </summary>
        private static void PlayAudio(byte[] audioData)
        {
            AudioClip? clip = WavUtility.ToAudioClip(audioData);
            if (clip == null) return;
            GameObject audioObject = new GameObject("PiperTTS");
            AudioSource source = audioObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.Play();
            Destroy(audioObject, clip.length);
        }

        private void OnDestroy()
        {
#if RunAsServer
            // Kill the persistent piper process if it hasn't exited.
            if (_piperProcess is { HasExited: false })
            {
                try
                {
                    _piperProcess.Kill();
                    _piperProcess.WaitForExit(2000); // wait up to 2 seconds
                    TextToSpeechLogger.LogInfo("Terminated Piper process.");
                }
                catch (Exception ex)
                {
                    TextToSpeechLogger.LogError("Error terminating Piper process: " + ex.Message);
                }
            }

            // kill any other stray piper processes that might be running from our folder.
            try
            {
                Process[] processes = Process.GetProcessesByName("piper");
                foreach (Process? proc in processes)
                {
                    // Check if the process's main module path starts with our piper folder.
                    try
                    {
                        if (proc.MainModule.FileName.StartsWith(piperFolder, StringComparison.InvariantCultureIgnoreCase))
                        {
                            proc.Kill();
                            proc.WaitForExit(2000);
                            TextToSpeechLogger.LogInfo($"Killed stray piper process (PID: {proc.Id}).");
                        }
                    }
                    catch
                    {
                        /* Ignore processes we can't inspect */
                    }
                }
            }
            catch (Exception ex)
            {
                TextToSpeechLogger.LogError("Error cleaning up stray piper processes: " + ex.Message);
            }
#endif
            SaveWithRespectToConfigSet();
            _watcher?.Dispose();
        }


        private void SetupWatcher()
        {
            _watcher = new FileSystemWatcher(Paths.ConfigPath, ConfigFileName);
            _watcher.Changed += ReadConfigValues;
            _watcher.Created += ReadConfigValues;
            _watcher.Renamed += ReadConfigValues;
            _watcher.IncludeSubdirectories = true;
            _watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            _watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            DateTime now = DateTime.Now;
            long time = now.Ticks - _lastConfigReloadTime.Ticks;
            if (time < RELOAD_DELAY)
            {
                return;
            }

            lock (_reloadLock)
            {
                if (!File.Exists(ConfigFileFullPath))
                {
                    TextToSpeechLogger.LogWarning("Config file does not exist. Skipping reload.");
                    return;
                }

                try
                {
                    TextToSpeechLogger.LogDebug("Reloading configuration...");
                    SaveWithRespectToConfigSet(true);
                    TextToSpeechLogger.LogInfo("Configuration reload complete.");
                }
                catch (Exception ex)
                {
                    TextToSpeechLogger.LogError($"Error reloading configuration: {ex.Message}");
                }
            }

            _lastConfigReloadTime = now;
        }

        private void SaveWithRespectToConfigSet(bool reload = false)
        {
            bool originalSaveOnSet = Config.SaveOnConfigSet;
            Config.SaveOnConfigSet = false;
            if (reload)
                Config.Reload();
            Config.Save();
            if (originalSaveOnSet)
            {
                Config.SaveOnConfigSet = originalSaveOnSet;
            }
        }


        #region ConfigOptions

        internal static ConfigEntry<string> VoiceModelUrl = null!;
        internal static ConfigEntry<Toggle> SkipSelf = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description)
        {
            return config(group, name, value, new ConfigDescription(description));
        }

        #endregion
    }

    public static class ToggleExtensions
    {
        public static bool IsOn(this TextToSpeechPlugin.Toggle toggle)
        {
            return toggle == TextToSpeechPlugin.Toggle.On;
        }

        public static bool IsOff(this TextToSpeechPlugin.Toggle toggle)
        {
            return toggle == TextToSpeechPlugin.Toggle.Off;
        }
    }
}