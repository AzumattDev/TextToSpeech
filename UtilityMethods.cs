using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using static TextToSpeech.TextToSpeechPlugin;

namespace TextToSpeech;

public abstract class UtilityMethods
{
    public static void GetPlayerAudioSource(out AudioSource? playerSource)
    {
        playerSource = Player.m_localPlayer?.transform.Find("PiperTTS")?.GetComponent<AudioSource>();
        if (playerSource == null)
        {
            TextToSpeechLogger.LogError("Player AudioSource not found.");
        }
    }

    public static string StripRichText(string input)
    {
        return string.IsNullOrEmpty(input)
            ? input
            :
            // This pattern matches a '<', followed by one or more characters that are not '>', and then a '>'.
            Regex.Replace(input, @"<[^>]+>", string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public static void FireAndForget(Task task)
    {
        task.ContinueWith(t =>
        {
            if (t.Exception != null)
                TextToSpeechLogger.LogError(t.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public static void PlayOneShotAtPointWithBypass(AudioClip clip, Vector3 position, AudioSource? targetSource = null!)
    {
        if (clip == null || targetSource == null)
            return;

        GameObject gameObject = new GameObject("TTSOneShotAudioAtPoint");
        gameObject.transform.position = position == default ? targetSource.transform.position : position;
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.spatialBlend = 1f;
        audioSource.bypassReverbZones = true;
        audioSource.bypassListenerEffects = true;
        audioSource.bypassEffects = true;
        audioSource.volume = 1f;
        audioSource.Play();
        Object.Destroy(gameObject, clip.length * ((double)Time.timeScale < 0.009999999776482582 ? 0.01f : Time.timeScale));
    }
}