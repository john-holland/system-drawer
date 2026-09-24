using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Plumbing/Consider Plumbing Cards")]
public sealed class ConsiderPlumbingCards : MonoBehaviour
{
    public PhysicsCardSolver cardSolver;
    public float scanRangeM = 8f;
    readonly List<GoodSection> _generated = new List<GoodSection>();

    void Awake()
    {
        if (cardSolver == null) cardSolver = GetComponent<PhysicsCardSolver>();
    }

    public List<GoodSection> GenerateCards()
    {
        _generated.Clear();
        var toilets = FindObjectsByType<ToiletFixture>(FindObjectsSortMode.None);
        Vector3 pos = transform.position;
        for (int i = 0; i < toilets.Length; i++)
        {
            var t = toilets[i];
            if (t == null) continue;
            if ((t.transform.position - pos).sqrMagnitude > scanRangeM * scanRangeM) continue;
            float clog = t.plumbing != null ? t.plumbing.clog.EffectiveClog01() : 0f;
            if (clog > 0.2f)
            {
                _generated.Add(PlungeToiletCard.Generate(t));
                _generated.Add(SnakeToiletCard.Generate(t));
            }
            else
                _generated.Add(ClogToiletCard.Generate(t, 0.3f));
        }
        if (_generated.Count == 0)
            _generated.Add(MakeDefaultCard());
        if (cardSolver != null) cardSolver.AddCards(_generated);
        return _generated;
    }

    public static GoodSection MakeDefaultCard() => PlungeToiletCard.Generate(null);
}
