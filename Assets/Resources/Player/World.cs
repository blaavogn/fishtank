﻿using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class World : MonoBehaviour {
    public static World i;
    public GameObject statusScreen;
    [HideInInspector]
    public Vector3 SpawnPoint = Vector3.zero;
    private GameObject spawnObject;
    [Header("Tracking")]
    public int AmountOfLevels = 3;
    [Header("Controls")]
    public bool FlightControls;
    public bool MouseLook;
    private string textToShow;
    private Color guiColour = Color.white;
    private int totalDeaths = 0;
    private int deaths = 0;
    private int pills = 0;
    private float timeTaken = 0;

    [HideInInspector]
    public float BeginTime = 0;
    [HideInInspector]
    private List<string> pillsTaken;
    [HideInInspector]
    public List<string> SavedPills;

    public int showDeaths = 0;
    public int showTotalDeaths = 0;

    private void Awake()
    {
        BeginTime = Time.time;
        pillsTaken = new List<string>();
        SavedPills = new List<string>();
        if (i == null)
        {
            i = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(this);
    }
    void InitPrefs()
    {
        FlightControls = ToBool(PlayerPrefs.GetInt("Flight Controls", 0));
        MouseLook = ToBool(PlayerPrefs.GetInt("MouseLook", 1));
    }
	// Use this for initialization
	void Start () {
        InitPrefs();
	}
	
	// Update is called once per frame
	void Update ()
	{
	    showDeaths = deaths;
	    showTotalDeaths = totalDeaths;
        if (guiColour.a > 0)
        {
            guiColour.a -= 0.005f;
        }
        if (Input.GetKeyDown(KeyCode.F))
            SetFlightControls();
        if (Input.GetMouseButtonDown(1))
            SetMouseLook();
        if (Input.GetKey(KeyCode.Alpha0))
        {
            ResetStats();
            GotoLevel(0);
        }
        if (Input.GetKey(KeyCode.Alpha1))
        {
            ResetStats();
            GotoLevel(1);
        }
        if (Input.GetKey(KeyCode.Alpha2))
        {
            ResetStats();
            GotoLevel(2);
        }
        if (Input.GetKey(KeyCode.Alpha3))
        {
            ResetStats();
            GotoLevel(3);
        }
        if (Input.GetKey(KeyCode.R))
            RestartLevel(false);
        if (Input.GetKeyDown(KeyCode.V))
            WinLevel();
    }

    public void SetFlightControls()
    {
        FlightControls = !FlightControls;
        PlayerPrefs.SetInt("Flight Controls", ToInt(FlightControls));

        ShowGuiText(FlightControls ? "Flight Controls enabled!" : "Flight Controls disabled!");
    }
    public void SetMouseLook()
    {
        MouseLook = !MouseLook;
        PlayerPrefs.SetInt("MouseLook", ToInt(MouseLook));

        ShowGuiText(MouseLook ? "Mouselook enabled!" : "Mouselook disabled!");
    }


    bool ToBool(int input)
    {
        if (input <= 0)
            return false;
        return true;
    }
    int ToInt(bool input)
    {
        if (input)
            return 1;
        return 0;
    }

    void ShowGuiText(string text)
    {
        textToShow = text;
        guiColour.a = 1;
    }
    private void OnGUI()
    {
        GUI.color = guiColour;
        GUI.Label(new Rect(0.02f * Screen.width, 0.02f * Screen.height, 0.25f * Screen.width, 0.05f * Screen.height), textToShow);
    }
    private void OnApplicationQuit()
    {
        //PlayerPrefs.SetInt("Flight Controls", ToInt(FlightControls));
        //PlayerPrefs.SetInt("MouseLook", ToInt(MouseLook));
    }
    public int Death()
    {
        var level = SceneManager.GetActiveScene().buildIndex;
        if (level > 0)
            level--;
        deaths++;
        totalDeaths++;
        return deaths;
    }
    public int EatPill(string instanceName)
    {
        pillsTaken.Add(instanceName);
        var level = SceneManager.GetActiveScene().buildIndex;
        if (level > 0)
            level--;
        pills += 1;
        return pills;
    }
    public void WinLevel()
    {
        var level = SceneManager.GetActiveScene().buildIndex;
        if (level > 0)
            level--;
        timeTaken = Time.time- BeginTime;
        var p=Instantiate(statusScreen);
        var totalPills=pills+GameObject.FindGameObjectsWithTag("PointObject").Length;
        p.GetComponent<StatusScreen>().WinScreen(deaths, pills, totalPills, timeTaken, totalDeaths);
        GameObject.FindGameObjectWithTag("Player").GetComponent<Player>().Freeze();
        //GotoLevel(SceneManager.GetActiveScene().buildIndex + 1);
    }
    public void GotoLevel(int level)
    {
        SceneManager.LoadScene(level);
        Debug.Log("going to level: " + level);
    }
    public void RestartLevel(bool useCheckpoint)
    {
        if (!useCheckpoint)
            ResetStats();
        else
            pills = SavedPills.Count;
        GotoLevel(SceneManager.GetActiveScene().buildIndex);
    }
    public void NextLevel()
    {
        deaths = 0;
        ResetStats();
        int levelToGoTo = SceneManager.GetActiveScene().buildIndex + 1;
        if (SceneManager.GetActiveScene().buildIndex >= AmountOfLevels)
            levelToGoTo = 0;
        GotoLevel(levelToGoTo);
    }
    public void CheckPoint(GameObject gameO)
    {
        if (SpawnPoint == gameO.transform.position || spawnObject == gameO) return;
        spawnObject = gameO;
        SpawnPoint = gameO.transform.position;
        foreach (var t in pillsTaken)
        {
            if (!SavedPills.Contains(t))
                SavedPills.Add(t);
        }
    }

    public void NewGame()
    {
        ResetStats();
        totalDeaths = 0;
        deaths = 0;
    }
    public void ResetStats()
    {
        pills = 0;
        timeTaken = 0;
        SpawnPoint = Vector3.zero;
        pillsTaken.Clear();
        SavedPills.Clear();
        BeginTime = Time.time;
    }
}
