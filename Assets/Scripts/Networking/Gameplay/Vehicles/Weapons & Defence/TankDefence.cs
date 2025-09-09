using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TankDefence : MonoBehaviour
{
    [SerializeField] private float health = 100;
    [SerializeField] private TankArmour tankArmour;
    [FormerlySerializedAs("hitMask")] [SerializeField]
    public LayerMask shellMask;
    private int instanceID;
    
    
    private void Awake()
    {
        instanceID = this.gameObject.GetInstanceID();
    }


    private void OnTriggerEnter(Collider other)
    {
        if ((shellMask.value & 1 << other.gameObject.layer) != 0)
        {
            if (other.transform.root.TryGetComponent(out TankShellAdv shell))
            {
                if (instanceID == shell.controller.gameObject.GetInstanceID())
                    return;

                Debug.Log($"Shell intercepted by other gameobject! {gameObject.name}. Is owner: {shell.IsOwner}");
            }
        }
    }
}
