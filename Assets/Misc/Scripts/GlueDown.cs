using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GlueDown : MonoBehaviour
{
    [Header("Ground Detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundMask = 1; // Default layer
    [SerializeField] private bool MustTouchToAttach = false;

    // aw poor don :(

    // Start is called before the first frame update
    void Start()
    {
        // let's check under the actor and see if there's a game object to apply FixedJointComponent to
        Vector3 checkPosition = groundCheck != null ?
            groundCheck.position : 
            transform.position + Vector3.down * (GetComponent<Rigidbody>() != null ? GetComponent<CapsuleCollider>()?.height ?? 0.5f : 0.5f);
        
        bool isGrounded = Physics.CheckSphere(checkPosition, groundCheckDistance, groundMask);

        if (!isGrounded)
        {
            // uninstalled sign with chains attached on the ground soon!
            return;
        }
        // todo find out how to raycast below the active gameobject

        // if there is one under the current object, then add the FixedJointComponent to the actor
        List<GameObject> scanUnder = new List<GameObject>();
        
        // Raycast downward to find objects below
        RaycastHit[] hits = Physics.RaycastAll(transform.position, Vector3.down, 10f, groundMask);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject != gameObject)
            {
                scanUnder.Add(hit.collider.gameObject);
            }
        }

        GameObject under = null;
        
        if (MustTouchToAttach) {
            // Check for objects with Rigidbody that are touching (using overlap check)
            Collider ourCollider = GetComponent<Collider>();
            if (ourCollider != null)
            {
                foreach (GameObject u in scanUnder)
                {
                    Rigidbody rb = u.GetComponent<Rigidbody>();
                    if (rb == null) continue;
                    
                    Collider theirCollider = u.GetComponent<Collider>();
                    if (theirCollider == null) continue;
                    
                    // Check if colliders are overlapping/touching
                    if (Physics.ComputePenetration(
                        ourCollider, 
                        transform.position, 
                        transform.rotation,
                        theirCollider,
                        u.transform.position,
                        u.transform.rotation,
                        out Vector3 direction,
                        out float distance) && distance < 0.01f)
                    {
                        under = u;
                        break;
                    }
                }
            }
        } else {
            under = scanUnder.FirstOrDefault(u => u.GetComponent<Rigidbody>() != null);
        }

        if (under != null && GetComponent<Rigidbody>() != null)
        {
            FixedJoint joint = gameObject.AddComponent<FixedJoint>();
            Rigidbody underBody = under.GetComponent<Rigidbody>();
            joint.connectedBody = underBody;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
