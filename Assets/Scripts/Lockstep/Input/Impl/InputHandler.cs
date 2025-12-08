using System;
using Lockstep.Core;
using Lockstep.Core.Impl;
using Lockstep.Math.Impl;

namespace Lockstep.Input.Impl
{
    /// <summary>
    /// Default input handler implementation
    /// </summary>
    public class InputHandler : IInputHandler
    {
        public int LocalPlayerId { get; set; }
        public bool IsEnabled { get; set; }

        public event Action<ICommand> OnCommandCreated;

        // Input state buffer
        private FPVector2 _moveInput;
        private bool _actionPressed;
        private int _actionId;
        private FPVector3 _targetPosition;
        private bool _hasTargetPosition;

        public void StartCapture()
        {
            IsEnabled = true;
            ClearBuffer();
        }

        public void StopCapture()
        {
            IsEnabled = false;
        }

        public void CaptureInput()
        {
            if (!IsEnabled)
                return;

#if UNITY_2021_1_OR_NEWER
            // Collect move input
            float horizontal = UnityEngine.Input.GetAxisRaw("Horizontal");
            float vertical = UnityEngine.Input.GetAxisRaw("Vertical");
            _moveInput = new FPVector2(horizontal, vertical);

            // Collect action input
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                _actionPressed = true;
                _actionId = 1; // Basic attack
            }
            else if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                _actionPressed = true;
                _actionId = 2; // Move command

                // Convert mouse position to world coordinates
                UnityEngine.Ray ray = UnityEngine.Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
                if (UnityEngine.Physics.Raycast(ray, out UnityEngine.RaycastHit hit, 1000f))
                {
                    _targetPosition = FPVector3.FromVector3(hit.point);
                    _hasTargetPosition = true;
                }
            }

            // Number key skills
            for (int i = 1; i <= 4; i++)
            {
                if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Alpha0 + i))
                {
                    _actionPressed = true;
                    _actionId = 10 + i; // Skills 1~4
                }
            }
#endif
        }

        public ICommand CreateCommand(int tick)
        {
            ICommand command = null;

            // Process move input
            if (_moveInput.sqrMagnitude > FP64.FromFloat(0.01f))
            {
                var moveCmd = new MoveCommand(
                    LocalPlayerId,
                    tick,
                    _moveInput.x.RawValue,
                    0,
                    _moveInput.y.RawValue
                );
                command = moveCmd;
            }
            // Process action input
            else if (_actionPressed)
            {
                var actionCmd = new ActionCommand(LocalPlayerId, tick, _actionId)
                {
                    PositionX = _hasTargetPosition ? _targetPosition.x.RawValue : 0,
                    PositionY = _hasTargetPosition ? _targetPosition.y.RawValue : 0,
                    PositionZ = _hasTargetPosition ? _targetPosition.z.RawValue : 0
                };
                command = actionCmd;
            }
            // No input
            else
            {
                command = new EmptyCommand(LocalPlayerId, tick);
            }

            // Clear buffer (one command per tick)
            ClearBuffer();

            OnCommandCreated?.Invoke(command);
            return command;
        }

        public void ClearBuffer()
        {
            _moveInput = FPVector2.Zero;
            _actionPressed = false;
            _actionId = 0;
            _hasTargetPosition = false;
        }

        /// <summary>
        /// Set move input externally
        /// </summary>
        public void SetMoveInput(FPVector2 input)
        {
            _moveInput = input;
        }

        /// <summary>
        /// Set action input externally
        /// </summary>
        public void SetActionInput(int actionId, FPVector3? targetPosition = null)
        {
            _actionPressed = true;
            _actionId = actionId;
            if (targetPosition.HasValue)
            {
                _targetPosition = targetPosition.Value;
                _hasTargetPosition = true;
            }
        }
    }

    /// <summary>
    /// Input state implementation
    /// </summary>
    [Serializable]
    public struct InputState : IInputState
    {
        public int Tick { get; set; }
        public int PlayerId { get; set; }

        public long MoveX;
        public long MoveY;
        public int ActionId;
        public int TargetEntityId;
        public long TargetX;
        public long TargetY;
        public long TargetZ;

        public bool IsEmpty => MoveX == 0 && MoveY == 0 && ActionId == 0;

        public IInputState Clone()
        {
            return new InputState
            {
                Tick = Tick,
                PlayerId = PlayerId,
                MoveX = MoveX,
                MoveY = MoveY,
                ActionId = ActionId,
                TargetEntityId = TargetEntityId,
                TargetX = TargetX,
                TargetY = TargetY,
                TargetZ = TargetZ
            };
        }

        public bool Equals(IInputState other)
        {
            if (other is InputState otherState)
            {
                return Tick == otherState.Tick &&
                       PlayerId == otherState.PlayerId &&
                       MoveX == otherState.MoveX &&
                       MoveY == otherState.MoveY &&
                       ActionId == otherState.ActionId &&
                       TargetEntityId == otherState.TargetEntityId;
            }
            return false;
        }
    }
}
