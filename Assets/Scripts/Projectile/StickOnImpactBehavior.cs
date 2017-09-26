using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;

public class StickOnImpactBehavior : MonoBehaviour, ProjectileCollisionBehaviour
{

	void Start () 
    {
	
	}

    public void HandleCollision(Projectile projectile, Vector3 hitPos, Vector3 hitNormal, Collider hitCollider)
    {
        Vector3 localStickPos = hitCollider.transform.InverseTransformPoint(hitPos);

        StickToObject(projectile, localStickPos, hitCollider.gameObject);
    }

    public void OnSave(Projectile projectile, Stream stream, IFormatter formatter)
    {
        SaveHandler stickObjectSaveHandler = null;
        if (m_StickObject != null)
        {
            stickObjectSaveHandler = m_StickObject.GetComponent<SaveHandler>();
        }

        formatter.Serialize(stream, stickObjectSaveHandler != null);
        if (stickObjectSaveHandler != null)
        {
            formatter.Serialize(stream, stickObjectSaveHandler.SaveId);

            GameUtils.SerializeVector3(stream, formatter, m_LocalStickPos);
        }
    }

    public void OnLoad(Projectile projectile, Stream stream, IFormatter formatter)
    {
        if ((bool)formatter.Deserialize(stream))
        {
            string stickObjectStickId = (string)formatter.Deserialize(stream);

            Vector3 localStickPos = GameUtils.DeserializeVector3(stream, formatter);

            GameObject stickObject = GameManager.Instance.GetGameObjectBySaveId(stickObjectStickId);

            if (stickObject != null)
            {
                StickToObject(projectile, localStickPos, stickObject);
            }
            else
            {
                DebugUtils.LogError("Couldn't find stick object with id: {0}", stickObjectStickId);
            }
        }
    }

    void StickToObject(Projectile projectile, Vector3 localStickPos, GameObject stickObject)
    {
        m_LocalStickPos = localStickPos;
        m_StickObject = stickObject;

        projectile.Velocity = Vector3.zero;
        projectile.transform.position = stickObject.transform.TransformPoint(m_LocalStickPos);

        GameObject attachPoint = new GameObject();

        attachPoint.transform.parent = stickObject.transform;
        projectile.transform.parent = attachPoint.transform;

        projectile.SetMotionBehaviour(null);
    }
    
    GameObject m_StickObject;
    Vector3 m_LocalStickPos;
}
