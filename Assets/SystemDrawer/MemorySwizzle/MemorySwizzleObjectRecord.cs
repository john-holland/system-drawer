using System;

/// <summary>Flat object row from a memory snapshot or live scan.</summary>
public sealed class MemorySwizzleObjectRecord
{
    public int NativeIndex = -1;
    public int ManagedIndex = -1;
    public string Name = "";
    public string TypeName = "";
    public Type SystemType;
    public long SizeBytes;
    public int InstanceId;
    public int ParentInstanceId;
    public string ScenePath = "";
    public bool IsGameObject;
    public bool IsComponent;
}
