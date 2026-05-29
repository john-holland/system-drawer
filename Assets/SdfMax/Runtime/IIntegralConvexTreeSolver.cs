using System.Collections.Generic;
using UnityEngine;

namespace SdfMax
{
    public interface IIntegralConvexTreeSolver
    {
        IReadOnlyList<IntegralConvexTreeNode> Leaves { get; }
        Bounds RootBounds { get; }
        void Build(SdfMaxEvaluator evaluator, Bounds localBounds, SdfMaxSolverProfile profile);
    }
}
