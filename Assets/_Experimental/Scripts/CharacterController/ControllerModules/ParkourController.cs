using Sciphone;
using UnityEngine;
using UnityEngine.UIElements;

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
    [TabGroup("Auto Traverse Options")][LeftToggle] public bool autoAscendLadder;
    [TabGroup("Auto Traverse Options")][LeftToggle] public bool autoDescendLadder;

    public RaycastHit climbHit;
    [TabGroup("Climb Over From Ground")] public bool climbAvailable;
    [TabGroup("Climb Over From Ground")] public float climbHeight;
    [TabGroup("Climb Over From Ground")][Header("Detector Settings")] public LayerMask climbLayer;
    [TabGroup("Climb Over From Ground")] public float climbOverFromGroundRadius;
    [TabGroup("Climb Over From Ground")] public Vector2 climbLowRange = new Vector2(0.3f, 0.75f);
    [TabGroup("Climb Over From Ground")] public Vector2 climbMediumRange = new Vector2(0.75f, 1.25f);
    [TabGroup("Climb Over From Ground")] public Vector2 climbHighRange = new Vector2(1.25f, 2.25f);
    internal Vector3 climbNormal;

    public RaycastHit fenceHit;
    [TabGroup("Vault Over Fence")] public bool fenceAvalilable;
    [TabGroup("Vault Over Fence")] public float fenceHeight;
    [TabGroup("Vault Over Fence")][Header("Detector Settings")] public LayerMask fenceLayer;
    [TabGroup("Vault Over Fence")] public float vaultOverFenceRadius = 1f;
    [TabGroup("Vault Over Fence")] public Vector2 vaultFenceLowRange;
    [TabGroup("Vault Over Fence")] public Vector2 vaultFenceMediumRange;
    [TabGroup("Vault Over Fence")] public Vector2 vaultFenceHighRange;
    [TabGroup("Vault Over Fence")] public float criticalFenceWidth = 0.3f;
    internal Vector3 fenceNormal;

    [TabGroup("Ladder Traversal")] public RaycastHit ladderHit;
    [TabGroup("Ladder Traversal")] public LadderTraverseStates ladderState;
    [TabGroup("Ladder Traversal")] public bool ladderAscendAvailable;
    [TabGroup("Ladder Traversal")] public bool ladderDescendAvailable;
    [TabGroup("Ladder Traversal")] public bool ladderAtFeetAvailable;
    [TabGroup("Ladder Traversal")] public bool ladderAtHeadAvailable;
    [TabGroup("Ladder Traversal")] public LayerMask ladderLayer;
    [TabGroup("Ladder Traversal")] public float distFromLadder = 0.75f;
    [TabGroup("Ladder Traversal")] public Vector3 ladderCenterAtHeight;
    [TabGroup("Ladder Traversal")] public Vector3 ladderUp;
    [TabGroup("Ladder Traversal")] public float ladderTraverseRadius = 1f;
    [TabGroup("Ladder Traversal")] public float minLadderHeight = 1.5f;
    [TabGroup("Ladder Traversal")] public float minLadderWidth = 1f;
    [TabGroup("Ladder Traversal")] public float ladderClimbUpEndHeight = 1.4f;
    [TabGroup("Ladder Traversal")] public float ladderClimbDownEndHeight = 0.4f;
    [TabGroup("Ladder Traversal")] public float ladderChecerRadius = 0.1f;
    [TabGroup("Ladder Traversal")] public int ladderCheckerCount = 3;
    internal Vector3 ladderNormal;
    public enum LadderTraverseStates
    {
        None,
        ClimbUpStart,
        ClimbUpEnd,
        ClimbDownStart,
        ClimbDownEnd,
        Idle,
        ClimbUp,
        ClimbDown,
        SlideDown
    }

    public LayerMask allParkourLayer;
    public float criticalParkourAngle;
    public float surroundingCheckerRadius = 3f;
    private int surroundingCollidersCount;
    private Collider[] surroundingColliders;
    private Vector3[] toSurroundingColliders;
    public float allCheckerInterval = 0.1f;

    public Vector3 scaleFactor = Vector3.one;
    public float startingDistFromWall;
    private Vector3 targetPos;
    private Vector3 worldMoveDir;
    public Vector3 worldParkourDir;

    private void Start()
    {
        character.PreUpdateLoop += Character_PreUpdateLoop;
        character.characterCommand.MoveDirCommand += CharacterCommand_MoveDirCommand;
        character.characterCommand.ParkourDirCommand += CharacterCommand_ParkourDirCommand;

        surroundingColliders = new Collider[15];
        toSurroundingColliders = new Vector3[15];

        climbLowRange.x = character.characterMover.maxStepHeight;
        vaultFenceLowRange.x = character.characterMover.maxStepHeight;
    }

    private void CharacterCommand_ParkourDirCommand(Vector2 dir)
    {
        worldParkourDir = (transform.right * dir.x + transform.up * dir.y).normalized;
    }

    private void CharacterCommand_MoveDirCommand(Vector3 vector)
    {
        worldMoveDir = vector;
    }

    private void Character_PreUpdateLoop()
    {
        GetSurroundingSurfaces(transform.position, surroundingCheckerRadius);
        CheckClimbAvailability(worldMoveDir);
        CheckFenceAvailability(worldMoveDir);
        if (character.PerformingAction<LadderTraverse>())
        {
            CheckLadderAvailability(transform.forward);
        }
        else
        {
            CheckLadderAvailability(worldMoveDir);
        }
    }

    public void GetSurroundingSurfaces(Vector3 center, float radius)
    {
        surroundingCollidersCount = Physics.OverlapSphereNonAlloc(center, radius, surroundingColliders, allParkourLayer);

        for (int i = 0; i < surroundingCollidersCount; i++)
        {
            Collider other = surroundingColliders[i];
            if (!other.IsConvexMesh())
            {
                surroundingColliders[i] = null;
                toSurroundingColliders[i] = Vector3.zero;
                continue;
            }

            Vector3 toOther = (other.ClosestPoint(center) - center);

            if (toOther == Vector3.zero)
            {
                surroundingColliders[i] = null;
                toSurroundingColliders[i] = Vector3.zero;
                continue;
            }
            toSurroundingColliders[i] = toOther;
        }

        for (int i = surroundingCollidersCount; i < toSurroundingColliders.Length; i++)
        {
            toSurroundingColliders[i] = Vector3.zero;
        }
    }

    public bool ObstructionPresent(Vector3 start, Vector3 end)
    {
        var startPoint = start - end * character.characterMover.capsuleRadius;
        var endPoint = start + end;
        return Physics.Linecast(startPoint, endPoint, allParkourLayer, QueryTriggerInteraction.Ignore);
    }

    public void CheckClimbAvailability(Vector3 direction)
    {
        if (direction == Vector3.zero)
        {
            climbAvailable = false;
            return;
        }

        for (int i = 0; i < surroundingCollidersCount; i++)
        {
            Collider other = surroundingColliders[i];
            Vector3 toOther = toSurroundingColliders[i];
            Vector3 toOtherOnPlane = Vector3.ProjectOnPlane(toOther, transform.up);

            if (other == null)
            {
                continue;
            }

            if (climbLayer.Contains(other.gameObject.layer) && toOtherOnPlane.magnitude <= climbOverFromGroundRadius &&
                Vector3.Angle(Vector3.ProjectOnPlane(toOther, transform.up), direction) <= criticalParkourAngle &&
                !ObstructionPresent(transform.position, toOther))
            {
                if (DetectClimbPoint(toOtherOnPlane, out RaycastHit climbHit))
                {
                    climbHeight = transform.InverseTransformPoint(climbHit.point).y;
                    if (climbHeight > climbLowRange.x && climbHeight < climbHighRange.y &&
                        DetectWall(toOtherOnPlane, climbOverFromGroundRadius, climbHeight, out RaycastHit wallHit))
                    {
                        this.climbHit = climbHit;
                        climbNormal = Vector3.ProjectOnPlane(wallHit.normal, transform.up).normalized;
                        climbAvailable = true;
                        return;
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
                climbHighRange.y - climbLowRange.x + character.characterMover.skinWidth,
                climbLayer, QueryTriggerInteraction.Ignore) 
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
        if (direction == Vector3.zero)
        {
            fenceAvalilable = false;
            return;
        }

        for (int i = 0; i < surroundingCollidersCount; i++)
        {
            Collider other = surroundingColliders[i];
            Vector3 toOther = toSurroundingColliders[i];
            Vector3 toOtherOnPlane = Vector3.ProjectOnPlane(toOther, transform.up);

            if (other == null)
            {
                continue;
            }

            if (fenceLayer.Contains(other.gameObject.layer) && toOtherOnPlane.magnitude <= vaultOverFenceRadius &&
                Vector3.Angle(Vector3.ProjectOnPlane(toOther, transform.up), direction) <= criticalParkourAngle &&
                !ObstructionPresent(transform.position, toOther))
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
                        fenceNormal = Vector3.ProjectOnPlane(wallHit.normal, transform.up).normalized;
                        fenceAvalilable = true;
                        return;
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

    private void CheckLadderAvailability(Vector3 direction)
    {
        if (direction == Vector3.zero)
        {
            ladderAscendAvailable = false;
            ladderDescendAvailable = false;
            ladderAtFeetAvailable = false;
            ladderAtHeadAvailable = false;
            return;
        }

        for (int i = 0; i < surroundingCollidersCount; i++)
        {
            Collider other = surroundingColliders[i];
            Vector3 toOther = toSurroundingColliders[i];
            Vector3 toOtherOnPlane = Vector3.ProjectOnPlane(toOther, transform.up);

            if (other == null)
            {
                continue;
            }

            if (ladderLayer.Contains(other.gameObject.layer) && Vector3.Angle(Vector3.ProjectOnPlane(toOther, transform.up), direction) <= criticalParkourAngle
                && toOtherOnPlane.magnitude <= ladderTraverseRadius)
            {
                Vector3 centerAtHeight = GetLadderCenterAtHeight(other);

                Vector3 ladderUp = other.transform.up;
                Vector3 ladderFwd = other.transform.forward;
                Vector3 ladderRight = other.transform.right;

                if (Vector3.Angle(ladderFwd, transform.up) < 15f)
                {
                    ladderAscendAvailable = false;
                    ladderDescendAvailable = false;
                    ladderAtFeetAvailable = false;
                    ladderAtHeadAvailable = false;
                    return;
                }

                if (!character.PerformingAction<LadderTraverse>())
                {
                    bool centerFound = DetectLadder(centerAtHeight, other);
                    bool topFound = DetectLadder(centerAtHeight + ladderUp * minLadderHeight, other);
                    bool bottomFound = DetectLadder(centerAtHeight - ladderUp * minLadderHeight, other);

                    ladderAscendAvailable = centerFound && topFound;
                    ladderDescendAvailable = centerFound && bottomFound;

                    ladderAtFeetAvailable = false;
                    ladderAtHeadAvailable = false;
                }
                else
                {
                    ladderAtFeetAvailable = DetectLadder(centerAtHeight - ladderUp * ladderClimbDownEndHeight, other);
                    ladderAtHeadAvailable = DetectLadder(centerAtHeight + ladderUp * ladderClimbUpEndHeight, other);

                    ladderAscendAvailable = false;
                    ladderDescendAvailable = false;
                }


                if (ladderAscendAvailable || ladderDescendAvailable || ladderAtFeetAvailable || ladderAtHeadAvailable)
                {
                    this.ladderUp = ladderUp;
                    ladderNormal = ladderFwd;
                    ladderCenterAtHeight = centerAtHeight;
                    return;
                }
            }
        }

        ladderAscendAvailable = false;
        ladderDescendAvailable = false;
        ladderAtFeetAvailable = false;
        ladderAtHeadAvailable = false;
    }

    private Vector3 GetLadderCenterAtHeight(Collider ladderCollider)
    {
        Vector3 boundsCenter = ladderCollider.bounds.center;
        var angleDiff = Vector3.Angle(ladderCollider.transform.up, transform.up);
        var heightDiff = transform.position.y - boundsCenter.y;
        var centerAtHeight = boundsCenter + heightDiff * Mathf.Cos(angleDiff * Mathf.Deg2Rad) * ladderCollider.transform.up;
        return centerAtHeight;
    }

    public bool DetectLadder(Vector3 position, Collider ladderCollider)
    {
        Vector3 ladderUp = ladderCollider.transform.up;
        Vector3 ladderFwd = ladderCollider.transform.forward;
        Vector3 ladderRight = ladderCollider.transform.right;
        float halfWidth = minLadderWidth / 2f;

        for (int i = 0; i < ladderCheckerCount; i++)
        {
            float t = (ladderCheckerCount == 1) ? 0f : i / (float)(ladderCheckerCount - 1);
            float offset = Mathf.Lerp(-halfWidth, halfWidth, t);
            var rayStart = position + ladderRight * offset - ladderFwd * ladderTraverseRadius * 0.5f;
            if (!Physics.SphereCast(new Ray(rayStart, ladderFwd), ladderChecerRadius, ladderTraverseRadius, ladderLayer, QueryTriggerInteraction.Ignore))
            {
                return false;
            }
        }
        return true;
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

    public void CalculateScaleFactor()
    {
        Vector3 targetValue = Vector3.zero;
        if (character.PerformingAction<ClimbOverFromGround>())
        {
            targetValue = new Vector3(0f, climbHeight, 0f);
        }
        else if (character.PerformingAction<VaultOverFence>())
        {
            targetValue = new Vector3(0f, fenceHeight, 0f);
        }
        else if (character.PerformingAction<LadderTraverse>())
        {
            targetValue = Vector3.zero;
        }

        if (character.animMachine.rootState.TryGetProperty<RootMotionCurvesProperty>(out var rootMotionProp)
            && character.animMachine.rootState.TryGetProperty<ScaleModeProperty>(out var scaleModeProp))
        {
            scaleFactor = AnimationMachineExtensions.EvaluateScaleFactor(rootMotionProp, scaleModeProp, targetValue);
        }
    }

    public void CalculateStartingDistanceFromAnchor()
    {
        if (character.animMachine.rootState.TryGetData(out StartingDistanceFromWall distData))
        {
            startingDistFromWall = distData.dist.z;
            return;
        }

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
            return;
        }
    }

    public void SetInitialTransform(Vector3 anchorPoint, Vector3 normal, float anchorHeight, float anchorDistance)
    {
        targetPos = anchorPoint - anchorHeight * transform.up + anchorDistance * normal;

        character.characterMover.TargetMatching(targetPos, -normal, true);
    }

    public void RunLadderStateMachine()
    {
        if (ladderState == LadderTraverseStates.None || ladderState == LadderTraverseStates.Idle || ladderState == LadderTraverseStates.ClimbUp ||
            ladderState == LadderTraverseStates.ClimbDown || ladderState == LadderTraverseStates.SlideDown)
        {
            if (worldParkourDir == Vector3.zero)
            {
                ChangeLadderState(LadderTraverseStates.Idle);
            }
            else if (Vector3.Angle(worldParkourDir, transform.up) < 60f)
            {
                if (ladderAtFeetAvailable && !ladderAtHeadAvailable)
                {
                    ChangeLadderState(LadderTraverseStates.ClimbUpEnd);
                }
                else
                {
                    ChangeLadderState(LadderTraverseStates.ClimbUp);
                }
            }
            else if (Vector3.Angle(worldParkourDir, transform.up) > 120f)
            {
                if (!ladderAtFeetAvailable && ladderAtHeadAvailable)
                {
                    ChangeLadderState(LadderTraverseStates.ClimbDownEnd);
                }
                else
                {
                    ChangeLadderState(LadderTraverseStates.ClimbDown);
                }
            }
        }
        else if (ladderState == LadderTraverseStates.ClimbUpStart || ladderState == LadderTraverseStates.ClimbDownStart )
        {
            if (Mathf.Abs(character.animMachine.rootState.NormalizedTime() - 1f) < 0.01f)
            {
                ChangeLadderState(LadderTraverseStates.Idle);
            }
        }

        HandleParkourMovement(ladderUp, -ladderNormal, Time.deltaTime * character.timeScale);
    }

    public void ChangeLadderState(LadderTraverseStates newState)
    {
        if (ladderState != newState)
        {
            ladderState = newState;

            if (newState == LadderTraverseStates.None)
                return;

            HandleLadderAnimation();
            CalculateScaleFactor();
            CalculateStartingDistanceFromAnchor();
            SetInitialTransform(ladderCenterAtHeight, ladderNormal, 0f, startingDistFromWall);
        }
    }

    public void HandleLadderAnimation()
    {
        switch (ladderState)
        {
            case LadderTraverseStates.ClimbUpStart:
                character.characterAnimator.ChangeAnimationState("LadderClimbUpStart", "Parkour");
                break;
            case LadderTraverseStates.ClimbUpEnd:
                character.characterAnimator.ChangeAnimationState("LadderClimbUpEnd", "Parkour");
                break;
            case LadderTraverseStates.ClimbDownStart:
                character.characterAnimator.ChangeAnimationState("LadderClimbDownStart", "Parkour");
                break;
            case LadderTraverseStates.ClimbDownEnd:
                character.characterAnimator.ChangeAnimationState("LadderClimbDownEnd", "Parkour");
                break;
            case LadderTraverseStates.Idle:
                character.characterAnimator.ChangeAnimationState("LadderIdle", "Parkour");
                break;
            case LadderTraverseStates.ClimbUp:
                character.characterAnimator.ChangeAnimationState("LadderClimbUpLoop", "Parkour");
                break;
            case LadderTraverseStates.ClimbDown:
                character.characterAnimator.ChangeAnimationState("LadderClimbDownLoop", "Parkour");
                break;
            //case LadderTraverseStates.SlideDown:
            //    character.characterAnimator.ChangeAnimationState("", "Parkour");
            //    break;
        }
    }

    public void HandleParkourMovement(Vector3 up, Vector3 forward, float dt)
    {
        Vector3 right = Vector3.Cross(up, forward).normalized;

        Vector3 rootDeltaPosition = character.animMachine.rootLinearVelocity * dt;
        Vector3 scaledDeltaPosition = new Vector3(rootDeltaPosition.x * scaleFactor.x, rootDeltaPosition.y * scaleFactor.y,
            rootDeltaPosition.z * scaleFactor.z);

        Vector3 worldDeltaPostition = scaledDeltaPosition.x * right + scaledDeltaPosition.y * up + scaledDeltaPosition.z * forward;

        targetPos += worldDeltaPostition;
        character.characterMover.TargetMatching(targetPos, forward, false);
    }
}