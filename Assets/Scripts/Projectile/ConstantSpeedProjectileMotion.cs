using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;

public class ConstantSpeedProjectileMotion : MonoBehaviour, ProjectileMotionBehavior
{

	void Start () 
    {
	
	}

    public void UpdateMotion(Projectile projectile)
    {
        projectile.transform.position += projectile.Velocity * Time.deltaTime;
    }

    public void OnSave(Projectile projectile, Stream stream, IFormatter formatter)
    {
    }

    public void OnLoad(Projectile projectile, Stream stream, IFormatter formatter)
    {
    }
}
