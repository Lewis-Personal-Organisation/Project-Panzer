using UnityEngine;

public class TankVFXController : MonoBehaviour
{
    [System.Serializable]
    private class CachedParticleData
    {
        public ParticleSystem.VelocityOverLifetimeModule  velocityOverLifetime;
        public AnimationCurve sizeOverLifetimeCurve;
        public ParticleSystem.MinMaxCurve lifetimeCurve;

        public CachedParticleData(ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime, AnimationCurve sizeOverLifetimeCurve, ParticleSystem.MinMaxCurve  lifetimeCurve)
        {
            this.velocityOverLifetime = velocityOverLifetime;
            this.sizeOverLifetimeCurve = sizeOverLifetimeCurve;
            this.lifetimeCurve = lifetimeCurve;
        }
    }
    
    [Header("Settings")]
    [Header("Lifetime")]
    public float startLifetime;
    public float endLifetime;
    [Space(1)]
    [Header("Velocity")]
    public float velocity;
    
    [Space(10)]
    [SerializeField] private CachedParticleData[] cachedParticleData;
    
    public ParticleSystem[] exhaustParticles;
    
    
    private void Awake()
    {
        CacheParticles();
    }

    private void CacheParticles()
    {
        cachedParticleData = new CachedParticleData[exhaustParticles.Length];

        for (int i = 0; i < exhaustParticles.Length; i++)
        {
            cachedParticleData[i] = new CachedParticleData(exhaustParticles[i].velocityOverLifetime, 
                                                           exhaustParticles[i].sizeOverLifetime.size.curve, 
                                                           exhaustParticles[i].main.startLifetime);
        }
    }

    public void ApplyLifetimeOptions()
    {
        for (int i = 0; i < exhaustParticles.Length; i++)
        {
            ParticleSystem.MainModule main = exhaustParticles[i].main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(startLifetime, endLifetime);
        }
    }

    public void RevertLifetimeOptions()
    {
        for (int i = 0; i < exhaustParticles.Length; i++)
        {
            ParticleSystem.MainModule main = exhaustParticles[i].main;
            main.startLifetime = cachedParticleData[i].lifetimeCurve;
        }
    }

    public void LerpLifetimeOptions(float t, float minValue = 0)
    {
        if (minValue > t)
            t = minValue;
        
        // Debug.Log($"Speed Lerp: {t}");
        
        ParticleSystem.MainModule main = exhaustParticles[0].main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(cachedParticleData[0].lifetimeCurve.constantMin * t, cachedParticleData[0].lifetimeCurve.constantMax * t);
        
        
        main = exhaustParticles[1].main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(cachedParticleData[1].lifetimeCurve.constantMin * t, cachedParticleData[1].lifetimeCurve.constantMax * t);
    }

    /// <summary>
    /// Immediately emit a burst of particles where index is the particle system index and amount is the amount of particles
    /// </summary>
    /// <param name="index"></param>
    /// <param name="amount"></param>
    public void EmitImmediate(int index, int amount)
    {
        Debug.Log("EMMITED!");
        exhaustParticles[index].Emit(amount);
    }

    public void EmitImmediate(int amount)
    {
        // Debug.Log("EMMITED!");
        for (int i = 0; i < exhaustParticles.Length; i++)
        {
            exhaustParticles[i].Emit(amount);
        }
    }
}
