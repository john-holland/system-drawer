/// <summary>
/// Optional explicit injection when cloned activation nodes cannot resolve <see cref="TravelExecutionContextProvider"/> via hierarchy.
/// </summary>
public interface ITravelExecutionContextConsumer
{
    void SetTravelExecutionContext(TravelExecutionContext ctx);
}
