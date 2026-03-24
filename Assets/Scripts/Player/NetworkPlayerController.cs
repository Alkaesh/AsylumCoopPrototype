using AsylumHorror.Monster;
using AsylumHorror.Network;
using AsylumHorror.Core;
using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AsylumHorror.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkPlayerStatus))]
    public class NetworkPlayerController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private Animator animator;
        [SerializeField] private Renderer[] firstPersonHiddenRenderers;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.6f;
        [SerializeField] private float runSpeed = 6.2f;
        [SerializeField] private float crouchSpeed = 2.2f;
        [SerializeField] private float gravity = 20f;
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.2f;
        [SerializeField] private float crouchTransitionSpeed = 12f;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaDrainPerSecond = 24f;
        [SerializeField] private float staminaRegenPerSecond = 16f;

        [Header("Look")]
        [SerializeField] private float mouseSensitivity = 2.1f;
        [SerializeField] private float lookPitchMin = -80f;
        [SerializeField] private float lookPitchMax = 80f;
        [SerializeField] private float incapacitatedYawLimit = 85f;

        [Header("Lobby Preview")]
        [SerializeField] private float lobbyWalkSpeed = 3.2f;
        [SerializeField] private float lobbyRunSpeed = 4.6f;
        [SerializeField] private Vector3 lobbyBoundsCenter = new Vector3(0f, 0f, 0.5f);
        [SerializeField] private Vector3 lobbyBoundsExtents = new Vector3(5.8f, 0f, 3.75f);
        [SerializeField] private Vector3 lobbyCameraOffset = new Vector3(0f, 0.1f, -4.4f);
        [SerializeField] private Vector3 lobbyCameraLookOffset = new Vector3(0f, 1.45f, 0f);
        [SerializeField] private float lobbyCameraSmoothing = 10f;

        [Header("Noise")]
        [SerializeField] private float walkNoiseRadius = 8f;
        [SerializeField] private float runNoiseRadius = 14.5f;
        [SerializeField] private float crouchNoiseRadius = 3f;
        [SerializeField] private float walkNoiseInterval = 0.65f;
        [SerializeField] private float runNoiseInterval = 0.4f;
        [SerializeField] private float crouchNoiseInterval = 1.25f;

        [SyncVar(hook = nameof(OnLocomotionStateChanged))] private PlayerLocomotionState locomotionState;

        private CharacterController characterController;
        private NetworkPlayerStatus playerStatus;
        private Vector3 characterVelocity;
        private float currentPitch;
        private float currentYawOffset;
        private float stamina;
        private float nextNoiseTime;
        private bool isCrouchingLocal;
        private float localMoveMagnitude;
        private bool cursorUnlockedByUser;
        private bool lobbyLookHeld;

        public PlayerLocomotionState LocomotionState => locomotionState;
        public float Stamina01 => maxStamina <= 0f ? 0f : stamina / maxStamina;
        public float LocalMoveMagnitude => localMoveMagnitude;
        public bool IsCrouchingLocal => isCrouchingLocal;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerStatus = GetComponent<NetworkPlayerStatus>();
            stamina = maxStamina;

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>(true);
            }

            if (audioListener == null && playerCamera != null)
            {
                audioListener = playerCamera.GetComponent<AudioListener>();
            }

            if (cameraRoot == null && playerCamera != null)
            {
                cameraRoot = playerCamera.transform.parent != null
                    ? playerCamera.transform.parent
                    : playerCamera.transform;
            }

            if ((firstPersonHiddenRenderers == null || firstPersonHiddenRenderers.Length == 0) && playerCamera != null)
            {
                List<Renderer> renderers = new List<Renderer>();
                foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    if (renderer.transform.IsChildOf(playerCamera.transform))
                    {
                        continue;
                    }

                    renderers.Add(renderer);
                }

                firstPersonHiddenRenderers = renderers.ToArray();
            }

            SettingsStore.EnsureLoaded();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyLocalVisualOwnership();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            cursorUnlockedByUser = false;
            UpdateLocalCursorState();
            ApplyLocalVisualOwnership();
        }

        private void OnEnable()
        {
            if (playerStatus != null)
            {
                playerStatus.ConditionChanged += OnConditionChanged;
            }

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            SettingsStore.SettingsChanged += OnSettingsChanged;
        }

        private void OnDisable()
        {
            if (playerStatus != null)
            {
                playerStatus.ConditionChanged -= OnConditionChanged;
            }

            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            SettingsStore.SettingsChanged -= OnSettingsChanged;

            if (isLocalPlayer)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            HandleCursorToggleInput();
            UpdateLocalCursorState();
            bool gameplayScene = IsGameplayScene();
            bool lobbyScene = IsLobbyScene();
            bool lookLocked = IsCursorCurrentlyLocked();

            if (gameplayScene && !lookLocked)
            {
                UpdateCameraMode();
                return;
            }

            if (!playerStatus.CanLookAround && !lobbyScene)
            {
                UpdateCameraMode();
                return;
            }

            ApplyRuntimeSettings();
            HandleAbilityInput();
            if (gameplayScene || (lobbyScene && lookLocked))
            {
                HandleMouseLook();
            }

            HandleMovement();
            UpdateCameraMode();
        }

        private void HandleMouseLook()
        {
            if (cameraRoot == null)
            {
                return;
            }

            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            float settingsMultiplier = SettingsStore.LookSensitivityMultiplier;
            mouseX *= settingsMultiplier;
            mouseY *= settingsMultiplier;

            currentPitch = Mathf.Clamp(currentPitch - mouseY, lookPitchMin, lookPitchMax);
            if (playerStatus == null || playerStatus.CanControlCharacter)
            {
                transform.Rotate(0f, mouseX, 0f);
                currentYawOffset = Mathf.MoveTowards(currentYawOffset, 0f, Time.deltaTime * 180f);
            }
            else
            {
                currentYawOffset = Mathf.Clamp(currentYawOffset + mouseX, -incapacitatedYawLimit, incapacitatedYawLimit);
            }

            cameraRoot.localRotation = Quaternion.Euler(currentPitch, currentYawOffset, 0f);
        }

        private void HandleMovement()
        {
            if (IsLobbyScene())
            {
                HandleLobbyMovement();
                return;
            }

            if (!playerStatus.CanControlCharacter)
            {
                localMoveMagnitude = 0f;
                SetLocomotionState(PlayerLocomotionState.Idle);
                return;
            }

            float inputX = Input.GetAxisRaw("Horizontal");
            float inputY = Input.GetAxisRaw("Vertical");
            Vector2 moveInput = new Vector2(inputX, inputY);
            localMoveMagnitude = Mathf.Clamp01(moveInput.magnitude);

            bool requestedCrouch = Input.GetKey(KeyCode.LeftControl);
            isCrouchingLocal = requestedCrouch;

            bool canRun = !isCrouchingLocal && localMoveMagnitude > 0.15f && stamina > 0f && Input.GetKey(KeyCode.LeftShift);
            float targetSpeed = isCrouchingLocal ? crouchSpeed : (canRun ? runSpeed : walkSpeed);
            targetSpeed *= playerStatus.CurrentMovementMultiplier;

            if (canRun)
            {
                stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * Time.deltaTime);
            }
            else
            {
                stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSecond * Time.deltaTime);
            }

            Vector3 moveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
            Vector3 planarVelocity = moveDirection * (targetSpeed * localMoveMagnitude);

            if (characterController.isGrounded && characterVelocity.y < 0f)
            {
                characterVelocity.y = -2f;
            }

            characterVelocity.y -= gravity * Time.deltaTime;
            Vector3 motion = planarVelocity + Vector3.up * characterVelocity.y;
            characterController.Move(motion * Time.deltaTime);

            float targetHeight = isCrouchingLocal ? crouchHeight : standingHeight;
            float nextHeight = Mathf.Lerp(characterController.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
            characterController.height = nextHeight;
            characterController.center = new Vector3(0f, nextHeight * 0.5f, 0f);

            PlayerLocomotionState nextState = ResolveLocomotionState(localMoveMagnitude, canRun, isCrouchingLocal);
            SetLocomotionState(nextState);
            TryEmitMovementNoise(nextState, localMoveMagnitude);
        }

        private void HandleLobbyMovement()
        {
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputY = Input.GetAxisRaw("Vertical");
            Vector2 moveInput = new Vector2(inputX, inputY);
            localMoveMagnitude = Mathf.Clamp01(moveInput.magnitude);
            isCrouchingLocal = false;

            bool isRunning = localMoveMagnitude > 0.15f && Input.GetKey(KeyCode.LeftShift);
            float targetSpeed = isRunning ? lobbyRunSpeed : lobbyWalkSpeed;
            Vector3 moveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized;
            Vector3 planarVelocity = moveDirection * (targetSpeed * localMoveMagnitude);

            if (characterController.isGrounded && characterVelocity.y < 0f)
            {
                characterVelocity.y = -2f;
            }

            characterVelocity.y -= gravity * Time.deltaTime;
            Vector3 motion = planarVelocity + Vector3.up * characterVelocity.y;
            characterController.Move(motion * Time.deltaTime);

            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x, lobbyBoundsCenter.x - lobbyBoundsExtents.x, lobbyBoundsCenter.x + lobbyBoundsExtents.x);
            position.z = Mathf.Clamp(position.z, lobbyBoundsCenter.z - lobbyBoundsExtents.z, lobbyBoundsCenter.z + lobbyBoundsExtents.z);
            transform.position = position;

            float targetHeight = standingHeight;
            float nextHeight = Mathf.Lerp(characterController.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
            characterController.height = nextHeight;
            characterController.center = new Vector3(0f, nextHeight * 0.5f, 0f);

            PlayerLocomotionState nextState = localMoveMagnitude <= 0.1f
                ? PlayerLocomotionState.Idle
                : (isRunning ? PlayerLocomotionState.Run : PlayerLocomotionState.Walk);
            SetLocomotionState(nextState);
        }

        private PlayerLocomotionState ResolveLocomotionState(float moveMagnitude, bool isRunning, bool isCrouching)
        {
            if (moveMagnitude <= 0.1f)
            {
                return PlayerLocomotionState.Idle;
            }

            if (isCrouching)
            {
                return PlayerLocomotionState.Crouch;
            }

            return isRunning ? PlayerLocomotionState.Run : PlayerLocomotionState.Walk;
        }

        private void SetLocomotionState(PlayerLocomotionState nextState)
        {
            if (locomotionState == nextState)
            {
                return;
            }

            CmdSetLocomotionState(nextState);
        }

        private void TryEmitMovementNoise(PlayerLocomotionState state, float moveMagnitude)
        {
            if (moveMagnitude <= 0.1f || Time.time < nextNoiseTime)
            {
                return;
            }

            float radius;
            float interval;
            float priority;

            switch (state)
            {
                case PlayerLocomotionState.Run:
                    radius = runNoiseRadius * (playerStatus != null ? playerStatus.MovementNoiseMultiplier : 1f);
                    interval = runNoiseInterval;
                    priority = 2f;
                    break;
                case PlayerLocomotionState.Crouch:
                    radius = crouchNoiseRadius;
                    interval = crouchNoiseInterval;
                    priority = 0.35f;
                    break;
                case PlayerLocomotionState.Walk:
                    radius = walkNoiseRadius * (playerStatus != null ? playerStatus.MovementNoiseMultiplier : 1f);
                    interval = walkNoiseInterval;
                    priority = 1f;
                    break;
                default:
                    return;
            }

            nextNoiseTime = Time.time + interval;
            CmdEmitMovementNoise(radius, priority);
        }

        [Command]
        private void CmdSetLocomotionState(PlayerLocomotionState nextState)
        {
            locomotionState = nextState;
        }

        [Command]
        private void CmdEmitMovementNoise(float radius, float priority)
        {
            NoiseSystem.Emit(transform.position, radius, priority, NoiseCategory.PlayerMovement);
        }

        private void ApplyLocalVisualOwnership()
        {
            bool isOwned = isLocalPlayer;
            bool showLocalBody = isOwned && IsLobbyScene();

            if (playerCamera != null)
            {
                playerCamera.enabled = isOwned;
            }

            if (audioListener != null)
            {
                audioListener.enabled = isOwned;
            }

            if (isOwned)
            {
                ApplyRuntimeSettings();
            }

            if (firstPersonHiddenRenderers != null)
            {
                foreach (Renderer renderer in firstPersonHiddenRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.enabled = !isOwned || showLocalBody;
                    }
                }
            }
        }

        private void OnLocomotionStateChanged(PlayerLocomotionState _, PlayerLocomotionState nextState)
        {
            if (animator != null)
            {
                animator.SetInteger("MoveState", (int)nextState);
            }
        }

        private void OnConditionChanged(PlayerCondition nextCondition)
        {
            if (animator != null)
            {
                animator.SetInteger("ConditionState", (int)nextCondition);
            }

            if (!isLocalPlayer)
            {
                return;
            }

            if (nextCondition == PlayerCondition.Knocked ||
                nextCondition == PlayerCondition.Carried ||
                nextCondition == PlayerCondition.Hooked ||
                nextCondition == PlayerCondition.Dead ||
                nextCondition == PlayerCondition.Escaped)
            {
                CmdSetLocomotionState(PlayerLocomotionState.Idle);
            }
            else
            {
                currentYawOffset = 0f;
            }

            UpdateLocalCursorState();
            ApplyLocalVisualOwnership();
        }

        private void OnActiveSceneChanged(Scene _, Scene __)
        {
            if (!isLocalPlayer)
            {
                return;
            }

            cursorUnlockedByUser = false;
            UpdateLocalCursorState();
        }

        private void HandleCursorToggleInput()
        {
            if (IsLobbyScene())
            {
                lobbyLookHeld = Input.GetMouseButton(1);
                cursorUnlockedByUser = false;
                return;
            }

            lobbyLookHeld = false;
            if (!SupportsMouseLookScene())
            {
                cursorUnlockedByUser = false;
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                cursorUnlockedByUser = true;
            }

            if (cursorUnlockedByUser && Input.GetMouseButtonDown(0))
            {
                cursorUnlockedByUser = false;
            }
        }

        private void UpdateLocalCursorState()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            bool canLook = playerStatus == null || playerStatus.CanLookAround;
            bool shouldLock;
            if (IsLobbyScene())
            {
                shouldLock = canLook && lobbyLookHeld;
            }
            else
            {
                shouldLock = SupportsMouseLookScene() && canLook && !cursorUnlockedByUser;
            }

            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }

        private bool IsLobbyScene()
        {
            HorrorNetworkManager manager = HorrorNetworkManager.Instance;
            if (manager == null)
            {
                return false;
            }

            return SceneManager.GetActiveScene().name == manager.LobbySceneName;
        }

        private bool IsGameplayScene()
        {
            HorrorNetworkManager manager = HorrorNetworkManager.Instance;
            if (manager == null)
            {
                return false;
            }

            return SceneManager.GetActiveScene().name == manager.GameplaySceneName;
        }

        private bool SupportsMouseLookScene()
        {
            return IsGameplayScene() || IsLobbyScene();
        }

        private static bool IsCursorCurrentlyLocked()
        {
            return Cursor.lockState == CursorLockMode.Locked && !Cursor.visible;
        }

        private void OnSettingsChanged()
        {
            ApplyRuntimeSettings();
        }

        private void ApplyRuntimeSettings()
        {
            if (!isLocalPlayer || playerCamera == null)
            {
                return;
            }

            playerCamera.fieldOfView = SettingsStore.FieldOfView;
        }

        private void HandleAbilityInput()
        {
            if (playerStatus == null)
            {
                return;
            }

            if (playerStatus.Condition == PlayerCondition.Hooked &&
                playerStatus.HookSelfEscapeAvailable &&
                !playerStatus.HookSelfEscapeRolling &&
                Input.GetKeyDown(KeyCode.F))
            {
                playerStatus.CmdAttemptHookSelfEscape();
                return;
            }

            if (IsLobbyScene() || !playerStatus.CanControlCharacter)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                CmdTryActivateFocusAbility();
            }
        }

        [Command]
        private void CmdTryActivateFocusAbility()
        {
            if (playerStatus == null)
            {
                playerStatus = GetComponent<NetworkPlayerStatus>();
            }

            playerStatus?.ServerActivateFocusAbility();
        }

        private void UpdateCameraMode()
        {
            if (!isLocalPlayer || playerCamera == null || cameraRoot == null)
            {
                return;
            }

            if (IsLobbyScene())
            {
                Vector3 desiredLocalPosition = lobbyCameraOffset;
                playerCamera.transform.localPosition = Vector3.Lerp(
                    playerCamera.transform.localPosition,
                    desiredLocalPosition,
                    Time.deltaTime * lobbyCameraSmoothing);
                playerCamera.transform.localRotation = Quaternion.identity;
                Vector3 focusPoint = transform.position + lobbyCameraLookOffset;
                Quaternion desiredRotation = Quaternion.LookRotation((focusPoint - playerCamera.transform.position).normalized, Vector3.up);
                playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, desiredRotation, Time.deltaTime * lobbyCameraSmoothing);
                return;
            }

            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition,
                Vector3.zero,
                Time.deltaTime * lobbyCameraSmoothing);
            playerCamera.transform.localRotation = Quaternion.identity;
        }
    }
}
