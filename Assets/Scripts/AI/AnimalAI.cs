using UnityEngine;
using UnityEngine.AI;

/*
 * Simple animal AI that can idle and wander around.
 * Now includes animation control using Vert and State parameters.
 * - Anthony NOTE: I did not code this and i have no idea how it works, so if you want to change it, good luck. 
*/

public enum AnimalState
{
    Idle,
    Wander,
    Eat,
    Sleep
}

public class AnimalAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    public AnimalState currentState;

    public float wanderRadius = 10f;
    public float stateTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        ChangeState(AnimalState.Wander);
    }

    void Update()
    {
        stateTimer -= Time.deltaTime;

        float speed = agent.velocity.magnitude;

        // Switch between idle and movement blend trees
        float vertValue = speed > 0.1f ? 1f : 0f;
        animator.SetFloat("Vert", vertValue);

        // Drive walk/run blend inside movement tree
        float normalizedSpeed = Mathf.Clamp01(speed / agent.speed);
        animator.SetFloat("State", normalizedSpeed);

        Debug.Log("Velocity: " + agent.velocity);
        Debug.Log("Vert: " + vertValue + " | State: " + normalizedSpeed);

        switch (currentState)
        {
            case AnimalState.Idle:
                if (stateTimer <= 0)
                    ChangeState(AnimalState.Wander);
                break;

            case AnimalState.Wander:
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                    ChangeState(AnimalState.Idle);
                break;
        }
    }

    void ChangeState(AnimalState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case AnimalState.Idle:
                stateTimer = Random.Range(2f, 5f);
                break;

            case AnimalState.Wander:
                Vector3 newPos = RandomNavSphere(transform.position, wanderRadius);
                agent.SetDestination(newPos);
                break;
        }
    }

    Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, -1);

        return navHit.position;
    }
}