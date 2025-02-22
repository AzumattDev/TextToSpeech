using System;
using UnityEngine;

namespace TextToSpeech;

public static class WavUtility
{
    /// <summary>
    /// Converts a WAV byte array into an AudioClip.
    /// Supports uncompressed 8-bit and 16-bit PCM WAV files. I chose to use this manual method over unity's built-in due to slightly less overhead.
    /// </summary>
    /// <param name="wavFile">The WAV file data as a byte array.</param>
    /// <param name="clipName">Optional name for the AudioClip.</param>
    /// <param name="streaming">If true, the AudioClip will be streamed.</param>
    /// <returns>The generated AudioClip, or null if an error occurs.</returns>
    public static AudioClip? ToAudioClip(byte[] wavFile, string clipName = "wavClip", bool streaming = false)
    {
        if (wavFile == null || wavFile.Length < 44)
        {
            Debug.LogError("Invalid or empty WAV file provided.");
            return null;
        }

        // Read header values
        int channels = BitConverter.ToInt16(wavFile, 22);
        int sampleRate = BitConverter.ToInt32(wavFile, 24);
        int bitDepth = BitConverter.ToInt16(wavFile, 34);

        // Locate the "data" chunk
        int pos = 12;
        while (pos + 8 < wavFile.Length)
        {
            string chunkID = System.Text.Encoding.ASCII.GetString(wavFile, pos, 4);
            int chunkSize = BitConverter.ToInt32(wavFile, pos + 4);
            if (chunkID == "data")
            {
                pos += 8;
                break;
            }

            pos += 8 + chunkSize;
        }

        if (pos >= wavFile.Length)
        {
            Debug.LogError("Data chunk not found in WAV file.");
            return null;
        }

        int dataSize = wavFile.Length - pos;
        int sampleCount = dataSize / (bitDepth / 8);
        int samplesPerChannel = sampleCount / channels;
        float[] data = new float[sampleCount];

        // Convert the byte data into float samples
        if (bitDepth == 16)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(wavFile, pos + i * 2);
                data[i] = sample / 32768f;
            }
        }
        else if (bitDepth == 8)
        {
            for (int i = 0; i < sampleCount; i++)
            {
                byte sample = wavFile[pos + i];
                data[i] = (sample - 128) / 128f;
            }
        }
        else
        {
            Debug.LogError("WavUtility supports only 8-bit and 16-bit audio formats.");
            return null;
        }

        AudioClip? audioClip = AudioClip.Create(clipName, samplesPerChannel, channels, sampleRate, streaming);
        audioClip.SetData(data, 0);
        return audioClip;
    }
}