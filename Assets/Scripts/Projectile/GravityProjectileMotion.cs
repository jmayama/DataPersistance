using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;

public class GravityProjectileMotion : MonoBehaviour, ProjectileMotionBehavior 
{
    public Vector3 GravityAccel = new Vector3(0.0f, -50, 0.0f);

	void Start () 
    {
	
	}

    public void UpdateMotion(Projectile projectile)
    {
        projectile.Velocity += GravityAccel * Time.deltaTime;

        projectile.transform.position += projectile.Velocity * Time.deltaTime;
    }

    public void OnSave(Projectile projectile, Stream stream, IFormatter formatter)
    {
    }

    public void OnLoad(Projectile projectile, Stream stream, IFormatter formatter)
    {
    }
}
