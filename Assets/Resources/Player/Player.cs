﻿using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using XInputDotNetPure;

public class Player : MonoBehaviour {
    enum State { SWIM, DYING };
    public enum DeathCause { ALIVE, EATEN, SUCKED };
    private State state = State.SWIM;
    private Rigidbody _rigidbody;
    //private AudioLowPassFilter _audioLowPassFilter;
    //private bool dashDown = false;
    private PlayerSound playerSound;
    public Wiggle Wiggle;

    //[HideInInspector]
    public float hunger;
   // [HideInInspector]
    public float MaxHunger=0;
    public float DashHungerDrain = 10;
    [Header("Hunger slimming")]
    public float MaxHungryXScale = 0.5f;
    public float MaxHungryYScale = 1;
    public float MaxHungryZScale = 2;
    public float HungerStartSlim = 30;

    [Header("Movement Controls")]
    public float MaxSwimVelocity = 15;
    public float Acceleration = 4000;
    public float DashSpeed = 30;
    public float MinimumDashSpeed = 1000;
    public float DashDivisorIncrease = 0.5f;
    public float DashRecoveryTime = 2.0f;
    private float dashTimer = 0;
    private float dashDiminish = 1;
    private float dashDiminishTimer = 0;
    [Tooltip("In Seconds")]
    public float DashThreshold = 0.2f;
    private float dashCooldownTimer = 0;
    private float swimCooldownTimer = 0;
    [Tooltip("In Seconds")]
    public float DashCooldown = 0.4f;

    [Header("Look Controls")]
    
    //[Tooltip("whether to use flight control style for up and down rotations")]
    //public bool FlightControls = true;
    [HideInInspector]
    public float MinimumY = -60F;
    [HideInInspector]
    public float MaximumY = 60F;
    [Range(0.1f, 10.0f)]
    public float SensitivityX = 2.2f;
    [Range(0.1f, 10.0f)]
    public float SensitivityY = 2.2f;
    //[Header("Mouse Controls")]
    //public bool MouseLookEnabled = false;
    [Range(0.1f, 10.0f)]
    public float MouseSensitivity = 4;
    float rotationX = 0F;
    float rotationY = 0F;
    Quaternion originalRotation;

    public GameObject WorldObject;
    private float dieTime = float.PositiveInfinity;
    private DeathCause deathCause = DeathCause.ALIVE;

    private void Awake()
    {
        if (!FindObjectOfType<World>())
        {
            Instantiate(WorldObject);
        }
    }
    void Start ()
    {
        if (World.i.SpawnPoint != Vector3.zero)
        {
            transform.position = World.i.SpawnPoint;
        }
        //targetRotation = transform.rotation;
        originalRotation = transform.rotation;
        this._rigidbody = GetComponent<Rigidbody>();
        playerSound = GetComponent<PlayerSound>();
    }

    void Update ()
    {
        /*if (!spawnLoaded && spawnPoint != Vector3.zero)
        {
            transform.position = spawnPoint;
            spawnLoaded = true;
        }*/

        switch (state)
        {
            case State.DYING:
                //deathCause can be used for death transition.
                if(deathCause == DeathCause.ALIVE) Debug.Log(""); //Just to supress warning
                if(dieTime < Time.time) {
                    //To do: Make scene reload
                    Scene scene = SceneManager.GetActiveScene();
                    SceneManager.LoadScene(scene.name);
                    //transform.position = spawnPoint;
                    //state = State.SWIM;
                }
                break;
            case State.SWIM:

                float constAcc = 0.0f, boost = 0;
                GamePad.SetVibration(0, 0, 0);
                //Forward movement
                //Swims if possible. Increments dash timer.
                if (Mathf.Max(Input.GetAxis("Forward"), Input.GetAxis("ForwardAlt"))> 0)
                {
                    if (MaxSwimVelocity > _rigidbody.velocity.magnitude)
                        constAcc = 1;
                    if (_rigidbody.velocity.magnitude < 7)
                    {
                        boost = 900;
                        GamePad.SetVibration(0, 1, 1);
                    }
                    dashTimer += Time.deltaTime;
                }
                //Dashes if timer within threshhold.
                if (Input.GetButtonDown("Dash")/* && !dashDown && hunger>DashHungerDrain*/)
                {
                    if (dashTimer < DashThreshold && dashCooldownTimer <= 0)
                    {
                        //if (MaxHunger>0)
                        //    hunger -= DashHungerDrain;
                        boost = DashSpeed/dashDiminish;
                        if (boost < MinimumDashSpeed)
                            boost = 0;
                        dashCooldownTimer = DashCooldown;
                        swimCooldownTimer = DashCooldown * 2;
                        dashDiminishTimer = DashRecoveryTime;
                        dashDiminish += DashDivisorIncrease;
                    }
                    dashTimer = 0;
                }
                float accAdded = boost + (constAcc * Acceleration * Time.deltaTime);
                _rigidbody.AddForce(accAdded * transform.forward);
                Wiggle.Speed = Mathf.Max(5,_rigidbody.velocity.magnitude + accAdded * 0.01f);

                //Turning
                if (!World.i.MouseLook)
                {
                    rotationX += Input.GetAxis("Horizontal") * SensitivityX;
                    if (World.i.FlightControls)
                        rotationY += -1 * Input.GetAxis("Vertical") * SensitivityY;
                    else
                        rotationY += Input.GetAxis("Vertical") * SensitivityY;
                }
                else
                {
                    rotationX += Input.GetAxis("Mouse X") * MouseSensitivity;
                    if (World.i.FlightControls)
                        rotationY += -1 * Input.GetAxis("Mouse Y") * MouseSensitivity;
                    else
                        rotationY +=Input.GetAxis("Mouse Y") * MouseSensitivity;
                }

                rotationX = ClampAngle(rotationX, -360, 360);
                rotationY = ClampAngle(rotationY, MinimumY, MaximumY);
                if (rotationY > MaximumY)
                    rotationY = MaximumY;
                if (rotationY < MinimumY)
                    rotationY = MinimumY;
                Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
                Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);
                transform.rotation = originalRotation * xQuaternion * yQuaternion;

                break;
        }
        _rigidbody.velocity = transform.forward.normalized * _rigidbody.velocity.magnitude;
        playerSound.SetSpeed(_rigidbody.velocity.magnitude);
        CoolDown();

        if (Input.GetKey(KeyCode.Alpha1))
            SceneManager.LoadScene(0);
        if (Input.GetKey(KeyCode.Alpha2))
            SceneManager.LoadScene(1);
        if (Input.GetKey(KeyCode.Alpha3))
            SceneManager.LoadScene(2);
        if (Input.GetKey(KeyCode.Alpha4))
            SceneManager.LoadScene(3);
        if (Input.GetKeyDown(KeyCode.J))
            Camera.main.GetComponent<ScreenShake>().ShakeScreen(1, 0.5f);
        if (Input.GetKeyDown(KeyCode.K))
            Kill(DeathCause.EATEN);
        if (Input.GetKey(KeyCode.R))
        {
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.name);
        }
        if (MaxHunger > 0 && hunger <= HungerStartSlim)
        {
            transform.localScale = new Vector3(Mathf.Lerp(MaxHungryXScale, 1, hunger / HungerStartSlim), Mathf.Lerp(MaxHungryYScale, 1, hunger / HungerStartSlim), Mathf.Lerp(MaxHungryZScale, 1, hunger / HungerStartSlim));
        }
    }

    private void CoolDown()
    {
        dashCooldownTimer -= Time.deltaTime;
        if (dashCooldownTimer < 0)
            dashCooldownTimer = 0;
        swimCooldownTimer -= Time.deltaTime;
        if (swimCooldownTimer < 0)
            swimCooldownTimer = 0;
        if (dashDiminish > 1)
        {
            dashDiminishTimer -= Time.deltaTime;
            if (dashDiminishTimer < 0)
            {
                dashDiminish = 1;
            }
        }
    }

    public static float ClampAngle(float angle, float min, float max)
    {
        while(angle < -360)
            angle += 360;
        while(angle > 360)
            angle -= 360;
        return angle;
    }

    public void Kill(DeathCause deathCause)
    {
        this.deathCause = deathCause;
        state = State.DYING;
        dieTime = Time.time + .2f;
        World.i.Death(SceneManager.GetActiveScene().buildIndex);

    }
}
