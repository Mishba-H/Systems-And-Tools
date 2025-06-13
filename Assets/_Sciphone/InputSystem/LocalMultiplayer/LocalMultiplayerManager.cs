using UnityEngine;
using UnityEngine.InputSystem;

public class LocalMultiplayerManager : MonoBehaviour
{
    private PlayerInputManager playerInputManager;
    private SplitScreenManager splitScreenManager;

    private void Awake()
    {
        playerInputManager = GetComponent<PlayerInputManager>();
        splitScreenManager = GetComponent<SplitScreenManager>();

        playerInputManager.onPlayerJoined += PlayerInputManager_onPlayerJoined;
        playerInputManager.onPlayerLeft += PlayerInputManager_onPlayerLeft;
    }

    private void PlayerInputManager_onPlayerJoined(PlayerInput obj)
    {
        int playerCount = playerInputManager.playerCount;
        splitScreenManager.ApplyPreset(playerCount - 1);
        
    }

    private void PlayerInputManager_onPlayerLeft(PlayerInput obj)
    {
        Destroy(obj);
        int playerCount = playerInputManager.playerCount;
        splitScreenManager.ApplyPreset(playerCount - 1);
    }
}
