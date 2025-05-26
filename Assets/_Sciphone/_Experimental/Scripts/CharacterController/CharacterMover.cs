using Nomnom.RaycastVisualization;
using Sciphone;
using UnityEngine;
using UnityEngine.TextCore.Text;
using Physics = Nomnom.RaycastVisualization.VisualPhysics;

public class CharacterMover : MonoBehaviour
{
    public Character character;

    public LayerMask groundLayer;
    public int sides = 8;
    public int noOfLayers = 3;
    public float groundCheckerHeight = 1f;
    public float groundCheckerDepth = 0.3f;

    public float timeScale;

    public float skinWidth = 0.015f;
    public int maxBounces = 3;
    public float sweepDistance = 1.5f;

    public bool simulateGravity;
    public Vector3 gravityDirection = Vector3.down;
    public float gravityMagnitude = 10f;

    public float maxStepHeight;
    public float maxStepWidth;

    [SerializeField] Orientation orientation;
    public Vector3 offset;
    public float radius;
    public float height;

    public float criticalSlopeAngle = 75f;
    public float criticalWallAngle = 30f;

    public Vector3 moveVector; // Direction relative to transform.forward
    public Vector3 scaleFactor;

    enum Orientation
    {
        X,
        Y,
        Z
    }

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Start()
    {
        character.animMachine.OnGraphEvaluate += AnimMachine_OnGraphEvaluate;
    }

    private void Update()
    {
    }

    private void AnimMachine_OnGraphEvaluate()
    {
    }

    public bool CapsuleSweep(Vector3 sweepDir, out RaycastHit capsuleHit)
    {
        Vector3 localOffset = transform.TransformDirection(offset);

        Vector3 capsuleDir = Vector3.zero;

        switch (orientation)
        {
            case Orientation.X:
                capsuleDir = transform.TransformDirection(Vector3.right);
                break;
            case Orientation.Y:
                capsuleDir = transform.TransformDirection(Vector3.up);
                break;
            case Orientation.Z:
                capsuleDir = transform.TransformDirection(Vector3.forward);
                break;
        }

        Vector3 point1 = transform.position + localOffset + capsuleDir * (height / 2f - radius);
        Vector3 point2 = transform.position + localOffset - capsuleDir * (height / 2f - radius);

        using (VisualLifetime.Create(1f))
        {
            if (Physics.CapsuleCast(point1, point2, radius - skinWidth, sweepDir, out capsuleHit, sweepDistance))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public Vector3 CollideAndSlide(Vector3 moveDir, Vector3 pos, int depth = 0)
    {
        if (depth >= maxBounces)
        {
            return Vector3.zero;
        }

        Vector3 localMoveDir = transform.TransformDirection(moveDir);
        if (CapsuleSweep(localMoveDir, out RaycastHit hit))
        {
            float groundAngle = Vector3.Angle(hit.normal, -gravityDirection);
            if (groundAngle <= criticalSlopeAngle)
            {

            }
            else
            {
                
            }

            Vector3 distToSurface = (hit.distance - skinWidth) * localMoveDir.normalized;
            if (distToSurface.magnitude <= skinWidth)
            {
                return Vector3.zero;
            }

            Vector3 leftOverMovement = localMoveDir - distToSurface;
            leftOverMovement = leftOverMovement.magnitude * Vector3.ProjectOnPlane(leftOverMovement, hit.normal).normalized;

            return distToSurface + CollideAndSlide(leftOverMovement, pos + distToSurface, depth + 1);
        }

        return localMoveDir;
    }

    public bool CheckGround(out RaycastHit groundHit)
    {
        if (Physics.Raycast(transform.position + Vector3.up * groundCheckerHeight,
            Vector3.down, out groundHit, groundCheckerHeight + groundCheckerDepth, groundLayer))
        {
            return true;
        }
        for (int i = 1; i < noOfLayers; i++)
        {
            float angleStep = 360f / sides;
            var radius = this.radius * i / (noOfLayers - 1);
            for (int j = 0; j < sides; j++)
            {
                float angle = j * angleStep * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).normalized;
                Vector3 rayStart = transform.position + Vector3.up * groundCheckerHeight + direction * radius;
                if (Physics.Raycast(rayStart, Vector3.down, out groundHit, groundCheckerHeight + groundCheckerDepth, groundLayer))
                {
                    return true;
                }
            }
        }
        groundHit = new RaycastHit();
        return false;
    }
}
