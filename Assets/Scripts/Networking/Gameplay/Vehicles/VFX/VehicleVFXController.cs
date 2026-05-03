using Unity.Netcode;
using UnityEngine;

public class VehicleVFXController : NetworkedVehicleComponent
{
    public NetworkVariable<bool> aliveParticles = new  NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
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
    
    public ValueTracker exhaustSmoke;
    
    public ParticleSystem[] exhaustParticles;
    [SerializeField] private ParticleSystem onDeathFireParticles;
    [SerializeField] private ParticleSystem onDeathSmokeParticles;

    
    private void Awake()
    {
        CacheParticles();
        
        aliveParticles.OnValueChanged += (bool oldValue, bool newValue) =>
        {
            Debug.Log($"VehicleVFXController :: aliveParticles :: {newValue}");
            ToggleExhausts(newValue);
            onDeathFireParticles.gameObject.SetActive(!newValue);

            if (newValue)
            {
                onDeathFireParticles.Pause();
            }
            else
            {
                onDeathFireParticles.Play(); 
            }
        };
    }

    public void Setup(VehicleController vehicleController)
    {
        this.vehicle = vehicleController;
        
        // TODO EXTRA: Could be moved to Awake method so all clients make use of this
        exhaustSmoke = new ValueTracker(this,
            () => vehicle.velocityTracker.z.velocity >= 10,
            () => vehicle.velocityTracker.z.velocity < 10, 
            () => {
                LerpLifetimeOptions(2, 0.2f);
                EmitImmediate(50);
            },
            () => ToggleExhausts(false)
        );
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

    public void OnRespawn()
    {
        onDeathFireParticles.gameObject.SetActive(false);
        onDeathFireParticles.Pause();
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

    /// <summary>
    /// Toggle exhaust particles on/off
    /// </summary>
    /// <param name="choice"></param>
    public void ToggleExhausts(bool choice)
    {
        Debug.Log($"EXHAUSTS {choice}");
        for (int i = 0; i < exhaustParticles.Length; i++)
        {
            if (choice)
            {
                exhaustParticles[i].Play();
            }
            else
            {
                exhaustParticles[i].Pause();
            }
        }
    }

    public void CookOffEffect()
    {
        
    }
}
