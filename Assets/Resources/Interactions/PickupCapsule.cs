﻿using UnityEngine;
using System.Collections;



public class PickupCapsule : MonoBehaviour {
    public Transform Player;
    public int rotateX = 30;
    public int rotateY = 20;

    void Update () {
        transform.Rotate(rotateX * Time.deltaTime, rotateY * Time.deltaTime, 0);
    }

    private void OnCollisionEnter(Collision collision)
    {
        var script = collision.gameObject.GetComponent<Player>();
        if (script != null)
        {
            DestroyObject(this.gameObject);
            script.hunger = script.MaxHunger;
        }
    }
}
