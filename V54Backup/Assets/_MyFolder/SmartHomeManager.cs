using Oculus.Interaction.Input;
using OculusSampleFramework;
using OVR.OpenVR;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


public class SmartHomeManager : MonoBehaviour
{
    /// <summary>
    /// To Do List:
    /// Pinch to grab the light bulb
    /// 
    /// Pinch and select
    /// Add Hue Lights
    /// </summary>

    // Menu 
    public GameObject mainMenu;
    public GameObject lightGuide;
    public GameObject rHandMenu;
    public GameObject allSetMenu;

    public CanvasGroup congradulationsCanvas;
    private float fadeSpeed = 0.01f;

    public GameObject bulbs;
    private int bulbCounter = 0;

    public Transform centralEyeAnchor;
    public LineRenderer laserPointer;
    private RaycastHit hitInfo;
    OVRInput.Controller controller = OVRInput.Controller.RTouch;
    private float thumbVal;
    private float maxPointerDist;

    private bool isMenuOn = true;
    private bool pickupCtrller = false;
    private bool bulbAllSet = false;
    private bool hapticTriggered = false;

    // Hand Interaction
    public OVRHand rHand;
    public OVRHand lHand;

    public OVRSkeleton rhandSkeleton;

    private const float minDistance = 0.01f;
    private const float maxDistance = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        // Menu Display
        mainMenu.SetActive(isMenuOn);
        lightGuide.SetActive(!isMenuOn);
        rHandMenu.SetActive(!isMenuOn);
        allSetMenu.SetActive(!isMenuOn);

        bulbs.SetActive(false);

        laserPointer.enabled = false;
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
        GetHandData();
    }

    private void GetHandData()
    {
        // The codes below is the function for hands to interact with the bulbs
        if (rHand.IsTracked)
        {
            Debug.Log("Right Hand is Tracked!");

            Vector3 thumbTipPos = rhandSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_ThumbTip].Transform.position;
            Vector3 indexTipPos = rhandSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_IndexTip].Transform.position;

            float rawDistance = Vector3.Distance(thumbTipPos, indexTipPos);

            // Normalize the distance between 0 and 1
            float normalizedDistance = NormalizeDistance(rawDistance, minDistance, maxDistance);

            Debug.Log("Distance between thumb and index is: " + normalizedDistance);

            if (rHand.GetFingerIsPinching(OVRHand.HandFinger.Thumb) && rHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
            {
                Debug.Log("I can see you are pinching your fingers!!");
            }
        }
    }


    /// <summary>
    /// Make sure that the menu is always facing the user
    /// </summary>
    /// <param name="menuObject"></param>
    private void MenuDisplay(GameObject menuObject)
    {
        Vector3 menuPosition = centralEyeAnchor.transform.position + centralEyeAnchor.transform.forward * 0.45f + Vector3.up * -0.13f;
        menuObject.transform.position = menuPosition;
        menuObject.transform.rotation = Quaternion.LookRotation(menuPosition - centralEyeAnchor.transform.position);
    }

    /// <summary>
    /// This function is controlled by the button
    /// </summary>
    public void LightMenuOn()
    {
        mainMenu.SetActive(!isMenuOn);
        lightGuide.SetActive(isMenuOn);
    }

    private void CheckControllerStates()
    {
        if (OVRInput.IsControllerConnected(OVRInput.Controller.RTouch) && OVRInput.IsControllerConnected(OVRInput.Controller.LTouch))
        {
            pickupCtrller = true;
            lightGuide.SetActive(!isMenuOn);
            rHandMenu.SetActive(isMenuOn);
            laserPointer.enabled = true;
           // Debug.Log("Both controllers are connected");
            RTouchLaserPointer();
        }
        else
        {
            pickupCtrller = false;
            bulbs.SetActive(false);
            laserPointer.enabled = false;
           // Debug.Log("No Controllers are connected");
        }
    }

    private void RTouchLaserPointer()
    {
        if (!bulbAllSet)
        {
            bulbs.SetActive(true);
        }

        Vector3 controllerPos = OVRInput.GetLocalControllerPosition(controller);
        Quaternion controllerRot = OVRInput.GetLocalControllerRotation(controller);
        Vector3 pos0 = controllerPos + controllerRot * (Vector3.forward * 0.05f);
        Vector3 rhandMenuPos = controllerPos + controllerRot * Vector3.forward * 0.02f + Vector3.up * 0.01f;
        rHandMenu.transform.position = rhandMenuPos;

        thumbVal = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;
        float changeFactor = 0.05f;
        maxPointerDist += thumbVal * changeFactor;
        maxPointerDist = Mathf.Clamp(maxPointerDist, 0.05f, 20f);

        // 2. Setting Laser Position:
        Vector3 endPoint = controllerPos + controllerRot * (Vector3.forward * maxPointerDist);

        laserPointer.SetPosition(0, pos0);
        laserPointer.SetPosition(1, endPoint);

        // 3. Bubble Positioning:
        bulbs.transform.position = endPoint;

        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            GameObject newBulb = Instantiate(bulbs, endPoint, Quaternion.identity);
            bulbCounter++;
            newBulb.name = "Bulb" + bulbCounter;
            // Add Right hand controller Haptic Feedback
            StartCoroutine(TriggerHapticFeedback());
        }

        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            bulbAllSet = true;
            bulbs.SetActive(false);
            StartCoroutine(TriggerHapticFeedback());
        }

        if (bulbAllSet)
        {
            Debug.Log("Entered bulbAllSet condition");

            rHandMenu.SetActive(!isMenuOn);
            allSetMenu.SetActive(isMenuOn);
            StartCoroutine(AllSetMenuFadeOut());

            // The Codes Below is the function for controllers to interact with the light bulbs

            if (Physics.Raycast(pos0, controllerRot * Vector3.forward, out hitInfo, maxPointerDist))
            {
                if (hitInfo.collider.gameObject.tag == "Bulbs")
                {
                    laserPointer.SetPosition(1, hitInfo.point);
                    if (!hapticTriggered)
                    {
                        StartCoroutine(TriggerHapticFeedback());
                        hapticTriggered = true; // Set to true once haptic feedback is triggered
                    }
                    Debug.Log("I Hit Bulb" + hitInfo.collider.gameObject.name);
                }
                else
                {
                    hapticTriggered = false; // Reset the flag if the ray is no longer hitting a "Bulbs" tagged object
                }
            }

            else
            {
                hapticTriggered = false; // Reset the flag if the ray is not hitting anything
            }


           
        }
    }

    private IEnumerator TriggerHapticFeedback()
    {
        OVRInput.SetControllerVibration(0.5f, 0.5f, controller);
        yield return new WaitForSeconds(0.1f);
        OVRInput.SetControllerVibration(0, 0, controller);
    }

    private IEnumerator AllSetMenuFadeOut()
    {
        // Pause the coroutine for few seconds 
        yield return new WaitForSeconds(5f);

        while (congradulationsCanvas.alpha > 0f)
        {
            congradulationsCanvas.alpha -= Time.deltaTime * fadeSpeed;
            yield return new WaitForEndOfFrame();
        }
    }


    /// <summary>
    /// Normalize the index and thumb distance between 0 and 1
    /// </summary>
    /// <param name="actualDist"></param>
    /// <param name="minDist"></param>
    /// <param name="maxDist"></param>
    /// <returns></returns>
    private float NormalizeDistance(float actualDist, float minDist, float maxDist)
    {
        actualDist = Mathf.Clamp(actualDist, minDist, maxDist);
        return (actualDist - minDist) / (maxDist - minDist);
    }

}
