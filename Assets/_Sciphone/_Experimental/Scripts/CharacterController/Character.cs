using System.Collections.Generic;
using System.Linq;
using Sciphone;
using UnityEngine;

[SelectionBase]
public class Character : MonoBehaviour
{
    [HideInInspector] public Rigidbody rb;

    [Min(0f)] public float timeScale;

    public bool isGrounded;
    public Transform cameraOrientation;
    public Vector2 moveInput;
    public Vector3 moveDir;
    public RaycastHit groundHit;

    public List<IControllerModule> modules;
    [HideInInspector] public AnimationMachine animMachine;
    [HideInInspector] public CharacterAnimator characterAnimator;
    [HideInInspector] public CharacterDetector characterDetector;
    [ReorderableList(Foldable = true), SerializeReference, Polymorphic] public List<CharacterAction> actions;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        animMachine = GetComponent<AnimationMachine>();

        characterAnimator = GetComponent<CharacterAnimator>();
        characterDetector = GetComponent<CharacterDetector>();

        modules = GetComponents<IControllerModule>().ToList();
        foreach (IControllerModule module in modules)
        {
            module.character = this;
        }

        foreach (CharacterAction action in actions)
        {
            action.character = this;
            if (action.GetType().IsSubclassOf(typeof(BaseAction)))
            {
                ((BaseAction)action).controller = GetControllerModule<BaseController>();
            }
            if (action.GetType().IsSubclassOf(typeof(MeleeCombatAction)))
            {
                ((MeleeCombatAction)action).controller = GetControllerModule<MeleeCombatController>();
            }
            if (action.GetType().IsSubclassOf(typeof(ParkourAction)))
            {
                ((ParkourAction)action).controller = GetControllerModule<ParkourController>();
            }
        }
    }
    private void Start()
    {
        foreach (CharacterAction characterAction in actions)
        {
            characterAction.OnEnable();
        }

        InputReader.instance.Move += OnMoveInput;
    }
    private void OnDisable()
    {
        foreach (CharacterAction characterAction in actions)
        {
            characterAction.OnDisable();
        }
    }
    private void Update()
    {
        animMachine.timeScale = timeScale;

        isGrounded = characterDetector.CheckGround(out groundHit);
        CalculateMoveDirection();

        foreach (var action in actions)
        {
            action.EvaluateStatus();
        }
        foreach (var action in actions)
        {
            action.Update();
        }
    }
    private void FixedUpdate()
    {
        foreach (var action in actions)
        {
            action.EvaluateStatus();
        }
        foreach (var action in actions)
        {
            action.FixedUpdate();
        }
    }
    private void OnMoveInput(Vector2 moveInput)
    {
        this.moveInput = moveInput;
    }
    public void CalculateMoveDirection()
    {
        moveDir = (moveInput.x * new Vector3(cameraOrientation.right.x, 0f, cameraOrientation.right.z) +
            moveInput.y * new Vector3(cameraOrientation.forward.x, 0f, cameraOrientation.forward.z)).normalized;
    }
    public float GetGroundCheckerDepth()
    {
        if (PerformingAction<Idle>() || PerformingAction<Walk>() || PerformingAction<Run>() || PerformingAction<Sprint>()
            || PerformingAction<Evade>() || PerformingAction<Roll>() || PerformingAction<Attack>())
        {
            return 1f;
        }
        else if (PerformingAction<Fall>() || PerformingAction<Jump>() || PerformingAction<AirJump>())
        {
            return 0f;
        }
        return 0f;
    }
    public CharacterAction GetAction<T>() where T : CharacterAction
    {
        return actions.FirstOrDefault(t => t.GetType() == typeof(T));
    }
    public bool CanPerformAction<T>() where T: CharacterAction
    {
        return actions.OfType<T>().Any(action => action.CanPerform);
    }
    public bool PerformingAction<T>() where T : CharacterAction
    {
        return actions.OfType<T>().Any(action => action.IsBeingPerformed);
    }
    public T GetControllerModule<T>() where T : IControllerModule
    {
        return modules.OfType<T>().FirstOrDefault();
    }
}
