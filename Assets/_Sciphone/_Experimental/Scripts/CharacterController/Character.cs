using System;
using System.Collections.Generic;
using System.Linq;
using Sciphone;
using UnityEngine;

[SelectionBase]
public class Character : MonoBehaviour
{
    public Action OnAllActionEvaluate;
    public Action<float> OnAnimationMachineUpdate;

    [Range(0f, 3f)] public float timeScale;

    public List<IControllerModule> modules;
    [ReorderableList(Foldable = true), SerializeReference, Polymorphic] public List<CharacterAction> actions;

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
        animMachine.OnGraphEvaluate += (float dt) => OnAnimationMachineUpdate?.Invoke(dt);

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
        animMachine.timeScale = timeScale;
        characterMover.timeScale = timeScale;

        foreach (var action in actions)
        {
            action.EvaluateStatus();
        }
        OnAllActionEvaluate?.Invoke();

        foreach (var action in actions)
        {
            action.Update();
        }
    }

    private void FixedUpdate()
    {
        foreach (var action in actions)
        {
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
