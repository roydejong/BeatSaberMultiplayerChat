///////////////////////////////////////////////////////////////////////////////////////////////
//
//    This File is Part of the CallButler Open Source PBX (http://www.codeplex.com/callbutler)
//
//    Copyright (c) 2005-2008, Jim Heising
//    All rights reserved.
//
//    Redistribution and use in source and binary forms, with or without modification,
//    are permitted provided that the following conditions are met:
//
//    * Redistributions of source code must retain the above copyright notice,
//      this list of conditions and the following disclaimer.
//
//    * Redistributions in binary form must reproduce the above copyright notice,
//      this list of conditions and the following disclaimer in the documentation and/or
//      other materials provided with the distribution.
//
//    * Neither the name of Jim Heising nor the names of its contributors may be
//      used to endorse or promote products derived from this software without specific prior
//      written permission.
//
//    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
//    ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
//    WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
//    IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
//    INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
//    NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//    PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
//    WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
//    ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
//    POSSIBILITY OF SUCH DAMAGE.
//
///////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;

namespace MultiplayerChat.Audio;

public class FifoFloatStream
{
    private const int BlockSize = 16384;
    private const int MaxBlocksInCache = (3 * 1024 * 1024) / BlockSize;

    private int _mSize;
    private int _mRPos;
    private int _mWPos;
    private Stack _mUsedBlocks = new();
    private ArrayList _mBlocks = new();

    private float[] AllocBlock()
    {
        float[] result = null!;
        result = _mUsedBlocks.Count > 0 ? (float[]) _mUsedBlocks.Pop() : new float[BlockSize];
        return result;
    }

    private void FreeBlock(float[] block)
    {
        if (_mUsedBlocks.Count < MaxBlocksInCache)
            _mUsedBlocks.Push(block);
    }

    private float[] GetWBlock()
    {
        float[] result = null!;
        if (_mWPos < BlockSize && _mBlocks.Count > 0)
            result = (float[]) _mBlocks[_mBlocks.Count - 1];
        else
        {
            result = AllocBlock();
            _mBlocks.Add(result);
            _mWPos = 0;
        }

        return result;
    }

    public long Length
    {
        get
        {
            lock (this)
                return _mSize;
        }
    }

    public void Close()
    {
        Flush();
    }

    public void Flush()
    {
        lock (this)
        {
            foreach (float[] block in _mBlocks)
                FreeBlock(block);
            _mBlocks.Clear();
            _mRPos = 0;
            _mWPos = 0;
            _mSize = 0;
        }
    }

    public int Read(float[] buf, int ofs, int count)
    {
        lock (this)
        {
            var result = Peek(buf, ofs, count);
            Advance(result);
            return result;
        }
    }

    public void Write(float[] buf, int ofs, int count)
    {
        lock (this)
        {
            var left = count;
            while (left > 0)
            {
                var toWrite = Math.Min(BlockSize - _mWPos, left);
                Array.Copy(buf, ofs + count - left, GetWBlock(), _mWPos, toWrite);
                _mWPos += toWrite;
                left -= toWrite;
            }

            _mSize += count;
        }
    }

    // extra stuff
    public int Advance(int count)
    {
        lock (this)
        {
            var sizeLeft = count;
            while (sizeLeft > 0 && _mSize > 0)
            {
                if (_mRPos == BlockSize)
                {
                    _mRPos = 0;
                    FreeBlock((float[]) _mBlocks[0]);
                    _mBlocks.RemoveAt(0);
                }

                var toFeed = _mBlocks.Count == 1
                    ? Math.Min(_mWPos - _mRPos, sizeLeft)
                    : Math.Min(BlockSize - _mRPos, sizeLeft);
                _mRPos += toFeed;
                sizeLeft -= toFeed;
                _mSize -= toFeed;
            }

            return count - sizeLeft;
        }
    }

    public int Peek(float[] buf, int ofs, int count)
    {
        lock (this)
        {
            var sizeLeft = count;
            var tempBlockPos = _mRPos;
            var tempSize = _mSize;

            var currentBlock = 0;
            while (sizeLeft > 0 && tempSize > 0)
            {
                if (tempBlockPos == BlockSize)
                {
                    tempBlockPos = 0;
                    currentBlock++;
                }

                var upper = currentBlock < _mBlocks.Count - 1 ? BlockSize : _mWPos;
                var toFeed = Math.Min(upper - tempBlockPos, sizeLeft);
                Array.Copy((float[]) _mBlocks[currentBlock], tempBlockPos, buf, ofs + count - sizeLeft, toFeed);
                sizeLeft -= toFeed;
                tempBlockPos += toFeed;
                tempSize -= toFeed;
            }

            return count - sizeLeft;
        }
    }
}