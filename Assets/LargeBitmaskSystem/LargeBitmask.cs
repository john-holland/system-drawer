using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This elementary struct can be used whenever you need a bitmask and uints/ulongs aren't enough.
// It has lots of utility available : operators, comparators, get/set methods...
// Author : Simon Albou <ominous.lab@gmail.com>

[System.Serializable]
public struct LargeBitmask : IEquatable<LargeBitmask>
{
    #region properties

    public byte[] bytes;
    
    // A quick reminder : 1 byte equals 8 bits
    public int sizeInBits => (bytes == null) ? 0 : bytes.Length * 8;
    public int sizeInBytes => (bytes == null) ? 0 : bytes.Length;

    // Indexer makes it so that myMask[42] can be treated as a regular boolean
    public bool this[int bitIndex]
    {
        get { return Contains(bitIndex); }
        set { Turn(bitIndex, value); }
    }

    #if UNITY_EDITOR
    public bool foldout;
    #endif

    #endregion

    #region Constructor

    // define a mask from a byte array
    public LargeBitmask(byte[] byteArray)
    {
        bytes = byteArray;
        #if UNITY_EDITOR
        foldout = false;
        #endif
    }

    // define an 8-bit mask from a single byte
    public LargeBitmask(byte singleByte)
    {
        bytes = new byte[]{singleByte};
        #if UNITY_EDITOR
        foldout = false;
        #endif
    }
    
    // define a mask from its size, in bits
    public LargeBitmask(int numberOfBits)
    {
        if (numberOfBits < 0) numberOfBits = 0;
        while (numberOfBits%8 != 0) numberOfBits++;
        int numberOfBytes = numberOfBits/8;
        bytes = new byte[numberOfBytes];

        #if UNITY_EDITOR
        foldout = false;
        #endif
    }

    // define a mask from its size, in bits, then specify which are true
    public LargeBitmask(int numberOfBits, int[] indexesOfTrueBits)
    {
        if (numberOfBits < 0) numberOfBits = 0;
        while (numberOfBits%8 != 0) numberOfBits++;
        int numberOfBytes = numberOfBits/8;
        bytes = new byte[numberOfBytes];

        #if UNITY_EDITOR
        foldout = false;
        #endif

        if (numberOfBits == 0) return;
        if (indexesOfTrueBits == null) return;
        if (indexesOfTrueBits.Length == 0) return;
        for (int i = 0; i < indexesOfTrueBits.Length; i++)
            Unchecked_TurnOn(indexesOfTrueBits[i]);
    }

    // define a mask as a copy of another
    public LargeBitmask(LargeBitmask copyFrom)
    {
        bytes = new byte[copyFrom.sizeInBytes];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = copyFrom.bytes[i];

        #if UNITY_EDITOR
        foldout = false;
        #endif
    }

    #endregion

    #region utility : getters

    bool Unchecked_Contains(int bitIndex) => (bytes[bitIndex/8] & (1<<(7-(bitIndex%8)))) > 0;
    public bool Contains(int bitIndex)
    {
        if (bitIndex < 0) return false;
        if (bitIndex >= sizeInBits) return false;
        return Unchecked_Contains(bitIndex);
    }

    public int GetNumberOfTrueBits()
    {
        if (sizeInBytes == 0) return 0;
        int result = 0;
        for (int i = 0; i < sizeInBits; i++)
            if (Unchecked_Contains(i))
                result ++;
        return result;
    }

    public int[] GetIndexesOfTrueBits()
    {
        if (sizeInBytes == 0) return null;
        List<int> ints = new List<int>();
        for (int i = 0; i < sizeInBits; i++)
            if (Unchecked_Contains(i))
                ints.Add(i);
        return ints.ToArray();
    }

    public string ToString(bool multiline=false)
    {
        if (sizeInBytes == 0) return "(empty bitmask)";
        string result = "";
        for (int i = 0; i < sizeInBytes; i++)
        {
            string subResult = "";
            for (int j = 0; j < 8; j++)
                if (Contains(i*8+j))
                    result += (i*8+j).ToString()+", ";
            
            if (!string.IsNullOrEmpty(subResult))
                subResult = subResult.Substring(0, subResult.Length-2);
            //else subResult = "(byte " +i.ToString()+ " is empty)";
            
            result += subResult;
            if (string.IsNullOrEmpty(subResult)) continue;
            if (multiline) result += "\n";
            else result += " / ";
        }
        result += "End of bitmask";
        return result;
    }

    #endregion

    #region utility : simple setters

    // Turns a bit to true.
    void Unchecked_TurnOn(int bitIndex) => bytes[bitIndex/8] |= (byte)(1<<(7-(bitIndex%8)));
    public void TurnOn(int bitIndex)
    {
        if (bitIndex < 0) return;
        if (bitIndex >= sizeInBits) return;
        Unchecked_TurnOn(bitIndex);
    }

    // Turns a bit to false.
    void Unchecked_TurnOff(int bitIndex) => bytes[bitIndex/8] &= (byte)(255-(1<<(7-(bitIndex%8))));
    public void TurnOff(int bitIndex)
    {
        if (bitIndex < 0) return;
        if (bitIndex >= sizeInBits) return;
        Unchecked_TurnOff(bitIndex);
    }

    // Turns a bit to true if false, to false if true.
    void Unchecked_Toggle(int bitIndex) => bytes[bitIndex/8] ^= (byte)(1<<(7-(bitIndex%8)));
    public void Toggle(int bitIndex)
    {
        if (bitIndex < 0) return;
        if (bitIndex >= sizeInBits) return;
        Unchecked_Toggle(bitIndex);
    }

    // Turns a bit to <wanted value>.
    public void Turn(int bitIndex, bool wantedValue)
    {
        if (wantedValue) TurnOn(bitIndex);
        else TurnOff(bitIndex);
    }

    // Returns whether the resize worked and had an effect.
    public bool Resize(int newSizeInBytes)
    {
        if (newSizeInBytes < 0) return false;
        if (newSizeInBytes == sizeInBytes) return false;

        byte[] newByteArray = new byte[newSizeInBytes];
        if (newSizeInBytes == 0) return true;

        int minSize = Mathf.Min(newSizeInBytes, sizeInBytes);
        for (int i = 0; i < minSize; i++)
            newByteArray[i] = bytes[i];

        bytes = newByteArray;
        return true;
    }

    #endregion

    #region utility : mass edition setters

    public void MakeFalseAll()
    {
        if (sizeInBytes == 0) return;
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = 0;
    }

    public void MakeFalseInRange(int startIndex, int bitCount)
    {
        if (sizeInBytes == 0) return;
        if (sizeInBytes >= startIndex) return;
        for (int i = 0; i < bitCount; i++)
            TurnOff(startIndex+i);
    }

    public void MakeFalseInList(int[] indexes)
    {
        if (sizeInBytes == 0) return;
        if (indexes == null) return;
        if (indexes.Length == 0) return;
        for (int i = 0; i < indexes.Length; i++)
            TurnOff(indexes[i]);
    }

    public void MakeTrueAll()
    {
        if (sizeInBytes == 0) return;
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = 255;
    }

    public void MakeTrueInRange(int startIndex, int bitCount)
    {
        if (sizeInBytes == 0) return;
        if (sizeInBytes >= startIndex) return;
        for (int i = 0; i < bitCount; i++)
            TurnOn(startIndex+i);
    }

    public void MakeTrueInList(int[] indexes)
    {
        if (sizeInBytes == 0) return;
        if (indexes == null) return;
        if (indexes.Length == 0) return;
        for (int i = 0; i < indexes.Length; i++)
            TurnOn(indexes[i]);
    }

    public void InvertAll()
    {
        if (sizeInBytes == 0) return;
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)(255-bytes[i]);
    }

    public void InvertInRange(int startIndex, int bitCount)
    {
        if (sizeInBytes == 0) return;
        if (sizeInBytes >= startIndex) return;
        for (int i = 0; i < bitCount; i++)
            Toggle(startIndex+i);
    }

    public void InvertInList(int[] indexes)
    {
        if (sizeInBytes == 0) return;
        if (indexes == null) return;
        if (indexes.Length == 0) return;
        for (int i = 0; i < indexes.Length; i++)
            Toggle(indexes[i]);
    }

    public void RandomizeAll()
    {
        if (sizeInBytes == 0) return;
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)UnityEngine.Random.Range(0, 256);
    }

    public void RandomizeInRange(int startIndex, int bitCount)
    {
        if (sizeInBytes == 0) return;
        if (sizeInBytes >= startIndex) return;
        for (int i = 0; i < bitCount; i++)
            Turn(startIndex+i, UnityEngine.Random.value < 0.5f);
    }

    public void RandomizeInList(int[] indexes)
    {
        if (sizeInBytes == 0) return;
        if (indexes == null) return;
        if (indexes.Length == 0) return;
        for (int i = 0; i < indexes.Length; i++)
            Turn(indexes[i], UnityEngine.Random.value < 0.5f);
    }

    #endregion

    #region equality

    public override bool Equals(object obj)
    {
        if (!(obj is LargeBitmask)) return false;
        return Equals((LargeBitmask)obj);
    }

    public bool Equals(LargeBitmask other)
    {
        if (other.sizeInBytes != sizeInBytes) return false;
        for (int i = 0; i < sizeInBytes; i++)
            if (bytes[i] != other.bytes[i]) return false;

        return true;
    }

    // Hashcode is obtained by making ints from packs of 4 bytes, then XORing these ints
    public override int GetHashCode()
    {
        int result = 0;
        int curByte = 0;
        while (curByte < sizeInBytes)
        {
            int curInt = 0;
            for (int i = 0; i < 4; i++)
            {
                if (curByte == sizeInBytes) break;
                curInt += (((int)bytes[curByte++]) << (8*i));
            }
            result = (result==0) ? curInt : (result^curInt);
        }

        return result;
    }

    #endregion

    #region operators

    public static LargeBitmask operator |(LargeBitmask a, LargeBitmask b)
    {
        LargeBitmask result = new LargeBitmask(Mathf.Max(a.sizeInBits, b.sizeInBits));
        int minSize = Mathf.Min(a.sizeInBytes, b.sizeInBytes);
        bool aIsBigger = a.sizeInBytes > b.sizeInBytes;
        for (int i = 0; i < result.sizeInBytes; i++)
        {
            if (i < minSize) result.bytes[i] = (byte)(a.bytes[i] | b.bytes[i]);
            else if (aIsBigger) result.bytes[i] = a.bytes[i];
            else result.bytes[i] = b.bytes[i];
        }

        return result;
    }

    public static LargeBitmask operator &(LargeBitmask a, LargeBitmask b)
    {
        LargeBitmask result = new LargeBitmask(Mathf.Max(a.sizeInBits, b.sizeInBits));
        int minSize = Mathf.Min(a.sizeInBytes, b.sizeInBytes);
        for (int i = 0; i < result.sizeInBytes; i++)
        {
            if (i < minSize) result.bytes[i] = (byte)(a.bytes[i] & b.bytes[i]);
            else result.bytes[i] = 0;
        }

        return result;
    }

    public static LargeBitmask operator ^(LargeBitmask a, LargeBitmask b)
    {
        LargeBitmask result = new LargeBitmask(Mathf.Max(a.sizeInBits, b.sizeInBits));
        int minSize = Mathf.Min(a.sizeInBytes, b.sizeInBytes);
        bool aIsBigger = a.sizeInBytes > b.sizeInBytes;
        for (int i = 0; i < result.sizeInBytes; i++)
        {
            if (i < minSize) result.bytes[i] = (byte)(a.bytes[i] ^ b.bytes[i]);
            else if (aIsBigger) result.bytes[i] = a.bytes[i];
            else result.bytes[i] = b.bytes[i];
        }

        return result;
    }

    public static LargeBitmask operator +(LargeBitmask a, LargeBitmask b)
    {
        LargeBitmask result = new LargeBitmask(a.sizeInBits + b.sizeInBits);
        if (result.sizeInBytes == 0) return result;
        if (a.sizeInBytes > 0)
            for (int i = 0; i < a.sizeInBytes; i++)
                result.bytes[i] = a.bytes[i];
        if (b.sizeInBytes > 0)
            for (int i = a.sizeInBytes; i < b.sizeInBytes; i++)
                result.bytes[i] = b.bytes[i];
        
        return result;
    }

    public static LargeBitmask operator <<(LargeBitmask a, int b) // note : this wraps
    {
        if (a.sizeInBytes == 0) return a;
        if (b == 0) return a;
        if (b < 0) return (a >> (-b));

        int byteShift = b / 8;
        byteShift = byteShift % a.sizeInBytes;
        if (byteShift > 0)
        {
            byte[] wrapped = new byte[byteShift];
            for (int i = 0; i < byteShift; i++)
                wrapped[i] = a.bytes[i];
            byte[] newBytes = new byte[a.sizeInBytes];
            for (int i = 0; i < a.sizeInBytes; i++)
                newBytes[i] = ((i + byteShift) < a.sizeInBytes) ? a.bytes[i + byteShift] : wrapped[i + byteShift - a.sizeInBytes];
            a.bytes = newBytes;
        }

        int bitShift = b % 8;
        if (bitShift > 0)
        {
            byte endPart = 0;
            for (int i = 0; i < bitShift; i++) endPart |= (byte)(1<<i);
            byte startPart = (byte)(255-endPart);

            byte wrap = (byte)(a.bytes[0] >> (8-bitShift));
            wrap &= endPart;

            byte nextWrap = (byte)(a.bytes[a.bytes.Length-1] >> (8-bitShift));
            nextWrap &= endPart;
            
            for (int i = a.bytes.Length-1; i >= 0; i--)
            {
                a.bytes[i] <<= bitShift;
                a.bytes[i] &= startPart;
                a.bytes[i] |= wrap;
                if (i == 0) continue;

                wrap = nextWrap;
                nextWrap = (byte)(a.bytes[i-1] >> (8-bitShift));
                nextWrap &= endPart;
            }
        }

        return a;
    }

    public static LargeBitmask operator >>(LargeBitmask a, int b) // note : this wraps
    {
        if (a.sizeInBytes == 0) return a;
        if (b == 0) return a;
        if (b < 0) return (a << (-b));

        int byteShift = b / 8;
        byteShift = byteShift % a.sizeInBytes;
        if (byteShift > 0)
        {
            byte[] wrapped = new byte[byteShift];
            for (int i = 0; i < byteShift; i++)
                wrapped[i] = a.bytes[a.bytes.Length-byteShift+i];
            byte[] newBytes = new byte[a.sizeInBytes];
            for (int i = 0; i < a.sizeInBytes; i++)
                newBytes[i] = (i < byteShift) ? wrapped[i] : a.bytes[i - byteShift];
            a.bytes = newBytes;
        }

        int bitShift = b % 8;
        if (bitShift > 0)
        {
            byte endPart = 0;
            for (int i = 0; i < 8-bitShift; i++) endPart |= (byte)(1<<i);
            byte startPart = (byte)(255-endPart);

            byte wrap = (byte)(a.bytes[a.bytes.Length-1] << (8-bitShift));
            wrap &= startPart;

            byte nextWrap = (byte)(a.bytes[0] << (8-bitShift));
            nextWrap &= startPart;
            
            for (int i = 0; i < a.bytes.Length; i++)
            {
                a.bytes[i] >>= bitShift;
                a.bytes[i] &= endPart;
                a.bytes[i] |= wrap;
                if (i == a.bytes.Length-1) continue;

                wrap = nextWrap;
                nextWrap = (byte)(a.bytes[i+1] << (8-bitShift));
                nextWrap &= startPart;
            }
        }

        return a;
    }

    public static LargeBitmask operator ~(LargeBitmask a)
    {
        LargeBitmask result = new LargeBitmask(a);
        for (int i = 0; i < result.sizeInBytes; i++)
            result.bytes[i] = (byte)(255-result.bytes[i]);

        return result;
    }

    public static bool operator ==(LargeBitmask a, LargeBitmask b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(LargeBitmask a, LargeBitmask b)
    {
        return !(a.Equals(b));
    }

    #endregion
}