using UnityEngine;
using System.Collections;

public class SpinningPlatform : MonoBehaviour, GroundSurface 
{
    public float RotateDegPerSec = 30.0f;

	void Start () 
    {
	
	}
	
	void FixedUpdate() 
    {
        //Update the platform's rotation
        float rotateThisFrame = RotateDegPerSec * Time.deltaTime;

        Quaternion newRotation = transform.rotation * Quaternion.AngleAxis(rotateThisFrame, Vector3.up);

        //Setting the rotation using the "MoveRotation" function.  This will make it interract with 
        //other physics objects better, since the motion will be taken into account instead of just
        //teleporting
        GetComponent<Rigidbody>().MoveRotation(newRotation);
	}

    //Calculating the linear velocity of a rotating surface at the sample point.
    //
    //      The angular speed will be equal to:
    //             
    //               v = a * r
    //      
    //      Where:
    //            v: linear speed
    //            a: angular speed in radians
    //            r: distance from rotational axis
    //      
    //      The direction of the linear velocity will be equal to the tangent of the rotation
    //
    public Vector3 GetSurfaceVelocity(Vector3 samplePt)
    {
        //Calculating the distance from the center
        Vector3 centerDiff = samplePt - transform.position;

        float distFromCenter = centerDiff.magnitude;

        if (distFromCenter <= 0.0f)
        {
            return Vector3.zero;
        }

        //Converting the angular speed to radians
        float rotateRadsPerSec = RotateDegPerSec * Mathf.Deg2Rad;

        //Calculating linear speed
        float linearSpeed = rotateRadsPerSec * distFromCenter;

        //Calculating the tangent to the rotation 
        Vector3 moveDir = Vector3.Cross(Vector3.up, centerDiff / distFromCenter);

        //Multiplay the speed and direction to get the velocity
        return moveDir * linearSpeed;
    }

    public Vector3 GetSurfaceAngularVelocity(Vector3 samplePt)
    {
        return new Vector3(0.0f, RotateDegPerSec, 0.0f);
    }
}
