using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourStep
{
    private Action behaviour;
    private Func<bool> completeCondition;
    private Action onComplete;
    private bool isComplete = false;
    private bool stopOnComplete = false;

    public BehaviourStep(Action behaviour, Func<bool> completeCondition, Action onComplete, bool stopOnComplete)
    {
        this.behaviour = behaviour;
        this.completeCondition = completeCondition;
        this.onComplete = onComplete;
        this.onComplete += () => isComplete = true;
        this.stopOnComplete = stopOnComplete;
    }

    public bool Process()
    {
        if (stopOnComplete && isComplete)
            return true;
            
        behaviour.Invoke();

        if (completeCondition == null || completeCondition != null && completeCondition())
        {
            onComplete?.Invoke();
            return true;
        }

        return false;
    }
}
