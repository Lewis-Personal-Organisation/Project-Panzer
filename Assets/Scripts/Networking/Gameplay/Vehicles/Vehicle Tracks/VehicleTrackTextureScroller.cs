using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class VehicleTrackTextureScroller : LocalVehicleComponent
{
    [SerializeField] private Transform rendererHolder;
    [SerializeField] private Material trackMaterial;
    [SerializeField] private float trackOffset = 0.0f;
    public bool debug = false;

    [ShowIf("debug"), PropertyRange(-1, 1)] public float turnInput;
    
    private new void Awake()
    {
        base.Awake();
        
        if (rendererHolder.TryGetComponent(out Renderer rend))
        {
            trackMaterial = rend.material;
        }
        else
        {
            Debug.LogError("VehicleTrackTextureScroller :: Awake :: Track Material not set or found!", this.gameObject);
        }
    }
    
    /// <summary>
    /// Sets the track material offset using velocity to mimi rotating tracks
    /// </summary>
    public void ApplyTrackScroll()
    {
        float directionalInput = vehicle.velocityTracker.z.velocity switch
        {
            > 0f => Mathf.Max(vehicle.velocityTracker.z.velocity, vehicle.inputManager.turnInputValue),
            < 0f => Mathf.Min(vehicle.velocityTracker.z.velocity, vehicle.inputManager.turnInputValue),
            _    => 0f
        };
        
        #if UNITY_EDITOR
        trackOffset += debug ? turnInput : directionalInput * Time.deltaTime;
        #else
        trackOffset +=  directionalInput * Time.deltaTime;
        #endif
        
        // Set track offset to match the lowest of velocity and track rotation.
        // track offset is always a remainder of 1.
        trackOffset %= 1.0f;                                                            
        trackMaterial.SetFloat("_TrackOffset", trackOffset);
    }
}
