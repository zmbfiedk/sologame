using UnityEngine;
using TMPro;

public class SpeedDisplay : MonoBehaviour
{
    [SerializeField] private Movement playerMovement;
    [SerializeField] private TMP_Text speedText;

    void Update()
    {
        if (playerMovement != null && speedText != null)
        {
            Vector3 horizontalVelocity = new Vector3(playerMovement.velocity.x, 0, playerMovement.velocity.z);
            float speed = horizontalVelocity.magnitude;
            speedText.text = $"Speed: {speed:F1}";
        }
    }
}
