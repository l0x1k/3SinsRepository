using UnityEngine;

public class JumpPoints : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CharacterControllerMovement movement))
        {
            movement.IsCanJump = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out CharacterControllerMovement movement))
        {
            movement.IsCanJump = false;
        }
    }
}
