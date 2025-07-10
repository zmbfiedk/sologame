using UnityEngine;

public class ParryBoostZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerCombat playerCombat = other.GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            playerCombat.EnterParryBoostZone();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCombat playerCombat = other.GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            playerCombat.ExitParryBoostZone();
        }
    }
}
