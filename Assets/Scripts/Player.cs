using UnityEngine;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;

public class Player : MonoBehaviour, Saveable
{
    public float InAirMoveAccel = 30.0f;
    public float OnGroundMoveAccel = 10.0f;

    public float OnGroundControlSpeed = 10.0f;
    public float MaxOnGroundAnimSpeed = 5.0f;
    public float InAirControlSpeed = 10.0f;

    public float OnGroundStopEaseSpeed = 10.0f;
    public float InAirStopEaseSpeed = 2.0f;

    public float TurnSpeed = 10.0f;
    public float GravityAccel = -10.0f;
    public float FadeEaseSpeed = 1.0f;
    public float JumpSpeed = 10.0f;
    public float MaxJumpHoldTime = 0.5f;

    public float CheckForGroundDist = 0.5f;

    public float OnGroundThresholdDist = 0.1f;

    public float MaxMidAirJumpTime = 0.3f;

    public float ChangeDirectionAccelThreashold = 1.0f;

    public float JumpPushOutOfGroundAmount = 0.5f;

    public float Mass = 1.0f;

    public float CollisionSpringMaxLength = 1.0f;
    public float CollisionSpringMaxForce = 5000.0f;
    public float CollisionSpringDamping = 1.0f;
    public float PushForceAmount = 400.0f;

    public float PushFrictionCoefficient = 0.5f;

    public float MaxVerticalCameraAngle = 70;

    public float GrappleLeanEaseSpeed = 2.0f;

    public List<GameObject> ItemList;

    public Vector3 AimAngleCorrection;

    public float AimPrepareTime = 1.0f;
    public float StopAimingEaseAmount = 5.0f;

    void Start ()
    {
        if (GameManager.Instance.GetPlayer() == null)
        {
            DontDestroyOnLoad(gameObject);

            GameManager.Instance.RegisterPlayer(this);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        //Initialing miscellaneous values
        m_CharacterController = GetComponent<CharacterController>();

        m_VerticalLookPivot = transform.FindChild("VerticalLookPivot").gameObject;

        m_GroundCheckMask = ~LayerMask.GetMask("Player");

        m_Velocity = new Vector3();

        m_AllowJump = true;

        //Set up animator
        {
            GameObject modelObject = transform.FindChild("Model").gameObject;

            m_Animator = modelObject.GetComponent<Animator>();
        }

        //Initialize the fading on the player
        m_CurrentFade = 1.0f;
        SetGoalFade(m_CurrentFade);

        //Init Item list
        foreach (GameObject itemObj in ItemList)
        {
            itemObj.SetActive(false);
        }
        SwitchActiveItem(0);
    }

    void Update()
    {
        UpdateMouseControlToggle();

        UpdateGroundInfo();

        //Get input
        Vector3 localMoveDir = new Vector3(
            Input.GetAxis("Horizontal"),
            0.0f,
            Input.GetAxis("Vertical")
            );
        localMoveDir.Normalize();

        bool isJumping = Input.GetButton("Jump");

        //Update is on Ground
        bool isOnGround = m_CharacterController.isGrounded || m_GroundDist < OnGroundThresholdDist;

        //Update mid air jump timer.  This timer is to make jump timing a little more forgiving by letting you
        //still jump a short time after falling off a ledge.
        if (isOnGround)
        {
            m_TimeLeftToAllowMidAirJump = MaxMidAirJumpTime;                
        }
        else
        {
            if (m_JumpTimeLeft <= 0.0f)
            {
                if (m_TimeLeftToAllowMidAirJump > 0.0f)
                {
                    m_TimeLeftToAllowMidAirJump -= Time.deltaTime;
                }
            }
            else
            {
                m_TimeLeftToAllowMidAirJump = 0.0f;
            }
        }

        //Update movement
        ApplyExternalForce();

        if (isOnGround)
        {
            UpdateOnGround(localMoveDir, isJumping);
        }
        else
        {
            UpdateInAir(localMoveDir, isJumping);
        }

        //Update other parts 
        UpdateRotation();

        UpdateItemInput();

        UpdateAnimations(isOnGround);

        UpdateFade();
    }

    public void SetGoalFade(float fade)
    {
        m_GoalFade = fade;
    }

    public void UpdateStopping(float stopEaseSpeed)
    {
        //Ease down to the ground velocity to stop relative to the ground
        m_Velocity = MathUtils.ExponentialEase(stopEaseSpeed, m_Velocity, m_GroundVelocity, Time.deltaTime);
    }

    public void OnCollisionStay(Collision collision)
    {
        //Get the capsule collider from the collision reporter
        CapsuleCollider capsuleCollider = (CapsuleCollider)CollisionReporter.GetComponent<Collider>();

        //Caclulate the amount we need to displace the character controller by caclulating the closest
        //point on the capsule and pushing out from there.
        Vector3 contactPt = collision.contacts[0].point;

        Vector3 capsulePt = MathUtils.GetClosestPtOnCapsule(
            contactPt,
            transform.position,
            capsuleCollider.height,
            capsuleCollider.radius
            );

        //If we are colliding near the bottom, make the capsule step up instead.  This will make jumping onto ledges
        //more forgiving.
        Vector3 bottomPt = transform.position - Vector3.up * (capsuleCollider.height * 0.5f);

        if (capsulePt.y - bottomPt.y < m_CharacterController.stepOffset)
        {
            capsulePt.y = bottomPt.y;
        }

        //Displace from the capsule point out to the contact point since it should be penetrating
        Vector3 collisionDisplacement = contactPt - capsulePt;

        //Apply penalty forces
        {
            float displacementAmount = collisionDisplacement.magnitude;

            if (displacementAmount > 0.0f)
            {
                Vector3 forceDir = collisionDisplacement / displacementAmount;

                float forceAmount = MathUtils.CalcSpringForce(
                    displacementAmount,
                    0.0f,
                    0.0f,
                    CollisionSpringMaxLength,
                    forceDir,
                    m_Velocity,
                    CollisionSpringMaxForce,
                    CollisionSpringDamping
                    );

                float accelThisFrame = Time.fixedDeltaTime * forceAmount / Mass;

                m_Velocity += accelThisFrame * forceDir;

                //Apply the opposite force to the other object
                if (collision.collider.GetComponent<Rigidbody>() != null)
                {
                    Vector3 forcePt = contactPt;
                    forcePt.y = GetComponent<Collider>().transform.position.y;

                    collision.collider.GetComponent<Rigidbody>().AddForceAtPosition(
                        -(PushForceAmount + forceAmount) * forceDir,
                        forcePt
                        );
                }
            }
        }
    }

    public void AddForce(Vector3 force)
    {
        m_NetExternalForce += force;
    }

    public CharacterCollisionReporter CollisionReporter { get; set; }

    public Vector3 Velocity
    {
        get
        {
            return m_Velocity;
        }
    }

    public GrapplingHook GrapplingHook { get; set; }

    public void OnSave(Stream stream, IFormatter formatter)
    {
        GameUtils.SerializeVector3(stream, formatter, transform.position);
        GameUtils.SerializeQuaternion(stream, formatter, transform.rotation);
    }

    public void OnLoad(Stream stream, IFormatter formatter)
    {
        transform.position = GameUtils.DeserializeVector3(stream, formatter);
        transform.rotation = GameUtils.DeserializeQuaternion(stream, formatter);
    }

    void UpdateRotation()
    {
        {//Vertcal rotation
            float rotationThisFrame = 0.0f;
                
            if (m_EnableMouseControl)
            {
                rotationThisFrame = TurnSpeed * Input.GetAxis("Mouse Y");
            }

            m_VerticalLookAngle += rotationThisFrame;
            m_VerticalLookAngle = Mathf.Clamp(m_VerticalLookAngle, -MaxVerticalCameraAngle, MaxVerticalCameraAngle);

            m_VerticalLookPivot.transform.localRotation = Quaternion.Euler(m_VerticalLookAngle, 0.0f, 0.0f);
        }

        {//Horizontal rotation;
            float rotationThisFrame = 0.0f;

            if (m_EnableMouseControl)
            {
                rotationThisFrame = TurnSpeed * Input.GetAxis("Mouse X");
            }

            Quaternion rotation = Quaternion.Euler(0.0f, rotationThisFrame, 0.0f);

            //Add in the rotation from the ground surface
            rotation *= Quaternion.Euler(m_GroundAngularVelocity * Time.deltaTime); 

            //Add to the current rotation
            transform.rotation *= rotation;
        }
    }

    void LateUpdate()
    {
        //Make adjustments to the animations.  This is done in LateUpdate() since this function happens after 
        //animations are updated each frame.  

        //Get the transforms we need. 
        //TODO: optimize this by making these member variables and finding them in Start()
        Transform modelTransform = transform.FindChild("Model");
        Transform spineTransform = transform.FindChild("Model/Pelvis/Spine1");

        if (GrapplingHook != null && GrapplingHook.IsLatched() && !m_NearGround)
        {
            //Update leaning if the Character is swinging on a grappling hook
            Vector3 cableDir = GrapplingHook.GetCableDir();

            //Easing the lean direction toward the cable direction.  This ease was mainly chosen because the 
            //implementation is simple.  Better results could be achieve by doing something with more of a physical
            //basis.
            m_GrappleLeanDir = MathUtils.ExponentialEase(GrappleLeanEaseSpeed, m_GrappleLeanDir, cableDir, Time.deltaTime);

            //Doing a look rotation toward the cable direction.  Since the look rotation points the z axis toward the direction
            //and we want our y direction to look in the cable direction we need to add extra rotation to account for this.
            modelTransform.rotation = Quaternion.LookRotation(m_GrappleLeanDir, -transform.forward) * Quaternion.FromToRotation(Vector3.up, Vector3.forward);
        }
        else
        {
            //Since our aiming animation doesn't point straight ahead we correct it by rotating the spine a little bit

            //Calculate the horizontal world space angle that we want to aim towards
            Vector3 shootDir = m_VerticalLookPivot.transform.forward;
            shootDir.y = 0.0f;

            float angle = MathUtils.CalcRotationDegs(shootDir.x, shootDir.z);

            //Use the aim layer weight to lerp towards the full aim angle correction.  This eliminates the abrupt
            //snap when you start aiming.
            float aimLayerWeight = CalcAimLayerWeight();
            Vector3 aimCorrection = Vector3.Lerp(Vector3.zero, AimAngleCorrection, aimLayerWeight);

            spineTransform.rotation = Quaternion.Euler(0.0f, -angle, -90.0f) * Quaternion.Euler(aimCorrection);

            //Reset the rotation from the grappling hook
            modelTransform.rotation = new Quaternion();

            m_GrappleLeanDir = Vector3.up;
        }
    }

    void UpdateFade()
    {
        m_CurrentFade = MathUtils.ExponentialEase(FadeEaseSpeed, m_CurrentFade, m_GoalFade, Time.deltaTime);

        GameUtils.SetAlpha(m_CurrentFade, gameObject);
    }

    void UpdateGroundInfo()
    {
        //Clear ground info.  Doing this here can simplify the code a bit since we deal with cases where the
        //ground isn't found more easily
        m_NearGround = false;
        m_GroundAngularVelocity.Set(0.0f, 0.0f, 0.0f);
        m_GroundVelocity.Set(0.0f, 0.0f, 0.0f);
        m_GroundDist = float.MaxValue;

        //Raycast downwards
        Vector3 rayStart = transform.position;
        Vector3 rayDir = -Vector3.up;

        float rayDist = m_CharacterController.height * 0.5f + CheckForGroundDist;

        RaycastHit hitInfo;
        if (!Physics.Raycast(rayStart, rayDir, out hitInfo, rayDist, m_GroundCheckMask))
        {
            return;
        }

        //Get ground info
        m_NearGround = true;

        m_GroundDist = hitInfo.distance - m_CharacterController.height * 0.5f;

        GroundSurface groundSurface = hitInfo.collider.GetComponent(typeof(GroundSurface)) as GroundSurface;
        if (groundSurface != null)
        {
            m_GroundAngularVelocity = groundSurface.GetSurfaceAngularVelocity(hitInfo.point);
            m_GroundVelocity = groundSurface.GetSurfaceVelocity(hitInfo.point);
        }
        else if (hitInfo.collider.GetComponent<Rigidbody>() != null)
        {
            m_GroundAngularVelocity = hitInfo.collider.GetComponent<Rigidbody>().angularVelocity;
            m_GroundVelocity = hitInfo.collider.GetComponent<Rigidbody>().velocity;
        }
    }

    void UpdateOnGround(Vector3 localMoveDir, bool isJumping)
    {
        //if movement is close to zero just stop
        if (localMoveDir.sqrMagnitude > MathUtils.CompareEpsilon)
        {
            //Since the movement calculations are easier to do with out taking the ground velocity into account
            //we are calculating the velocity relative to the ground
            Vector3 localVelocity = m_Velocity - m_GroundVelocity;

            //Temporarily removing the vertical component from our velocity to simplfy the movement calculations
            float origLocalVelY = localVelocity.y;
            localVelocity.y = 0.0f;

            //The world movement accelration
            Vector3 moveAccel = transform.TransformDirection(localMoveDir);

            //The velocity along the movement direction
            Vector3 velAlongMoveDir = Vector3.Project(localVelocity, moveAccel);

            //If we are changing direction, come to a stop first.  This makes the movement more responsive
            //since the stopping happens a lot faster than the acceleration typically allows
            if (Vector3.Dot(velAlongMoveDir, moveAccel) > -ChangeDirectionAccelThreashold)
            {
                //Use a similar method to stopping to ease the movement to just be in the desired move direction
                //This makes turning more responsive
                localVelocity = MathUtils.ExponentialEase(OnGroundStopEaseSpeed, localVelocity, velAlongMoveDir, Time.deltaTime);

                //If we are less than our max speed, apply the acceleration
                float speedSqrd = velAlongMoveDir.sqrMagnitude;
                if (speedSqrd < OnGroundControlSpeed * OnGroundControlSpeed)
                {
                    moveAccel *= OnGroundMoveAccel;

                    localVelocity += moveAccel * Time.deltaTime;
                }

                //Restore the vertical velocty
                localVelocity.y = origLocalVelY;

                //Update the world velocity
                m_Velocity = localVelocity + m_GroundVelocity;
            }
            else
            {
                UpdateStopping(OnGroundStopEaseSpeed);
            }
        }
        else 
        {
            UpdateStopping(OnGroundStopEaseSpeed);
        }

        //Make sure vertical velocity is the same as the ground.  This could be made more accurate by taking 
        //gravity into account and not going down faster than gravity would allow.
        m_Velocity.y = m_GroundVelocity.y;
        
        //Handle jump input
        if (isJumping)
        {
            ActivateJump();
        }
        else
        {
            m_AllowJump = true;
        }

        //Move the character controller
        m_CharacterController.Move(m_Velocity * Time.deltaTime);
    }

    void UpdateInAir(Vector3 localMoveDir, bool isJumping)
    {
        //if movement is close to zero just stop
        if (localMoveDir.sqrMagnitude > MathUtils.CompareEpsilon)
        {
            //The world movement accelration
            Vector3 moveAccel = transform.TransformDirection(localMoveDir);

            //The velocity along the movement direction
            Vector3 velAlongMoveDir = Vector3.Project(m_Velocity, moveAccel);

            //If we are changing direction, come to a stop first.  This makes the movement more responsive
            //since the stopping happens a lot faster than the acceleration typically allows
            if (Vector3.Dot(velAlongMoveDir, moveAccel) > -ChangeDirectionAccelThreashold)
            {
                //If we are less than our max speed, apply the acceleration
                float speedSqrd = velAlongMoveDir.sqrMagnitude;
                if (speedSqrd < InAirControlSpeed * InAirControlSpeed)
                {
                    moveAccel *= InAirMoveAccel;

                    m_Velocity += moveAccel * Time.deltaTime;
                }
            }
            else
            {
                UpdateStopping(InAirStopEaseSpeed);
            }
        }

        //Activate jump mid-air if these conditions are true.  This is done to make jump input more forgiving
        if (m_NearGround || m_TimeLeftToAllowMidAirJump > 0.0f)
        {
            if (isJumping)
            {
                ActivateJump();
            }
            else
            {
                m_AllowJump = true;
            }
        }

        //Update gravity and jump height control
        if (m_JumpTimeLeft > 0.0f && isJumping)
        {
            m_JumpTimeLeft -= Time.deltaTime;
        }
        else
        {
            m_Velocity.y += GravityAccel * Time.deltaTime;

            m_JumpTimeLeft = 0.0f;
        }

        //Move the character controller
        m_CharacterController.Move(m_Velocity * Time.deltaTime);
    }

    void ActivateJump()
    {
        //The allowJump bool is to prevent the player from holding down the jump button to bounce up and down
        //Instead they will have to release the button first.
        if (m_AllowJump)
        {
            //Set the vertical speed to be the jump speed + the ground velocity
            m_Velocity.y = JumpSpeed + m_GroundVelocity.y;

            //This is to ensure that the player wont still be touching the ground after the jump
            transform.position += new Vector3(0.0f, JumpPushOutOfGroundAmount, 0.0f);

            //Set the jump timer
            m_JumpTimeLeft = MaxJumpHoldTime;

            m_AllowJump = false;
        }
    }

    void UpdateMouseControlToggle()
    {
        if (GUIUtility.hotControl != 0)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Screen.lockCursor = true;
        }

        m_EnableMouseControl = Screen.lockCursor;
    }

    void UpdateItemInput()
    {
        if (m_CurrentItem == null)
        {
            return;
        }

        //Handle the fire input.
        if (Input.GetAxis("Fire1") > 0.0f)
        {
            //If the aiming hasn't been prepared yet.  Start the timer.
            if (!m_CurrentItem.InUse && !m_PreparingAim)
            {
                m_TimeLeftToPrepareAim = AimPrepareTime;
                m_PreparingAim = true;
            }

            //Update the prepare aim timer.
            if (m_TimeLeftToPrepareAim > 0.0f)
            {
                m_TimeLeftToPrepareAim -= Time.deltaTime;
            }
            else
            {
                m_PreparingAim = false;
                m_CurrentItem.InUse = true;
            }
        }
        else
        {
            //Reset preparing aim and item InUse
            m_PreparingAim = false;
            m_CurrentItem.InUse = false;
            m_TimeLeftToPrepareAim = 0.0f;
        }

        //TODO: use input axes for this
        if (Input.GetMouseButtonDown(1))
        {
            SwitchActiveItem((m_CurrentItemIndex + 1) % ItemList.Count);
        }
    }

    void ApplyExternalForce()
    {
        Vector3 accel = m_NetExternalForce / Mass;

        m_Velocity += accel * Time.deltaTime;

        m_NetExternalForce.Set(0.0f, 0.0f, 0.0f);
    }

    void SwitchActiveItem(int index)
    {
        m_CurrentItemIndex = index;

        if (m_CurrentItem != null)
        {
            m_CurrentItem.Unequip();
        }

        GameObject itemObj = ItemList[m_CurrentItemIndex];

        m_CurrentItem = itemObj.GetComponent<UsableItem>();

        if (m_CurrentItem != null)
        {
            m_CurrentItem.Equip(this);
        }
    }

    void UpdateAnimations(bool isOnGround)
    {
        //Update on ground
        m_Animator.SetBool("OnGround", m_NearGround);

        //Update velocity params
        Vector3 localRelativeVelocity = m_Velocity - m_GroundVelocity;
        localRelativeVelocity = transform.InverseTransformDirection(localRelativeVelocity);
        localRelativeVelocity.y = 0.0f;

        m_Animator.SetFloat("ForwardSpeed", localRelativeVelocity.z / MaxOnGroundAnimSpeed);
        m_Animator.SetFloat("RightSpeed", localRelativeVelocity.x / MaxOnGroundAnimSpeed);

        //Update aiming
        const int animLayerIndex = 1; //TODO: write a function to get this by name

        if (IsAiming())
        {
            //Set the aim layer weight
            m_AimLayerWeight = CalcAimLayerWeight();
            m_Animator.SetLayerWeight(animLayerIndex, m_AimLayerWeight);

            //Set the aiming angle in the animator depending if the grappling hook
            //is in use, or a regular item.
            if (GrapplingHook != null && GrapplingHook.IsLatched())
            {
                Vector3 cableDir = GrapplingHook.GetCableDir();

                float veritcallookAngle = Vector3.Angle(Vector3.up, cableDir);
                veritcallookAngle = 90.0f - veritcallookAngle;

                m_Animator.SetFloat("AimAngle", veritcallookAngle);
            }
            else
            {
                m_Animator.SetFloat("AimAngle", -m_VerticalLookAngle);
            }
        }
        else
        {
            //Turn off the aiming animation by easing the weight back to zero.  
            m_AimLayerWeight = MathUtils.ExponentialEase(StopAimingEaseAmount, m_AimLayerWeight, 0.0f, Time.deltaTime);

            m_Animator.SetLayerWeight(animLayerIndex, m_AimLayerWeight);
        }
    }

    //Checks the circumstances where the player should be aiming or not
    bool IsAiming()
    {
        if (m_PreparingAim || m_TimeLeftToPrepareAim > 0.0f)
        {
            return true;
        }

        if (m_CurrentItem != null && m_CurrentItem.InUse)
        {
            return true;
        }

        if (GrapplingHook != null && GrapplingHook.ShouldAim())
        {
            return true;
        }            

        return false;
    }

    //Calculates the aiming weight based on if the character should be aiming and 
    //The time left to prepare the aim.
    float CalcAimLayerWeight()
    {
        if (!IsAiming())
        {
            return 0.0f;
        }

        return 1.0f - Mathf.Clamp01(m_TimeLeftToPrepareAim / AimPrepareTime);
    }
    
    CharacterController m_CharacterController;
    Animator m_Animator;

    Vector3 m_Velocity;
    Vector3 m_NetExternalForce;

    int m_GroundCheckMask;

    Vector3 m_GroundVelocity;
    Vector3 m_GroundAngularVelocity;
    bool m_NearGround;
    float m_GroundDist;

    float m_GoalFade;
    float m_CurrentFade;

    float m_JumpTimeLeft;
    bool m_AllowJump;

    float m_TimeLeftToAllowMidAirJump;

    GameObject m_VerticalLookPivot;
    float m_VerticalLookAngle;

    bool m_EnableMouseControl;

    UsableItem m_CurrentItem;
    int m_CurrentItemIndex;

    Vector3 m_GrappleLeanDir;

    float m_TimeLeftToPrepareAim;
    bool m_PreparingAim;
    float m_AimLayerWeight;
}
