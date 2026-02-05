using UnityEngine;

public class ParticleWarmup : MonoBehaviour
{
    void Start()
    {
        Destroy(gameObject, 0.1f);
    }
}
