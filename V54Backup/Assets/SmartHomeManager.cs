using OVR.OpenVR;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartHomeManager : MonoBehaviour
{
    public GameObject settingMenu;
    public GameObject LightMenu;
    public GameObject lightInfo;
    public Transform centralEyeAnchor;

    private bool isMenuOn = true;
    private bool pickupCtrller = false;

    public LineRenderer laserPointer;
    OVRInput.Controller controller = OVRInput.Controller.RTouch;
    private float thumbVal;
    private float maxPointerDist;

    // Start is called before the first frame update
    void Start()
    {
        settingMenu.SetActive(isMenuOn);
        LightMenu.SetActive(!isMenuOn);
        laserPointer.positionCount = 2;
    }

    // Update is called once per frame
    void Update()
    {
        // Apply MenuDisplay to all objects with the "Menu" tag
        GameObject[] menuObjects = GameObject.FindGameObjectsWithTag("Menu");

        foreach (GameObject menu in menuObjects)
        {
            MenuDisplay(menu);
        }

        CheckControllerStates();
    }

    private void MenuDisplay(GameObject menuObject)
    {
        Vector3 menuPosition = centralEyeAnchor.transform.position + centralEyeAnchor.transform.forward * 0.45f + Vector3.up * -0.13f;
        menuObject.transform.position = menuPosition;
        menuObject.transform.rotation = Quaternion.LookRotation(menuPosition - centralEyeAnchor.transform.position);
    }

    public void LightMenuOn()
    {
        settingMenu.SetActive(!isMenuOn);
        LightMenu.SetActive(isMenuOn);
    }

    private void CheckControllerStates()
    {
        if (OVRInput.IsControllerConnected(OVRInput.Controller.RTouch) && OVRInput.IsControllerConnected(OVRInput.Controller.LTouch))
        {
            pickupCtrller = true;
            Debug.Log("Both controllers are connected");
            lightInfo.SetActive(false);
            RTouchLaserPointer();

        }
        else
        {
            pickupCtrller = false;
            Debug.Log("No Controllers are connected");
        }
    }

    /// <summary>
    /// Laser Rendering is not finsihed....
    /// </summary>


    private void RTouchLaserPointer()
    {


        Vector3 controllerPos = OVRInput.GetLocalControllerPosition(controller);
        Quaternion controllerRot = OVRInput.GetLocalControllerRotation(controller);

        Vector3 pos0 = controllerPos + controllerRot * (Vector3.forward * 0.05f);

        thumbVal = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;
        float changeFactor = 0.05f;
        maxPointerDist += thumbVal * changeFactor;
        float minDistance = 0.001f;
        maxPointerDist = Mathf.Max(maxPointerDist, minDistance);

        // 2. Setting Laser Position:
        Vector3 endPoint = controllerPos + controllerRot * (Vector3.forward * maxPointerDist);
        laserPointer.SetPosition(0, pos0);
        laserPointer.SetPosition(1, endPoint);

        // 3. Lamp Positioning:



    }
}
