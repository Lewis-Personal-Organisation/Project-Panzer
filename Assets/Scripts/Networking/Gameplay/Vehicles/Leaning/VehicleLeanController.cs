using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class VehicleLeanController : LocalVehicleComponent
{
    protected float baseXLean;
    protected float baseZLean;
    public abstract float LeanX { get; }
    public abstract float LeanZ { get; }

    private new void Awake()
    {
        base.Awake();
    }
    
    public abstract void UpdateLeanValues();
}
