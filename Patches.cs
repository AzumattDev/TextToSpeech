using HarmonyLib;
//using Splatform;
using UnityEngine;
using static TextToSpeech.UtilityMethods;

namespace TextToSpeech;

[HarmonyPatch(typeof(TextViewer), nameof(TextViewer.ShowText))]
public static class TextViewerShowTextPatch
{
    static void Postfix(TextViewer __instance, TextViewer.Style style, string topic, string textId, bool autoHide)
    {
        if (Player.m_localPlayer == null)
            return;

        GetPlayerAudioSource(out AudioSource? playerSource);
        if (playerSource == null)
            return;

        string tTopic;
        string tText;
        switch (style)
        {
            case TextViewer.Style.Rune:
                tTopic = __instance.m_topic.text;
                tText = __instance.m_text.text;
                break;
            case TextViewer.Style.Intro:
                tTopic = __instance.m_introTopic.text;
                tText = __instance.m_introText.text;
                break;
            case TextViewer.Style.Raven:
                tTopic = __instance.m_ravenTopic.text;
                tText = __instance.m_ravenText.text;
                break;
            default:
                tTopic = Localization.instance.Localize(topic);
                tText = Localization.instance.Localize(textId);
                break;
        }

        string ttsMessage = topic.Length > 0 ? $"{tTopic} {tText}" : tText;
        FireAndForget(TextToSpeechPlugin.Speak(StripRichText(ttsMessage), playerSource));
    }
}

[HarmonyPatch(typeof(Chat), nameof(Chat.SetNpcText))]
public static class ShowRavenMessagesChatSetNpcTextPatch
{
    static void Postfix(Chat __instance, GameObject talker, Vector3 offset, float cullDistance, float ttl, string topic, string text, bool large)
    {
        if (Player.m_localPlayer == null)
            return;
        GetPlayerAudioSource(out AudioSource? playerSource);
        if (playerSource == null)
            return;

        string ttsMessage = topic.Length > 0
            ? StripRichText(Localization.instance.Localize(topic) + Localization.instance.Localize(text))
            : StripRichText(Localization.instance.Localize(text));

        FireAndForget(TextToSpeechPlugin.Speak(ttsMessage, playerSource, true, talker.transform.position));
    }
}

[HarmonyPatch(typeof(MessageHud), nameof(MessageHud.ShowMessage))]
public static class MessageHudShowMessagePatch
{
    static void Postfix(MessageHud __instance, MessageHud.MessageType type, string text)
    {
        if (Player.m_localPlayer == null || type != MessageHud.MessageType.Center)
            return;

        GetPlayerAudioSource(out AudioSource? playerSource);
        if (playerSource == null)
            return;

        string ttsMessage = StripRichText(Localization.instance.Localize(text));
        FireAndForget(TextToSpeechPlugin.Speak(ttsMessage, playerSource));
    }
}

// For the current PTB, uncomment when it goes live.
/*[HarmonyPatch(typeof(Terminal), nameof(Terminal.AddString), new System.Type[] { typeof(PlatformUserID), typeof(string), typeof(Talker.Type), typeof(bool) })]
public static class TerminalAddStringPatch
{
    static void Postfix(PlatformUserID user, string text, Talker.Type type, bool timestamp)
    {
        GetPlayerAudioSource(out AudioSource? playerSource);
        if (playerSource == null)
            return;

        if (ZNet.TryGetPlayerByPlatformUserID(user, out ZNet.PlayerInfo playerInfo))
        {
            string filteredName = CensorShittyWords.FilterUGC(playerInfo.m_name, UGCType.CharacterName, user);
            // Switch text of "said" based on the type of message   public enum Type {Ping, Shout, Whisper, Normal}

            string textType = type switch
            {
                Talker.Type.Ping => "pinged",
                Talker.Type.Shout => "shouted",
                Talker.Type.Whisper => "whispered",
                _ => "said"
            };


            if (filteredName == Player.m_localPlayer.GetPlayerName() && TextToSpeechPlugin.SkipSelf.Value.IsOn())
                return;

            string ttsMessage = $"{filteredName} {textType} in  chat: {text}";
            FireAndForget(TextToSpeechPlugin.Speak(StripRichText(ttsMessage), playerSource));
        }
        else
        {
            FireAndForget(TextToSpeechPlugin.Speak(StripRichText(text), playerSource));
        }
    }
}*/

[HarmonyPatch(typeof(Terminal), nameof(Terminal.AddString), new System.Type[] { typeof(string), typeof(string), typeof(Talker.Type), typeof(bool) })]
public static class TerminalAddStringPatch
{
    static void Postfix(string user, string text, Talker.Type type, bool timestamp)
    {
        GetPlayerAudioSource(out AudioSource? playerSource);
        if (playerSource == null)
            return;

        // Switch text of "said" based on the type of message   public enum Type {Ping, Shout, Whisper, Normal}

        string textType = type switch
        {
            Talker.Type.Ping => "pinged",
            Talker.Type.Shout => "shouted",
            Talker.Type.Whisper => "whispered",
            _ => "said"
        };


        if (user == Player.m_localPlayer.GetPlayerName() && TextToSpeechPlugin.SkipSelf.Value.IsOn())
            return;

        string ttsMessage = $"{user} {textType} in  chat: {text}";
        FireAndForget(TextToSpeechPlugin.Speak(StripRichText(ttsMessage), playerSource));
    }
}

[HarmonyPatch(typeof(DreamTexts), nameof(DreamTexts.GetRandomDreamText))]
public static class DreamTextsGetRandomDreamTextPatch
{
    static void Postfix(DreamTexts __instance, ref DreamTexts.DreamText? __result)
    {
        if (__result == null)
            return;

        if (Player.m_localPlayer == null)
            return;

        GetPlayerAudioSource(out AudioSource? playerSource);
        if (playerSource == null)
            return;
        string ttsMessage = StripRichText(Localization.instance.Localize(__result.m_text));
        FireAndForget(TextToSpeechPlugin.Speak(ttsMessage, playerSource));
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.SetLocalPlayer))]
public static class PlayerSetLocalPlayerPatch
{
    static void Postfix(Player __instance)
    {
        if (__instance.transform.Find("PiperTTS") != null)
            return;
        GameObject audioObject = new GameObject("PiperTTS");
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // Set to 3D sound
        audioSource.dopplerLevel = 0f; // Disable Doppler effect
        audioSource.rolloffMode = AudioRolloffMode.Linear; // Set rolloff mode to linear
        audioSource.minDistance = 1f; // Set minimum distance for sound
        audioSource.maxDistance = 100f; // Set maximum distance for sound
        audioSource.ignoreListenerVolume = true; // Ignore listener volume
        audioSource.ignoreListenerPause = true; // Ignore listener pause
        audioSource.bypassEffects = true; // Bypass the reverb on the Valheim camera
        audioSource.bypassReverbZones = true; // Bypass the reverb on the Valheim camera
        audioObject.transform.SetParent(__instance.transform);
    }
}