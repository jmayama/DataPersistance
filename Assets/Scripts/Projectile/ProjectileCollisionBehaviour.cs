using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;

public interface ProjectileCollisionBehaviour 
{
    void HandleCollision(Projectile projectile, Vector3 hitPos, Vector3 hitNormal, Collider hitCollider);

    void OnSave(Projectile projectile, Stream stream, IFormatter formatter);

    void OnLoad(Projectile projectile, Stream stream, IFormatter formatter);
}
