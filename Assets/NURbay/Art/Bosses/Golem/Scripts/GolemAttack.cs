using UnityEngine;

public class GolemAttack : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            animator.SetTrigger("MeleeAttack");
        }
    }
}