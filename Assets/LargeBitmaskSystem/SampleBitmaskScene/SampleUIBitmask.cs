using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LargeBitmaskSystem.Demo
{
    public class SampleUIBitmask : MonoBehaviour
    {
        public bool interactable = true;
        public LargeBitmask bitmask = new LargeBitmask(100);

        #region bitmask shortcuts

        public void MakeTrueAll()
        {
            bitmask.MakeTrueAll();
            RefreshDisplay();
        }

        public void MakeFalseAll()
        {
            bitmask.MakeFalseAll();
            RefreshDisplay();
        }

        public void InvertAll()
        {
            bitmask.InvertAll();
            RefreshDisplay();
        }

        public void RandomizeAll()
        {
            bitmask.RandomizeAll();
            RefreshDisplay();
        }

        public void LeftShift(int shift)
        {
            bitmask <<= shift;
            RefreshDisplay();
        }

        public void RightShift(int shift)
        {
            bitmask >>= shift;
            RefreshDisplay();
        }

        #endregion

        public VisibleBit[] visibleBits;

        void Awake()
        {
            for (int i = 0; i < visibleBits.Length; i++)
            {
                visibleBits[i].index = i;
                visibleBits[i].associatedBitmask = this;
                if (!interactable) visibleBits[i].interactability.SetActive(false);
            }
        }

        void Start()
        {
            RandomizeAll();
        }

        public void RefreshDisplay()
        {
            for (int i = 0; i < visibleBits.Length; i++)
                visibleBits[i].image.color = bitmask[i] ? Color.green : Color.gray;

            if (interactable) SampleBitmaskManager.instance.RefreshResults();
        }
    }

}