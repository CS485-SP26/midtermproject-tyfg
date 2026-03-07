using UnityEngine;
using UnityEngine.AI;
/*
    * Simple animal AI that can idle and wander around. You can expand this by adding more states like eating, sleeping, etc.
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

    public AnimalState currentState;

    public float wanderRadius = 10f;
    public float stateTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        ChangeState(AnimalState.Wander);
    }

    void Update()
    {
        stateTimer -= Time.deltaTime;

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