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
