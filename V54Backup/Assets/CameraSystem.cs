using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.WebCam;

public class CameraSystem : MonoBehaviour
{
    [SerializeField] private RawImage img = default;
    [SerializeField] private int cameraIndex = 2;

    private WebCamTexture webCam;


    private void Awake()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        Debug.Log("Number of webcams detected: " + devices.Length);
        
        // loop through each on & get its name
        for(int i = 0; i < devices.Length; i++)
        {
            Debug.Log("Webcam available: " + (i+1) + ":" + devices[i].name);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Check is there are any webcams
        if(WebCamTexture.devices.Length == 0)
        {
            Debug.Log("No webcams detected");
            return;
        }

        webCam = new WebCamTexture();
        if(!webCam.isPlaying)
        {
            webCam.Play();
            img.texture = webCam;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
