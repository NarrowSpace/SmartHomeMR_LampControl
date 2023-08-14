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
    /// 制作手指来控制philips Hue灯泡的亮度
    /// </summary>

    // Menu 
    public GameObject mainMenu;
    public GameObject lightGuide;
    public GameObject rHandMenu;
    public GameObject allSetMenu;

    // Menu Fade OUT
    public CanvasGroup congradulationsCanvas;
    private float fadeSpeed = 0.01f;

    // Bulb Game Objects
    public GameObject bulbs;
    private List<GameObject> allBulbs = new List<GameObject>();
    private int bulbCounter = 0;
    private int bulbNum;

    // Bools
    private bool isMenuOn = true;
    private bool pickupCtrller = false;
    private bool bulbAllSet = false;
    private bool hapticTriggered = false;

    // Controller
    public Transform centralEyeAnchor;
    public LineRenderer laserPointer;
    private RaycastHit hitInfo;
    OVRInput.Controller controller = OVRInput.Controller.RTouch;
    private float thumbVal;
    private float maxPointerDist;

    // Hand Interaction
    public OVRHand rHand;
    public OVRHand lHand;

    public OVRSkeleton rhandSkeleton;

    private const float minDistance = 0.01f;
    private const float maxDistance = 0.1f;

    // Philips Hue Control
    public HueLightsController hueLightsController;

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

    // This function makes sure that the menu is always facing the user
    private void MenuDisplay(GameObject menuObject)
    {
        Vector3 menuPosition = centralEyeAnchor.transform.position + centralEyeAnchor.transform.forward * 0.45f + Vector3.up * -0.13f;
        menuObject.transform.position = menuPosition;
        menuObject.transform.rotation = Quaternion.LookRotation(menuPosition - centralEyeAnchor.transform.position);
    }


    // This function is controlled by the poke button
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
    

    // HAND SELECTION:
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

           // Debug.Log("Distance between thumb and index is: " + normalizedDistance);

            if (rHand.GetFingerIsPinching(OVRHand.HandFinger.Thumb) && rHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
            {
                Debug.Log("I can see you are pinching your fingers!!");
                Vector3 pinchPoint = (thumbTipPos + indexTipPos) / 2;
                // Vector3 rayDirection = rHand.transform.forward;
                Vector3 rayDirection = (thumbTipPos - indexTipPos).normalized; // Pointing from index to thumb
                HandlePinchSelection(pinchPoint,rayDirection);
            }
        }
    }

    private void HandlePinchSelection(Vector3 pinchPoint,Vector3 handDirection)
    {
        RaycastHit pinchHit;
        float pinchRayLength = 2.0f;

        // Visualize the ray in the Unity Scene view (remove this in the production build)
        Debug.DrawRay(pinchPoint, handDirection * pinchRayLength, Color.red, 2.0f);

        if (Physics.Raycast(pinchPoint, handDirection, out pinchHit, pinchRayLength))
        {
            if(pinchHit.collider.gameObject.tag == "Bulbs")
            {
                GameObject selectedBulb = pinchHit.collider.gameObject;

                bulbNum = GetBulbNumber(pinchHit.collider.gameObject);
                Debug.Log("Retrieved bulb number: " + bulbNum);

                if (bulbNum!=-1)
                {
                    Debug.Log("Bulb" +bulbNum + selectedBulb.name + "is Selected");
                }

            }
        }
    }

    // CONTROLLER SELECTION:
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
            bulbNum = GetBulbNumber(newBulb);

            // Debug.Log("*** BULB NUMBER ***: " + bulbNum);

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
            // Debug.Log("Entered bulbAllSet condition");
            rHandMenu.SetActive(!isMenuOn);
            allSetMenu.SetActive(isMenuOn);
            StartCoroutine(AllSetMenuFadeOut());

            // The Codes Below is the function for controllers to interact with the light bulbs:
            if (Physics.Raycast(pos0, controllerRot * Vector3.forward, out hitInfo, maxPointerDist))
            {
                if (hitInfo.collider.gameObject.tag == "Bulbs")
                {
                    laserPointer.SetPosition(1, hitInfo.point);

                    bulbNum = GetBulbNumber(hitInfo.collider.gameObject);

                    if (OVRInput.GetDown(OVRInput.Button.One))
                    {
                        // PHILIPS HUE LIGHTS:
                        if (bulbNum != -1)
                        {
                            hueLightsController.SetLightState(bulbNum, true);
                            StartCoroutine(TriggerHapticFeedback());
                        }
                    }
                    if (!hapticTriggered)
                    {
                        StartCoroutine(TriggerHapticFeedback());
                        hapticTriggered = true; // Set to true once haptic feedback is triggered
                    }
                    Debug.Log("I Hit Bulb" + hitInfo.collider.gameObject.name + "Bulb Number" + bulbNum);
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

    // NORMALIZE THE DISTANCE BETWEEN THUMB TIP AND INDEX TIP OF THE RIGHT HAND:
    private float NormalizeDistance(float actualDist, float minDist, float maxDist)
    {
        actualDist = Mathf.Clamp(actualDist, minDist, maxDist);
        return (actualDist - minDist) / (maxDist - minDist);
    }

    // GET THE INSTANTIATED BULB NUMBER:
    private int GetBulbNumber(GameObject bulb)
    {
        string bulbName = bulb.name;
        if (bulbName.StartsWith("Bulb"))
        {
            string numberString = bulbName.Replace("Bulb", "");

            if (int.TryParse(numberString, out int bulbNumber))
            {
                return bulbNumber;
            }
        }
        return -1; // Return an invalid number if parsing fails
    }
}
