using UnityEngine;
using System.Collections;

public class ThirdPersonCamera : MonoBehaviour 
{
    public float CameraPosEaseSpeed = 1.0f;
    public float LookPosEaseSpeed = 1.0f;
    public float PlayerFadeOutDist = 2.0f;
    public float ObstacleCheckSpreadDist = 1.0f;
    public float ObstaclePushForwardDist = 0.25f;

	void Start () 
    {        
        //Get player layer mask
        //
        //The layer masks are integers where each bit represents a layer.
        //If mask is passed into the raycast function only layers with a bit set to 1 will be hit.
        //Since we want to hit everything EXCEPT the player, we use the bitwise not operator (~) to
        //invert the bits of the mask we get back from the player.
        m_RaycastHitMask = ~LayerMask.GetMask("Player");

	    //Get player
        m_Player = GameObject.Find("Player").GetComponent<Player>();

        //Get Follow object
        if (m_Player != null)
        {
            m_FollowPosObj = m_Player.transform.FindChild("VerticalLookPivot/CameraFollowPos").gameObject;

            m_LookPos = m_FollowPosObj.transform.position;
        }
	}
	
	void LateUpdate () 
    {
        //Update Position
        Vector3 goalCameraPosition = m_FollowPosObj.transform.position;

	    transform.position = MathUtils.ExponentialEase(
            CameraPosEaseSpeed, 
            transform.position, 
            goalCameraPosition, 
            Time.deltaTime
            );

        //Update angle
        Vector3 goalLookPos = m_Player.transform.position;

        m_LookPos = MathUtils.ExponentialEase(
            LookPosEaseSpeed,
            m_LookPos,
            goalLookPos,
            Time.deltaTime
            );

        Vector3 lookDir = m_LookPos - transform.position;

        transform.rotation = Quaternion.LookRotation(lookDir);

        //Deal with obstacles
        HandleObstacles();
	}

    void HandleObstacles()
    {
        //This code casts four rays out to check for obstacles. 
        //Each call below will cast a ray.  Since we want to move up past the closest obstacle
        //we need to track the minimum distance to the obstacles found.  These are passed into
        //each function then updated with the return argument.

        float minMoveUpDist = MoveInFrontOfObstacles(
            m_LookPos, 
            transform.position,
            new Vector3(ObstacleCheckSpreadDist, ObstacleCheckSpreadDist, 0.0f), 
            float.MaxValue
            );

        minMoveUpDist = MoveInFrontOfObstacles(
            m_LookPos,
            transform.position,
            new Vector3(ObstacleCheckSpreadDist, -ObstacleCheckSpreadDist, 0.0f),
            minMoveUpDist
            );

        minMoveUpDist = MoveInFrontOfObstacles(
            m_LookPos,
            transform.position,
            new Vector3(-ObstacleCheckSpreadDist, ObstacleCheckSpreadDist, 0.0f),
            minMoveUpDist
            );

        minMoveUpDist = MoveInFrontOfObstacles(
            m_LookPos,
            transform.position,
            new Vector3(-ObstacleCheckSpreadDist, -ObstacleCheckSpreadDist, 0.0f),
            minMoveUpDist
            );

        //If the player is too close to the nearest obstacle, fade out.
        if (minMoveUpDist <= PlayerFadeOutDist)
        {
            m_Player.SetGoalFade(0.0f);
        }
        else
        {
            m_Player.SetGoalFade(1.0f);
        }
    }

    //Returns the min distance to the player
    float MoveInFrontOfObstacles(Vector3 rayStart, Vector3 rayEnd, Vector3 localRayOffset, float minMoveUpDist)
    {
        //Multiply the local ray offset by the player's rotation to get the world offset
        Vector3 worldRayOffset = transform.rotation * localRayOffset;

        //Calculate the ray direction and return early if the ray has a length of zero
        Vector3 rayDir = rayEnd - rayStart;

        float rayDist = rayDir.magnitude;
        if (rayDist <= 0.0f)
        {
            return 0.0f;
        }

        rayDir /= rayDist;

        //Get all objects that intersect with the ray, so we can process them all.
        //Note: the objects returned by this function are not sorted.
        RaycastHit[] hitInfos = Physics.RaycastAll(rayStart + worldRayOffset, rayDir, rayDist, m_RaycastHitMask);
        if (hitInfos.Length <= 0)
        {
            return minMoveUpDist;
        }

        //Process each obstacle
        foreach (RaycastHit hitInfo in hitInfos)
        {
            GameObject obstacleObject = hitInfo.collider.gameObject;

            //Check if the obstacle should fade out instead of moving the camera upward.
            CameraObstacle cameraObstacle = obstacleObject.GetComponent<CameraObstacle>();
            if (cameraObstacle != null && cameraObstacle.FadeOutIfObstructing)
            {
                cameraObstacle.FadeOutThisFrame();
                continue;
            }

            //Calculate the move up distance, and ignore the obstacle if we already processed an 
            //obstacle that was closer
            float moveUpDist = hitInfo.distance + ObstaclePushForwardDist;
            if (moveUpDist > minMoveUpDist)
            {
                continue;
            }

            //Interpolate along the original ray to get the position
            float interpolateAmount = Mathf.Clamp01(moveUpDist / rayDist);

            transform.position = Vector3.Lerp(rayStart, rayEnd, interpolateAmount);

            minMoveUpDist = moveUpDist;
        }

        return minMoveUpDist;
    }

    Player m_Player;
    GameObject m_FollowPosObj;

    Vector3 m_LookPos;

    int m_RaycastHitMask;
}
