﻿using UnityEngine;
using System.Collections;
using UnityStandardAssets.ImageEffects;

public class Fog : MonoBehaviour {
    public enum State { NORMAL, FLASH_INC, FLASH_DEC, DISSIPATE};
    public State state;
    public float MaxFogLevel = 0.008f;
    public Color FogColor;
    private float fogTimer = 0;
    
    void Start () {
        RenderSettings.fogDensity = 0.0F;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fog = true;
        RenderSettings.fogDensity = MaxFogLevel;
    }
    
    void Update ()
    {
        switch (state)
        {
            case State.FLASH_INC:
                RenderSettings.fogDensity = RenderSettings.fogDensity - Time.deltaTime * 0.04f;
                if (RenderSettings.fogDensity <= 0.006)
                    state = State.FLASH_DEC;
                break;
            case State.FLASH_DEC:
                RenderSettings.fogDensity = RenderSettings.fogDensity + Time.deltaTime * 0.06f;
                if (RenderSettings.fogDensity < MaxFogLevel)
                {
                    RenderSettings.fogDensity = MaxFogLevel;
                    state = State.NORMAL;
                }
                break;
            case State.DISSIPATE:
                RenderSettings.fogDensity = Mathf.Lerp(MaxFogLevel, 0, fogTimer);
                fogTimer += Time.deltaTime / 10;
                break;
        }
        if (Input.GetKeyDown(KeyCode.X))
            state = State.FLASH_INC;
    }

    public void Flash()
    {
        state = State.FLASH_INC;
    }
}
