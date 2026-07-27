using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Drives hinge/configurable joint open and close motion (physics, animation, or hybrid).</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class OpenableJointDriver : MonoBehaviour
    {
        public float targetOpenAngle = 90f;
        public float openSpeed = 120f;
        public bool usePhysicsMotor = true;
        public bool snapClosedOnRelease;
        public OpenableJointState state = OpenableJointState.Closed;
        public OpenCloseDriveMode driveMode = OpenCloseDriveMode.Hybrid;
        public string openAnimationRef;
        public string closeAnimationRef;
        [Range(0f, 1f)] public float animationNormalizedTime;
        public Animator animator;
        public string animatorOpenParam = "Open01";

        HingeJoint _hinge;
        ConfigurableJoint _configurable;
        float _currentAngle;
        float _closedAngle;
        OpenableLatch _latch;
        float _open01;

        public bool IsOpen => state == OpenableJointState.Open;
        public bool IsClosed => state == OpenableJointState.Closed || state == OpenableJointState.Locked;
        public float CurrentAngle => _currentAngle;
        /// <summary>Normalized open progress 0..1 for radial / instrument consumers.</summary>
        public float Open01 => _open01;

        public bool IsAnimationReady
        {
            get
            {
                if (driveMode == OpenCloseDriveMode.Physics)
                    return true;
                if (animator != null)
                    return true;
                return !string.IsNullOrEmpty(openAnimationRef) || animationNormalizedTime >= 0f;
            }
        }

        void Awake()
        {
            _hinge = GetComponent<HingeJoint>();
            _configurable = GetComponent<ConfigurableJoint>();
            _latch = GetComponent<OpenableLatch>();
            if (animator == null)
                animator = GetComponent<Animator>();
            _closedAngle = transform.localEulerAngles.y;
            if (_hinge != null)
                _closedAngle = _hinge.angle;
            _currentAngle = _closedAngle;
            RefreshLockedState();
            RefreshOpen01();
        }

        public void ApplyProfile(OpenCloseBeatProfile profile)
        {
            if (profile == null)
                return;
            if (profile.openAngleDeg > 0f)
                targetOpenAngle = profile.openAngleDeg;
            driveMode = profile.driveMode;
            openAnimationRef = profile.openAnimationRef;
            closeAnimationRef = profile.closeAnimationRef;
            usePhysicsMotor = driveMode == OpenCloseDriveMode.Physics
                || (driveMode == OpenCloseDriveMode.Hybrid && usePhysicsMotor);
        }

        void RefreshLockedState()
        {
            if (_latch != null && !_latch.isUnlocked)
                state = OpenableJointState.Locked;
            else if (state == OpenableJointState.Locked)
                state = OpenableJointState.Closed;
        }

        public bool CanOpen()
        {
            RefreshLockedState();
            return state != OpenableJointState.Locked;
        }

        public bool BeginOpen()
        {
            if (!CanOpen())
                return false;
            if ((driveMode == OpenCloseDriveMode.Animation || driveMode == OpenCloseDriveMode.Hybrid)
                && !IsAnimationReady
                && driveMode == OpenCloseDriveMode.Animation)
                return false;
            state = OpenableJointState.Opening;
            animationNormalizedTime = 0f;
            return true;
        }

        public bool BeginClose()
        {
            if (state == OpenableJointState.Locked)
                return false;
            state = OpenableJointState.Closing;
            animationNormalizedTime = 1f;
            return true;
        }

        /// <summary>External animation gate: set normalized clip time (0..1) to drive open01 in Animation mode.</summary>
        public void SetAnimationProgress(float normalized01)
        {
            animationNormalizedTime = Mathf.Clamp01(normalized01);
            if (driveMode != OpenCloseDriveMode.Animation && driveMode != OpenCloseDriveMode.Hybrid)
                return;

            float open01 = state == OpenableJointState.Closing
                ? 1f - animationNormalizedTime
                : animationNormalizedTime;
            ApplyAngleFromOpen01(open01);
            if (state == OpenableJointState.Opening && _open01 >= 0.99f)
                state = OpenableJointState.Open;
            else if (state == OpenableJointState.Closing && _open01 <= 0.01f)
                state = OpenableJointState.Closed;
        }

        public void SetOpen01(float open01)
        {
            ApplyAngleFromOpen01(Mathf.Clamp01(open01));
            if (_open01 >= 0.99f)
                state = OpenableJointState.Open;
            else if (_open01 <= 0.01f)
                state = OpenableJointState.Closed;
        }

        void FixedUpdate()
        {
            if (state != OpenableJointState.Opening && state != OpenableJointState.Closing)
                return;

            if (driveMode == OpenCloseDriveMode.Animation)
            {
                TickAnimationDrive();
                return;
            }

            if (driveMode == OpenCloseDriveMode.Hybrid && animator != null && !string.IsNullOrEmpty(openAnimationRef))
            {
                TickAnimationDrive();
                if (usePhysicsMotor && _hinge != null)
                    TickPhysicsTowardTarget();
                return;
            }

            TickPhysicsTowardTarget();
        }

        void TickAnimationDrive()
        {
            float target01 = state == OpenableJointState.Opening ? 1f : 0f;
            float step = (openSpeed / Mathf.Max(1f, targetOpenAngle)) * Time.fixedDeltaTime;
            if (animator != null && !string.IsNullOrEmpty(animatorOpenParam))
            {
                float current = animator.GetFloat(animatorOpenParam);
                float next = Mathf.MoveTowards(current, target01, step);
                animator.SetFloat(animatorOpenParam, next);
                animationNormalizedTime = next;
                ApplyAngleFromOpen01(next);
            }
            else
            {
                animationNormalizedTime = Mathf.MoveTowards(animationNormalizedTime, target01, step);
                ApplyAngleFromOpen01(animationNormalizedTime);
            }

            if (state == OpenableJointState.Opening && _open01 >= 0.99f)
                state = OpenableJointState.Open;
            if (state == OpenableJointState.Closing && _open01 <= 0.01f)
                state = OpenableJointState.Closed;
        }

        void TickPhysicsTowardTarget()
        {
            float target = state == OpenableJointState.Opening
                ? _closedAngle + targetOpenAngle
                : _closedAngle;

            if (usePhysicsMotor && _hinge != null && driveMode != OpenCloseDriveMode.Animation)
            {
                var motor = _hinge.motor;
                motor.force = 100f;
                motor.targetVelocity = state == OpenableJointState.Opening ? openSpeed : -openSpeed;
                _hinge.motor = motor;
                _hinge.useMotor = true;
                _currentAngle = _hinge.angle;
            }
            else
            {
                _currentAngle = Mathf.MoveTowards(_currentAngle, target, openSpeed * Time.fixedDeltaTime);
                transform.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);
            }

            RefreshOpen01();

            if (state == OpenableJointState.Opening && Mathf.Abs(_currentAngle - target) < 1f)
                state = OpenableJointState.Open;
            if (state == OpenableJointState.Closing && Mathf.Abs(_currentAngle - _closedAngle) < 1f)
                state = OpenableJointState.Closed;
        }

        void ApplyAngleFromOpen01(float open01)
        {
            _open01 = Mathf.Clamp01(open01);
            _currentAngle = _closedAngle + targetOpenAngle * _open01;
            if (_hinge == null || !usePhysicsMotor || driveMode == OpenCloseDriveMode.Animation)
                transform.localRotation = Quaternion.Euler(0f, _currentAngle, 0f);
        }

        void RefreshOpen01()
        {
            if (Mathf.Abs(targetOpenAngle) < 0.001f)
            {
                _open01 = state == OpenableJointState.Open ? 1f : 0f;
                return;
            }
            _open01 = Mathf.Clamp01((_currentAngle - _closedAngle) / targetOpenAngle);
        }

        public void ForceOpen()
        {
            ApplyAngleFromOpen01(1f);
            state = OpenableJointState.Open;
        }

        public void ForceClosed()
        {
            ApplyAngleFromOpen01(0f);
            state = OpenableJointState.Closed;
        }
    }
}
