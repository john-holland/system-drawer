using UnityEngine;

/// <summary>
/// Runtime thought dispatch for narrative actions without a Narrative → Runtime assembly reference.
/// </summary>
public static class LocomotionThoughtDispatch
{
    public static bool TrySendThought(GameObject senderGo, GameObject receiverGo, int thoughtTypeOrdinal, object payload)
    {
        if (senderGo == null || receiverGo == null)
            return false;

        var senderBrain = senderGo.GetComponent<Brain>();
        var receiverBrain = receiverGo.GetComponent<Brain>();
        if (senderBrain == null || receiverBrain == null)
            return false;

        if (!System.Enum.IsDefined(typeof(ThoughtType), thoughtTypeOrdinal))
            return false;

        var thoughtType = (ThoughtType)thoughtTypeOrdinal;
        object runtimePayload = ConvertPayload(thoughtType, payload);
        var thought = new ThoughtData(senderBrain, receiverBrain, thoughtType, runtimePayload);
        senderBrain.SendThought(receiverBrain, thought);
        return true;
    }

    static object ConvertPayload(ThoughtType thoughtType, object narrativePayload)
    {
        if (narrativePayload == null)
            return DefaultPayload(thoughtType);

        var runtimeType = GetRuntimePayloadType(thoughtType);
        if (runtimeType == null)
            return null;

        var runtimePayload = System.Activator.CreateInstance(runtimeType);
        CopyFields(narrativePayload, runtimePayload);
        return runtimePayload;
    }

    static object DefaultPayload(ThoughtType thoughtType)
    {
        return thoughtType switch
        {
            ThoughtType.Decision => new DecisionThoughtPayload(),
            ThoughtType.Query => new QueryThoughtPayload { queryId = System.Guid.NewGuid().ToString("N"), channels = QueryChannel.All },
            ThoughtType.Alert => new AlertThoughtPayload(),
            ThoughtType.BehaviorTree => new BehaviorTreeThoughtPayload(),
            ThoughtType.RequestPrune => new RequestPruneThoughtPayload(),
            _ => null
        };
    }

    static System.Type GetRuntimePayloadType(ThoughtType thoughtType) => thoughtType switch
    {
        ThoughtType.Decision => typeof(DecisionThoughtPayload),
        ThoughtType.Query => typeof(QueryThoughtPayload),
        ThoughtType.Response => typeof(ResponseThoughtPayload),
        ThoughtType.Alert => typeof(AlertThoughtPayload),
        ThoughtType.BehaviorTree => typeof(BehaviorTreeThoughtPayload),
        ThoughtType.RequestPrune => typeof(RequestPruneThoughtPayload),
        _ => null
    };

    static void CopyFields(object source, object target)
    {
        var sourceType = source.GetType();
        var targetType = target.GetType();
        foreach (var field in sourceType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            var targetField = targetType.GetField(field.Name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (targetField == null || targetField.FieldType != field.FieldType)
                continue;
            targetField.SetValue(target, field.GetValue(source));
        }
    }
}
