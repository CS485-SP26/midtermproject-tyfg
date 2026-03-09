using UnityEngine;
using UnityEngine.InputSystem;

public class FireworksManager : MonoBehaviour
{
    [SerializeField] ParticleSystem fireworks;

    public void OnFireworks(InputValue value)
    {
        if (value.isPressed)
        {
            LaunchFireworks();
        }
    }

    public void LaunchFireworks()
    {
        if (fireworks == null)
            return;

        fireworks.Play();
    }
}