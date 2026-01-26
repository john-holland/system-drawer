using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LargeBitmaskSystem.Demo
{
    public class TutorialOnLargeBitmasks : MonoBehaviour
    {
        // LargeBitmasks are serializable.
        public LargeBitmask myBitmask;

        void Tutorial()
        {
            // Read and write every bit as if it was a boolean in an array
            myBitmask[42] = true;
            if (myBitmask[42]) Debug.Log("Hello world!");

            // Create a bitmask with capacity of, say, 800 bits!
            LargeBitmask otherMask = new LargeBitmask(800);
            
            // All common operators are supported :
            myBitmask += otherMask; // concatenation
            myBitmask |= otherMask; // union (OR)
            myBitmask &= otherMask; // intersection (AND)
            myBitmask ^= otherMask; // symmetric difference (XOR)
            myBitmask = ~otherMask; // complement (NOT)
            myBitmask <<= 7; // bit shift to the left (wraps)
            myBitmask >>= 7; // bit shift to the right (wraps)

            // Mass-editing methods are available :
            otherMask.MakeTrueAll();
            otherMask.MakeFalseAll();
            otherMask.RandomizeAll();
            otherMask.InvertAll();
            // Range-editing. Example sets 17 bits : from 500 (incl.) to 517 (excl.)
            otherMask.MakeTrueInRange(500, 17);
            // List-editing. This example sets bits at some specific indexes :
            int[] indexes = new int[] { 4, 8, 15, 16, 23, 42 };
            otherMask.MakeTrueInList(indexes);

            // Get useful data about your mask:
            int myBitSize = myBitmask.sizeInBits;
            int myByteSize = myBitmask.sizeInBytes; // byteSize * 8 = bitSize
            int trueBits = myBitmask.GetNumberOfTrueBits(); // how many bits are set to "true" in the mask?
            int[] trueIndexes = myBitmask.GetIndexesOfTrueBits(); // retrieves indexes of said "true" bits.

            // Nice string formatting for debug purposes :
            Debug.Log(myBitmask.ToString());
        }
    }
}