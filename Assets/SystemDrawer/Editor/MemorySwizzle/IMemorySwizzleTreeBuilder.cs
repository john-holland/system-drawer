/// <summary>Builds a memory swizzle tree for one view mode.</summary>
public interface IMemorySwizzleTreeBuilder
{
    MemorySwizzleViewMode Mode { get; }
    MemorySwizzleNode Build(MemorySwizzleBuildContext ctx);
}
