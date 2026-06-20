using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kinematic ragdoll playback via clip sampling (no physics-card muscle actuation).
/// </summary>
public sealed class NonIkRagdollAnimator
{
    struct RigidbodySnapshot
    {
        public Rigidbody body;
        public bool isKinematic;
        public RigidbodyConstraints constraints;
    }

    readonly List<RigidbodySnapshot> _snapshots = new List<RigidbodySnapshot>();
    RagdollSystem _ragdoll;
    AnimationClip _clip;
    float _time;
    float _speed = 1f;
    int _direction = 1;
    float _layerWeight = 1f;
    bool _playing;
    bool _loggedPlayOnce;

    public bool IsPlaying => _playing;

    public void Play(RagdollSystem ragdoll, AnimationClip clip, int direction = 1, float speed = 1f)
    {
        if (ragdoll == null || clip == null)
            return;

        Stop();
        _ragdoll = ragdoll;
        _clip = clip;
        _direction = direction >= 0 ? 1 : -1;
        _speed = Mathf.Max(0.0001f, speed);
        _time = _direction >= 0 ? 0f : clip.length;
        _playing = true;

        SnapshotAndSetKinematic(ragdoll);
        ragdoll.suppressMotorActuation = true;
        RagdollPoseUtility.SampleClipOntoRagdoll(ragdoll, clip, _time);
        RagdollPoseUtility.ZeroRagdollVelocities(ragdoll);

        if (!_loggedPlayOnce)
        {
            Debug.Log("[NonIkRagdollAnimator] Non-IK kinematic playback started.");
            _loggedPlayOnce = true;
        }
    }

    public void PlayFromBehaviorTree(RagdollSystem ragdoll, AnimationBehaviorTree tree, int direction = 1, float speed = 1f)
    {
        if (tree == null)
            return;
        ABTClipConfig cfg = tree.GetActiveConfiguration();
        if (cfg?.clip != null)
            Play(ragdoll, cfg.clip, direction, speed);
    }

    public void Stop()
    {
        if (_ragdoll != null)
            _ragdoll.suppressMotorActuation = false;
        RestoreRigidbodies();
        _playing = false;
        _ragdoll = null;
        _clip = null;
        _time = 0f;
    }

    public void TickLayer(RagdollSystem ragdoll, AnimationLayerSlot slot, float deltaTime)
    {
        if (!_playing || ragdoll == null || _clip == null || slot == null)
            return;
        if (slot.playbackMode != AnimationLayerPlaybackMode.NonIkKinematic)
            return;
        if (slot.weight <= 0.0001f)
            return;

        _layerWeight = Mathf.Clamp01(slot.weight);
        int dir = slot.playDirection != 0 ? slot.playDirection : 1;
        _time += deltaTime * _speed * dir;

        if (_clip.length <= 0.0001f)
            return;

        if (dir >= 0)
            _time = Mathf.Clamp(_time, 0f, _clip.length);
        else
            _time = Mathf.Clamp(_time, 0f, _clip.length);

        RagdollPoseUtility.SampleClipOntoRagdoll(ragdoll, _clip, _time);
        RagdollPoseUtility.ZeroRagdollVelocities(ragdoll);
    }

    void SnapshotAndSetKinematic(RagdollSystem ragdoll)
    {
        _snapshots.Clear();
        if (ragdoll?.ragdollRoot == null)
            return;

        Rigidbody[] rbs = ragdoll.ragdollRoot.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in rbs)
        {
            if (rb == null)
                continue;
            _snapshots.Add(new RigidbodySnapshot
            {
                body = rb,
                isKinematic = rb.isKinematic,
                constraints = rb.constraints
            });
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }
    }

    void RestoreRigidbodies()
    {
        foreach (var snap in _snapshots)
        {
            if (snap.body == null)
                continue;
            snap.body.isKinematic = snap.isKinematic;
            snap.body.constraints = snap.constraints;
        }
        _snapshots.Clear();
    }
}
