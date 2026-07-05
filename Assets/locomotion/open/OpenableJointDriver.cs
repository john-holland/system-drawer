using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>Drives hinge/configurable joint open and close motion.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class OpenableJointDriver : MonoBehaviour
    {
        public float targetOpenAngle = 90f;
        public float openSpeed = 120f;
        public bool usePhysicsMotor = true;
        public bool snapClosedOnRelease;
        public OpenableJointState state = OpenableJointState.Closed;

        HingeJoint _hinge;
        ConfigurableJoint _configurable;
        float _currentAngle;
        float _closedAngle;
        OpenableLatch _latch;

        public bool IsOpen => state == OpenableJointState.Open;
        public bool IsClosed => state == OpenableJointState.Closed || state == OpenableJointState.Locked;
        public float CurrentAngle => _currentAngle;

        void Awake()
        {
            _hinge = GetComponent<HingeJoint>();
            _configurable = GetComponent<ConfigurableJoint>();
            _latch = GetComponent<OpenableLatch>();
            _closedAngle = transform.localEulerAngles.y;
            if (_hinge != null)
                _closedAngle = _hinge.angle;
            _currentAngle = _closedAngle;
            RefreshLockedState();
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
            state = OpenableJointState.Opening;
            return true;
        }

        public bool BeginClose()
        {
            if (state == OpenableJointState.Locked)
                return false;
            state = OpenableJointState.Closing;
            return true;
        }

        void FixedUpdate()
        {
            if (state != OpenableJointState.Opening && state != OpenableJointState.Closing)
                return;

            float target = state == OpenableJointState.Opening
                ? _closedAngle + targetOpenAngle
                : _closedAngle;

            if (usePhysicsMotor && _hinge != null)
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

            if (state == OpenableJointState.Opening && Mathf.Abs(_currentAngle - target) < 1f)
                state = OpenableJointState.Open;
            if (state == OpenableJointState.Closing && Mathf.Abs(_currentAngle - _closedAngle) < 1f)
                state = OpenableJointState.Closed;
        }

        public void ForceOpen()
        {
            _currentAngle = _closedAngle + targetOpenAngle;
            state = OpenableJointState.Open;
        }

        public void ForceClosed()
        {
            _currentAngle = _closedAngle;
            state = OpenableJointState.Closed;
        }
    }
}
