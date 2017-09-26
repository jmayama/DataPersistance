using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;

public interface ProjectileMotionBehavior
{
    void UpdateMotion(Projectile projectile);

    void OnSave(Projectile projectile, Stream stream, IFormatter formatter);

    void OnLoad(Projectile projectile, Stream stream, IFormatter formatter);
}
