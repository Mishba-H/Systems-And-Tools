using System;
using UnityEngine;
using System.Collections.Generic;
using Sciphone.ComboGraph;
using Sciphone;
using Nomnom.RaycastVisualization;

[Serializable]
public class MeleeCombatController : MonoBehaviour, IControllerModule
{
    public event Action OnAttackSelected;

    public Character character { get; set; }

    #region DODGE_PARAMETERS
    [TabGroup("Dodge")] public float evadeDistance = 2f;
    [TabGroup("Dodge")] public float rollDistance = 4f;
    [TabGroup("Dodge")] public float dodgeInterval = 0.15f;
    [TabGroup("Dodge")] public float rollWindowTime = 0.1f;
    [TabGroup("Dodge"), Disable] public float evadePerformTime;
    [TabGroup("Dodge"), Disable] public float rollPerformTime;
    [TabGroup("Dodge"), Disable] public float evadeStopTime;
    [TabGroup("Dodge"), Disable] public float rollStopTime;
    [TabGroup("Dodge")] public Vector3 worldDodgeDir;
    [TabGroup("Dodge")] public Vector2 relativeDodgeDir;
    #endregion

    #region ATTACK_PARAMETERS
    [TabGroup("Attack")][SerializeReference, Polymorphic] public List<ComboData> activeCombos;
    [TabGroup("Attack")] public float comboEndTime;
    [TabGroup("Attack")] public List<AttackData> attacksPerformed;
    [TabGroup("Attack")] public float lastAttackTime;
    [TabGroup("Attack")] public bool readyToAttack;
    [TabGroup("Attack")] public float attackCacheDuration;
    [TabGroup("Attack")] public float attackCommandTime;
    [TabGroup("Attack")] public AttackType cachedAttack;
    [TabGroup("Attack")] public Vector3 worldAttackDir;
    [TabGroup("Attack")] public float rotateSpeed;
    
    [TabGroup("Attack")][Header("Detector Settings")] public int noOfRays;
    [TabGroup("Attack")] public int seperationAngle;
    [TabGroup("Attack")] public float targetCheckerHeight;
    [TabGroup("Attack")] public LayerMask targetLayer;
    #endregion

    public Vector3 worldMoveDir;
    bool recalculateScaleFactor;
    public Vector3 scaleFactor;

    List<FourWayBlendState> dodgeStates;
    public float error = 0.1f;
    public float blendSpeed = 0.5f;

    private void Awake()
    {
        attacksPerformed = new List<AttackData>();
    }

    private void Start()
    {
        character.characterCommand.MoveDirCommand += OnMoveDirCommand;
        character.characterCommand.AttackDirCommand += OnAttackDirCommand;

        character.UpdateLoop += Character_UpdateLoop;
        foreach (var action in character.actions)
        {
            if (action is Evade || action is Roll || action is Attack)
            {
                action.IsBeingPerformed_OnValueChanged += (bool value) =>
                {
                    if (value) recalculateScaleFactor = true;
                };
            }
        }

        dodgeStates = new List<FourWayBlendState>
        {
            character.animMachine.layers.GetLayerInfo("Base").GetStateInfo("Evade") as FourWayBlendState,
            character.animMachine.layers.GetLayerInfo("Base").GetStateInfo("Roll") as FourWayBlendState
        };
    }

    private void Character_UpdateLoop()
    {
        HandleAnimationParameters(Time.deltaTime * character.timeScale);
    }

    private void OnMoveDirCommand(Vector3 vector)
    {
        worldMoveDir = vector;
    }

    private void OnAttackDirCommand(Vector3 vector)
    {
        worldAttackDir = vector == Vector3.zero ? transform.forward : vector;
    }

    public void CalculateScaleFactor()
    {
        if (recalculateScaleFactor)
        {
            if (character.animMachine.activeState.TryGetProperty<RootMotionCurvesProperty>(out var rootMotionProp) &&
                character.animMachine.activeState.TryGetProperty<ScaleModeProperty>(out var scaleModeProp))
            {
                Vector3 targetValue = Vector3.zero;

                if (character.PerformingAction<Evade>())
                {
                    targetValue = new Vector3(0f, 0f, evadeDistance);
                }
                else if (character.PerformingAction<Roll>())
                {
                    targetValue = new Vector3(0f, 0f, rollDistance);
                }

                scaleFactor = AnimationMachineExtensions.EvaluateScaleFactor(rootMotionProp as RootMotionCurvesProperty, scaleModeProp as ScaleModeProperty, targetValue);
            }
        }
    }

    public void InitiateDodge()
    {
        if (character.PerformingAction<Evade>())
            worldDodgeDir = worldMoveDir == Vector3.zero ? transform.forward : worldMoveDir;
        else if (character.PerformingAction<Roll>())
            worldDodgeDir = worldMoveDir == Vector3.zero ? worldDodgeDir : worldMoveDir;

        if (character.PerformingAction<Sprint>())
        {
            relativeDodgeDir = Vector2.up;
        }
        else
        {
            if (Vector3.Angle(transform.forward, worldDodgeDir) <= 45)
            {
                relativeDodgeDir = Vector2.up;
            }
            else if (Vector3.Angle(transform.right, worldDodgeDir) <= 45)
            {
                relativeDodgeDir = Vector2.right;
            }
            else if (Vector3.Angle(-transform.right, worldDodgeDir) <= 45)
            {
                relativeDodgeDir = Vector2.left;
            }
            else if (Vector3.Angle(-transform.forward, worldDodgeDir) <= 45)
            {
                relativeDodgeDir = Vector2.down;
            }
        }
    }

    public void HandleDodgeMotion(float dt)
    {
        Vector3 up = transform.up;
        Vector3 forward = transform.forward;
        Vector3 right = Vector3.Cross(up, forward).normalized;

        Vector3 rootDeltaPosition = character.animMachine.rootLinearVelocity * dt;
        Vector3 scaledDeltaPosition = new Vector3(rootDeltaPosition.x * scaleFactor.x, rootDeltaPosition.y * scaleFactor.y,
            rootDeltaPosition.z * scaleFactor.z);

        Vector3 worldDeltaPosition;
        Vector3 moveAmount = Vector3.zero;

        if (relativeDodgeDir == Vector2.up)
        {
            worldDeltaPosition = scaledDeltaPosition.x * right + scaledDeltaPosition.y * up + scaledDeltaPosition.z * forward;
            moveAmount = character.characterMover.ProcessCollideAndSlide(worldDeltaPosition);
        }
        else if (relativeDodgeDir == Vector2.down)
        {
            worldDeltaPosition = scaledDeltaPosition.x * -right + scaledDeltaPosition.y * up + scaledDeltaPosition.z * -forward;
            moveAmount = character.characterMover.ProcessCollideAndSlide(worldDeltaPosition);
        }
        else if (relativeDodgeDir == Vector2.right)
        {
            worldDeltaPosition = scaledDeltaPosition.x * -forward + scaledDeltaPosition.y * up + scaledDeltaPosition.z * right;
            moveAmount = character.characterMover.ProcessCollideAndSlide(worldDeltaPosition);
        }
        else if (relativeDodgeDir == Vector2.left)
        {
            worldDeltaPosition = scaledDeltaPosition.x * forward + scaledDeltaPosition.y * up + scaledDeltaPosition.z * -right;
            moveAmount = character.characterMover.ProcessCollideAndSlide(worldDeltaPosition);
        }

        character.characterMover.SetWorldVelocity(moveAmount / dt);
    }

    public void HandleAttackMotion(float dt)
    {
        Vector3 up = transform.up;
        Vector3 forward = transform.forward;
        Vector3 right = Vector3.Cross(up, forward).normalized;

        Vector3 rootDeltaPosition = character.animMachine.rootLinearVelocity * dt;
        Vector3 scaledDeltaPosition = new Vector3(rootDeltaPosition.x * scaleFactor.x, rootDeltaPosition.y * scaleFactor.y,
            rootDeltaPosition.z * scaleFactor.z);

        Vector3 worldDeltaPosition = Vector3.zero;
        Vector3 moveAmount = Vector3.zero;

        worldDeltaPosition = scaledDeltaPosition.x * right + scaledDeltaPosition.y * up + scaledDeltaPosition.z * forward;
        moveAmount = character.characterMover.ProcessCollideAndSlide(worldDeltaPosition);

        character.characterMover.SetWorldVelocity(moveAmount / dt);
    }

    public void HandleRotation(float dt)
    {
        if (character.PerformingAction<Evade>() || character.PerformingAction<Roll>())
        {
            if (relativeDodgeDir.y == 1f)
            {
                character.characterMover.SetFaceDir(worldDodgeDir);
            }
            else if (relativeDodgeDir.y == -1f)
            {
                character.characterMover.SetFaceDir(-worldDodgeDir);
            }
            else if (relativeDodgeDir.x == 1f)
            {
                character.characterMover.SetFaceDir(Vector3.Cross(worldDodgeDir, transform.up));
            }
            else if (relativeDodgeDir.x == -1f)
            {
                character.characterMover.SetFaceDir(Vector3.Cross(-worldDodgeDir, transform.up));
            }
        }
        if (character.PerformingAction<Attack>())
        {
            character.characterMover.SetFaceDir(Vector3.RotateTowards(transform.forward, worldAttackDir, rotateSpeed * dt * Mathf.Deg2Rad, 0f));
        }
    }

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

    public bool TrySelectAttack(AttackType attackType, int depth = 1)
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
                if (TrySelectTarget(worldAttackDir, attackData.attackRange, out RaycastHit attackHit))
                {
                    worldAttackDir = Vector3.ProjectOnPlane(attackHit.point - transform.position, transform.up).normalized;
                }
                OnAttackSelected?.Invoke();
                return true;
            }
        }
        attacksPerformed.Clear();
        return TrySelectAttack(attackType, ++depth);
    }
    
    public void HandleAnimationParameters(float dt)
    {
        foreach (var state in dodgeStates)
        {
            state.blendX = relativeDodgeDir.x;
            state.blendY = relativeDodgeDir.y;
        }
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

    // Place a try select target method here 
    // Each attack can then have different settings
}