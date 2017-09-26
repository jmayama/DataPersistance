using UnityEngine;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;


public class Projectile : MonoBehaviour, Saveable
{
    public MonoBehaviour CollisionBehaviour;
    public MonoBehaviour MotionBehaviour;
    public MonoBehaviour MiscBehaviour;

    void Awake()
    {
		m_CollisionBehaviour = (ProjectileCollisionBehaviour)CollisionBehaviour;
		m_MotionBehaviour = (ProjectileMotionBehavior)MotionBehaviour;
		m_MiscBehavior = (ProjectileMiscBehavior)MiscBehaviour;
    }

	void Start () 
    {

	}

    public void Init(Vector3 position, Vector3 velocity)
    {
        transform.position = position;
        m_PrevPosition = position;

        Velocity = velocity;
    }

	void Update ()
    {
        //Update motion
        m_PrevPosition = transform.position;

        if (m_MotionBehaviour != null)
        {
            m_MotionBehaviour.UpdateMotion(this);
        }

        //Update misc behaviours
        if (m_MiscBehavior != null)
        {
            m_MiscBehavior.UpdateMisc(this);
        }

        //Check for collision
        for (int i = 0; i < 1000; ++i)
        {
            HandleCollisions();
        }
    }

    public Vector3 Velocity { get; set; }

    public void SetMotionBehaviour(ProjectileMotionBehavior behaviour)
    {
        m_MotionBehaviour = behaviour;
    }

    //Make sure that the order that you serialize things is the same as the order 
    //that you deserialize things
    public void OnSave(Stream stream, IFormatter formatter)
    {
        //Serializing needed data
        GameUtils.SerializeVector3(stream, formatter, transform.position);
        GameUtils.SerializeVector3(stream, formatter, m_PrevPosition);
        GameUtils.SerializeVector3(stream, formatter, Velocity);

        //Serializing the different projectile modifiers.  Note that since
        //these can be null we first save a bool to track whether they exist or
        //not then we save them if they do.

        formatter.Serialize(stream, m_MotionBehaviour != null);
        if (m_MotionBehaviour != null)
        {
            m_MotionBehaviour.OnSave(this, stream, formatter);
        }

        formatter.Serialize(stream, m_CollisionBehaviour != null);
        if (m_CollisionBehaviour != null)
        {
            m_CollisionBehaviour.OnSave(this, stream, formatter);
        }

        formatter.Serialize(stream, m_MiscBehavior != null);
        if (m_MiscBehavior != null)
        {
            m_MiscBehavior.OnSave(this, stream, formatter);
        }
    }

    //Make sure that the order that you deserialize things is the same as 
    //the order that you serialize things
    public void OnLoad(Stream stream, IFormatter formatter)
    {
        //Deserializing needed data
        transform.position = GameUtils.DeserializeVector3(stream, formatter);
        m_PrevPosition = GameUtils.DeserializeVector3(stream, formatter);
        Velocity = GameUtils.DeserializeVector3(stream, formatter);

        //Deserializing the different projectile modifiers.  Note that since
        //these can be null to check whether they exist or not then load 
        //them if they do.
        if ((bool)formatter.Deserialize(stream))
        {
            m_MotionBehaviour.OnLoad(this, stream, formatter);
        }

        if ((bool)formatter.Deserialize(stream))
        {
            m_CollisionBehaviour.OnLoad(this, stream, formatter);
        }

        if ((bool)formatter.Deserialize(stream))
        {
            m_MiscBehavior.OnLoad(this, stream, formatter);
        }
    }

    void HandleCollisions()
    {
        Vector3 positionDiff = transform.position - m_PrevPosition;

        float moveDist = positionDiff.magnitude;
        if (moveDist <= 0.0f)
        {
            return;
        }

        positionDiff /= moveDist;

        RaycastHit hitInfo;
        if (!Physics.Raycast(m_PrevPosition, positionDiff, out hitInfo, moveDist))
        {
            return;
        }

        //Handle Collision
        if (m_CollisionBehaviour != null)
        {
            m_CollisionBehaviour.HandleCollision(this, hitInfo.point, hitInfo.normal, hitInfo.collider);
        }
    }

    Vector3 m_PrevPosition;

    ProjectileMotionBehavior m_MotionBehaviour;
    ProjectileCollisionBehaviour m_CollisionBehaviour;
    ProjectileMiscBehavior m_MiscBehavior;
}

