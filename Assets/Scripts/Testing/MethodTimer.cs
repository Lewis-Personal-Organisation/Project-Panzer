using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MethodTimer
{
    // public static float averageMS;
    private static float[] recordings = new []{-1F, -1F, -1F, -1F, -1F, -1F, -1F, -1F, -1F, -1F};
    private static int activeIndex = 0;

    // private static void Awake()
    // {
    //     for (int i = 0; i < recordings.Length; i++)
    //     {
    //         recordings[i] = -1F;
    //     }
    // }

    public static void TryAddRecording(float value)
    {
        if (activeIndex > recordings.Length -1) return;
        
        recordings[activeIndex] = value;
        activeIndex++;
    }

    public static float GetAverageMS()
    {
        float average = 0;

        for (int i = 0; i < recordings.Length; i++)
        {
            average += recordings[i];
        }
        
        average /= recordings.Length;
        return average;
    }
}