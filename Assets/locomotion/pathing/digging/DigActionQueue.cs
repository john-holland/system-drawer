using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

[Serializable]
public sealed class DigAction
{
    public Vector3 contactWorld;
    public float amount;
    public float time;
}

/// <summary>SG4D centroid at first shovel contact; queue of scoops.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Digging/Dig Action Queue")]
public sealed class DigActionQueue : MonoBehaviour
{
    public Vector3 contactCentroid;
    public bool hasContact;
    public List<DigAction> scoops = new List<DigAction>();
    public Bounds4 lastVolume;

    public void EnqueueContact(Vector3 world, float amount, float time)
    {
        if (!hasContact)
        {
            contactCentroid = world;
            hasContact = true;
        }
        scoops.Add(new DigAction { contactWorld = world, amount = amount, time = time });
        lastVolume = new Bounds4(contactCentroid, Vector3.one * Mathf.Max(0.5f, amount), time, time + 1f);
    }

    public void Clear()
    {
        scoops.Clear();
        hasContact = false;
        contactCentroid = Vector3.zero;
    }
}
