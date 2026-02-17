using UnityEngine;

public class GroundProximity : MonoBehaviour
{
    public bool nearGround;
    [SerializeField] LayerMask groundLayers = ~0;
    [SerializeField] string[] extraGroundTags;

    private void OnTriggerEnter(Collider other)
    {
        if (IsGroundCollider(other))
            nearGround = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsGroundCollider(other))
            nearGround = false;
    }

    bool IsGroundCollider(Collider collider)
    {
        if (!collider)
            return false;

        int layerBit = 1 << collider.gameObject.layer;
        if ((groundLayers.value & layerBit) != 0)
            return true;

        if (collider.CompareTag("Ground"))
            return true;

        if (extraGroundTags == null || extraGroundTags.Length == 0)
            return false;

        foreach (string tag in extraGroundTags)
        {
            if (!string.IsNullOrWhiteSpace(tag) && collider.CompareTag(tag))
                return true;
        }

        return false;
    }
}
