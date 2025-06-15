using System;
using Sciphone;
using UnityEngine;
#if UNITY_EDITOR
using Physics = Nomnom.RaycastVisualization.VisualPhysics;
#else
using Physics = UnityEngine.Physics;
#endif

public class ParkourController : MonoBehaviour, IControllerModule
{
    public Character character {  get; set; }

    [TabGroup("Climb Over From Ground")] public bool climbPointAvailable;
    [TabGroup("Climb Over From Ground")] public Vector2 climbLowRange = new Vector2(0.3f, 0.75f);
    [TabGroup("Climb Over From Ground")] public Vector2 climbMediumRange = new Vector2(0.75f, 1.25f);
    [TabGroup("Climb Over From Ground")] public Vector2 climbHighRange = new Vector2(1.25f, 2.25f);

    public RaycastHit climbHit;
    [TabGroup("Climb Over From Ground")] public Vector3 localClimbPoint;
    [TabGroup("Climb Over From Ground")] public float climbHeight;
    [TabGroup("Climb Over From Ground")] public float startingDistFromWall;

    [Header("Detector Settings")]
    [TabGroup("Climb Over From Ground")] public LayerMask climbLayer;
    [TabGroup("Climb Over From Ground")] public float climbCheckerDistance = 1f;
    [TabGroup("Climb Over From Ground")] public float climbCheckerInterval = 0.1f;
    [TabGroup("Climb Over From Ground")] public Vector2 climbCheckerOffset;

    public RaycastHit wallHit;
    [TabGroup("Climb Over From Ground")] public float wallCheckerInterval;
    [TabGroup("Climb Over From Ground")] public float wallCheckerDistance;

    public Vector3 scaleFactor = Vector3.one;
    public Vector3 initialPos;

    private void Start()
    {
        climbLowRange.x = character.characterMover.maxStepHeight;
        climbCheckerOffset.x = character.characterMover.capsuleRadius;
        climbCheckerOffset.y = climbHighRange.y + 0.1f;

        character.DetectionLoop += Character_DetectionLoop;
    }

    private void Character_DetectionLoop()
    {
        CheckClimbPoint();
    }

    public Vector3 GetWallNormal(Vector3 direction)
    {
        int count = Mathf.CeilToInt(climbHeight / wallCheckerInterval);
        var pos = transform.position + climbHeight * transform.up;
        for (int i = 0; i <= count; i++)
        {
            if (Physics.Raycast(pos + i * wallCheckerInterval * -transform.up, direction, out wallHit, wallCheckerDistance))
            {
                return Vector3.ProjectOnPlane(wallHit.normal, transform.up).normalized;
            }
        }
        return Vector3.zero;
    }

    public bool DetectClimbPoint(out RaycastHit climbHit)
    {
        climbHit = new RaycastHit();
        var pos = transform.position + transform.forward * climbCheckerOffset.x + transform.up * climbCheckerOffset.y;
        var climbCheckerCount = climbCheckerDistance / climbCheckerInterval;
        for (int i = 0; i <= climbCheckerCount ; i++)
        {
            if (Physics.Raycast(pos + climbCheckerInterval * i * transform.forward, -transform.up, out climbHit, climbCheckerOffset.y))
            {
                return true;
            }
        }
        return false;
    }

    public void CheckClimbPoint()
    {
        if (DetectClimbPoint(out climbHit))
        {
            localClimbPoint = transform.InverseTransformPoint(climbHit.point);
            climbHeight = localClimbPoint.y;
            if (climbHeight > climbLowRange.x && climbHeight < climbHighRange.y)
            {
                climbPointAvailable = true;
            }
        }
        else
        {
            climbPointAvailable = false;
            return;
        }
    }

    public void InitiateClimb()
    {
        CalculateScaleFactor();
        CalculateStartingDistanceFromWall();
        SetInitialTransform();
    }

    private void CalculateScaleFactor()
    {
        if (character.animMachine.activeState.TryGetProperty<RootMotionCurvesProperty>(out AnimationStateProperty property))
        {
            RootMotionData curves = (RootMotionData)property.Value;
            float totalTime = curves.rootTZ.keys[curves.rootTZ.length - 1].time;

            AnimationCurve rootTYCurve = curves.rootTY;

            float totalYDisp = rootTYCurve.Evaluate(totalTime) - rootTYCurve.Evaluate(0f);
            scaleFactor = new Vector3(1f, climbHeight / totalYDisp, 1f);
        }
    }

    private void CalculateStartingDistanceFromWall()
    {
        if (character.animMachine.activeState.TryGetProperty<RootMotionCurvesProperty>(out AnimationStateProperty property))
        {
            RootMotionData curves = (RootMotionData)property.Value;
            float totalTime = curves.rootTZ.keys[curves.rootTZ.length - 1].time;

            AnimationCurve rootTZCurve = curves.rootTZ;
            if (character.animMachine.activeState.TryGetData<TimeOfContactData>(out IAnimationData data))
            {
                var timeOfContact = character.animMachine.activeState.GetAdjustedNormalizedTime((data as TimeOfContactData).timeOfContact) * totalTime;
                startingDistFromWall = rootTZCurve.Evaluate(timeOfContact) - rootTZCurve.Evaluate(0f);
            }
        }
    }

    private void SetInitialTransform()
    {
        var wallNormal = GetWallNormal(transform.forward);
        initialPos = transform.position + startingDistFromWall * wallNormal - wallNormal * Vector3.Dot(transform.position - wallHit.point, wallNormal);
        transform.position = initialPos;
        transform.forward = -wallNormal;
    }

    public void HandleClimb(float dt)
    {
        Vector3 up = transform.up;
        Vector3 forward = transform.forward;
        Vector3 right = Vector3.Cross(up, forward).normalized;

        Vector3 rootDeltaPosition = character.animMachine.rootLinearVelocity * dt;
        Vector3 scaledDeltaPosition = new Vector3(rootDeltaPosition.x * scaleFactor.x, rootDeltaPosition.y * scaleFactor.y,
            rootDeltaPosition.z * scaleFactor.z);

        Vector3 worldDeltaPostition = scaledDeltaPosition.x * right + scaledDeltaPosition.y * up + scaledDeltaPosition.z * forward;

        transform.position += worldDeltaPostition;
    }
}