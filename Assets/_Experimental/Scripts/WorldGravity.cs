using System;
using UnityEngine;
using UnityEngine.Events;

[SelectionBase]
public class WorldGravity : MonoBehaviour
{
    public UnityEvent<Vector3> OnGravityChangeEvent;

    public Vector3 gravityDirection;
    public float gravityMagnitude = 10f;

    private void Update()
    {
        if (-transform.up != gravityDirection)
        {
            OnGravityChangeEvent.Invoke(gravityDirection.normalized * gravityMagnitude);
            gravityDirection = - transform.up;
        }
    }
}