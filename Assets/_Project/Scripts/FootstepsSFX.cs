using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FootstepsSFX : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController; // можно назначить в инспекторе; если не назначен — будет автопоиск в Awake

    [Header("Audio")]
    public AudioClip[] footstepSounds;
    public AudioClip jumpSound;
    public AudioClip landingSound;

    [Header("Timing")]
    public float stepInterval = 0.5f;
    [Tooltip("How much faster steps get when sprinting (LeftShift)")]
    public float sprintStepMultiplier = 1.5f;

    private AudioSource audioSource;
    private float stepTimer = 0f;
    private CharacterController charController;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (playerController != null)
            charController = playerController.GetComponent<CharacterController>();

        // start with timer 0 so first step will play after interval while moving
        stepTimer = 0f;
    }

    private void Update()
    {
        if (playerController == null)
            return;

        bool grounded = playerController.isGrounded;
        Vector3 velocity = Vector3.zero;
        if (charController != null)
            velocity = charController.velocity;

        // consider player moving if horizontal velocity magnitude > threshold
        Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);
        bool isMoving = horizontalVel.sqrMagnitude > 0.01f; // ~0.1 magnitude

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        if (grounded && isMoving)
        {
            stepTimer -= Time.deltaTime;
            float interval = isSprinting ? (stepInterval / sprintStepMultiplier) : stepInterval;
            if (stepTimer <= 0f)
            {
                PlayRandomFootstep();
                stepTimer = interval;
            }
        }
        else
        {
            // reset timer so steps don't immediately spam when moving restarts;
            // set to a small positive to create a short delay on resume
            stepTimer = 0.05f;
        }
    }

	private void PlayRandomFootstep()
	{
		if (footstepSounds == null || footstepSounds.Length == 0) return;

		int idx = Random.Range(0, footstepSounds.Length);
		AudioClip clip = footstepSounds[idx];
		if (clip == null) return;

		// random pitch ±20% (0.8 - 1.2)
		float randomPitch = Random.Range(0.8f, 1.2f);
		audioSource.pitch = randomPitch;
		audioSource.PlayOneShot(clip);
		audioSource.pitch = 1f; // сбрасываем на дефолт
}

    // Optional: вызови эти методы из PlayerController, если будешь добавлять прыжок/детекцию приземления.
    public void PlayJump()
    {
        if (jumpSound != null)
            audioSource.PlayOneShot(jumpSound);
    }

    public void PlayLanding()
    {
        if (landingSound != null)
            audioSource.PlayOneShot(landingSound);
    }
}
