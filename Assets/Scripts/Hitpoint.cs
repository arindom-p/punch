using UnityEngine;

public class Hitpoint : MonoBehaviour
{
    [SerializeField] private Player playerScript;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<Player>() != playerScript)
            playerScript.DidHitOpponent();
    }
}
