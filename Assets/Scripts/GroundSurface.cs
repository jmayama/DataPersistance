using UnityEngine;
using System.Collections;

//This interface allows other objects to obtain information from ground surfaces
interface GroundSurface
{
    Vector3 GetSurfaceVelocity(Vector3 samplePt);

    Vector3 GetSurfaceAngularVelocity(Vector3 samplePt);
}
