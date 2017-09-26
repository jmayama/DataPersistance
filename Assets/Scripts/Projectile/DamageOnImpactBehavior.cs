using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;

public class DamageOnImpactBehavior : MonoBehaviour, ProjectileCollisionBehaviour
{

	void Start () 
    {
	
	}

    public void HandleCollision(Projectile projectile, Vector3 hitPos, Vector3 hitNormal, Collider hitCollider)
    {
        GameObject.Destroy(projectile.gameObject);
    }

    public void OnSave(Projectile projectile, Stream stream, IFormatter formatter)
    {
    }

    public void OnLoad(Projectile projectile, Stream stream, IFormatter formatter)
    {
    }
}
