using UnityEngine;
using System.Collections;

public class GrapplingHook : UsableItem 
{
    public Vector3 LocalFireDir = Vector3.up;
    public Vector3 LocalFirePos = Vector3.zero;
    public GameObject CablePrefab;
    public float FireSpeed = 60.0f;
    public Vector3 GravityAccel = new Vector3(0.0f, -50.0f, 0.0f);
    
    public float CableThickness = 0.2f;
    
    public float LengthChangeSpeed = 1.0f;
    public float MinLength = 1.0f;
    public float MaxLength = 10.0f;
    public float SpringMaxForce = 100.0f;
    public float SpringDamping = 1.0f;

	void Start () 
    {
        m_CollisionCheckMask = ~LayerMask.GetMask("Player");

        m_CableObj = (GameObject)GameObject.Instantiate(CablePrefab);

        SetState(HookState.ReadyToFire);

        GameObject playerObj = transform.root.gameObject;
        m_Player = playerObj.GetComponent<Player>();
        DebugUtils.Assert(m_Player != null, "Could not find player for grappling hook");
	}
	
	void Update () 
    {
        if (m_Player == null)
        {
            return;
        }

        bool triggerFire = false;
        if (InUse != m_CurrentUseState)
        {
            triggerFire = InUse;

            m_CurrentUseState = InUse;
        }

        switch (m_HookState)
        { 
            case HookState.ReadyToFire:
                UpdateReadyToFire(triggerFire);
                break;

            case HookState.Ballistic:
                UpdateBallistic(triggerFire);
                break;

            case HookState.Latched:
                UpdateLatched(triggerFire);
                break;

            default:
                DebugUtils.LogError("Invalid Hook State: {0}", m_HookState);
                break;
        }
	}

    public override void Equip(Player player)
    {
        base.Equip(player);

        m_Player = player;

        m_Player.GrapplingHook = this;
    }

    public override void Unequip()
    {
        base.Unequip();

        SetState(HookState.ReadyToFire);

        m_Player.GrapplingHook = null;

        m_Player = null;
    }

    public bool IsLatched()
    {
        return m_HookState == HookState.Latched;
    }

    public bool ShouldAim()
    {
        return m_HookState != HookState.ReadyToFire;
    }

    public Vector3 GetCableDir()
    {
        Vector3 cableDir = m_HookPosition - m_BodyPosition;
        cableDir.Normalize();

        return cableDir;
    }

    void UpdateReadyToFire(bool triggerFire)
    {
        if (triggerFire)
        {
            m_HookVelocity = transform.TransformDirection(LocalFireDir);
            m_HookVelocity.Normalize();
            m_HookVelocity *= FireSpeed;
            
            m_BodyPosition = CalcBodyPos();

            m_HookPosition = m_BodyPosition;
            
            SetState(HookState.Ballistic);
        }
    }

    void UpdateBallistic(bool triggerFire)
    {
        if (triggerFire)
        {
            SetState(HookState.ReadyToFire);
            return;
        }

        m_BodyPosition = CalcBodyPos();

        m_HookVelocity += GravityAccel * Time.deltaTime;
        m_HookPosition += m_HookVelocity * Time.deltaTime;

        //Clamp length
        {
            Vector3 endPtDiff = m_HookPosition - m_BodyPosition;

            float cableLength = endPtDiff.magnitude;
            if (cableLength > MaxLength )
            {
                m_HookPosition = (MaxLength / cableLength) * endPtDiff + m_BodyPosition;
            }
        }

        //Check for collisions
        Vector3 hitPoint;
        GameObject hitObject;
        if (DetectCollisions(out hitPoint, out hitObject))
        {
            m_AnchorObject = hitObject;

            m_LocalAnchorPos = m_AnchorObject.transform.InverseTransformPoint(hitPoint);
            m_HookPosition = hitPoint;

            SetState(HookState.Latched);
        }

        //Update cable
        UpdateCable();
    }

    void UpdateLatched(bool triggerFire)
    {
        if (triggerFire)
        {
            SetState(HookState.ReadyToFire);
            return;
        }

        //Check if the anchored object has been destroyed
        if (m_AnchorObject == null)
        {
            SetState(HookState.ReadyToFire);
            return;
        }

        //Update end points
        m_BodyPosition = CalcBodyPos();
        m_HookPosition = m_AnchorObject.transform.TransformPoint(m_LocalAnchorPos);

        //Update other parts
        UpdateLengthControls();

        UpdateSpringForce();

        UpdateCable();
    }
    
    void UpdateCable()
    {
        Vector3 endPtDiff = m_HookPosition - m_BodyPosition;

        float cableLength = endPtDiff.magnitude;
        if (cableLength <= 0.0f)
        {
            return;
        }

        m_CableObj.transform.position = endPtDiff * 0.5f + m_BodyPosition;

        m_CableObj.transform.localScale = new Vector3(CableThickness, CableThickness, cableLength);

        m_CableObj.transform.rotation = Quaternion.LookRotation(endPtDiff);
    }

    void UpdateLengthControls()
    {
        float changeAmount = Input.GetAxis("GrapplingHookLengthChange");
        changeAmount *= LengthChangeSpeed;

        m_DesiredLength += changeAmount * Time.deltaTime;
        m_DesiredLength = Mathf.Clamp(m_DesiredLength, MinLength, MaxLength);
    }

    void UpdateSpringForce()
    {
        Vector3 endPtDiff = m_HookPosition - m_BodyPosition;

        float cableLength = endPtDiff.magnitude;
        if (cableLength <= 0.0f)
        {
            return;
        }

        Vector3 forceDir = endPtDiff / cableLength;

        float forceAmount = MathUtils.CalcSpringForce(
            cableLength,
            m_DesiredLength,
            MinLength,
            MaxLength,
            forceDir,
            m_Player.Velocity,
            SpringMaxForce,
            SpringDamping
            );

        m_Player.AddForce(forceAmount * forceDir);

        if (m_AnchorObject.GetComponent<Rigidbody>() != null)
        {
            m_AnchorObject.GetComponent<Rigidbody>().AddForceAtPosition(-forceAmount * forceDir, m_HookPosition);
        }
    }

    bool DetectCollisions(out Vector3 hitPoint, out GameObject hitObject)
    {
        hitPoint = Vector3.zero;
        hitObject = null;

        Vector3 rayDir = m_HookPosition - m_BodyPosition;
        float rayDist = rayDir.magnitude;
        if (rayDist <= 0.0f)
        {
            return false;
        }

        RaycastHit hitInfo;
        if (!Physics.Raycast(m_BodyPosition, rayDir, out hitInfo, rayDist, m_CollisionCheckMask))
        {
            return false;
        }

        hitPoint = hitInfo.point;
        hitObject = hitInfo.collider.gameObject;

        return true;
    }

    void SetState(HookState state)
    {
        switch (state)
        {
            case HookState.ReadyToFire:
                m_AnchorObject = null;
                m_CableObj.SetActive(false);
                break;

            case HookState.Ballistic:
                m_CableObj.SetActive(true);
                break;

            case HookState.Latched:
                m_DesiredLength = CalcCableLength();
                break;

            default:
                DebugUtils.LogError("Invalid Hook State: {0}", m_HookState);
                break;
        }

        m_HookState = state;
    }

    Vector3 CalcBodyPos()
    {
        return transform.TransformPoint(LocalFirePos);
    }

    float CalcCableLength()
    {
        Vector3 endPtDiff = m_HookPosition - m_BodyPosition;

        return endPtDiff.magnitude;
    }

    enum HookState
    {
        ReadyToFire,
        Ballistic,
        Latched
    }

    GameObject m_AnchorObject;
    Vector3 m_LocalAnchorPos;

    Vector3 m_HookPosition;
    Vector3 m_HookVelocity;

    Vector3 m_BodyPosition;
    
    HookState m_HookState;

    bool m_CurrentUseState;

    GameObject m_CableObj;

    int m_CollisionCheckMask;

    float m_CurrentLength;
    float m_DesiredLength;

    Player m_Player;
}
