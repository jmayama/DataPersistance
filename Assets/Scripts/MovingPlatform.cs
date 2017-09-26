using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.IO;
using System;

//This is a moving platform that moves back and forth between the positions of two specified gameobjects 
public class MovingPlatform : MonoBehaviour, GroundSurface, Saveable
{
    public GameObject MovePtObject1;
    public GameObject MovePtObject2;

    public float MoveSpeed = 5.0f;

    void Start () 
    {
        m_PersistentData.travellingForward = true;
	}

    void FixedUpdate()
    {
        //Calculate the vector and distance between the two move points        
        Vector3 movePtDiff = MovePtObject2.transform.position - MovePtObject1.transform.position;

        float distBetweenMovePts = movePtDiff.magnitude;

        //Calcualte the move speed as a percent between the points
        float percentMoveSpeed = MoveSpeed / distBetweenMovePts;

        //Update the percent between the two objects
        if (m_PersistentData.travellingForward)
        {
            m_PersistentData.percentBetweenObjects += percentMoveSpeed * Time.fixedDeltaTime;

            if (m_PersistentData.percentBetweenObjects >= 1.0f)
            {
                m_PersistentData.travellingForward = false;
            }
        }
        else
        {
            m_PersistentData.percentBetweenObjects -= percentMoveSpeed * Time.fixedDeltaTime;

            if (m_PersistentData.percentBetweenObjects <= 0.0f)
            {
                m_PersistentData.travellingForward = true;
            }
        }

        m_PersistentData.percentBetweenObjects = Mathf.Clamp01(m_PersistentData.percentBetweenObjects);

        //Update position
        Vector3 newPosition = Vector3.Lerp(MovePtObject1.transform.position, MovePtObject2.transform.position, m_PersistentData.percentBetweenObjects);
        
        //Setting the position using the "MovePosition" function.  This will make it interract with 
        //other physics objects better, since the motion will be taken into account instead of just
        //teleporting
        GetComponent<Rigidbody>().MovePosition(newPosition);
    }

    void Update()
    {
        //Update surfaceVelocity
        Vector3 moveDir = MovePtObject2.transform.position - MovePtObject1.transform.position;
        moveDir.Normalize();

        m_SurfaceVelocity = MoveSpeed * moveDir;
        if (!m_PersistentData.travellingForward)
        {
            m_SurfaceVelocity *= -1.0f;
        }
    }

    public Vector3 GetSurfaceVelocity(Vector3 samplePt)
    {
        return m_SurfaceVelocity;
    }

    public Vector3 GetSurfaceAngularVelocity(Vector3 samplePt)
    {
        return Vector3.zero;
    }

    public void OnSave(Stream stream, IFormatter formatter)
    {
        formatter.Serialize(stream, m_PersistentData);

        GameUtils.SerializeVector3(stream, formatter, m_SurfaceVelocity);
        GameUtils.SerializeVector3(stream, formatter, transform.position);
    }

    public void OnLoad(Stream stream, IFormatter formatter)
    {
        m_PersistentData = (PersistentData)formatter.Deserialize(stream);

        m_SurfaceVelocity = GameUtils.DeserializeVector3(stream, formatter);
        transform.position = GameUtils.DeserializeVector3(stream, formatter);
    }

    public Vector3 m_SurfaceVelocity;

    PersistentData m_PersistentData = new PersistentData();
}

[Serializable]
struct PersistentData
{
    public float percentBetweenObjects;
    public bool travellingForward;
}