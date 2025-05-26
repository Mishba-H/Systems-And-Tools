using UnityEngine;

public class ParkourController : MonoBehaviour, IControllerModule
{
    public Character character {  get; set; }

    private void Start()
    {
        character.characterDetector.climbCheckerHeight = climbHighRange.y + 0.1f;
        character.characterDetector.climbCheckerDepth = climbHighRange.y - climbLowRange.x;
    }

    private void Update()
    {
        if (character.characterDetector.DetectClimbPoint(out climbHit))
            CheckClimbPoint();
        else
        {
            climbLowAvailable = false;
            climbHighAvailable = false;
        }
    }

    public RaycastHit climbHit;
    public Vector3 climbPoint;
    public Vector2 climbLowRange;
    public Vector2 climbHighRange;
    public Vector3 climbLowOffset;
    public Vector3 climbHighOffset;
    public bool climbLowAvailable;
    public bool climbHighAvailable;
    private Vector3 initialPos;
    private RaycastHit wallHit;
    public void CheckClimbPoint()
    {
        climbPoint = climbHit.point;
        var climbPointHeight = climbPoint.y - transform.position.y;
        if (climbPointHeight > climbLowRange.x && climbPointHeight < climbLowRange.y)
        {
            climbLowAvailable = true;
            climbHighAvailable = false;
        }
        else if (climbPointHeight > climbHighRange.x && climbPointHeight < climbHighRange.y)
        {
            climbHighAvailable = true;
            climbLowAvailable = false;
        }
        else
        {
            climbLowAvailable = false;
            climbHighAvailable = false;
        }
    }
    private float timer = 0f;
    public void InitiateClimb()
    {
        wallHit = character.characterDetector.GetWallHit(transform.forward);
        timer = 0f;
        if (character.PerformingAction<ClimbOverLow>())
        {
            initialPos = new Vector3(wallHit.point.x, climbPoint.y + climbLowOffset.y, wallHit.point.z)
                + wallHit.normal * climbLowOffset.z;
        }
        else if (character.PerformingAction<ClimbOverHigh>())
        {
            initialPos = new Vector3(wallHit.point.x, climbPoint.y + climbHighOffset.y, wallHit.point.z)
                + wallHit.normal * climbHighOffset.z;
        }
    }
    public void HandleClimb()
    {
        if (character.animMachine.activeState.TryGetProperty<RootMotionCurvesProperty>(out AnimationStateProperty property))
        {
            RootMotionData curves = (RootMotionData)property.Value;
            float totalTime = curves.rootTZ.keys[curves.rootTZ.length - 1].time;

            AnimationCurve rootTYCurve = curves.rootTY;
            AnimationCurve rootTZCurve = curves.rootTZ;

            if (timer >= totalTime) return;
            var yDisplacement = rootTYCurve.Evaluate(timer);
            var zDisplacement = rootTZCurve.Evaluate(timer);

            timer += Time.fixedDeltaTime;
            var targetPos = initialPos + Vector3.up * yDisplacement + transform.forward * zDisplacement;
            transform.position = Vector3.Lerp(transform.position, targetPos, 0.2f);
            transform.forward = Vector3.Lerp(transform.forward, -wallHit.normal, 0.2f);
            character.rb.linearVelocity = Vector3.zero;
        }
    }
}