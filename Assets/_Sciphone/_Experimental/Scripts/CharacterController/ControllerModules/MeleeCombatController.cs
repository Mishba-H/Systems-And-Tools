using System;
using UnityEngine;
using System.Collections.Generic;
using Sciphone.ComboGraph;
using Sciphone;

[Serializable]
public class MeleeCombatController : MonoBehaviour, IControllerModule
{
    public Character character { get; set; }

    public event Action<bool> OnAttackSelected;

    #region DODGE_VARIABLES
    [TabGroup("Dodge")] public float evadeDistance;
    [TabGroup("Dodge")] public float rollDistance;
    [TabGroup("Dodge")] public float dodgeInterval;
    [TabGroup("Dodge")] public float rollWindowTime;
    [TabGroup("Dodge"), Disable] public float evadeInputTime;
    [TabGroup("Dodge"), Disable] public float rollInputTime;
    [TabGroup("Dodge"), Disable] public float lastEvadeTime;
    [TabGroup("Dodge"), Disable] public float lastRollTime;
    [TabGroup("Dodge")] public Vector2 dodgeDir;
    private Vector3 cachedDodgeDir;
    #endregion

    [TabGroup("Attack")][SerializeReference, Polymorphic] public List<ComboData> activeCombos;
    [TabGroup("Attack")] public float comboEndTime;
    [TabGroup("Attack")] public List<AttackData> attacksPerformed;
    [TabGroup("Attack")] public float lastAttackTime;
    [TabGroup("Attack")] public bool readyToAttack;
    [TabGroup("Attack")] public float attackCacheDuration;
    [TabGroup("Attack")] public float attackInputTime;
    [TabGroup("Attack")] public AttackType cachedAttack;
    [TabGroup("Attack")] public Vector3 attackDir;
    [TabGroup("Attack")] public Vector3 faceDir;
    internal float attackDirTimer;

    private float speedFactor;


    private void Awake()
    {
        attacksPerformed = new List<AttackData>();
    }

    private void Start()
    {
        character.animMachine.OnActiveStateChanged += CalculateSpeedFactor;
    }

    private void CalculateSpeedFactor()
    {
        if (character.animMachine.activeState.TryGetProperty<RootMotionCurvesProperty>(out AnimationStateProperty property))
        {
            var curves = (RootMotionData)property.Value;
            float totalTime = curves.rootTZ.keys[curves.rootTZ.length - 1].time;
            float totalDisplacement = curves.rootTZ.Evaluate(totalTime) - curves.rootTZ.Evaluate(0f);
            totalDisplacement = Mathf.Abs(totalDisplacement) < 0.1f? curves.rootTZ.GetMaxValue() : totalDisplacement;

            float targetDist = 0f;
            if (character.PerformingAction<Evade>() ||  character.PerformingAction<Roll>())
            {
                targetDist = character.PerformingAction<Evade>() ? evadeDistance : rollDistance;
            }
            else if (character.PerformingAction<Attack>())
            {
                targetDist = totalDisplacement;
            }
            speedFactor = targetDist / totalDisplacement;
        }
    }
    public void InitiateDodge()
    {
        cachedDodgeDir = character.moveDir == Vector3.zero ? transform.forward : character.moveDir;
        if (character.PerformingAction<Sprint>())
        {
            dodgeDir = Vector2.up;
        }
        else if (character.moveInput != Vector2.zero)
        {
            if (Mathf.Abs(character.moveInput.x) > Mathf.Abs(character.moveInput.y))
            {
                dodgeDir = character.moveInput.x > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                dodgeDir = character.moveInput.y > 0 ? Vector2.up : Vector2.down;
            }
        }
        else
        {
            dodgeDir = Vector2.up;
        }
    }
    public void HandleDodgeMotion()
    {
        if (dodgeDir == Vector2.up)
        {
            character.rb.linearVelocity = Quaternion.LookRotation(transform.forward, Vector3.up) *
                character.animMachine.rootLinearVelocity.With(y: 0f, z: speedFactor * character.animMachine.rootLinearVelocity.z);
        }
        else if (dodgeDir == Vector2.down)
        {
            character.rb.linearVelocity = Quaternion.LookRotation(-transform.forward, Vector3.up) *
                character.animMachine.rootLinearVelocity.With(y: 0f, z: speedFactor * character.animMachine.rootLinearVelocity.z);
        }
        else if (dodgeDir == Vector2.right)
        {
            character.rb.linearVelocity = Quaternion.LookRotation(transform.right, Vector3.up) *
                character.animMachine.rootLinearVelocity.With(y: 0f, z: speedFactor * character.animMachine.rootLinearVelocity.z);
        }
        else if (dodgeDir == Vector2.left)
        {
            character.rb.linearVelocity = Quaternion.LookRotation(-transform.right, Vector3.up) *
                character.animMachine.rootLinearVelocity.With(y: 0f, z: speedFactor * character.animMachine.rootLinearVelocity.z);
        }
    }
    public bool SelectAttack(AttackType attackType, int depth = 1)
    {
        if (depth > 2) return false;

        ComboData currentCombo;
        for (int i = 0; i < activeCombos.Count; i++)
        {
            currentCombo = activeCombos[i];
            if (currentCombo.attacks.Count <= attacksPerformed.Count)
            {
                continue;
            }

            bool attacksDidNotMatch = false;
            for (int j = 0; j < attacksPerformed.Count; j++)
            {
                if (currentCombo.attacks[j].attackName != attacksPerformed[j].attackName)
                {
                    attacksDidNotMatch = true;
                }
            }
            if (attacksDidNotMatch)
            {
                continue;
            }

            var attackData = currentCombo.attacks[attacksPerformed.Count];
            if (attackData.attackType == attackType)
            {
                lastAttackTime = Time.time;
                attacksPerformed.Add(attackData);
                if (character.characterDetector.TrySelectTarget(attackDir, attackData.attackRange, out RaycastHit attackHit))
                {
                    faceDir = attackHit.point.With(y: 0f) - transform.position;
                }
                else
                {
                    faceDir = attackDir;
                }
                OnAttackSelected?.Invoke(true);
                return true;
            }
        }
        attacksPerformed.Clear();
        return SelectAttack(attackType, ++depth);
    }
    public void CalculateAttackDir(float dt)
    {
        if (character.moveInput.magnitude > 0.7f)
        {
            attackDir = character.moveDir;
            attackDirTimer = 0f;
        }
        else
        {
            attackDirTimer += dt;
            if (attackDirTimer > attackCacheDuration)
            {
                attackDir = character.transform.forward;
            }
        }
    }
    public void HandleAttackMotion()
    {
        character.rb.linearVelocity = Quaternion.LookRotation(transform.forward, Vector3.up) *
                character.animMachine.rootLinearVelocity.With(y: 0f, z: speedFactor * character.animMachine.rootLinearVelocity.z);
    }
    public void HandleRotation(float dt)
    {
        if (character.PerformingAction<Attack>())
        {
            transform.forward = Vector3.Slerp(transform.forward, faceDir, 10 * dt);
        }
        else if (character.PerformingAction<Evade>() || character.PerformingAction<Roll>())
        {
            if (dodgeDir.y == 1f)
            {
                transform.forward = cachedDodgeDir;
            }
            else if (dodgeDir.y == -1f)
            {
                transform.forward = -cachedDodgeDir;
            }
            else if (dodgeDir.x == 1f)
            {
                transform.right = cachedDodgeDir;
            }
            else if (dodgeDir.x == -1f)
            {
                transform.right = -cachedDodgeDir;
            }
        }
    }
    public void SnapToGround()
    {
        transform.position = new Vector3(transform.position.x, character.groundHit.point.y, transform.position.z);
    }
}

[Serializable]
public class ComboData
{
    public string comboName;
    [SerializeReference, Polymorphic] public List<AttackData> attacks;
}

[Serializable]
public class AttackData
{
    public string attackName;
    public AttackType attackType;
    public float attackRange = 3f;
}