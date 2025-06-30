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
    [TabGroup("Climb Over From Ground")] public float climbHeight;
    [TabGroup("Climb Over From Ground")] public Vector2 climbLowRange = new Vector2(0.3f, 0.75f);
    [TabGroup("Climb Over From Ground")] public Vector2 climbMediumRange = new Vector2(0.75f, 1.25f);
    [TabGroup("Climb Over From Ground")] public Vector2 climbHighRange = new Vector2(1.25f, 2.25f);
    [TabGroup("Climb Over From Ground")][Header("Detector Settings")] public LayerMask climbLayer;
    [TabGroup("Climb Over From Ground")] public float climbOverFromGroundRadius;

    public RaycastHit fenceHit;
    [TabGroup("Vault Over Fence")] public bool fenceAvalilable;
    [TabGroup("Vault Over Fence")] public float fenceHeight;
    [TabGroup("Vault Over Fence")] public Vector2 vaultFenceLowRange;
    [TabGroup("Vault Over Fence")] public Vector2 vaultFenceMediumRange;
    [TabGroup("Vault Over Fence")] public Vector2 vaultFenceHighRange;
    [TabGroup("Vault Over Fence")][Header("Detector Settings")] public LayerMask fenceLayer;
    [TabGroup("Vault Over Fence")] public float vaultOverFenceRadius = 1f;
    [TabGroup("Vault Over Fence")] public float criticalFenceWidth = 0.3f;

    public RaycastHit ladderHit;
    public bool ladderAvailable;

    public LayerMask allParkourLayer;
    public float criticalParkourAngle;
    public float surroundingCheckerRadius = 3f;
    private int surroundingCollidersCount;
    private Collider[] surroundingColliders;
    private Vector3[] toSurroundingColliders;
    public float allCheckerInterval = 0.1f;
    private RaycastHit wallHit;

    public Vector3 scaleFactor = Vector3.one;
    public float startingDistFromWall;
    private Vector3 targetPos;
    private Vector3 targetForward;
    private Vector3 worldMoveDir;
    public Vector3 parkourDir;

    private void Start()
    {
        character.PreUpdateLoop += Character_PreUpdateLoop;
        character.characterCommand.MoveDirCommand += CharacterCommand_MoveDirCommand;

        surroundingColliders = new Collider[15];
        toSurroundingColliders = new Vector3[15];

        climbLowRange.x = character.characterMover.maxStepHeight;
        vaultFenceLowRange.x = character.characterMover.maxStepHeight;
    }

    private void CharacterCommand_MoveDirCommand(Vector3 vector)
    {
        worldMoveDir = vector;
    }

    private void Character_PreUpdateLoop()
    {
        parkourDir = worldMoveDir == Vector3.zero ? transform.forward : worldMoveDir;
        GetSurroundingSurfaces(transform.position, surroundingCheckerRadius);
        CheckClimbAvailability(parkourDir);
        CheckFenceAvailability(parkourDir);
    }

    public void GetSurroundingSurfaces(Vector3 center, float radius)
    {
        surroundingCollidersCount = Physics.OverlapSphereNonAlloc(center, radius, surroundingColliders, allParkourLayer);

        for (int i = 0; i < surroundingCollidersCount; i++)
        {
            Collider other = surroundingColliders[i];
            Vector3 toOther = (other.ClosestPoint(center) - center);
            toSurroundingColliders[i] = toOther;
        }

        for (int i = surroundingCollidersCount; i < toSurroundingColliders.Length; i++)
        {
            toSurroundingColliders[i] = Vector3.zero;
        }
    }

    public void CheckClimbAvailability(Vector3 direction)
    {
        for (int i = 0; i < surroundingCollidersCount; i++)
        {
            Collider other = surroundingColliders[i];
            Vector3 toOther = toSurroundingColliders[i];
            Vector3 toOtherOnPlane = Vector3.ProjectOnPlane(toOther, transform.up);

            if (toOther == Vector3.zero || toOtherOnPlane.magnitude > climbOverFromGroundRadius)
            {
                continue;
            }

            if (climbLayer.Contains(other.gameObject.layer))
            {
                if (Vector3.Angle(Vector3.ProjectOnPlane(toOther, transform.up), direction) <= criticalParkourAngle)
                {
                    if (DetectClimbPoint(toOtherOnPlane, out RaycastHit climbHit))
                    {
                        climbHeight = transform.InverseTransformPoint(climbHit.point).y;
                        if (climbHeight > climbLowRange.x && climbHeight < climbHighRange.y &&
                            DetectWall(toOtherOnPlane, climbOverFromGroundRadius, climbHeight, out RaycastHit wallHit))
                        {
                            this.climbHit = climbHit;
                            this.wallHit = wallHit;
                            climbAvailable = true;
                            return;
                        }
                    }
                }
            }
        }
        climbAvailable = false;
    }

    public bool DetectClimbPoint(Vector3 direction, out RaycastHit climbHit)
    {
        direction.Normalize();
        var pos = transform.position + transform.up * climbHighRange.y;
        var climbCheckerCount = Mathf.CeilToInt(climbOverFromGroundRadius / allCheckerInterval);
        for (int i = 0; i < climbCheckerCount; i++)
        {
            if (Physics.Raycast(pos + allCheckerInterval * i * direction, -transform.up, out climbHit, 
                climbHighRange.y - climbLowRange.x + character.characterMover.skinWidth, climbLayer, QueryTriggerInteraction.Ignore) 
                && Vector3.Angle(climbHit.normal, transform.up) < character.characterMover.criticalSlopeAngle)
            {
                return true;
            }
        }
        climbHit = new RaycastHit();
        return false;
    }

    public void CheckFenceAvailability(Vector3 direction)
    {
        for (int i = 0; i < surroundingCollidersCount; i++)
        {
            Collider other = surroundingColliders[i];
            Vector3 toOther = toSurroundingColliders[i];
            Vector3 toOtherOnPlane = Vector3.ProjectOnPlane(toOther, transform.up);

            if (toOther == Vector3.zero || toOtherOnPlane.magnitude > vaultOverFenceRadius)
            {
                continue;
            }

            if (fenceLayer.Contains(other.gameObject.layer))
            {
                if (Vector3.Angle(Vector3.ProjectOnPlane(toOther, transform.up), direction) <= criticalParkourAngle)
                {
                    if (DetectFence(toOtherOnPlane, out RaycastHit fenceHit, out Vector3 fenceCheckerPosOnHit))
                    {
                        fenceHeight = transform.InverseTransformPoint(fenceHit.point).y;
                        if ((fenceHeight > vaultFenceLowRange.x && fenceHeight <= vaultFenceLowRange.y ||
                            fenceHeight > vaultFenceMediumRange.x && fenceHeight <= vaultFenceMediumRange.y ||
                            fenceHeight > vaultFenceHighRange.x && fenceHeight <= vaultFenceHighRange.y) &&
                            DetectWall(toOtherOnPlane, vaultOverFenceRadius, fenceHeight, out RaycastHit wallHit) &&
                            !Physics.Raycast(fenceCheckerPosOnHit + Vector3.ProjectOnPlane(-wallHit.normal, transform.up) * criticalFenceWidth,
                            -transform.up, fenceHit.distance + character.characterMover.skinWidth, fenceLayer, QueryTriggerInteraction.Ignore))
                        {
                            this.fenceHit = fenceHit;
                            this.wallHit = wallHit;
                            fenceAvalilable = true;
                            return;
                        }
                    }
                }
            }
        }
        fenceAvalilable = false;
    }

    public bool DetectFence(Vector3 direction, out RaycastHit fenceHit, out Vector3 fenceCheckerPosOnHit)
    {
        direction.Normalize();
        var pos = transform.position + transform.up * vaultFenceHighRange.y;
        var fenceCheckerCount = Mathf.CeilToInt(vaultOverFenceRadius / allCheckerInterval);
        for (int i = 0; i < fenceCheckerCount; i++)
        {
            if (Physics.Raycast(pos + allCheckerInterval * i * direction, -transform.up, out fenceHit, 
                vaultFenceHighRange.y - vaultFenceLowRange.x + character.characterMover.skinWidth,
                fenceLayer, QueryTriggerInteraction.Ignore) &&
                Vector3.Angle(fenceHit.normal, transform.up) < character.characterMover.criticalSlopeAngle)
            {
                fenceCheckerPosOnHit = pos + allCheckerInterval * i * direction;
                return true;
            }
        }
        fenceCheckerPosOnHit = Vector3.zero;
        fenceHit = new RaycastHit();
        return false;
    }

    public bool DetectWall(Vector3 direction, float maxDistance, float maxHeight, out RaycastHit wallHit)
    {
        direction.Normalize();
        int count = Mathf.CeilToInt(maxHeight / allCheckerInterval);
        var pos = transform.position + maxHeight * transform.up;
        for (int i = 0; i < count; i++)
        {
            if (Physics.Raycast(pos + i * allCheckerInterval * -transform.up, direction, out wallHit, maxDistance,
                allParkourLayer, QueryTriggerInteraction.Ignore)
                && Vector3.Angle(wallHit.normal, transform.up) > character.characterMover.criticalSlopeAngle)
            {
                return true;
            }
        }
        wallHit = new RaycastHit();
        return false;
    }

    public void CalculateScaleFactor(float targetHeight)
    {
        if (character.animMachine.rootState.TryGetProperty<RootMotionCurvesProperty>(out var rootMotionProp)
            && character.animMachine.rootState.TryGetProperty<ScaleModeProperty>(out var scaleModeProp))
        {
            scaleFactor = AnimationMachineExtensions.EvaluateScaleFactor(rootMotionProp, scaleModeProp, 
                new Vector3(0f, targetHeight, 0f));
        }
    }

    public void CalculateStartingDistanceFromWall()
    {
        if (character.animMachine.rootState.TryGetProperty(out RootMotionCurvesProperty property))
        {
            RootMotionData curves = property.rootMotionData;
            float totalTime = curves.totalTime;

            if (character.animMachine.rootState.TryGetData(out TimeOfContact data))
            {
                var timeOfContact = data.timeOfContact * totalTime;

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
        character.characterMover.TargetMatching(targetPos, targetForward, true);
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
        character.characterMover.TargetMatching(targetPos, targetForward, false);
    }
}