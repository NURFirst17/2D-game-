using UnityEngine;

public class Test : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
        {
            animator.SetTrigger("ArmShoot");
        }
    }
}