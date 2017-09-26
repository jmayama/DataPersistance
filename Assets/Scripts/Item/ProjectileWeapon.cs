using UnityEngine;
using System.Collections;

public class ProjectileWeapon : UsableItem 
{
    public Vector3 LocalFireDir = Vector3.up;
    public Vector3 LocalFirePos = Vector3.zero;
    public float TimeBetweenShots;
    public float FireSpeed;

    public GameManager.DynamicObjectsCreateId ProjectileCreateId;

	void Start () 
    {
	
	}
	
	void Update () 
    {
        if (m_TimeTillNextShot > 0.0f)
        {
            m_TimeTillNextShot -= Time.deltaTime;
        }
        else if (InUse)
        {
            FireProjectile();
        }
	}

    void FireProjectile()
    {
        m_TimeTillNextShot = TimeBetweenShots;

        Vector3 fireDir = transform.TransformDirection(LocalFireDir);
        fireDir.Normalize();

        Vector3 firePos = transform.TransformPoint(LocalFirePos);

        GameObject projectileObj = GameManager.Instance.CreateDynamicObject(
            ProjectileCreateId,
            firePos,
            Quaternion.LookRotation(fireDir)
            );

        Projectile projectile = projectileObj.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Init(firePos, fireDir * FireSpeed);
        }
    }

    float m_TimeTillNextShot;
}
