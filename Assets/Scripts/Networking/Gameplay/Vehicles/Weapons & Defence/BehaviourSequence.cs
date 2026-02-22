using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourSequence
{
    private List<BehaviourStep> behaviourSteps = new List<BehaviourStep>();
    [SerializeField] private int stepIndex = 0;

    public void AddStep(Action behaviour, Func<bool> completeCondition, Action onComplete, bool stopOnComplete)
    {
        behaviourSteps.Add(new BehaviourStep(behaviour, completeCondition, onComplete, stopOnComplete));
    }
    
    public void AddStep(BehaviourStep behaviourStep)
    {
        behaviourSteps.Add(behaviourStep);
    }

    public void Remove(int index)
    {
        behaviourSteps.RemoveAt(index);
    }

    public void Process()
    {
        bool isComplete = behaviourSteps[stepIndex].Process();

        if (isComplete)
        {
            // Move onto the next step if there is one
            if (stepIndex < behaviourSteps.Count - 1)
                stepIndex++;
        }
    }
}
