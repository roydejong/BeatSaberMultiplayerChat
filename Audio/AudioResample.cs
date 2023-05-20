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
    public static int Resample(float[] source, float[] target, int sourceFrequency,
        int outputFrequency, int outputChannels = 1)
    {
        if (sourceFrequency == outputFrequency)
            throw new ArgumentException("Source and target frequencies cannot be the same");

        var ratio = sourceFrequency / (float)outputFrequency;

        var sourceLength = source.Length;
        var targetLength = target.Length;

        var length = 0;
        
        if (ratio % 1f <= float.Epsilon)
        {
            var intRatio = Mathf.RoundToInt(ratio);
            var sizeRequired = sourceLength * intRatio * outputChannels;
            
            if (sizeRequired > target.Length)
                throw new ArgumentException(
                    $"target's length of '{target.Length}' does not meet the minimum length of '{sizeRequired}'.");
            
            for (var i = 0; i < (targetLength / outputChannels) && (i * intRatio) < sourceLength; i++)
            {
                for (var j = 0; j < outputChannels; j++)
                {
                    var targetIndex = i * outputChannels + j;
                    var sourceSample = source[i * intRatio];
                    target[targetIndex] = sourceSample;
                    length++;
                }
            }
        }
        else
        {
            if (ratio > 1f)
            {
                var sizeRequired = Mathf.CeilToInt(sourceLength * ratio);
                
                if (sizeRequired > target.Length)
                    throw new ArgumentException(
                        $"target's length of '{target.Length}' does not meet the minimum length of '{sizeRequired}'.");
                
                for (var i = 0; i < (targetLength / outputChannels) && Mathf.CeilToInt(i * ratio) < sourceLength; i++)
                {
                    for (var j = 0; j < outputChannels; j++)
                    {
                        var targetIndex = i * outputChannels + j;
                        var sourceSample = Mathf.Lerp(source[Mathf.FloorToInt(i * ratio)],
                            source[Mathf.CeilToInt(i * ratio)], ratio % 1);
                        target[targetIndex] = sourceSample;
                        length++;
                    }
                }
            }
            else
            {
                var sizeRequired = Mathf.FloorToInt(sourceLength * ratio);
                
                if (sizeRequired > target.Length)
                    throw new ArgumentException(
                        $"target's length of '{target.Length}' does not meet the minimum length of '{sizeRequired}'.");
                
                for (var i = 0; i < (targetLength / outputChannels) && Mathf.FloorToInt(i * ratio) < sourceLength; i++)
                {
                    for (var j = 0; j < outputChannels; j++)
                    {
                        var targetIndex = i * outputChannels + j;
                        var sourceSample = source[Mathf.FloorToInt(i * ratio)];
                        target[targetIndex] = sourceSample;
                        length++;
                    }
                }
            }
        }

        return length;
    }
}