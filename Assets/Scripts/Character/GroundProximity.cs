using UnityEngine;

/*
* Detects if the character is near the ground, which can be used for:
*   - Coyote time
*   - Allowing jump input slightly before landing
*   - Allowing jump input slightly after leaving a platform
*   - Adjusting animation states
* Uses a trigger collider to detect proximity to the ground, and checks:    
*   - If the collider is on a layer included in groundLayers
*   - If the collider has the "Ground" tag
*   - If the collider has any tag included in extraGroundTags
* Exposes:
*   - nearGround (bool)
*/

public class GroundProximity : MonoBehaviour
{
    // True while the trigger volume overlaps a valid ground collider.
    public bool nearGround;
    // Layers considered valid ground.
    [SerializeField] LayerMask groundLayers = ~0;
    // Optional extra tags also treated as ground.
    [SerializeField] string[] extraGroundTags;

    // Marks near-ground state when entering a qualifying ground trigger.
    private void OnTriggerEnter(Collider other)
    {
        if (IsGroundCollider(other))
            nearGround = true;
    }

    // Clears near-ground state when exiting a qualifying ground trigger.
    private void OnTriggerExit(Collider other)
    {
        if (IsGroundCollider(other))
            nearGround = false;
    }

    // Returns true when collider should count as ground by layer/tag rules.
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
