using System;
using System.Collections.Generic;
using System.Linq;
using Sciphone;
using UnityEngine;

[SelectionBase]
public class Character : MonoBehaviour
{
    public event Action DetectionLoop;
    public event Action UpdateLoop;
    public event Action FixedUpdateLoop;

    public float time = 0.0f;
    [Range(0.001f, 3f)] public float timeScale = 1f;

    public List<IControllerModule> modules;
    [SerializeReference, Polymorphic] public List<CharacterAction> actions;

    [HideInInspector] public AnimationMachine animMachine;
    [HideInInspector] public CharacterCommand characterCommand;
    [HideInInspector] public CharacterAnimator characterAnimator;
    [HideInInspector] public CharacterDetector characterDetector;
    [HideInInspector] public CharacterMover characterMover;

    private void Awake()
    {
        animMachine = GetComponent<AnimationMachine>();
        characterCommand = GetComponent<CharacterCommand>();
        characterAnimator = GetComponent<CharacterAnimator>();
        characterDetector = GetComponent<CharacterDetector>();
        characterMover = GetComponent<CharacterMover>();

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
        SynchronizeTime();

        DetectionLoop?.Invoke();
        EvaluateAndUpdateAllActions();
        if (!PerformingAction<CharacterAction>())
        {
            EvaluateAndUpdateAllActions();
        }
        UpdateLoop?.Invoke();
    }

    private void FixedUpdate()
    {
        EvaluateAndFixedUpdateAllActions();
        FixedUpdateLoop?.Invoke();
    }

    private void SynchronizeTime()
    {
        time += Time.deltaTime * timeScale;
        animMachine.timeScale = timeScale;
    }

    public void EvaluateAndUpdateAllActions()
    {
        foreach (var action in actions)
        {
            action.EvaluateStatus();
            action.Update();
        }
    }

    public void EvaluateAndFixedUpdateAllActions()
    {
        foreach (var action in actions)
        {
            action.EvaluateStatus();
            action.FixedUpdate();
        }
    }

    public bool TryGetAction<T>(out CharacterAction action) where T : CharacterAction
    {
        action = actions.FirstOrDefault(t => t.GetType() == typeof(T));
        return action != null;
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
