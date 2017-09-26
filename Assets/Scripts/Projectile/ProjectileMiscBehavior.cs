using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;

public interface ProjectileMiscBehavior 
{
    void UpdateMisc(Projectile projectile);

    void OnSave(Projectile projectile, Stream stream, IFormatter formatter);

    void OnLoad(Projectile projectile, Stream stream, IFormatter formatter);
}
