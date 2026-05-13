using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleTrackTextureScroller : LocalVehicleComponent
{
    [SerializeField] private Transform rendererHolder;
    [SerializeField] private Material trackMaterial;
    [SerializeField] private float trackOffset = 0.0f;

    
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
        // Set track offset to match the lowest of velocity and track rotation.
        // track offset is always a remainder of 1.
        trackOffset += Mathf.Max(vehicle.velocityTracker.z.velocity, vehicle.inputManager.turnInputValue) * Time.deltaTime;
        trackOffset %= 1.0f;                                                            
        trackMaterial.SetFloat("_TrackOffset", trackOffset);
    }
}
