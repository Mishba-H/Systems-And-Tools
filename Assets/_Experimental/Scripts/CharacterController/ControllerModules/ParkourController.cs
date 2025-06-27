using Sciphone;
using UnityEngine;

//#if UNITY_EDITOR
//using Physics = Nomnom.RaycastVisualization.VisualPhysics;
//#else
//using Physics = UnityEngine.Physics;
//#endif

public class ParkourController : MonoBehaviour, IControllerModule
{
    public Character character {  get; set; }

    [TabGroup("Auto Traverse Options")][LeftToggle] public bool autoClimbOverFromGroundLow;
    [TabGroup("Auto Traverse Options")][LeftToggle] public bool autoClimbOverFromGroundMedium;
    [TabGroup("Auto Traverse Options")][LeftToggle] public bool autoClimbOverFromGroundHigh;
    [TabGroup("Auto Traverse Options")][LeftToggle] public bool autoVaultOverFenceLow;
    [TabGroup("Auto Traverse Options")][LeftToggle] public bool autoVaultOverFenceMedium;
    [TabGroup("Auto Traverse Options")][LeftToggle] public bool autoVaultOverFenceHigh;

    public RaycastHit climbHit;
    [TabGroup("Climb Over From Ground")] public bool climbAvailable;
    [TabGroup("Climb Over From Ground")] public Vector2 climbLowRange = new Vector2(0.3f, 0.75f);
    [TabGroup("Climb Over From Ground")] public Vector2 climbMediumRange = new Vector2(0.75f, 1.25f);
    [TabGroup("Climb Over From Ground")] public Vector2 climbHighRange = new Vector2(1.25f, 2.25f);
    [TabGroup("Climb Over From Ground")] public float climbHeight;
    [TabGroup("Climb Over From Ground")][Header("Detector Settings")] public LayerMask climbLayer;
    [TabGroup("Climb Over From Ground")] public float climbCheckerDistance = 1f;
    [TabGroup("Climb Over From Ground")] public Vector2 climbCheckerOffset;

    public RaycastHit fenceHit;
    [TabGroup("Vault Over Fence")] public bool fenceAvalilable;
    [TabGroup("Vault Over Fence")] public float fenceHeight;
    [TabGroup("Vault Over Fence")] public Vector2 vaultFenceLowRange;
    [TabGroup("Vault Over Fence")] public Vector2 vaultFenceMediumRange;
    [TabGroup("Vault Over Fence")] public Vector2 vaultFenceHighRange;
    [TabGroup("Vault Over Fence")][Header("Detector Settings")] public LayerMask fenceLayer;
    [TabGroup("Vault Over Fence")] public Vector2 fenceCheckerOffset;
    [TabGroup("Vault Over Fence")] public float fenceCheckerDistance = 1f;

    public RaycastHit wallHit;
    public float animHeight;
    public Vector3 scaleFactor = Vector3.one;
    public LayerMask wallLayer;
    public float startingDistFromWall;
    public float allCheckerInterval = 0.1f;
    public Vector3 targetPos;
    public Vector3 targetForward;

    private Vector3 worldMoveDir;
    public Vector3 parkourDir;

    private void Start()
    {
        climbLowRange.x = character.characterMover.maxStepHeight;
        climbCheckerOffset.x = character.characterMover.capsuleRadius;
        climbCheckerOffset.y = climbHighRange.y + character.characterMover.skinWidth;

        vaultFenceLowRange.x = character.characterMover.maxStepHeight;
        fenceCheckerOffset.x = character.characterMover.capsuleRadius;
        fenceCheckerOffset.y = vaultFenceHighRange.y + character.characterMover.skinWidth;

        character.PreUpdateLoop += Character_PreUpdateLoop;
        character.characterCommand.MoveDirCommand += CharacterCommand_MoveDirCommand;
    }

    private void CharacterCommand_MoveDirCommand(Vector3 vector)
    {
        worldMoveDir = vector;
    }

    private void Character_PreUpdateLoop()
    {
        parkourDir = worldMoveDir == Vector3.zero ? transform.forward : worldMoveDir;
        CheckClimbAvailability(parkourDir);
        CheckFenceAvailability(parkourDir);
    }

    public void CheckClimbAvailability(Vector3 direction)
    {
        if (DetectClimbPoint(direction, out climbHit))
        {
            Vector3 localClimbPoint = transform.InverseTransformPoint(climbHit.point);
            climbHeight = localClimbPoint.y;
            if (climbHeight > climbLowRange.x && climbHeight < climbHighRange.y && 
                DetectWall(direction, climbCheckerDistance + character.characterMover.skinWidth, climbHeight, out wallHit))

            {
                climbAvailable = true;
            }
        }
        else
        {
            climbAvailable = false;
        }
    }

    public bool DetectClimbPoint(Vector3 direction, out RaycastHit climbHit)
    {
        var pos = transform.position + direction * climbCheckerOffset.x + transform.up * climbCheckerOffset.y;
        var climbCheckerCount = Mathf.FloorToInt((climbCheckerDistance - character.characterMover.capsuleRadius) / allCheckerInterval);
        for (int i = 0; i <= climbCheckerCount; i++)
        {
            if (Physics.Raycast(pos + allCheckerInterval * i * direction, -transform.up, out climbHit, climbHighRange.y + character.characterMover.skinWidth,
                climbLayer, QueryTriggerInteraction.Ignore)
                && Vector3.Angle(climbHit.normal, transform.up) < character.characterMover.criticalSlopeAngle)
            {
                return true;
            }
        }
        climbHit = new RaycastHit();
        return false;
    }

    public bool DetectWall(Vector3 direction, float distance, float height, out RaycastHit wallHit)
    {
        int count = Mathf.FloorToInt(height / allCheckerInterval);
        var pos = transform.position + climbHeight * transform.up;
        for (int i = 0; i <= count; i++)
        {
            if (Physics.Raycast(pos + i * allCheckerInterval * -transform.up, direction, out wallHit, distance,
                wallLayer, QueryTriggerInteraction.Ignore)
                && Vector3.Angle(wallHit.normal, transform.up) > character.characterMover.criticalSlopeAngle)
            {
                return true;
            }
        }
        wallHit = new RaycastHit();
        return false;
    }

    public void CheckFenceAvailability(Vector3 direction)
    {
        if (DetectFence(direction, out fenceHit))
        {
            Vector3 localFenceHitPoint = transform.InverseTransformPoint(fenceHit.point);
            fenceHeight = localFenceHitPoint.y;
            if (fenceHeight > vaultFenceLowRange.x && fenceHeight <= vaultFenceLowRange.y ||
                fenceHeight > vaultFenceMediumRange.x && fenceHeight <= vaultFenceMediumRange.y ||
                fenceHeight > vaultFenceHighRange.x && fenceHeight <= vaultFenceHighRange.y)
            {
                if (DetectWall(direction, fenceCheckerDistance + character.characterMover.skinWidth, fenceHeight, out wallHit))
                {
                    fenceAvalilable = true;
                }
            }
        }
        else
        {
            fenceAvalilable = false;
        }
    }

    public bool DetectFence(Vector3 direction, out RaycastHit fenceHit)
    {
        var pos = transform.position + direction * fenceCheckerOffset.x + transform.up * fenceCheckerOffset.y;
        var fenceCheckerCount = Mathf.FloorToInt((fenceCheckerDistance - character.characterMover.capsuleRadius) / allCheckerInterval);
        for (int i = 0; i <= fenceCheckerCount; i++)
        {
            if (Physics.Raycast(pos + allCheckerInterval * i * direction, -transform.up, out fenceHit, fenceCheckerOffset.y, fenceLayer, QueryTriggerInteraction.Ignore)
                && Vector3.Angle(fenceHit.normal, transform.up) < character.characterMover.criticalSlopeAngle)
            {
                return true;
            }
        }
        fenceHit = new RaycastHit();
        return false;
    }

    public void CalculateScaleFactorAndStartingDistance(float targetHeight)
    {
        if (character.animMachine.activeState.TryGetProperty<RootMotionCurvesProperty>(out var rootMotionProp)
            && character.animMachine.activeState.TryGetProperty<ScaleModeProperty>(out var scaleModeProp))
        {
            scaleFactor = AnimationMachineExtensions.EvaluateScaleFactor(rootMotionProp as RootMotionCurvesProperty, scaleModeProp as ScaleModeProperty);
            animHeight = scaleFactor.y;
            scaleFactor = scaleFactor.With(y: targetHeight / scaleFactor.y);
        }

        if (character.animMachine.activeState.TryGetProperty<RootMotionCurvesProperty>(out AnimationStateProperty property))
        {
            RootMotionData curves = ((RootMotionCurvesProperty)property).rootMotionData;
            float totalTime = curves.totalTime;

            if (character.animMachine.activeState.TryGetData<TimeOfContact>(out IAnimationData data))
            {
                var timeOfContact = (data as TimeOfContact).timeOfContact * totalTime;

                AnimationCurve rootTZCurve = curves.rootTZ;
                startingDistFromWall = rootTZCurve.Evaluate(timeOfContact) - rootTZCurve.Evaluate(0f);
            }
        }
    }

    public void SetInitialTransform(RaycastHit hitInfo, float totalHeight)
    {
        Vector3 wallNormalOnPlane = Vector3.ProjectOnPlane(wallHit.normal, transform.up).normalized;
        var localHitPoint = transform.InverseTransformPoint(hitInfo.point);
        var localWallPoint = transform.InverseTransformPoint(wallHit.point);
        var localAnchorPoint = new Vector3(localWallPoint.x, localHitPoint.y, localWallPoint.z);
        var anchorPoint = transform.TransformPoint(localAnchorPoint);

        targetPos = anchorPoint - totalHeight * transform.up + startingDistFromWall * wallNormalOnPlane;
        targetForward = -wallNormalOnPlane;
        character.characterMover.SetWorldVelocity(Vector3.zero);
        character.characterMover.SetGravitySimulation(false);
    }

    public void HandleParkourMovement(float dt)
    {
        Vector3 up = transform.up;
        Vector3 forward = targetForward;
        Vector3 right = Vector3.Cross(up, forward).normalized;

        Vector3 rootDeltaPosition = character.animMachine.rootLinearVelocity * dt;
        Vector3 scaledDeltaPosition = new Vector3(rootDeltaPosition.x * scaleFactor.x, rootDeltaPosition.y * scaleFactor.y,
            rootDeltaPosition.z * scaleFactor.z);

        Vector3 worldDeltaPostition = scaledDeltaPosition.x * right + scaledDeltaPosition.y * up + scaledDeltaPosition.z * forward;

        targetPos += worldDeltaPostition;
        transform.position = targetPos;
        character.characterMover.SetFaceDir(targetForward);
    }
}