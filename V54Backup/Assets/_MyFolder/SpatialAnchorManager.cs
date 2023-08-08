using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

public class SpatialAnchorManager : MonoBehaviour
{

    public Transform targetSpawnPoint;

    public LineRenderer _lineRender;

    private float maxPointerDistance;

    public GameObject bubbleOnEndPoint;

    private float thumbstickVal;

    // private bool isBubblePlacing = true;


    void Start()
    {
        _lineRender.positionCount = 2;
    }

    void Update()
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
            Instantiate(bubbleOnEndPoint, endPoint, Quaternion.identity);
        }

    }


}