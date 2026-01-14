// component that would have been helpful for the earth in HitchHikerGuide
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BuildAround : MonoBehaviour
{
    void Start()
    {
        GameObject earth = GameObject.FindGameObjectWithTag("earth");
        if (earth != null)
        {
            earth.AddComponent<BuildAround>();
        }
    }
}