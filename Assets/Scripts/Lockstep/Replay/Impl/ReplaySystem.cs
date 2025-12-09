using System;
using System.Collections.Generic;
using System.IO;
using xpTURN.Lockstep.Core;

namespace xpTURN.Lockstep.Replay.Impl
{
    /// <summary>
    /// Complete replay system implementation
    /// Combines recording and playback functionality with file I/O
    /// </summary>
    public class ReplaySystem : IReplaySystem
    {
        private readonly ReplayRecorder _recorder;
        private readonly ReplayPlayer _player;
        private IReplayData _currentReplayData;
        private readonly ICommandFactory _commandFactory;

        public ReplaySystem() : this(new Core.Impl.CommandFactory())
        {
        }

        public ReplaySystem(ICommandFactory commandFactory)
        {
            _commandFactory = commandFactory;
            _recorder = new ReplayRecorder(commandFactory);
            _player = new ReplayPlayer();
            
            // Forward recorder events
            _recorder.OnRecordingStarted += () => OnRecordingStarted?.Invoke();
            _recorder.OnRecordingStopped += (data) =>
            {
                _currentReplayData = data;
                OnRecordingStopped?.Invoke(data);
            };
            
            // Forward player events
            _player.OnTickPlayed += (tick, commands) => OnTickPlayed?.Invoke(tick, commands);
            _player.OnPlaybackFinished += () => OnPlaybackFinished?.Invoke();
            _player.OnSeekCompleted += (tick) => OnSeekCompleted?.Invoke(tick);
        }

        #region IReplayRecorder Implementation

        ReplayState IReplayRecorder.State => _recorder.State;
        int IReplayRecorder.CurrentTick => _recorder.CurrentTick;

        public event Action OnRecordingStarted;
        public event Action<IReplayData> OnRecordingStopped;

        public void StartRecording(int playerCount, int tickIntervalMs, int randomSeed)
        {
            _recorder.StartRecording(playerCount, tickIntervalMs, randomSeed);
        }

        public void RecordTick(int tick, IEnumerable<ICommand> commands)
        {
            _recorder.RecordTick(tick, commands);
        }

        public IReplayData StopRecording(int totalTicks)
        {
            var data = _recorder.StopRecording(totalTicks);
            _currentReplayData = data;
            return data;
        }

        #endregion

        #region IReplayPlayer Implementation

        ReplayState IReplayPlayer.State => _player.State;
        int IReplayPlayer.CurrentTick => _player.CurrentTick;
        public int TotalTicks => _player.TotalTicks;
        
        public ReplaySpeed Speed
        {
            get => _player.Speed;
            set => _player.Speed = value;
        }

        public float Progress => _player.Progress;

        public event Action<int, IReadOnlyList<ICommand>> OnTickPlayed;
        public event Action OnPlaybackFinished;
        public event Action<int> OnSeekCompleted;

        public void Load(IReplayData replayData)
        {
            _currentReplayData = replayData;
            _player.Load(replayData);
        }

        public void Play()
        {
            _player.Play();
        }

        public void Pause()
        {
            _player.Pause();
        }

        public void Resume()
        {
            _player.Resume();
        }

        public void Stop()
        {
            _player.Stop();
        }

        public void SeekToTick(int tick)
        {
            _player.SeekToTick(tick);
        }

        public void SeekToProgress(float progress)
        {
            _player.SeekToProgress(progress);
        }

        public IReadOnlyList<ICommand> GetCurrentTickCommands()
        {
            return _player.GetCurrentTickCommands();
        }

        public void Update(float deltaTime)
        {
            _player.Update(deltaTime);
        }

        #endregion

        #region IReplaySystem Implementation

        public bool IsRecording => _recorder.State == ReplayState.Recording;
        public bool IsPlaying => _player.State == ReplayState.Playing;
        public IReplayData CurrentReplayData => _currentReplayData;

        public void SaveToFile(string filePath)
        {
            if (_currentReplayData == null)
            {
                LockstepLogger.Error("[ReplaySystem] No replay data to save");
                return;
            }

            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                byte[] data = _currentReplayData.Serialize();
                File.WriteAllBytes(filePath, data);
                
                LockstepLogger.Info($"[ReplaySystem] Replay saved to: {filePath} ({data.Length} bytes)");
            }
            catch (Exception e)
            {
                LockstepLogger.Error($"[ReplaySystem] Failed to save replay: {e.Message}");
            }
        }

        public void LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    LockstepLogger.Error($"[ReplaySystem] Replay file not found: {filePath}");
                    return;
                }

                byte[] data = File.ReadAllBytes(filePath);
                
                var replayData = new ReplayData(_commandFactory);
                replayData.Deserialize(data);
                
                _currentReplayData = replayData;
                _player.Load(replayData);
                
                LockstepLogger.Info($"[ReplaySystem] Replay loaded from: {filePath}");
            }
            catch (Exception e)
            {
                LockstepLogger.Error($"[ReplaySystem] Failed to load replay: {e.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Cancel recording
        /// </summary>
        public void CancelRecording()
        {
            _recorder.CancelRecording();
        }

        /// <summary>
        /// Step forward one tick (for frame-by-frame playback)
        /// </summary>
        public void StepForward()
        {
            _player.StepForward();
        }

        /// <summary>
        /// Step backward one tick
        /// </summary>
        public void StepBackward()
        {
            _player.StepBackward();
        }

        /// <summary>
        /// Get current state (prioritizes playing over recording)
        /// </summary>
        public ReplayState State
        {
            get
            {
                if (_player.State == ReplayState.Playing || _player.State == ReplayState.Paused)
                    return _player.State;
                return _recorder.State;
            }
        }
    }
}
