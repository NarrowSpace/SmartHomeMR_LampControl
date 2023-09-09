using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    // Get the main menu
    public GameObject main_Menu;

    public GameObject camSelection;

    // Get the video of streaming camera menu
    public GameObject streamVid;

    private void Awake()
    {   
        streamVid.SetActive(false);
        camSelection.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CamSelection()
    {
        main_Menu.SetActive(false);
        camSelection.SetActive(true);
    }

    public void OpenStreamPage()
    {
        camSelection.SetActive(false);
        streamVid.SetActive(true);
    }
}
