using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LargeBitmaskSystem.Demo
{
    public enum OperationType { AND, OR, XOR, NAND, NOR, XNOR }

    public class SampleBitmaskManager : MonoBehaviour
    {
        public static SampleBitmaskManager instance;

        public SampleUIBitmask maskA, maskB, maskResult;
        public Text titleText;
        [System.NonSerialized]
        public OperationType currentOperation;

        void Awake()
        {
            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }
            else instance = this;
        }

        public void OperationAND() => ChangeOperation(OperationType.AND);
        public void OperationOR() => ChangeOperation(OperationType.OR);
        public void OperationXOR() => ChangeOperation(OperationType.XOR);
        public void OperationNAND() => ChangeOperation(OperationType.NAND);
        public void OperationNOR() => ChangeOperation(OperationType.NOR);
        public void OperationXNOR() => ChangeOperation(OperationType.XNOR);

        void ChangeOperation(OperationType newOp)
        {
            currentOperation = newOp;
            if (currentOperation == OperationType.AND)
                titleText.text = "<b>A AND B :</b>";
            else if (currentOperation == OperationType.OR)
                titleText.text = "<b>A OR B :</b>";
            else if (currentOperation == OperationType.XOR)
                titleText.text = "<b>A XOR B :</b>";
            else if (currentOperation == OperationType.NAND)
                titleText.text = "<b>A NAND B :</b>";
            else if (currentOperation == OperationType.NOR)
                titleText.text = "<b>A NOR B :</b>";
            else if (currentOperation == OperationType.XNOR)
                titleText.text = "<b>A XNOR B :</b>";
                
            RefreshResults();
        }

        public void RefreshResults()
        {
            if (currentOperation == OperationType.AND)
                maskResult.bitmask = maskA.bitmask & maskB.bitmask;
            else if (currentOperation == OperationType.OR)
                maskResult.bitmask = maskA.bitmask | maskB.bitmask;
            else if (currentOperation == OperationType.XOR)
                maskResult.bitmask = maskA.bitmask ^ maskB.bitmask;
            else if (currentOperation == OperationType.NAND)
                maskResult.bitmask = ~(maskA.bitmask & maskB.bitmask);
            else if (currentOperation == OperationType.NOR)
                maskResult.bitmask = ~(maskA.bitmask | maskB.bitmask);
            else if (currentOperation == OperationType.XNOR)
                maskResult.bitmask = ~(maskA.bitmask ^ maskB.bitmask);
                
            maskResult.RefreshDisplay();
        }
    }

}