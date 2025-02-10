using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CharAnim : MonoBehaviour
{
    public GameObject player;
    private PlayerMovement data;
    private Animator mAnimator;
    bool isJumping = false;
    bool isRunning = false;
    
    void Start()
    {
        mAnimator = gameObject.GetComponent<Animator>();
        data = player.GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (mAnimator != null)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
            {
                isRunning = true;
            }
            if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D))
            {
                isRunning = false;
            }
            if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
            {
                mAnimator.SetTrigger("takeOff");
                StartCoroutine(SetJumpingAfterDelay());
            }
            if (data.RB.linearVelocity.y == 0 && isJumping)
            {
                isJumping = false;
            }
            mAnimator.SetBool("isJumping", isJumping);
            mAnimator.SetBool("isRunning", isRunning);
        }
    }
    private IEnumerator SetJumpingAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        isJumping = true;
    }
}
