using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LargeBitmaskSystem.Demo
{
    public class VisibleBit : MonoBehaviour
    {
        [System.NonSerialized]
        public int index;

        [System.NonSerialized]
        public SampleUIBitmask associatedBitmask;

        public Image image;
        public GameObject interactability;

        public void Flip()
        {
            associatedBitmask.bitmask.Toggle(index);
            associatedBitmask.RefreshDisplay();
        }
    }
}
