using UnityEngine;

public class GroundProximity : MonoBehaviour
{
    public bool nearGround;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground"))
            nearGround = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Ground"))
            nearGround = false;
    }
}
