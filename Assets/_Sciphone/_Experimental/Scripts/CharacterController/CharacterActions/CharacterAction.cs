using System;
using System.Linq.Expressions;
using UnityEngine;

[Serializable]
public abstract class CharacterAction
{
    public event Action<bool> CanPerform_OnValueChanged;
    public event Action<bool> IsBeingPerformed_OnValueChanged;

    [HideInInspector] public Character character;

    [LeftToggle] public bool enabled = true;
    [LeftToggle, SerializeField] private bool canPerform;
    [LeftToggle, SerializeField] private bool isBeingPerformed;
    public Func<bool> condition;

    public bool CanPerform
    {
        get => canPerform;
        set
        {
            if (canPerform != value)
            {
                canPerform = value;
                CanPerform_OnValueChanged?.Invoke(canPerform);
            }
        }
    }
    public bool IsBeingPerformed
    {
        get => isBeingPerformed;
        set
        {
            if (isBeingPerformed != value)
            {
                isBeingPerformed = value;
                IsBeingPerformed_OnValueChanged?.Invoke(isBeingPerformed);
            }
        }
    }

    public virtual void OnEnable() 
    {
        CompileCondition();
    }
    public virtual void CompileCondition() { }
    public virtual void EvaluateStatus() 
    {
        if (!enabled)
        {
            CanPerform = false;
            return;
        }
        CanPerform = condition();
    }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void OnDisable() { }
    public static Expression<Func<bool>> CombineExpressions(Expression<Func<bool>> left, Expression<Func<bool>> right)
    {
        var combinedBody = Expression.AndAlso(left.Body, right.Body);
        return Expression.Lambda<Func<bool>>(combinedBody);
    }
}