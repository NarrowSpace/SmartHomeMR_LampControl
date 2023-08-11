using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using MiniJSON;
using System.Net;
using System.Linq;

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

    private int bubbleCounter = 0;
    private LaserState currentLaserState = LaserState.NotHitting;

    private GameObject lastHitBubble = null;

    public Camera centralEyeCam;
    public GameObject setupInfo;

    private List<TextMeshProUGUI> allTextMeshes  = new List<TextMeshProUGUI>();


    private void Awake()
    {
        // This will get all the TextMeshProUGUI components that are children of setupInfo.
        allTextMeshes = setupInfo.GetComponentsInChildren<TextMeshProUGUI>().ToList();
    }

    void Start()
    {
        _lineRender.positionCount = 2;
    }


    void Update()
    {
        UserMenu();

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

            newBubble.name = "Bubble" + bubbleCounter;
        }

        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            bubbleAllSet = true;
            bubbleOnEndPoint.SetActive(false);
        }

        // Check for hitting a bubble continuously after the bubbles are set.
        if (bubbleAllSet)
        {
            if (Physics.Raycast(targetSpawnPoint.position, targetSpawnPoint.forward, out hitInfo, maxPointerDistance))
            {
                if (hitInfo.collider.gameObject.CompareTag("Bubble"))
                {
                    currentLaserState = LaserState.HittingBubble;
                    _lineRender.SetPosition(1, hitInfo.point); // Set the ray's endpoint to where it hit the bubble
                    Debug.Log("I HIT " + hitInfo.collider.gameObject.name);

                    // If there is a bubble that was previously hit, turn off its light
                    if (lastHitBubble != null)
                    {
                        lastHitBubble.transform.GetChild(0).GetComponent<Light>().enabled = false;
                    }

                    // if hit, Update the lastHitBubble and turn on the light
                    lastHitBubble = hitInfo.collider.gameObject;
                    lastHitBubble.transform.GetChild(0).GetComponent<Light>().enabled = true;
                }
            }

            else if (lastHitBubble != null) // if the bubble did not hit anything
            {
                // If laser is not hitting any bubble, turn off the light of the last hit bubble
                lastHitBubble.transform.GetChild(0).GetComponent<Light>().enabled = false;
                lastHitBubble = null; // Reset the last hit bubble reference
            }

            // Example: change the text of the first TextMeshProUGUI component (if it exists)
            if (allTextMeshes.Count > 0)
            {
                allTextMeshes[0].text = "First Test!!";
            }
        }

    }

    void UserMenu()
    {
        // Get the position of the right controller and add a slight upward offset.
        Vector3 menuPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch) + Vector3.up * 0.02f;

        // Set the position of setupInfo canvas to the calculated position.
        setupInfo.transform.position = menuPosition;

        // Adjust the rotation of the setupInfo canvas to face the main camera.
        setupInfo.transform.rotation = Quaternion.LookRotation(menuPosition - centralEyeCam.transform.position);

    }

}