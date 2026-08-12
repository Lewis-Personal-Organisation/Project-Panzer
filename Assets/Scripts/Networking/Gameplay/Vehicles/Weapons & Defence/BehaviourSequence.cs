using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BehaviourSequence
{
    private List<BehaviourStep> behaviourSteps = new List<BehaviourStep>();
    [SerializeField] private int stepIndex = 0;
    public bool isPaused = false;

    public BehaviourSequence(bool startPaused = false, params BehaviourStep[] steps)
    {
        isPaused = startPaused;
        for (int i = 0; i < steps.Length; i++)
        {
            behaviourSteps.Add(steps[i]);
        }
    }

    public void AddStep(Action startBehaviour, Action behaviour, Func<bool> completeCondition, Action onComplete, bool stopOnComplete)
    {
        behaviourSteps.Add(new BehaviourStep(startBehaviour, behaviour, completeCondition, onComplete, stopOnComplete));
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
        if (behaviourSteps.Count == 0 || isPaused)
            return;
        
        // Process the step. Cancel if required or move to the next step if possible
        if (behaviourSteps[stepIndex].Process())
        {
            if (behaviourSteps[stepIndex].ShouldCancel)
            {
                behaviourSteps.Clear();
                return;
            }
            
            if (stepIndex < behaviourSteps.Count - 1)
                stepIndex++;
        }
    }
}

