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
    private NavMeshAgent agent;//runs the ai

    public AnimalState currentState;//current state of the animal

    public float wanderRadius = 10f;//radius for wandering around
    public float stateTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        ChangeState(AnimalState.Wander);
    }
// Update is called once per frame
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
// Function to change the current state of the animal
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
// Helper function to get a random point on the NavMesh within a certain radius
    Vector3 RandomNavSphere(Vector3 origin, float dist)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit navHit;

        NavMesh.SamplePosition(randDirection, out navHit, dist, -1);

        return navHit.position;
    }
}