using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ValueTracker
{
    private readonly MonoBehaviour host;
    private readonly Func<bool> activatePredicate;
    private readonly Func<bool> resetPredicate;
    private readonly UnityAction onValueReached = null;
    private readonly UnityAction onShutdown = null;
    private Coroutine routine = null;
        
    public ValueTracker(MonoBehaviour host, Func<bool> activatePredicate, Func<bool> resetPredicate, UnityAction onValueReached, UnityAction onShutdown = null)
    {
        this.host = host;
        this.activatePredicate = activatePredicate;
        this.resetPredicate = resetPredicate;
        this.onValueReached = onValueReached;
        this.onShutdown = onShutdown;
        Shutdown();
        routine = host.StartCoroutine(WaitForActivate());
    }
        
    private IEnumerator WaitForActivate()
    {
        yield return new WaitUntil(activatePredicate);
        onValueReached.Invoke();
        routine = host.StartCoroutine(Reset());
    }
        
    private IEnumerator Reset()
    {
        yield return new WaitUntil(resetPredicate);
        routine = host.StartCoroutine(WaitForActivate());
    }

    public void Shutdown()
    {
        Debug.Log("SHUTDOWN!");
        if (routine != null)
        {
            host.StopCoroutine(routine);
            onShutdown?.Invoke();
        }
    }
}