using UnityEngine;
using System.Collections;

public class ConveyorBelt : MonoBehaviour, GroundSurface 
{
    public Vector2 SurfaceVelocity;
    public Vector2 TextureScrollVelocity;

	void Start () 
    {
	
	}
	
	void Update () 
    {
        //Update the material texture transform to make it look like a moving surface        
        GetComponent<Renderer>().material.mainTextureOffset += TextureScrollVelocity * Time.deltaTime;
	}

    public Vector3 GetSurfaceVelocity(Vector3 samplePt)
    {
        //Convert the 2D surface velocity into a 3D value and return it
        return new Vector3(SurfaceVelocity.x, 0.0f, SurfaceVelocity.y);
    }

    public Vector3 GetSurfaceAngularVelocity(Vector3 samplePt)
    {
        return Vector3.zero;
    }
}
