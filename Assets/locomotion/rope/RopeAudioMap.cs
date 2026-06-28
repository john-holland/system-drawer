using Locomotion.Audio;
using UnityEngine;

/// <summary>Arc-indexed collision/scrape/snap audio events along the rope.</summary>
public class RopeAudioMap
{
    readonly RopeConfig _config;
    readonly RopeArcLengthState _arc;
    readonly RopeOverlapIndex _overlap;
    readonly float[] _impact;
    readonly float[] _scrape;
    readonly bool[] _snapPending;

    public RopeAudioMap(RopeConfig config, RopeArcLengthState arc, RopeOverlapIndex overlap)
    {
        _config = config;
        _arc = arc;
        _overlap = overlap;
        int bins = config.ArcBinCount;
        _impact = new float[bins];
        _scrape = new float[bins];
        _snapPending = new bool[bins];
    }

    public void ClearFrame()
    {
        for (int i = 0; i < _impact.Length; i++)
        {
            _impact[i] = 0f;
            _scrape[i] = 0f;
            _snapPending[i] = false;
        }
    }

    public void AccumulateFromOverlaps()
    {
        foreach (RopeOverlapEntry e in _overlap.Entries)
        {
            int bin = _arc.ArcToBin(e.arcA);
            _impact[bin] = Mathf.Max(_impact[bin], e.penetration * 20f);
            _scrape[bin] = Mathf.Max(_scrape[bin], e.penetration * 5f);
        }
    }

    public void QueueSnap(float arcM)
    {
        int bin = _arc.ArcToBin(arcM);
        _snapPending[bin] = true;
    }

    public void EmitEvents(Transform listener, AudioClip scrapeClip, AudioClip impactClip, AudioClip snapClip)
    {
        if (listener == null)
            return;

        for (int i = 0; i < _impact.Length; i++)
        {
            float arcM = i * _config.arcBinSizeM;
            Vector3 pos = listener.position + listener.forward * (arcM - _arc.WoundLengthM);

            if (_snapPending[i] && snapClip != null)
                PlayOneShot(snapClip, pos, 1f, listener);

            if (_impact[i] > 0.2f && impactClip != null)
                PlayOneShot(impactClip, pos, Mathf.Clamp01(_impact[i]), listener);

            if (_scrape[i] > 0.05f && scrapeClip != null)
                PlayOneShot(scrapeClip, pos, Mathf.Clamp01(_scrape[i] * 0.5f), listener);
        }
    }

    static void PlayOneShot(AudioClip clip, Vector3 position, float volume, Transform listener)
    {
        var go = new GameObject("RopeAudioOneShot");
        go.transform.position = position;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = 1f;
        src.volume = volume;
        src.Play();
        Object.Destroy(go, clip.length + 0.1f);

        if (AudioPathingSolver.Instance != null && listener != null)
        {
            // Occlusion hook: future integration can attenuate src by transmission heuristic.
        }
    }

    public float GetImpactAtArc(float arcM)
    {
        return _impact[_arc.ArcToBin(arcM)];
    }
}
