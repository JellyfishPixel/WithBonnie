using UnityEngine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Header("Water Slow")]
        [Tooltip("Tag ของน้ำในฉาก (Collider ที่เป็นน้ำ)")]
        public string WaterTag = "Water";

        [Tooltip("ค่า multiplier สำหรับความเร็วเมื่ออยู่ในน้ำ (เช่น 0.4 = เหลือ 40%)")]
        public float WaterSpeedMultiplier = 0.4f;

        // runtime flag
        private bool _inWater = false;

        private bool _dustPlaying;
        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        public Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        public bool _hasAnimator;
        private bool _movementLocked = false;

        [Header("Footstep Timer")]
        public float FootstepRate = 0.5f;
        private float _footstepTimer;
        [Header("Jump Sound")]
        public AudioClip JumpAudioClip;
        private bool _wasGrounded;
        [Range(0, 1)]
        public float JumpAudioVolume = 0.6f;
        public UnityEngine.VFX.VisualEffect JumpFX;
        private bool _wasGroundedLastFrame;
        [Header("Dust Effect")]
        public UnityEngine.VFX.VisualEffect DustFX;
        public float DustMinSpeed = 0.5f;
        public float DustMaxRate = 50f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        void OnEnable()
        {

            _cinemachineTargetYaw = 0f;
            _cinemachineTargetPitch = 0f;

            if (CinemachineCameraTarget != null)
            {
                CinemachineCameraTarget.transform.localRotation = Quaternion.identity;
            }
        }
        private void OnDisable()
        {

        }
        public void HardResetCamera()
        {
            _cinemachineTargetYaw = 0f;
            _cinemachineTargetPitch = 0f;

            if (CinemachineCameraTarget != null)
            {
                CinemachineCameraTarget.transform.localRotation = Quaternion.identity;
            }
        }
        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _animator = GetComponentInChildren<Animator>();
            _hasAnimator = _animator != null;

            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            _animator = GetComponentInChildren<Animator>();
            _hasAnimator = _animator != null;

        }

        private void Update()
        {


            if (_movementLocked)
            {
                // รีเซ็ตความเร็วไว้ กันลื่น/ไหล
                _speed = 0f;
                _verticalVelocity = 0f;
                return;
            }
            JumpAndGravity();
            GroundedCheck();
            Move();
            HandleFootsteps();
            HandleLandingSound();
            HandleDust();

        }

        private void LateUpdate()
        {
            if (_movementLocked) return;

            CameraRotation();
        }
        public void SetLookAngles(float pitch, float yaw)
        {
            _cinemachineTargetPitch = pitch;
            _rotationVelocity = 0f;

            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

        public void LockMovement()
        {
            _movementLocked = true;
            _verticalVelocity = 0f;
            _speed = 0f;

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, 0f);
                _animator.SetFloat(_animIDMotionSpeed, 0f);
            }
        }

        public void UnlockMovement()
        {
            _movementLocked = false;
            ForceGround();
        }
        public void ForceGround()
        {
            if (!_controller) return;

            // ดึงลงเบา ๆ ให้ CC รับรู้ว่าติดพื้น
            _controller.Move(Vector3.down * 0.02f);

            // รีเซ็ตความเร็วแนวดิ่ง
            _verticalVelocity = -2f;
            // ❗ ไม่ต้อง set grounded เอง
            // ใช้ _controller.isGrounded ใน Update แทน
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(WaterTag))
            {
                _inWater = true;
                Debug.Log("[TPC] Enter water -> slow movement");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(WaterTag))
            {
                _inWater = false;
                Debug.Log("[TPC] Exit water -> normal movement");
            }
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }
        public void SetVerticalVelocity(float v)
        {
            _verticalVelocity = v;
        }
        public void AddJumpForce(float force)
        {
            SetVerticalVelocity(force);

            // ใช้เสียงเดียวกับ Jump
            if (JumpAudioClip != null)
            {
                PlayJumpSound();
            }
        }
        private void HandleLandingSound()
        {
            if (!_wasGrounded && Grounded)
            {
                if (LandingAudioClip != null)
                {
                    AudioManager.Instance.PlaySFX(
                     LandingAudioClip,
                     transform.position);
                }
            }

            _wasGrounded = Grounded;
        }
        private void Move()
        {
            if (_controller == null) return;
            if (!_controller.enabled) return;
            if (!gameObject.activeInHierarchy) return;


            // set target speed based on move speed, sprint speed and if sprint is pressed
            float baseWalkSpeed = MoveSpeed;
            float baseSprintSpeed = SprintSpeed;
            bool isSprinting = _input.sprint;

            // 🔹 ถ้าอยู่ในน้ำ → ช้าลง
            if (_inWater)
            {
                baseWalkSpeed *= WaterSpeedMultiplier;
                baseSprintSpeed *= WaterSpeedMultiplier;
                isSprinting = false; // ในน้ำไม่ให้ sprint
            }

            float targetSpeed = isSprinting ? baseSprintSpeed : baseWalkSpeed;


            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }
        private void HandleDust()
        {
            if (DustFX == null) return;

            float realSpeed =
                new Vector3(_controller.velocity.x, 0, _controller.velocity.z).magnitude;

            bool walking =
                Grounded &&
                realSpeed > 0.2f;

            DustFX.SetBool("Walking", walking);

            DustFX.SetFloat(
                "Speed",
                walking ? realSpeed / SprintSpeed : 0f
            );
        }
        public void ForceResetYaw(float yaw)
        {
            _cinemachineTargetYaw = yaw;
            _cinemachineTargetPitch = 0f;

            if (CinemachineCameraTarget != null)
            {
                CinemachineCameraTarget.transform.localRotation =
                Quaternion.Euler(0f, yaw, 0f);
            }
        }
        private void HandleFootsteps()
        {
            if (!Grounded) return;
            if (_speed < 0.1f) return;

            _footstepTimer -= Time.deltaTime;

            if (_footstepTimer <= 0f)
            {
                PlayFootstep();

                // 🔥 ปรับความเร็วตามการเดิน/วิ่ง
                float speedPercent = Mathf.Clamp01(_speed / SprintSpeed);

                float dynamicRate =
                    Mathf.Lerp(FootstepRate, FootstepRate * 0.5f, speedPercent);

                _footstepTimer = dynamicRate;
            }
        }
        private void PlayFootstep()
        {
            if (FootstepAudioClips.Length == 0) return;

            int index = Random.Range(0, FootstepAudioClips.Length);

            AudioManager.Instance.PlaySFX(
                FootstepAudioClips[index],
                transform.position);
        }
        void PlayJumpSound()
        {
            if (JumpAudioClip == null) return;

            AudioManager.Instance.PlaySFX(
                JumpAudioClip,
                transform.position
            );
        }
        private void JumpAndGravity()
        {
            bool justJumped = false;

            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    PlayJumpSound();

                    if (_hasAnimator)
                        _animator.SetBool(_animIDJump, true);

                    justJumped = true;
                }

                if (_jumpTimeoutDelta >= 0.0f)
                    _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                        _animator.SetBool(_animIDFreeFall, true);
                }

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
                _verticalVelocity += Gravity * Time.deltaTime;

            // 🔥 เล่น JumpFX แค่ตอน "เริ่มกระโดดจริง"
            if (justJumped && JumpFX != null)
            {
                JumpFX.Stop();
                JumpFX.Reinit();
                JumpFX.Play();
            }

            _wasGroundedLastFrame = Grounded;
        }
        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

    }
}