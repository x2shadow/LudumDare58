using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Interaction Settings")]
    public LayerMask playerMask;

    [Header("Настройки движения")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 1.0f;

    [Header("Sprint / Crouch")]
    [Tooltip("Множитель скорости при беге")]
    public float sprintMultiplier = 1.7f;
    [Tooltip("Множитель скорости при приседе")]
    public float crouchSpeedMultiplier = 0.5f;
    [Tooltip("Высота CharacterController в стоячем состоянии")]
    public float standingHeight = 2.0f;
    [Tooltip("Высота CharacterController в приседе")]
    public float crouchHeight = 1.0f;
    [Tooltip("Скорость перехода между высотами")]
    public float crouchTransitionSpeed = 8f;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private CharacterController characterController;

    [Header("Камера игрока")]
    public Camera playerCamera;
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 90.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -90.0f;
    [Tooltip("Invert Y look (true = typical 'flight' invert)")]
    public bool invertY = false;

    // cinemachine
    private float cinemachineTargetPitch;
    private float rotationVelocity;
    private const float threshold = 0.01f;

    [Header("Camera Shake")]
    public PlayerCameraShake playerCameraShake;

    [HideInInspector]
    public InputActions inputActions;

    [Header("Пауза")]
    [SerializeField] private GameObject pauseCanvas;
    private bool isPaused = false;

    private bool isInputBlocked = false;
    private bool interactionsBlocked = false;
    private bool pcInteractionBlocked = false;
    private bool onlyAllowSleep = false; // если true — можно взаимодействовать только со SleepSpot

    [Header("Дебаг")]
    public bool isDialogueActive;
    public DialogueScript dialogueCantUsePC;
    public DialogueScript dialogueGottaSleep;

    [Header("Physics / Ground check")]
    public Transform groundCheck;
    public float groundDistance = 0.18f;
    [SerializeField] LayerMask groundMask;
    public float gravity = -9.81f;
    public bool isGrounded = false;

    private Vector3 velocity;

    [Header("Взаимодействие")]
    public float interactDistance = 3f; // дальность луча
    public LayerMask interactMask; // слои для проверки
    public LayerMask obstacleMask; // слои препятствий
    public GameObject interactPromptUI; // UI-элемент "E" в Canvas
    public DialogueRunner dialogueRunner;

    private IInteractable currentInteractable;
    private InteractionIndicatorManager indicatorManager;

    [Header("Frame Rate Settings")]
    public int targetFrameRate = 60;

    // Sprint/Crouch internal states
    private bool isSprinting = false;
    private bool isCrouching = false;
    private float currentHeight;
    private Vector3 cameraInitialLocalPos;
    private Vector3 cameraCrouchLocalPos;
    private float originalControllerHeight;
    private Vector3 originalControllerCenter;

    //
    public bool IsInputBlockedPublic()
    {
        return isInputBlocked;
    }

    public bool IsInteractionBlockedPublic()
    {
        return interactionsBlocked;
    }

    public bool IsOnlyAllowSleepPublic()
    {
        return onlyAllowSleep;
    }

    private bool IsCurrentDeviceMouse
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return inputActions.Player.Look.activeControl?.device is UnityEngine.InputSystem.Mouse;
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        inputActions = new InputActions();
        characterController = GetComponent<CharacterController>();
        playerCamera = Camera.main;

        originalControllerHeight = characterController.height;
        originalControllerCenter = characterController.center;
        currentHeight = characterController.height;

        cameraInitialLocalPos = CinemachineCameraTarget.transform.localPosition;
        float heightDiff = Mathf.Max(0f, standingHeight - crouchHeight);
        cameraCrouchLocalPos = cameraInitialLocalPos - new Vector3(0f, heightDiff * 0.5f, 0f);

        indicatorManager = InteractionIndicatorManager.Instance;
    }

    void Start()
    {
        float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
        mouseSensitivity = savedSensitivity;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

#if !UNITY_EDITOR
            ApplyFrameRateSettings();
#endif
    }

    private void OnEnable()
    {
        inputActions.Enable();
        // Подписка на события для экшенов
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Look.performed += OnLook;
        inputActions.Player.Look.canceled += OnLook;
        //inputActions.Player.Click.performed += OnClick;
        inputActions.Player.Interact.performed += OnInteract;
        inputActions.Player.Flashlight.performed += OnFlashlight;
        inputActions.Player.Pause.performed += OnPause;

        inputActions.Player.Sprint.performed += OnSprintPerfomed;
        inputActions.Player.Sprint.canceled += OnSprintCanceled;
        inputActions.Player.Crouch.performed += OnCrouch;
        
        if (InteractionIndicatorManager.Instance != null)
        InteractionIndicatorManager.Instance.OnSuppressionChanged += OnIndicatorSuppressionChanged;
    }

    private void OnDisable()
    {
        inputActions.Disable();
        // Отписка от событий
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Look.performed -= OnLook;
        inputActions.Player.Look.canceled -= OnLook;
        //inputActions.Player.Click.performed -= OnClick;
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Player.Flashlight.performed -= OnFlashlight;
        inputActions.Player.Pause.performed -= OnPause;

        inputActions.Player.Sprint.performed -= OnSprintPerfomed;
        inputActions.Player.Sprint.canceled -= OnSprintCanceled;
        inputActions.Player.Crouch.performed -= OnCrouch;
        
        if (InteractionIndicatorManager.Instance != null)
        InteractionIndicatorManager.Instance.OnSuppressionChanged -= OnIndicatorSuppressionChanged;
    }

    private void Update()
    {
        if (isInputBlocked) return;

        HandleMovement();
        HandleInteractionRay();
        HandleDebugKeys();
        HandleCrouchHeightTransition();
    }

    void LateUpdate()
    {
        HandleLook();

        // HandleCameraShake
        if (playerCameraShake != null)
        {
            Vector3 horizVel = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
            float maxPossibleSpeed = moveSpeed * sprintMultiplier; // можно изменить логику при желании
            playerCameraShake.UpdateShake(horizVel, maxPossibleSpeed, isSprinting, isGrounded);
        }
    }

    void HandleMovement()
    {
        // Движение персонажа
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move = move.normalized; // нормализация

        // вычисляем текущую скорость с учётом Sprint/Crouch
        float speedMultiplier = 1f;
        if (isSprinting) speedMultiplier *= sprintMultiplier;
        if (isCrouching) speedMultiplier *= crouchSpeedMultiplier;
        float effectiveSpeed = moveSpeed * speedMultiplier;

        // Проверка земли
        if (groundCheck != null)
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        else
            isGrounded = characterController.isGrounded;

        // если на земле и идёт небольшая вниз. скорость — удерживаем на небольшом значении, чтобы персонаж "прислонялся" к земле
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        // применяем гравитацию
        velocity.y += gravity * Time.deltaTime;

        // объединяем движение
        Vector3 finalMove = move * effectiveSpeed + velocity;

        // двигаем CharacterController
        characterController.Move(finalMove * Time.deltaTime);
    }

    void HandleLook()
    {
        // if there is an input
        if (lookInput.sqrMagnitude >= threshold)
        {
            //Don't multiply mouse input by Time.deltaTime
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            // обработка инверсии по Y
            float yInput = invertY ? lookInput.y : -lookInput.y;

            cinemachineTargetPitch += yInput * mouseSensitivity * deltaTimeMultiplier;
            rotationVelocity = lookInput.x * mouseSensitivity * deltaTimeMultiplier;

            // clamp our pitch rotation
            cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, BottomClamp, TopClamp);

            // Update Cinemachine camera target pitch
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(cinemachineTargetPitch, 0.0f, 0.0f);

            // rotate the player left and right
            transform.Rotate(Vector3.up * rotationVelocity);
        }
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        angle %= 360f; // нормализуем в диапазон -360...360
        if (angle < -180f) angle += 360f; // теперь диапазон -180...180
        return Mathf.Clamp(angle, min, max);
    }

    void OnDrawGizmos()
    {
        if (isGrounded)
        {
            Gizmos.color = Color.green;
            //Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundDistance);
        }
        else Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
    }

    private void OnIndicatorSuppressionChanged(bool suppressed)
    {
        // если suppression снят — обновим луч/иконку сразу
        // Если suppressed==true, возможно нужно скрыть подсказку немедленно
        // используем HandleInteractionRay() — оно отключит или включит UI правильно
        HandleInteractionRay();
    }

    private void HandleInteractionRay()
    {
        //Debug.Log($"Suppressed={InteractionIndicatorManager.Instance?.IsSuppressed}, HasDisc={GetComponent<PlayerHoldItem>()?.HasDisc}, onlyAllowSleep={onlyAllowSleep}, pcInteractionBlocked={pcInteractionBlocked}, isInputBlocked={isInputBlocked}");

        if (InteractionIndicatorManager.Instance != null && InteractionIndicatorManager.Instance.IsSuppressed)
        {
            interactPromptUI.SetActive(false);
            currentInteractable = null;
            return;
        }

        currentInteractable = null;
        interactPromptUI.SetActive(false);

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        //if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask | obstacleMask))
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, (interactMask | obstacleMask) & ~playerMask)) // Исключаем слой игрока
        {
            currentInteractable = hit.collider.GetComponent<IInteractable>();

            if (currentInteractable != null)
            {
                if (currentInteractable.GetUsed()) return;

                // Если глобально запрещено показывать подсказку — не показываем
                if (InteractionIndicatorManager.Instance != null && InteractionIndicatorManager.Instance.IsSuppressed)
                {
                    interactPromptUI.SetActive(false);
                }
                else
                {
                    interactPromptUI.SetActive(true);
                }
            }

            /* Мой старый код
            if (currentInteractable != null)
            {
                if (currentInteractable.GetUsed()) return;
                interactPromptUI.SetActive(true);
            }
            */
        }
    }

    private void OnInteract(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;
        if (context.performed && currentInteractable != null)
        {
            // Если игрок держит диск, то запрещаем взаимодействие с терминалом ПК
            var hold = GetComponent<PlayerHoldItem>();
            if (hold != null && hold.HasDisc && currentInteractable is PCTerminal)
            {
                Debug.Log("You can't use the PC while holding a disc. Deposit it first.");
                dialogueRunner.StartDialogue(dialogueCantUsePC, 0);
                return;
            }

            // Если ПК заблокирован (после сдачи сюжетного диска) — запретим использовать именно ПК
            if (pcInteractionBlocked && currentInteractable is PCTerminal)
            {
                Debug.Log("PC is locked right now. You must sleep before using it.");
                dialogueRunner.StartDialogue(dialogueGottaSleep, 0);
                return;
            }

            // обычный интеракт
            currentInteractable.Interact(this);
        }
    }

    private void OnFlashlight(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;
        if (context.performed)
        {
            Debug.Log("Flashlight");
        }
    }

    private void HandleDebugKeys()
    {
        if (Keyboard.current != null && !isInputBlocked)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                Debug.Log("1");
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.Log("2");
            }

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                Debug.Log("3");    
            }

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                Debug.Log("4");
            }
        }
    }

    public void ApplyFrameRateSettings()
    {
        // Установка целевого FPS
        Application.targetFrameRate = targetFrameRate;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;
        lookInput = context.ReadValue<Vector2>();
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pauseCanvas.SetActive(isPaused);

        if (isPaused)
        {
            Time.timeScale = 0f;  // Останавливаем время
            SetInputBlocked(true); // Блокируем управление
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;  // Возвращаем время
            SetInputBlocked(false); // Возвращаем управление
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SetInputBlocked(bool blocked)
    {
        isInputBlocked = blocked;
        if (blocked)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
        }

        // синхронизируем с индикатором — если ввод заблокирован, скрываем подсказку
        if (InteractionIndicatorManager.Instance != null)
        {
            if (blocked) InteractionIndicatorManager.Instance.Suppress();
            else InteractionIndicatorManager.Instance.Release();
        }
    }

    public void SetInputBlocked2(bool blocked)
    {
        isInputBlocked = blocked;
    }

    public void SetInteractionBlocked(bool blocked)
    {
        interactionsBlocked = blocked;
    }

    public void SetInteractionOnlySleepMode(bool onlySleep)
    {
        onlyAllowSleep = onlySleep;
        // оставляем движение/смотреть — но при попытке Interact проверяем флаг
    }

    public void SetPCBlocked(bool blocked)
    {
        pcInteractionBlocked = blocked;
    }
    public bool IsPCBlocked() => pcInteractionBlocked;
    
    private void OnSprintPerfomed(InputAction.CallbackContext ctx)
    {
        if (isCrouching) return; // не бегаем в приседе
        isSprinting = true;
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        isSprinting = false;
    }

    private void OnCrouch(InputAction.CallbackContext ctx)
    {
        if (!isCrouching)
        {
            // садимся
            isCrouching = true;
            isSprinting = false;
        }
        else
        {
            isCrouching = false;
        }
    }

    private void HandleCrouchHeightTransition()
    {
        float desiredHeight = isCrouching ? crouchHeight : standingHeight;
        float newHeight = Mathf.Lerp(characterController.height, desiredHeight, Time.deltaTime * crouchTransitionSpeed);

        // Вычисляем смещение центра относительно оригинального центра (чтобы не "тонуть" в землю)
        float heightDeltaFromOriginal = newHeight - originalControllerHeight;
        Vector3 newCenter = originalControllerCenter + Vector3.up * (heightDeltaFromOriginal / 2f);

        characterController.height = newHeight;
        characterController.center = newCenter;

        if (CinemachineCameraTarget != null)
        {
            Vector3 targetCamPos = isCrouching ? cameraCrouchLocalPos : cameraInitialLocalPos;
            CinemachineCameraTarget.transform.localPosition = Vector3.Lerp(CinemachineCameraTarget.transform.localPosition, targetCamPos, Time.deltaTime * crouchTransitionSpeed);
        }
    }
}
