using System;
using System.Collections.Generic;
using xpTURN.Lockstep.Core;

namespace xpTURN.Lockstep.Replay.Impl
{
    /// <summary>
    /// Replay player implementation
    /// Plays back recorded replay data
    /// </summary>
    public class ReplayPlayer : IReplayPlayer
    {
        private IReplayData _replayData;
        private ReplayState _state = ReplayState.Idle;
        private int _currentTick;
        private ReplaySpeed _speed = ReplaySpeed.Normal;
        private float _accumulator;
        
        // Cached empty list
        private static readonly List<ICommand> EmptyCommandList = new List<ICommand>();

        public ReplayState State => _state;
        public int CurrentTick => _currentTick;
        public int TotalTicks => _replayData?.Metadata.TotalTicks ?? 0;
        
        public ReplaySpeed Speed
        {
            get => _speed;
            set => _speed = value;
        }

        public float Progress
        {
            get
            {
                if (TotalTicks <= 0) return 0f;
                return (float)_currentTick / TotalTicks;
            }
        }

        public event Action<int, IReadOnlyList<ICommand>> OnTickPlayed;
        public event Action OnPlaybackFinished;
        public event Action<int> OnSeekCompleted;

        public void Load(IReplayData replayData)
        {
            if (replayData == null)
            {
                LockstepLogger.Error("[ReplayPlayer] Cannot load null replay data");
                return;
            }

            _replayData = replayData;
            _currentTick = 0;
            _accumulator = 0;
            _state = ReplayState.Idle;
            
            LockstepLogger.Info($"[ReplayPlayer] Replay loaded - Ticks: {replayData.Metadata.TotalTicks}, Duration: {replayData.Metadata.DurationMs}ms");
        }

        public void Play()
        {
            if (_replayData == null)
            {
                LockstepLogger.Error("[ReplayPlayer] No replay data loaded");
                return;
            }

            if (_state == ReplayState.Finished)
            {
                // Restart from beginning
                _currentTick = 0;
                _accumulator = 0;
            }

            _state = ReplayState.Playing;
            
            LockstepLogger.Info("[ReplayPlayer] Playback started");
        }

        public void Pause()
        {
            if (_state == ReplayState.Playing)
            {
                _state = ReplayState.Paused;
                LockstepLogger.Info("[ReplayPlayer] Playback paused");
            }
        }

        public void Resume()
        {
            if (_state == ReplayState.Paused)
            {
                _state = ReplayState.Playing;
                LockstepLogger.Info("[ReplayPlayer] Playback resumed");
            }
        }

        public void Stop()
        {
            _state = ReplayState.Idle;
            _currentTick = 0;
            _accumulator = 0;
            
            LockstepLogger.Info("[ReplayPlayer] Playback stopped");
        }

        public void SeekToTick(int tick)
        {
            if (_replayData == null)
                return;

            tick = System.Math.Max(0, System.Math.Min(tick, TotalTicks));
            _currentTick = tick;
            _accumulator = 0;
            
            LockstepLogger.Info($"[ReplayPlayer] Seeked to tick {tick}");
            
            OnSeekCompleted?.Invoke(tick);
        }

        public void SeekToProgress(float progress)
        {
            progress = System.Math.Max(0f, System.Math.Min(1f, progress));
            int tick = (int)(TotalTicks * progress);
            SeekToTick(tick);
        }

        public IReadOnlyList<ICommand> GetCurrentTickCommands()
        {
            if (_replayData == null || _currentTick > TotalTicks)
            {
                return EmptyCommandList;
            }

            return _replayData.GetCommandsForTick(_currentTick);
        }

        public void Update(float deltaTime)
        {
            if (_state != ReplayState.Playing || _replayData == null)
                return;

            // Apply speed multiplier
            float speedMultiplier = (int)_speed / 100f;
            float tickIntervalMs = _replayData.Metadata.TickIntervalMs;
            
            _accumulator += deltaTime * 1000f * speedMultiplier;

            // Process ticks
            while (_accumulator >= tickIntervalMs && _currentTick <= TotalTicks)
            {
                _accumulator -= tickIntervalMs;
                
                // Get and play current tick commands
                var commands = _replayData.GetCommandsForTick(_currentTick);
                OnTickPlayed?.Invoke(_currentTick, commands);
                
                _currentTick++;
                
                // Check if finished
                if (_currentTick > TotalTicks)
                {
                    _state = ReplayState.Finished;
                    _accumulator = 0;
                    
                    LockstepLogger.Info("[ReplayPlayer] Playback finished");
                    OnPlaybackFinished?.Invoke();
                    break;
                }
            }
        }

        /// <summary>
        /// Step forward one tick (for frame-by-frame playback)
        /// </summary>
        public void StepForward()
        {
            if (_replayData == null || _currentTick > TotalTicks)
                return;

            var commands = _replayData.GetCommandsForTick(_currentTick);
            OnTickPlayed?.Invoke(_currentTick, commands);
            
            _currentTick++;
            
            if (_currentTick > TotalTicks)
            {
                _state = ReplayState.Finished;
                OnPlaybackFinished?.Invoke();
            }
        }

        /// <summary>
        /// Step backward one tick
        /// Note: This requires simulation rollback for proper state restoration
        /// </summary>
        public void StepBackward()
        {
            if (_replayData == null || _currentTick <= 0)
                return;

            _currentTick--;
            OnSeekCompleted?.Invoke(_currentTick);
        }

        /// <summary>
        /// Get replay metadata
        /// </summary>
        public IReplayMetadata GetMetadata()
        {
            return _replayData?.Metadata;
        }

        /// <summary>
        /// Check if replay data is loaded
        /// </summary>
        public bool HasReplayData => _replayData != null;
    }
}
