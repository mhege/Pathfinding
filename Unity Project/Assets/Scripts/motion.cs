using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]

public class motion : MonoBehaviour
{
    //Animator components
    private Animator animator;
    private static readonly int IsWalking = Animator.StringToHash("isWalking");
    private static readonly int WasWalking = Animator.StringToHash("wasWalking");

    //Variables and constants for steering arrive and align
    private Vector3 direction;
    private float velocity = 0.0f;
    private float fvelo = 0.0f;
    private float rvelo = 0.0f;
    private const float vmax = 1.0f;
    private const float rvmax = 1.5f;
    private float acceleration = .15f;
    private const float amax = .45f;
    private const float ramax = 1.5f;

    private const float ra = 1.0f;
    private const float t2t = 1.0f;

    // Setup animator
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Path following for nodes
    public void moveToNode(node node)
    {
        animator.SetBool(WasWalking, false);
        animator.SetBool(IsWalking, true);

        direction = (node.transform.position - transform.position).normalized;
        velocity = Math.Min(velocity + amax * Time.deltaTime, vmax);
        rvelo = Math.Min(rvelo + ramax * Time.deltaTime, rvmax);

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rvelo * Time.deltaTime);
        transform.position = transform.position + velocity * Time.deltaTime * transform.forward.normalized;
    }

    // Path following for target node
    public void moveToTarget(node node)
    {
        animator.SetBool(WasWalking, false);
        animator.SetBool(IsWalking, true);

        direction = (node.transform.position - transform.position).normalized;
        fvelo = vmax * ((node.transform.position - transform.position).magnitude / ra);
        velocity = Math.Min(velocity + acceleration * Time.deltaTime, vmax);
        rvelo = Math.Min(rvelo + ramax * Time.deltaTime, rvmax);
        acceleration = Math.Min((fvelo - velocity) / t2t, amax);

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rvelo * Time.deltaTime);
        transform.position = transform.position + velocity * Time.deltaTime * transform.forward.normalized;

    }

    // Delegate to arrive
    public void arrive()
    {
        animator.SetBool(IsWalking, false);
        animator.SetBool(WasWalking, true);

        velocity = velocity - amax * Time.deltaTime;

        if (velocity < 0.0f)
            velocity = 0.0f;
        else
            transform.position = transform.position + (velocity * Time.deltaTime) * transform.forward.normalized;

    }

    // Delegate to align
    public void align(node node)
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(node.transform.position - transform.position), rvmax * Time.deltaTime);
    }

}
