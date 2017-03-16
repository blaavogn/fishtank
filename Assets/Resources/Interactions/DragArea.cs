﻿using UnityEngine;
using System.Collections.Generic;

public class DragArea : MonoBehaviour {
    public float MaxDrag = 10, BaseDrag = 3, DragMultiplier = 0.5f, EnemyMultiplier = 2;
    private List<GameObject> dragables;
    private Vector3 dragPoint; //From hierachy

    void Start()
    {
        dragables = new List<GameObject>();
        foreach(Transform t in transform)
            dragPoint = t.position;
    }

    void FixedUpdate () {
        Debug.Log("Here");
        foreach(GameObject g in dragables) {
            Transform t = g.transform;
            float mult = (g.tag == "Enemy") ? EnemyMultiplier : 1;

            float invDist = Mathf.Max(0, (BaseDrag + MaxDrag/Vector3.Distance(t.position, dragPoint)));
            invDist = Mathf.Min(invDist, MaxDrag) * EnemyMultiplier * DragMultiplier;

            //dragforce = Basedrag + distance

            t.position = Vector3.MoveTowards(t.position, dragPoint, invDist * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider col)
    {
        Debug.Log(col);
        if(col.tag == "Enemy" || col.tag == "Player")
            dragables.Add(col.gameObject);
    }

    void OnTriggerExit(Collider col)
    {
        if(col.tag == "Enemy" || col.tag == "Player")
            dragables.Remove(col.gameObject);
    }
}
