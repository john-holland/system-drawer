using UnityEngine;

/// <summary>
/// Drives frame-backward animation playback tied to travel reverse arc-length budget.
/// </summary>
[AddComponentMenu("Locomotion/Travel/Reverse Playback Controller")]
public sealed class ReversePlaybackController : MonoBehaviour
{
    [Header("References")]
    public TravelAgent travelAgent;
    public RagdollAnimationSetManager animationSetManager;

    [Tooltip("Optional SystemDrawerAnimator component; resolved from children when unset.")]
    public Component systemDrawerAnimator;

    ISystemDrawerLayerControl _layerControl;

    int _playDirection = 1;
    float _reverseBudgetRemainingMeters;
    float _arcLengthCursor;
    bool _inReverse;

    public int PlayDirection => _playDirection;
    public float ReverseBudgetRemainingMeters => _reverseBudgetRemainingMeters;
    public float ArcLengthCursor => _arcLengthCursor;
    public bool InReverse => _inReverse;

    void Awake()
    {
        if (travelAgent == null)
            travelAgent = GetComponentInParent<TravelAgent>();
        if (animationSetManager == null && travelAgent != null)
            animationSetManager = travelAgent.ragdollAnimationSetManager;
        _layerControl = ResolveLayerControl();
    }

    public void EnterReverse(TravelExecutionContext ctx)
    {
        if (ctx == null || !ctx.inReverseTail)
        {
            ExitReverse();
            return;
        }

        float budget = ctx.reverseBudgetRemainingMeters > 0f
            ? ctx.reverseBudgetRemainingMeters
            : ctx.reverseBudgetMeters;
        if (budget <= 1e-4f || !TravelPathReverseLimits.AllowsReverse(
                ctx.travelAgent != null ? ctx.travelAgent.reverseLegLimit01 : 1f))
        {
            ExitReverse();
            return;
        }

        _playDirection = -1;
        _inReverse = true;
        _reverseBudgetRemainingMeters = budget;
        _arcLengthCursor = budget;
        ApplyDirectionToStack();
    }

    public void ExitReverse()
    {
        _playDirection = 1;
        _inReverse = false;
        _reverseBudgetRemainingMeters = 0f;
        _arcLengthCursor = 0f;
        ApplyDirectionToStack();
    }

    public void AdvanceArcLength(float deltaMeters)
    {
        if (!_inReverse || deltaMeters <= 0f)
            return;

        _arcLengthCursor = Mathf.Max(0f, _arcLengthCursor - deltaMeters);
        _reverseBudgetRemainingMeters = _arcLengthCursor;
        if (_reverseBudgetRemainingMeters <= 1e-4f)
            ExitReverse();
    }

    public void SetPlayDirection(int direction)
    {
        int clamped = direction >= 0 ? 1 : -1;
        if (_playDirection == clamped)
            return;
        _playDirection = clamped;
        _inReverse = clamped < 0;
        ApplyDirectionToStack();
    }

    public void SyncFromProvider(TravelExecutionContextProvider provider)
    {
        if (provider == null)
            return;

        if (provider.InReverseTail && provider.ReverseBudgetRemainingMeters > 1e-4f)
        {
            _playDirection = -1;
            _inReverse = true;
            _reverseBudgetRemainingMeters = provider.ReverseBudgetRemainingMeters;
            _arcLengthCursor = provider.ReverseBudgetRemainingMeters;
        }
        else if (_inReverse)
        {
            ExitReverse();
        }

        ApplyDirectionToStack();
    }

    void ApplyDirectionToStack()
    {
        if (_layerControl == null)
            _layerControl = ResolveLayerControl();

        animationSetManager?.SetPlayDirection(_playDirection);
        _layerControl?.SetGlobalPlayDirection(_playDirection);
    }

    ISystemDrawerLayerControl ResolveLayerControl()
    {
        ISystemDrawerLayerControl fromField = SystemDrawerLayerControlLookup.FromComponent(systemDrawerAnimator);
        if (fromField != null)
            return fromField;

        return SystemDrawerLayerControlLookup.FindInChildren(this);
    }
}
