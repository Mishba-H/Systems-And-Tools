using Nomnom.RaycastVisualization;
using Sciphone;
using UnityEngine;

#if UNITY_EDITOR
using Physics = Nomnom.RaycastVisualization.VisualPhysics;
#else
using Physics = UnityEngine.Physics;
#endif

public class CharacterDetector : MonoBehaviour
{
    [HideInInspector] public Character character;
    private void Awake()
    {
        character = GetComponent<Character>();
    }

    [TabGroup("Ground")] public LayerMask groundLayer;
    [TabGroup("Ground")] public int sides = 8;
    [TabGroup("Ground")] public float radius = 0.3f;
    [TabGroup("Ground")] public int noOfLayers = 3;
    [TabGroup("Ground")] public float groundCheckerHeight = 1f;
    [TabGroup("Ground")] public float groundCheckerDepth = 0.3f;
    public bool CheckGround(out RaycastHit groundHit)
    {
        groundCheckerDepth = character.GetGroundCheckerDepth();

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

    [TabGroup("Attack")] public int noOfRays;
    [TabGroup("Attack")] public int seperationAngle;
    [TabGroup("Attack")] public float targetCheckerHeight;
    [TabGroup("Attack")] public LayerMask targetLayer;
    public bool TrySelectTarget(Vector3 checkDir, float range, out RaycastHit attackHit)
    {
        for (int i = 0; i < noOfRays; i++)
        {
            var index = i % 2 == 0 ? i / 2 : -(i / 2 + 1);
            var dir = Quaternion.AngleAxis(index * seperationAngle, Vector3.up) * checkDir;
            Vector3 startPoint = transform.position + targetCheckerHeight * Vector3.up;
            using (VisualLifetime.Create(1f))
            {
                if (Physics.Raycast(startPoint, dir, out attackHit, range, targetLayer))
                {
                    return true;
                }
            }
        }
        attackHit = new RaycastHit();
        return false;
    }

    [TabGroup("Climb")] public LayerMask climbLayer;
    [TabGroup("Climb")] public int climbCheckerCount = 3;
    [TabGroup("Climb")] public float climbCheckerInterval = 0.1f;
    [TabGroup("Climb")] public float climbCheckerHeight;
    [TabGroup("Climb")] public float climbCheckerDepth;
    public bool DetectClimbPoint(out RaycastHit climbHit)
    {
        climbHit = new RaycastHit();
        var pos = transform.position + Vector3.up * climbCheckerHeight;
        for (int i = 0; i < climbCheckerCount; i++)
        {
            if (Physics.Raycast(pos + climbCheckerInterval * i * transform.forward, Vector3.down, out climbHit, climbCheckerDepth, climbLayer))
            {
                return true;
            }
        }
        return false;
    }

    [TabGroup("Wall")] public LayerMask wallLayer;
    [TabGroup("Wall")] public float startingHeight;
    [TabGroup("Wall")] public float wallCheckerInterval;
    [TabGroup("Wall")] public float wallCheckerLength;
    public RaycastHit GetWallHit(Vector3 direction)
    {
        int count = Mathf.FloorToInt(startingHeight / wallCheckerInterval);
        var pos = transform.position + startingHeight * Vector3.up;
        for (int i = 0; i < count; i ++)
        {
            if (Physics.Raycast(pos + i * wallCheckerInterval * Vector3.down, direction, out RaycastHit wallHit, wallCheckerLength, wallLayer))
            {
                return wallHit;
            }
        }
        return new RaycastHit();
    }

    [TabGroup("Width")] public LayerMask widthLayer;
    [TabGroup("Width")] public float maxWidth = 1f;
    [TabGroup("Width")] public float widthInterval = 0.15f;
    [TabGroup("Width")] public float widthCheckerDepth = 0.15f;
    public float DetectFence(Vector3 direction, Vector3 height)
    {
        float totalWidth = 0f;
        var count = Mathf.FloorToInt(maxWidth / widthInterval);
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = direction * i * widthInterval + (height.y - transform.position.y) * Vector3.up;
            if (Physics.Raycast(transform.position + offset, Vector3.down, widthCheckerDepth, widthLayer))
            {
                totalWidth += widthInterval;
            }
        }
        return totalWidth;
    }
}
