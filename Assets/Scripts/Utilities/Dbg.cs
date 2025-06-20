using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Dbg : Singleton<Dbg>
{
    public enum LogRegion
    {
       Lobby,
       Gameplay,
       Connection
    }
    
    [Header("Loggable Types")]
    [SerializeField] private List<LogRegion> loggableRegions = new List<LogRegion>();
    [SerializeField] private static List<LogRegion> staticLoggableRegions = new List<LogRegion>();


    private new void Awake()
    {
        base.Awake();
    }
    
    public void Log(LogRegion type, object message)
    {
        if (!loggableRegions.Contains(type))
            return;
        
        Debug.unityLogger.Log(message);
    }

    public bool CompareLists() => staticLoggableRegions == loggableRegions;
}
