/// <summary>Feature systems that apply ratio-linked budget granularity each frame.</summary>
public interface IFeatureGranularityConsumer
{
    string FeatureId { get; }
    void ApplyFeatureGranularity(FeatureBudgetRatioRegistry ratios);
}
