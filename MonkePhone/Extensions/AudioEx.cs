using UnityEngine;

namespace MonkePhone.Extensions;

// https://youtu.be/dzD0qP8viLw
public static class AudioEx
{
    private const int SampleWindow = 64;

    public static float GetLoudness(this AudioClip audioClip, int clipPosition)
    {
        int startPosition = clipPosition - SampleWindow;

        if (startPosition < 0) return 0;

        float[] waveData = new float[SampleWindow];
        audioClip.GetData(waveData, startPosition);

        // compute loudness
        float totalLoudness = 0;

        for (int i = 0; i < SampleWindow; i++)
            totalLoudness += Mathf.Abs(waveData[i]);

        return totalLoudness / SampleWindow;
    }

    public static float GetLoudness(this AudioSource audioSource)
    {
        float loudness = audioSource.clip.GetLoudness(audioSource.timeSamples) * 100f;

        if (loudness < 0.1f)
            loudness = 0f;

        return loudness;
    }
}