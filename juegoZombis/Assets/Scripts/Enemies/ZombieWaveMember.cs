using UnityEngine;

public class ZombieWaveMember : MonoBehaviour
{
    public ZombieSpawner spawner;
    public bool isInfiniteZombie = false;
    private bool notified = false;

    void OnDestroy()
    {
        if (notified) return;
        notified = true;

        if (spawner != null)
        {
            spawner.NotifyZombieDestroyed(isInfiniteZombie);

            ZombieAI ai = GetComponent<ZombieAI>();
            if (ai != null)
            {
                spawner.UnregisterZombie(ai);
            }
        }
    }
}
