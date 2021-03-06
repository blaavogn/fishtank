﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    //                  Not chasing                      Chasing:
    public enum State { PATROL, EAT, FROZEN, OUTOFSIGHT, INSIGHT, CHARGE};
    private int curPatTarget = -1;
    private Vector3 target;
    private Transform targetTransform;
    private float velocity = 1;
    private float timer = 0;
    private Vector3 initialPosition;

    public Vector3 ModelOffset;
    public State state;
    private PlayerFollowers playerFollowers;
    private List<Vector3> patrolPath = new List<Vector3>(); //Found from game hierachy
    private Animator animator;
    public LayerMask layerMask;

    public float SightRange = 20;
    public float chaseVelocity = 6, pathVelocity = 4, chargeVelocity = 30;
    public float maxChaseDistance = float.PositiveInfinity;
    public float PhysicalSizeRadius = 2, ChargeDistance = 6, EatTime = 2;
    public Transform CameraTarget;
    private AudioSource audioSource;
    
    void Start ()
    {
        audioSource = GetComponents<AudioSource>()[1];
        initialPosition = transform.position;
        if(maxChaseDistance < 10)
            throw new Exception("maxChaseDistance has to be larger than 10");
        playerFollowers = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerFollowers>();
        animator = GetComponent<Animator>();
        Navigator nav = GameObject.FindGameObjectWithTag("Navigator").GetComponent<Navigator>();

        Transform path = transform.parent.FindChild("Path");
        Vector3[] milestones = new Vector3[path.childCount + 1];
        milestones[0] = transform.position;
        for (int i = 0; i < path.childCount; i++)
            milestones[i + 1] = path.GetChild(i).transform.position;

        for(int i = 0; i < milestones.Length; i++)
        {
            Vector3 from = milestones[i];
            Vector3 to = milestones[(i + 1) % milestones.Length];
            List<Vector3> localPath;
            try {
                if (nav.TryFindPath(from, to, out localPath))
                    patrolPath.AddRange(localPath);
                else
                    Debug.Log("Path not found NOTHING SHOULD WORK", path.GetChild(i % milestones.Length));
            } catch(Exception e)
            {
                Debug.Log(e);
                Debug.Log("Path not found NOTHING SHOULD WORK", path.GetChild(i));
            }
        }
        state = State.PATROL;
        SetPathPoint();        
    }

    void SetPathPoint()
    {
        target = patrolPath[++curPatTarget % patrolPath.Count];
    }

    protected void FixedUpdate()
    {
        switch(state)
        {
            case State.PATROL:
                audioSource.Stop();
                if ((transform.position - target).magnitude < 0.01f)
                    SetPathPoint();
                velocity = pathVelocity;
                var t = playerFollowers.GetTarget();
                if (t != null && CheckSight(t.position) &&
                    Vector3.Distance(playerFollowers.transform.position, initialPosition) < maxChaseDistance - 5) {
                    audioSource.Play();
                    audioSource.volume = 0;
                    state = State.INSIGHT;
                }
                break;
            case State.INSIGHT:
                audioSource.volume = Mathf.Lerp(audioSource.volume, 1, 0.02f);
                targetTransform = playerFollowers.GetTarget();
                if (targetTransform == null) { // Dirty fix
                    state = State.EAT;
                    timer = 5;
                    goto case State.EAT;
                }
                target = targetTransform.position + ModelOffset;
                velocity = Mathf.Lerp(velocity, chaseVelocity, 0.1f);
                if(!CheckSight(target - ModelOffset) || Vector3.Distance(transform.position, initialPosition) > maxChaseDistance) {
                    state = State.PATROL;
                    target = patrolPath[curPatTarget % patrolPath.Count];
                }
                else if(Vector3.Distance(transform.position,  target) < ChargeDistance)
                    state = State.CHARGE;
                break;
            case State.OUTOFSIGHT:
                break;
            case State.CHARGE:
                targetTransform = playerFollowers.GetTarget();
                target = targetTransform.position + ModelOffset;
                velocity = Mathf.Lerp(velocity, chargeVelocity, 0.1f);
                //Transition to eat through collision
                break;
            case State.EAT:
                velocity = Mathf.Lerp(velocity, chaseVelocity, 0.1f);
                if(timer < 0)
                    state = State.INSIGHT;
                if ((transform.position - target).magnitude < 0.01f)
                    SetPathPoint();
                break;
            case State.FROZEN:
                foreach (var audS in GetComponents<AudioSource>())
                    audS.volume = Mathf.Lerp(audS.volume, 0, 0.02f);
                if (animator != null)
                    animator.speed = 0;
                return;
        }

        timer -= Time.deltaTime;
        if (animator != null && targetTransform != null)
        {
            float dist = Vector3.Distance(transform.position, targetTransform.position);
            if ((state == State.INSIGHT || dist < 22) && dist > 12 && state != State.EAT)
                animator.SetInteger("Mouth", -1);
            else
                animator.SetInteger("Mouth", 1);
        }

        Vector3 newPosition = Vector3.MoveTowards(transform.position, target, velocity * Time.deltaTime);
        Vector3 movement = newPosition - transform.position;
        transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, movement, 0.1f, 0));
        transform.position += transform.forward * movement.magnitude;
    }

    private bool CheckSight(Vector3 target)
    {
        
        Vector3 direction = target - transform.position;
        Ray ray = new Ray(transform.position, direction);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * SightRange, Color.red, 0.1f);
        if (Physics.SphereCast(ray.origin, PhysicalSizeRadius, ray.direction, out hit, SightRange, layerMask))
        {
            switch (hit.transform.tag)
            {
                case "Player":
                    if (hit.transform.GetComponent<Player>().state == Player.State.SWIM)
                        return true;
                    break;
                case "Follower":
                    return true;
            }
        }
        return false;
    }

    public void SetFrozen(){
        state = State.FROZEN;        
    }

    public void OnTriggerEnter(Collider col)
    {
        if(state == State.EAT || state == State.FROZEN)
            return;
        if(col.transform.tag == "Follower")
        {
            col.transform.parent.GetComponent<FollowFish>().Respawn();
            state = State.EAT;
            timer = EatTime;
            target = patrolPath[curPatTarget % patrolPath.Count];
        }
        else if(col.transform.tag == "Player" && (targetTransform == col.transform))
        {
            state = State.EAT;
            timer = 10000;
            target = patrolPath[curPatTarget % patrolPath.Count];
            col.transform.GetComponent<Player>().Kill(Player.DeathCause.EATEN);
        }
    }
}
