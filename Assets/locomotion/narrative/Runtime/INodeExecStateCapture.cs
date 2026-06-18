namespace Locomotion.Narrative
{
    public interface INodeExecStateCapture
    {
        void CaptureBeforeExec(INodeExecContext ctx);
        void RestoreBeforeExec(INodeExecContext ctx);
        void UndoOnRewind(INodeExecContext ctx, float targetNarrativeTime);
    }
}
