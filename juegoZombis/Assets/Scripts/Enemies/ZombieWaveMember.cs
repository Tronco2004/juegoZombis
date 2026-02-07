using UnityEngine;

public class ZombieWaveMember : MonoBehaviour
{
    public ZombieSpawner spawner;
    private bool notified = false;

    void OnDestroy()
    {
        if (notified) return;
        notified = true;

        if (spawner != null)
        {
            spawner.NotifyZombieDestroyed();
        }
    }
}
