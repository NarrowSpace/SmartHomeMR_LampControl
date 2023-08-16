using Oculus.Interaction.Input;
using OculusSampleFramework;
using Oculus.Interaction.Surfaces;
using OVR.OpenVR;
using System;
using System.Collections;
using System.Collections.Generic;
// using UnityEditor.Experimental.GraphView;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SmartHomeManager : MonoBehaviour
{
    /// <summary>
    /// TO DO LIST:
    /// Need to optimize the brightness and hue adjustment
    /// The brightness & Hue button cannot be modified twice.
    /// </summary>

    // Menu 
    public GameObject mainMenu;
    public GameObject lightGuide;
    public GameObject rHandMenu;
    public GameObject allSetMenu;
    public GameObject bulbCtrlPanel;
    public TextMeshProUGUI bulbNameText;

    // Menu Fade OUT
    public CanvasGroup congradulationsCanvas;
    private float fadeSpeed = 0.01f;

    // Bulb Game Objects
    public GameObject bulbs;
    private int bulbCounter = 0;
    private int bulbNum;

    // Bools:
    private bool isMenuOn = true;
    private bool pickupCtrller = false;
    private bool bulbAllSet = false;
    private bool hapticTriggered = false;

    // Controller:
    public Transform centralEyeAnchor;
    public LineRenderer laserPointer;
    private RaycastHit hitInfo;
    OVRInput.Controller controller = OVRInput.Controller.RTouch;
    private float thumbVal;
    private float maxPointerDist;

    // Hand Interaction:
    public OVRHand rHand;
    public OVRHand lHand;

    public OVRSkeleton rhandSkeleton;

    private const float minDistance = 0.01f;
    private const float maxDistance = 0.1f;

    // Philips Hue Control：
    public HueLightsController hueLightsController;
    private int selectedBulb = -1;
    private int brightness;

    [SerializeField] Image circleBri;
    [SerializeField] TextMeshProUGUI txtBriVal;
    // [SerializeField] [Range(0, 1)] float briProgressBar = 0f;

    // Hue/Color:
    private int hueVal;
    private bool isInHueAdjustmentMode = false;
    private int previousHue = -1;
    private bool isHueLocked = false;
    private float stableHueTimer = 0f;
    [SerializeField] Image circleHue;
    [SerializeField] TextMeshProUGUI txtHueVal;
    // [SerializeField][Range(0, 1)] float hueProgressBar = 0f;

    // Lock Brightness
    private bool isInBrightnessAdjustmentMode = false;

    private float stableBrightnessTimer = 0f;
    private const float timeToLockBrightness = 3f; // adjust this value as needed
    private int previousBrightness = -1;
    private bool isBrightnessLocked = false;

    // Start is called before the first frame update
    void Start()
    {
        // Menu Display
        mainMenu.SetActive(isMenuOn);
        lightGuide.SetActive(!isMenuOn);
        rHandMenu.SetActive(!isMenuOn);
        allSetMenu.SetActive(!isMenuOn);
        bulbCtrlPanel.SetActive(!isMenuOn);

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

    /// HAND SELECTION:
    private void GetHandData()
    {
        if (rHand.IsTracked)
        {
            // Get the position of the thumb and index tip
            Vector3 thumbTipPos = rhandSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_ThumbTip].Transform.position;
            Vector3 indexTipPos = rhandSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_IndexTip].Transform.position;

            //Check if the fingers are pinching
            if (rHand.GetFingerIsPinching(OVRHand.HandFinger.Thumb) && rHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
            {
                SelectBulb(indexTipPos);
            }

            // Update the brightness and UI if the brightness has changed
            if (isInBrightnessAdjustmentMode && !isBrightnessLocked)
            {
                float rawDistance = Vector3.Distance(thumbTipPos, indexTipPos);
                float normalizedDistance = NormalizeDistance(rawDistance, minDistance, maxDistance);
                int currentBrightness = (int)Mathf.Round(normalizedDistance * 253 + 1);

                // This conditional checks if the brightness level(based on finger distance) has changed since the last frame.
                // For small brightness changes (i.e., within the 3 unit range), this will continue to increase the stableBrightnessTimer.
                if (Mathf.Abs(currentBrightness - previousBrightness) > 5)
                {
                    // Set the brightness value and update display/UI
                    SetBrightness(currentBrightness);
                    circleBri.fillAmount = normalizedDistance;
                    txtBriVal.text = currentBrightness.ToString();
                    Debug.Log("Current brightness is: " + currentBrightness);

                    // Reset the timer when brightness changes
                    stableBrightnessTimer = 0f;
                }

                // if the brightness level has not changed, increase the timer
                else
                {
                    stableBrightnessTimer += Time.deltaTime;
                    // If this stable duration surpasses a defined threshold (timeToLockBrightness), the brightness is locked:
                    if (stableBrightnessTimer >= timeToLockBrightness)
                    {
                        isBrightnessLocked = true;
                        Debug.Log("Brightness locked at: " + currentBrightness);
                    }
                }

                // Remembering Previous Brightness:
                previousBrightness = currentBrightness;
            }

            // Update the Hue color 
            if (isInHueAdjustmentMode && !isHueLocked)
            {
                float rawDistance = Vector3.Distance(thumbTipPos, indexTipPos);
                float normalizedDistance = NormalizeDistance(rawDistance, minDistance, maxDistance);
                int currentHue = (int)Mathf.Round(normalizedDistance * 65534);

                Debug.Log("currentHue: " + currentHue);

                if (Mathf.Abs(currentHue - previousHue) > 600) // 100 can be adjusted as required
                {
                    SetHue(currentHue);
                    circleHue.fillAmount = normalizedDistance;
                    txtHueVal.text = currentHue.ToString();
                    Debug.Log("Current hue is: " + currentHue);

                    // Reset the timer when hue changes
                    stableHueTimer = 0f;
                }
                else
                {
                    stableHueTimer += Time.deltaTime;
                    if (stableHueTimer >= timeToLockBrightness)
                    {
                        isHueLocked = true;
                        Debug.Log("Hue locked at: " + currentHue);
                    }
                }
                previousHue = currentHue;
            }
        }
    }

    private void SelectBulb(Vector3 indexTipPos)
    {
        Ray ray = new Ray(indexTipPos, rHand.PointerPose.forward);
        RaycastHit hit;
        const float maxInteractionDistance = 5f;

        if (Physics.Raycast(ray, out hit, maxInteractionDistance))
        {
            if (hit.collider.CompareTag("Bulbs"))
            {
                int bulbBeingInteractedWith = GetBulbNumber(hit.collider.gameObject);

                if (bulbBeingInteractedWith != selectedBulb)
                {
                    selectedBulb = bulbBeingInteractedWith;
                    bulbNameText.text = hit.collider.name;
                    Debug.Log("Bulb Number is: " + selectedBulb);
                }
            }
        }
    }

    public void OnButtonPress()
    {
        if (selectedBulb != -1)
        {
            hueLightsController.SetLightState(selectedBulb, true);
        }
    }
    public void OffButtonPress()
    {
        if (selectedBulb != -1)
        {
            hueLightsController.SetLightState(selectedBulb, false);
        }
    }

    public void BrgButtonPress()
    {
        isInBrightnessAdjustmentMode = !isInBrightnessAdjustmentMode;  // Toggle adjustment mode
        // Debug.Log("ENTER: Brightness Button Pressed");

        if (isInBrightnessAdjustmentMode)
        {
            isInHueAdjustmentMode = false;

            // reset the lock when re-entering mode
            isBrightnessLocked = false; 
            stableBrightnessTimer = 0f;
        }

        Debug.Log(isInBrightnessAdjustmentMode ? "ENTER: Brightness Mode" : "EXIT: Brightness Mode");
    }
    private void SetBrightness(int brightnessValue)
    {
        if(selectedBulb != -1)
        {
            hueLightsController.SetLightBrightness(selectedBulb, brightnessValue);
        }
    }

    public void HueButtonPress()
    {
        isInHueAdjustmentMode = !isInHueAdjustmentMode;  // Toggle adjustment mode

        if (isInHueAdjustmentMode)
        {
            isInBrightnessAdjustmentMode = false;

            // reset the lock when re-entering mode
            isHueLocked = false; 
            stableHueTimer = 0f;
        }
        Debug.Log(isInHueAdjustmentMode ? "ENTER: Hue Mode" : "EXIT: Hue Mode");
    }

    private void SetHue(int hueValue)
    {
        if (selectedBulb != -1)
        {
            hueLightsController.SetLightHue(selectedBulb, hueValue);
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
            newBulb.name = "BULB" + bulbCounter;
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
            bulbCtrlPanel.SetActive(isMenuOn);

            // The Codes Below is the function for controllers to interact with the light bulbs:
            if (Physics.Raycast(pos0, controllerRot * Vector3.forward, out hitInfo, maxPointerDist))
            {
                if (hitInfo.collider.gameObject.tag == "Bulbs")
                {
                    laserPointer.SetPosition(1, hitInfo.point);

                    // bulbNum = GetBulbNumber(hitInfo.collider.gameObject);

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

    // This function makes sure that the menu is always facing the user
    private void MenuDisplay(GameObject menuObject)
    {
        Vector3 menuPosition = centralEyeAnchor.transform.position + centralEyeAnchor.transform.forward * 0.4f + Vector3.up * -0.13f;
        menuObject.transform.position = menuPosition;
        menuObject.transform.rotation = Quaternion.LookRotation(menuPosition - centralEyeAnchor.transform.position);
    }

    // This function is controlled by the poke button
    public void LightMenuOn()
    {
        mainMenu.SetActive(!isMenuOn);
        lightGuide.SetActive(isMenuOn);
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
        if (bulbName.StartsWith("BULB"))
        {
            string numberString = bulbName.Replace("BULB", "");

            if (int.TryParse(numberString, out int bulbNumber))
            {
                return bulbNumber;
            }
        }
        return -1; // Return an invalid number if parsing fails
    }

}
