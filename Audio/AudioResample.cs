///////////////////////////////////////////////////////////////////////////////////////////////
//
// Based on code from UnityVOIP.
//
// The MIT License (MIT)
// 
// Copyright(c) 2016 Dwayne Bull
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
///////////////////////////////////////////////////////////////////////////////////////////////

using System;
using UnityEngine;

namespace MultiplayerChat.Audio;

public static class AudioResample
{
    public static int ResampledSampleCount(int sampleCount, int sourceFrequency, int targetFrequency)
    {
        return (int)((float)sampleCount * (float)targetFrequency / (float)sourceFrequency);
    }

    public static int Resample(float[] source, float[] target, int sourceFrequency, int targetFrequency)
    {
        int sourceLength = source.Length;
        int targetLength = target.Length;

        if (sourceFrequency == targetFrequency)
            throw new ArgumentException("Source and target frequencies cannot be the same");

        float sampleRatio = (float)sourceFrequency / (float)targetFrequency;

        var requiredSize = ResampledSampleCount(sourceLength, sourceFrequency, targetFrequency);
        if (targetLength < requiredSize)
            throw new ArgumentException(
                $"target's length of '{targetLength}' does not meet the minimum length of '{requiredSize}'.");

        var writtenLength = 0;
        var remainder = sampleRatio % 1.0f;
        // basically an integer
        if (remainder < float.Epsilon)
        {
            int intSampleRatio = Mathf.RoundToInt(sampleRatio);
            for (int i = 0; i < targetLength && i * intSampleRatio < sourceLength; i++)
            {
                target[i] = source[i * intSampleRatio];
                writtenLength++;
            }
        }
        else
        {
            // we need more samples!
            if (targetFrequency > sourceFrequency)
            {
                for (int i = 0; i < targetLength && Mathf.CeilToInt(i * sampleRatio) < sourceLength; i++)
                {
                    var lower = Mathf.FloorToInt(i * sampleRatio);
                    var upper = Mathf.CeilToInt(i * sampleRatio);
                    var sample = Mathf.Lerp(source[lower], source[upper], remainder);
                    target[i] = sample;
                    writtenLength++;
                }
            }
            // we need less samples!
            else
            {
                for (int i = 0; i < targetLength && Mathf.FloorToInt(i * sampleRatio) < sourceLength; i++)
                {
                    var sampleIdx = Mathf.FloorToInt(i * sampleRatio);
                    target[i] = source[sampleIdx];
                    writtenLength++;
                }
            }
        }
        
        return writtenLength;
    }
}