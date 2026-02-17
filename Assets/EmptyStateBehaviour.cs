using UnityEngine;

public class EmptyStateBehaviour : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var farmer = animator.GetComponent<Farmer>();
        if (farmer != null)
            farmer.SetTool("None");
    }
}
    