using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class VehicleDistanceTracker : MonoBehaviour
{
    [SerializeField] private bool track = false;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private float spawnDistance = 3F;
    [SerializeField] private Vector3 deltaDistance;
    [SerializeField] private Vector3 lastPos;

    [SerializeField] private GameObject debugPrefab;
    [SerializeField] private List<GameObject> debugObjs = new List<GameObject>();
    private ObjectPool<GameObject> pool;

    private void Awake()
    {
        pool = new ObjectPool<GameObject>(
            CreateDbgObject,
            OnGetFromPool,
            OnReturnToPool,
            OnDestroyPooledObject,
            false,
            25,
            50);
    }

    private void FixedUpdate()
    {
        if (!track || targetTransform == null)
            return;
        
        // If positions are roughly the same, dont measure
        if (Mathf.Approximately(lastPos.x, targetTransform.position.x) && 
            Mathf.Approximately(lastPos.y, targetTransform.position.y) && 
            Mathf.Approximately(lastPos.z, targetTransform.position.z))
            return;
        
        // Find the absolute distances
        deltaDistance += new Vector3(Mathf.Abs(targetTransform.position.x - lastPos.x), Mathf.Abs(targetTransform.position.y - lastPos.y), Mathf.Abs(targetTransform.position.z - lastPos.z));

        if (deltaDistance.x > spawnDistance || deltaDistance.z > spawnDistance)
        {
            deltaDistance = Vector3.zero;
            // debugObjs.Add(GameObject.Instantiate(debugPrefab, null, true));
            // debugObjs[^1].transform.position = targetTransform.position;
            // debugObjs[^1].transform.rotation = targetTransform.rotation;
            // debugObjs[^1].transform.localScale = Vector3.one * 0.2F;
            
            // Spawn Particle
            GameObject obj = pool.Get();
            // obj.transform.position = targetTransform.position;
            // obj.transform.rotation = targetTransform.rotation;
            // obj.transform.localScale = Vector3.one * 0.2F;
        }
        
        // Cache last pos
        lastPos =  targetTransform.position;
    }

    private GameObject CreateDbgObject()
    {
        GameObject obj = Instantiate(debugPrefab, null, true);
        return obj;
    }
    
    void OnGetFromPool(GameObject obj)
    {
        obj.SetActive(true);
        obj.transform.position = targetTransform.position;
        obj.transform.rotation = targetTransform.rotation;
        obj.transform.localScale = Vector3.one * 0.2F;
    }

    void OnReturnToPool(GameObject obj)
    {
        obj.SetActive(false);
    }
    
    void OnDestroyPooledObject(GameObject obj)
    {
        Destroy(obj);
    }

    public GameObject Get()
    {
        return pool.Get();
    }

    public void Release(GameObject obj)
    {
        pool.Release(obj);
    }
}

