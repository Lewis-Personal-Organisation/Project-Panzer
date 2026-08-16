using System;

public class BehaviourStep
{
    private enum BehaviourStage
    {
        Starting,
        Running,
        Complete
    }
    
    private BehaviourStage stage;
    private Action startAction;
    private Action behaviour;
    private Func<bool> completeCondition;
    private Action onComplete;
    private bool stopOnComplete;
    public bool ShouldCancel => stage == BehaviourStage.Complete && stopOnComplete;
    
    
    public BehaviourStep(Action startAction, Action behaviour, Func<bool> completeCondition, Action onComplete, bool stopOnComplete)
    {
        this.startAction = startAction;
        this.behaviour = behaviour;
        this.completeCondition = completeCondition;
        this.onComplete = onComplete;
        this.stopOnComplete = stopOnComplete;
    }

    /// <summary>
    /// Process this behaviour step and return finishe state
    /// </summary>
    /// <returns></returns>
    public bool Process()
    {
        switch (stage)
        {
            case BehaviourStage.Starting:
                startAction?.Invoke();
                stage = BehaviourStage.Running;
                break;
            
            case BehaviourStage.Running:
                behaviour.Invoke();

                if (completeCondition == null || completeCondition != null && completeCondition())
                {
                    stage = BehaviourStage.Complete;
                    onComplete?.Invoke();
                }
                break;
            
            case BehaviourStage.Complete:
                onComplete?.Invoke();
                break;
        }
        
        return stage == BehaviourStage.Complete;
    }
}
