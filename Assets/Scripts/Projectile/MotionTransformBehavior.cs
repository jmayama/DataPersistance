using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;

public class MotionTransformBehavior : MonoBehaviour, ProjectileMiscBehavior
{
    public float MaxStretchAmount = 10.0f;
    public float MaxStretchSpeed = 100.0f;

	void Start () 
    {
        m_OriginalScale = transform.localScale.z;
	}

    public void UpdateMisc(Projectile projectile)
    {
        float speed = projectile.Velocity.magnitude;

        //Update Scale
        {
            float stretchPercent = Mathf.Clamp01(speed / MaxStretchSpeed);

            Vector3 newScale = transform.localScale;

            newScale.z = m_OriginalScale * Mathf.Lerp(1.0f, MaxStretchAmount, stretchPercent);

            projectile.transform.localScale = newScale;
        }

        //Update rotation
        if (speed > MathUtils.CompareEpsilon)
        {
            projectile.transform.rotation = Quaternion.LookRotation(projectile.Velocity);
        }
    }

    public void OnSave(Projectile projectile, Stream stream, IFormatter formatter)
    {
        formatter.Serialize(stream, m_OriginalScale);
    }

    public void OnLoad(Projectile projectile, Stream stream, IFormatter formatter)
    {
        m_OriginalScale = (float)formatter.Deserialize(stream);
    }

    float m_OriginalScale;
}
