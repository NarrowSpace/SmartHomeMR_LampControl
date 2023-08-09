using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HueLightManager : MonoBehaviour
{
    public enum LaserState
    {
        NotHitting,
        HittingBubble
    }

    public Transform targetSpawnPoint;
    private float maxPointerDistance;
    public GameObject bubbleOnEndPoint;
    public LineRenderer _lineRender;
    private RaycastHit hitInfo;
    private float thumbstickVal;

    private bool bubbleAllSet = false;

    public GameObject bubbleGuideInfo;
    public GameObject bubbleMenu; // Menu displays when the bubbles are set up

    private int bubbleCounter = 0;
    private TextMeshProUGUI bubbleNumber;
    private LaserState currentLaserState = LaserState.NotHitting;

    private GameObject lastHitBubble = null;

    private void Awake()
    {
        bubbleGuideInfo.SetActive(true);
        bubbleMenu.SetActive(false);
        bubbleNumber = bubbleMenu.GetComponentInChildren<TextMeshProUGUI>();
    }

    void Start()
    {
        _lineRender.positionCount = 2;
    }


    void Update()
    {
        SetUpBubble();
        SetupFinished();
    }


    void SetUpBubble()
    {
        // 1. Get the Thumbstick Input and Adjust the Laser Length:
        thumbstickVal = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y;
        float changeFactor = 0.05f;  // Control how much Input Value changes maxPointerDistance
        maxPointerDistance += thumbstickVal * changeFactor;
        float minDistance = 0.001f;
        maxPointerDistance = Mathf.Max(maxPointerDistance, minDistance);


        // 2. Setting Laser Position:
        Vector3 endPoint = targetSpawnPoint.position + targetSpawnPoint.forward * maxPointerDistance;
        _lineRender.SetPosition(0, targetSpawnPoint.position);
        _lineRender.SetPosition(1, endPoint);


        // 3. Bubble Positioning:
        bubbleOnEndPoint.transform.position = endPoint;

        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            // Create an instance of the Bubble at the endPoint position and with no additional rotation
            GameObject newBubble = Instantiate(bubbleOnEndPoint, endPoint, Quaternion.identity);

            // Increment the bubble counter
            bubbleCounter++;

            // Debug.Log(bubbleCounter);

            newBubble.name = "Bubble" + bubbleCounter;
        }
    }

    void SetupFinished()
    {
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            bubbleAllSet = true;
            bubbleOnEndPoint.SetActive(false);
        }


        if (bubbleAllSet)
        {
            ShowMenu();
            currentLaserState = LaserState.NotHitting;
            bubbleNumber.text = "Choose a Light to Turn ON/OFF";

            if (Physics.Raycast(targetSpawnPoint.position, targetSpawnPoint.forward, out hitInfo, maxPointerDistance))
            {
                if (hitInfo.collider.gameObject.CompareTag("Bubble"))
                {
                    currentLaserState = LaserState.HittingBubble;
                    _lineRender.SetPosition(1, hitInfo.point); // Set the ray's endpoint to where it hit the bubble
                    // Debug.Log("I HIT " + hitInfo.collider.gameObject.name);

                    // Extracts the 6th character which is the number from the name like "Bubble1", "Bubble2"...
                    bubbleNumber.text = " Press 'A' to Turn ON/OFF Light " + hitInfo.collider.gameObject.name.Substring(6);

                    // Turn On the Point Light:

                    // If there is bubble that were previously hit, turn off its light
                    if (lastHitBubble != null)
                    {
                        lastHitBubble.transform.GetChild(0).GetComponent<Light>().enabled = false;
                    }

                    // if hit, Update the lastHitBubble and turn on the light
                    lastHitBubble = hitInfo.collider.gameObject;
                    lastHitBubble.transform.GetChild(0).GetComponent<Light>().enabled = true;

                }
            }

            else if (lastHitBubble != null) // if the bubble didnot hit anything
            {
                // If laser is not hitting any bubble, turn off the light of the last hit bubble
                lastHitBubble.transform.GetChild(0).GetComponent<Light>().enabled = false;
                lastHitBubble = null; // Reset the last hit bubble reference
            }
        }
    }


    void ShowMenu()
    {
        bubbleGuideInfo.SetActive(false);
        bubbleMenu.SetActive(true);
    }


}
