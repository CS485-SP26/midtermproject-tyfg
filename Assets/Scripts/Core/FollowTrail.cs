using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core {
    public class FollowTrail : MonoBehaviour
    {
        Queue<Vector3> trail = new Queue<Vector3>();
        float respawnTime = 0f;
        [SerializeField] private float respawnFreq = 0.1f;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            trail.Enqueue(transform.position);
        }

        public Vector3 Peek() { return trail.Peek(); }

        void Update()
        {
            respawnTime -= Time.deltaTime;
            if (respawnTime < 0f)
            {
                while (trail.Count >= 10)
                {
                    trail.Dequeue();
                }
                trail.Enqueue(transform.position);
                respawnTime = respawnFreq;
            }
        }
    }
}
