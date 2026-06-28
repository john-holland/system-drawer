using System.Collections.Generic;
using UnityEngine;

namespace DestructibleEnvironment
{
    [DisallowMultipleComponent]
    public class DestructiblePlaybackController : MonoBehaviour
    {
        public BehaviorTree behaviorTree;
        public DestructibleEnvironmentMeshRenderer owner;

        DestructibleFallContext _context;
        List<int> _activePieceIds;

        public DestructibleFallContext Context => _context;

        void Awake()
        {
            if (behaviorTree == null)
                behaviorTree = GetComponent<BehaviorTree>();
            enabled = false;
        }

        public void Bind(
            DestructibleEnvironmentMeshRenderer destructible,
            DestructibleFallContext context,
            List<int> activePieceIds)
        {
            owner = destructible;
            _context = context;
            _activePieceIds = activePieceIds;
        }

        public void BeginPlayback()
        {
            if (behaviorTree == null || _context == null || _activePieceIds == null)
                return;

            enabled = true;
            behaviorTree.decisionTime = 0f;
            behaviorTree.currentNode = behaviorTree.rootNode;
            behaviorTree.Execute();
        }

        void Update()
        {
            if (behaviorTree == null || _context == null)
                return;

            BehaviorTreeStatus status = behaviorTree.Execute();
            if (status == BehaviorTreeStatus.Success || status == BehaviorTreeStatus.Failure)
                enabled = false;
        }
    }
}
