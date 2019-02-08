using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Vuforia;

/// <summary>
/// Used to provide functionality to a marker.
/// This script should only be attached to an Image Target.
/// </summary>
public class MarkerAction : MonoBehaviour
{

    public Vector3 triggerPosition; // The position that will trigger the At Position event
    public float xYTolerance, zTolerance; // How close the marker must be to (x, y, z) to trigger the At Position event

    public UnityEvent atPosition;

    private Vector3 tolerance;
    private bool begunTracking;

    // Start is called before the first frame update
    void Start()
    {
        tolerance = new Vector3(xYTolerance, xYTolerance, zTolerance);
        begunTracking = false;
    }

    private bool SameWithinTolerance(Vector3 v1, Vector3 v2, Vector3 tol)
    {
        bool sameX = (v1.x + tol.x >= v2.x) && (v1.x - tol.x <= v2.x);
        bool sameY = (v1.y + tol.y >= v2.y) && (v1.y - tol.y <= v2.y);
        bool sameZ = (v1.z + tol.z >= v2.z) && (v1.z - tol.z <= v2.z);

        return sameX && sameY && sameZ;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 curPos = this.gameObject.transform.position;

        if (curPos.z != 0.0f) // Make sure the marker is being tracked
        {
            if (!begunTracking)
            {
                Debug.Log("Started tracking at " + curPos);
                begunTracking = true;
            }

            if (SameWithinTolerance(curPos, triggerPosition, tolerance))
            {
                atPosition.Invoke();
            }
        }
    }
}
