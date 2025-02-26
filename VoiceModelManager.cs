using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace TextToSpeech;

public class VoiceModelManager
{
    private readonly string _baseFolder;
    private readonly HttpClient _httpClient;
    private readonly Dictionary<string, VoiceModel> _models = new();

    public VoiceModelManager(string baseFolder)
    {
        _baseFolder = baseFolder;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Adds or updates a voice model in the manager.
    /// </summary>
    public void AddOrUpdateModel(VoiceModel model)
    {
        _models[model.Key] = model;
    }

    public VoiceModel GetVoiceModel(string key)
    {
        return _models.TryGetValue(key, out VoiceModel? model) ? model : _models["default"];
    }

    /// <summary>
    /// Asynchronously ensures that all models and their config files are downloaded.
    /// </summary>
    public async Task LoadModelsAsync()
    {
        foreach (VoiceModel? model in _models.Values)
        {
            await EnsureModelAsync(model);
        }
    }

    private async Task EnsureModelAsync(VoiceModel model)
    {
        if (!File.Exists(model.ModelPath))
        {
            await DownloadFile(model.ModelUrl, model.ModelPath);
        }

        if (!File.Exists(model.ConfigPath))
        {
            await DownloadFile(model.ConfigUrl, model.ConfigPath);
        }
    }

    private async Task DownloadFile(string url, string destinationPath)
    {
        try
        {
            byte[] data = await _httpClient.GetByteArrayAsync(url);
            File.WriteAllBytes(destinationPath, data);
            TextToSpeechPlugin.TextToSpeechLogger.LogInfo($"Downloaded file from {url} to {destinationPath}");
        }
        catch (Exception ex)
        {
            TextToSpeechPlugin.TextToSpeechLogger.LogError($"Failed to download file from {url}: {ex.Message}");
        }
    }
}

public static class VoiceAssignment
{
    public static readonly Dictionary<string, string> NpcToVoiceModel = new Dictionary<string, string>
    {
        { "Haldor", "Haldor" },
        { "Hildir", "Hildir" },
        { "Hugin", "Hugin" },
        { "Munin", "Munin" },
        {"BogWitch", "BogWitch"}
    };
}