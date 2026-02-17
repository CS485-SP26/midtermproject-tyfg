using System;
using UnityEngine;

namespace Character 
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] public GameObject player;
        [SerializeField] private Vector3 offset = new(0f, 0f, -3f);

        void Start()
        {
            Debug.Assert(player, "CameraFollow requires a player (GameObject).");
        }

        void LateUpdate()
        {
            transform.position = player.transform.position + offset;         
        }
    }
}
