using System;
using System.Collections.Generic;
using xpTURN.Lockstep.Core;
using xpTURN.Lockstep.Network;
using xpTURN.Lockstep.Input.Impl;
using xpTURN.Lockstep.State.Impl;
using xpTURN.Lockstep.Replay;
using xpTURN.Lockstep.Replay.Impl;

namespace xpTURN.Lockstep.Core.Impl
{
    /// <summary>
    /// Lockstep configuration implementation
    /// </summary>
    [Serializable]
    public class LockstepConfig : ILockstepConfig
    {
#if UNITY_2021_1_OR_NEWER
        [UnityEngine.SerializeField]
#endif
        private int _tickIntervalMs = 50; // 20 ticks/second

#if UNITY_2021_1_OR_NEWER
        [UnityEngine.SerializeField]
#endif
        private int _inputDelayTicks = 2;

#if UNITY_2021_1_OR_NEWER
        [UnityEngine.SerializeField]
#endif
        private int _maxRollbackTicks = 10;

#if UNITY_2021_1_OR_NEWER
        [UnityEngine.SerializeField]
#endif
        private int _syncCheckInterval = 30; // Every 1.5 seconds

#if UNITY_2021_1_OR_NEWER
        [UnityEngine.SerializeField]
#endif
        private bool _usePrediction = true;

        public int TickIntervalMs { get => _tickIntervalMs; set => _tickIntervalMs = value; }
        public int InputDelayTicks { get => _inputDelayTicks; set => _inputDelayTicks = value; }
        public int MaxRollbackTicks { get => _maxRollbackTicks; set => _maxRollbackTicks = value; }
        public int SyncCheckInterval { get => _syncCheckInterval; set => _syncCheckInterval = value; }
        public bool UsePrediction { get => _usePrediction; set => _usePrediction = value; }
    }

    /// <summary>
    /// Lockstep engine implementation
    /// </summary>
    public class LockstepEngine : ILockstepEngine
    {
        public LockstepState State { get; private set; } = LockstepState.Idle;
        public int CurrentTick { get; private set; }
        public int LocalPlayerId { get; private set; }
        public int TickInterval => _config.TickIntervalMs;
        public int InputDelay => _config.InputDelayTicks;

        public event Action<int> OnTickExecuted;
        public event Action<int, int> OnDesyncDetected;

        private ISimulation _simulation;
        private ILockstepNetworkService _networkService;
        private LockstepConfig _config;

        private InputBuffer _inputBuffer;
        private StateSnapshotManager _snapshotManager;
        private SimpleInputPredictor _inputPredictor;

        private float _accumulator;
        private int _targetTick;
        private int _confirmedTick;
        private int _playerCount;

        private readonly List<ICommand> _pendingCommands = new List<ICommand>();
        private readonly Dictionary<int, long> _localHashes = new Dictionary<int, long>();
        
        // Cached lists for object pooling (GC prevention)
        private readonly List<ICommand> _tickCommandsCache = new List<ICommand>();
        private readonly List<ICommand> _previousCommandsCache = new List<ICommand>();
        private readonly List<int> _hashKeysToRemoveCache = new List<int>();

        // Replay system
        private ReplaySystem _replaySystem;
        private bool _isReplayMode;
        private int _randomSeed;

        /// <summary>
        /// Replay system instance
        /// </summary>
        public IReplaySystem ReplaySystem => _replaySystem;

        /// <summary>
        /// Whether currently in replay playback mode
        /// </summary>
        public bool IsReplayMode => _isReplayMode;

        /// <summary>
        /// Whether currently recording
        /// </summary>
        public bool IsRecording => _replaySystem?.IsRecording ?? false;

        public LockstepEngine()
        {
            _config = new LockstepConfig();
            _inputBuffer = new InputBuffer();
            _snapshotManager = new StateSnapshotManager();
            _inputPredictor = new SimpleInputPredictor();
            _replaySystem = new ReplaySystem();
            _randomSeed = (int)DateTime.Now.Ticks;
        }

        public LockstepEngine(LockstepConfig config) : this()
        {
            _config = config ?? new LockstepConfig();
        }

        public LockstepEngine(LockstepConfig config, int randomSeed) : this(config)
        {
            _randomSeed = randomSeed;
        }

        public void Initialize(ISimulation simulation, ILockstepNetworkService networkService)
        {
            _simulation = simulation;
            _networkService = networkService;

            LocalPlayerId = networkService.LocalPlayerId;
            _playerCount = networkService.PlayerCount;

            // Connect network events
            _networkService.OnCommandReceived += HandleCommandReceived;
            _networkService.OnDesyncDetected += HandleNetworkDesync;
            _networkService.OnGameStart += HandleGameStart;

            // Initialize simulation
            _simulation.Initialize();

            State = LockstepState.WaitingForPlayers;
        }

        public void Start()
        {
            Start(enableRecording: true);
        }

        public void Start(bool enableRecording)
        {
            if (State != LockstepState.WaitingForPlayers)
                return;

            CurrentTick = 0;
            _targetTick = 0;
            _confirmedTick = -1;
            _accumulator = 0;

            // Save initial snapshot
            SaveSnapshot(0);

            // Start recording if enabled
            if (enableRecording && !_isReplayMode)
            {
                _replaySystem.StartRecording(_playerCount, _config.TickIntervalMs, _randomSeed);
            }

            State = LockstepState.Running;
        }

        public void Update(float deltaTime)
        {
            if (State != LockstepState.Running)
                return;

            // Replay mode: update replay system instead of normal tick processing
            if (_isReplayMode)
            {
                _replaySystem.Update(deltaTime);
                return;
            }

            _networkService.Update();

            // Accumulate time
            _accumulator += deltaTime * 1000f; // Convert to ms

            // Execute ticks
            while (_accumulator >= _config.TickIntervalMs)
            {
                _accumulator -= _config.TickIntervalMs;

                // Target tick considering input delay
                int inputTick = CurrentTick + _config.InputDelayTicks;

                // Check if can advance to confirmed tick
                if (CanAdvanceTick())
                {
                    ExecuteTick();
                }
                else if (_config.UsePrediction)
                {
                    // Prediction mode: predict input and proceed
                    ExecuteTickWithPrediction();
                }
                else
                {
                    // Wait for input
                    State = LockstepState.Paused;
                    break;
                }
            }
        }

        public void InputCommand(ICommand command)
        {
            // Apply input delay
            int targetTick = CurrentTick + _config.InputDelayTicks;

            if (command is CommandBase cmdBase)
            {
                cmdBase.Tick = targetTick;
                cmdBase.PlayerId = LocalPlayerId;
            }

            // Save to local buffer
            _inputBuffer.AddCommand(command);

            // Send over network
            _networkService.SendCommand(command);
        }

        public void Stop()
        {
            // Stop recording if active
            if (_replaySystem.IsRecording)
            {
                int totalTicks = CurrentTick + _config.InputDelayTicks;
                _replaySystem.StopRecording(totalTicks);
            }

            State = LockstepState.Finished;
            _networkService.OnCommandReceived -= HandleCommandReceived;
            _networkService.OnDesyncDetected -= HandleNetworkDesync;
            _networkService.OnGameStart -= HandleGameStart;
        }

        #region Replay Methods

        /// <summary>
        /// Start replay playback
        /// </summary>
        public void StartReplay(IReplayData replayData)
        {
            if (replayData == null)
            {
                LockstepLogger.Error("Cannot start replay: null replay data");
                return;
            }

            _isReplayMode = true;
            _randomSeed = replayData.Metadata.RandomSeed;
            
            // Reset state
            CurrentTick = 0;
            _targetTick = 0;
            _confirmedTick = -1;
            _accumulator = 0;
            _inputBuffer.Clear();
            
            // Initialize simulation with replay seed
            _simulation.Initialize();
            
            // Save initial snapshot
            SaveSnapshot(0);
            
            // Load replay
            _replaySystem.Load(replayData);
            _replaySystem.OnTickPlayed += HandleReplayTick;
            _replaySystem.OnPlaybackFinished += HandleReplayFinished;
            
            State = LockstepState.Running;
            _replaySystem.Play();
            
            LockstepLogger.Info($"Replay started: {replayData.Metadata.TotalTicks} ticks, {replayData.Metadata.DurationMs}ms");
        }

        /// <summary>
        /// Start replay from file
        /// </summary>
        public void StartReplayFromFile(string filePath)
        {
            _replaySystem.LoadFromFile(filePath);
            var replayData = _replaySystem.CurrentReplayData;
            
            if (replayData != null)
            {
                StartReplay(replayData);
            }
        }

        /// <summary>
        /// Stop replay playback
        /// </summary>
        public void StopReplay()
        {
            if (!_isReplayMode)
                return;

            _replaySystem.Stop();
            _replaySystem.OnTickPlayed -= HandleReplayTick;
            _replaySystem.OnPlaybackFinished -= HandleReplayFinished;
            
            _isReplayMode = false;
            State = LockstepState.Finished;
            
            LockstepLogger.Info("Replay stopped");
        }

        /// <summary>
        /// Pause replay playback
        /// </summary>
        public void PauseReplay()
        {
            if (_isReplayMode)
            {
                _replaySystem.Pause();
                State = LockstepState.Paused;
            }
        }

        /// <summary>
        /// Resume replay playback
        /// </summary>
        public void ResumeReplay()
        {
            if (_isReplayMode && State == LockstepState.Paused)
            {
                _replaySystem.Resume();
                State = LockstepState.Running;
            }
        }

        /// <summary>
        /// Set replay playback speed
        /// </summary>
        public void SetReplaySpeed(ReplaySpeed speed)
        {
            _replaySystem.Speed = speed;
        }

        /// <summary>
        /// Seek to specific tick in replay
        /// </summary>
        public void SeekReplay(int tick)
        {
            if (!_isReplayMode)
                return;

            // Rollback to beginning and resimulate
            var snapshot = _snapshotManager.GetNearestSnapshot(0);
            if (snapshot != null)
            {
                _simulation.Rollback(0);
            }
            
            // Resimulate up to target tick
            CurrentTick = 0;
            var replayData = _replaySystem.CurrentReplayData;
            
            while (CurrentTick < tick && CurrentTick <= replayData.Metadata.TotalTicks)
            {
                var commands = replayData.GetCommandsForTick(CurrentTick);
                _simulation.Tick((System.Collections.Generic.IReadOnlyList<ICommand>)commands);
                
                if (CurrentTick % 5 == 0)
                {
                    SaveSnapshot(CurrentTick);
                }
                
                CurrentTick++;
            }
            
            _replaySystem.SeekToTick(tick);
            
            LockstepLogger.Info($"Replay seeked to tick {tick}");
        }

        /// <summary>
        /// Save current replay to file
        /// </summary>
        public void SaveReplayToFile(string filePath)
        {
            _replaySystem.SaveToFile(filePath);
        }

        /// <summary>
        /// Get current replay data
        /// </summary>
        public IReplayData GetCurrentReplayData()
        {
            return _replaySystem.CurrentReplayData;
        }

        /// <summary>
        /// Get random seed used for this game
        /// </summary>
        public int GetRandomSeed()
        {
            return _randomSeed;
        }

        private void HandleReplayTick(int tick, System.Collections.Generic.IReadOnlyList<ICommand> commands)
        {
            // Save snapshot for seeking
            if (tick % 5 == 0)
            {
                SaveSnapshot(tick);
            }
            
            // Execute simulation with replay commands
            _simulation.Tick(commands);
            
            CurrentTick = tick + 1;
            OnTickExecuted?.Invoke(tick);
        }

        private void HandleReplayFinished()
        {
            State = LockstepState.Finished;
            _isReplayMode = false;
            
            _replaySystem.OnTickPlayed -= HandleReplayTick;
            _replaySystem.OnPlaybackFinished -= HandleReplayFinished;
            
            LockstepLogger.Info("Replay playback finished");
        }

        #endregion

        private bool CanAdvanceTick()
        {
            // Check if all player inputs have arrived
            return _inputBuffer.HasAllCommands(CurrentTick, _playerCount);
        }

        private void ExecuteTick()
        {
            // Save snapshot (for rollback)
            if (CurrentTick % 5 == 0) // Save every 5 ticks
            {
                SaveSnapshot(CurrentTick);
            }

            // Collect commands (GC-Free)
            var commands = _inputBuffer.GetCommandList(CurrentTick);

            // Record commands for replay
            if (_replaySystem.IsRecording)
            {
                _replaySystem.RecordTick(CurrentTick, commands);
            }

            // Execute simulation tick
            _simulation.Tick(commands);

            // Sync check
            if (CurrentTick % _config.SyncCheckInterval == 0)
            {
                long hash = _simulation.GetStateHash();
                _localHashes[CurrentTick] = hash;
                _networkService.SendSyncHash(CurrentTick, hash);
            }

            _confirmedTick = CurrentTick;
            CurrentTick++;

            OnTickExecuted?.Invoke(CurrentTick - 1);

            // Cleanup old data
            CleanupOldData();
        }

        private void ExecuteTickWithPrediction()
        {
            // Save snapshot
            if (CurrentTick % 5 == 0)
            {
                SaveSnapshot(CurrentTick);
            }

            // Reuse cached list (GC prevention)
            _tickCommandsCache.Clear();

            // Collect received commands
            foreach (var cmd in _inputBuffer.GetCommands(CurrentTick))
            {
                _tickCommandsCache.Add(cmd);
            }

            // Predict missing player inputs
            for (int playerId = 0; playerId < _playerCount; playerId++)
            {
                if (!_inputBuffer.HasCommandForTick(CurrentTick, playerId))
                {
                    GetPreviousCommands(playerId, 5);
                    var predicted = _inputPredictor.PredictInput(playerId, CurrentTick, _previousCommandsCache);
                    _tickCommandsCache.Add(predicted);
                    _pendingCommands.Add(predicted);
                }
            }

            // Prediction-based simulation
            _simulation.Tick(_tickCommandsCache);

            CurrentTick++;
            OnTickExecuted?.Invoke(CurrentTick - 1);
        }

        /// <summary>
        /// Rollback and re-simulate
        /// </summary>
        public void Rollback(int targetTick)
        {
            if (targetTick >= CurrentTick)
                return;

            if (targetTick < CurrentTick - _config.MaxRollbackTicks)
            {
                LockstepLogger.Error($"Rollback too far: {targetTick}, current: {CurrentTick}");
                return;
            }

            // Restore from snapshot
            var snapshot = _snapshotManager.GetNearestSnapshot(targetTick);
            if (snapshot == null)
            {
                LockstepLogger.Error($"No snapshot found for tick {targetTick}");
                return;
            }

            // Restore state
            _simulation.Rollback(snapshot.Tick);

            // Remove predicted commands
            _inputBuffer.ClearAfter(snapshot.Tick);
            _pendingCommands.Clear();

            // Re-simulate (GC-Free)
            int resimTick = snapshot.Tick;
            while (resimTick < CurrentTick)
            {
                if (_inputBuffer.HasAllCommands(resimTick, _playerCount))
                {
                    var commands = _inputBuffer.GetCommandList(resimTick);
                    _simulation.Tick(commands);
                }
                resimTick++;
            }

            LockstepLogger.Info($"Rollback completed: {snapshot.Tick} -> {CurrentTick}");
        }

        private void SaveSnapshot(int tick)
        {
            var worldState = _simulation as Simulation;
            if (worldState != null)
            {
                var snapshot = worldState.CreateSnapshot(tick);
                _snapshotManager.SaveSnapshot(tick, snapshot);
            }
        }

        /// <summary>
        /// Get previous commands - GC-Free version (uses cached list)
        /// </summary>
        private void GetPreviousCommands(int playerId, int count)
        {
            _previousCommandsCache.Clear();
            for (int t = CurrentTick - 1; t >= 0 && _previousCommandsCache.Count < count; t--)
            {
                var cmd = _inputBuffer.GetCommand(t, playerId);
                if (cmd != null)
                    _previousCommandsCache.Add(cmd);
            }
        }

        private void HandleCommandReceived(ICommand command)
        {
            _inputBuffer.AddCommand(command);

            // Prediction verification (manual loop instead of lambda for GC prevention)
            ICommand predicted = null;
            for (int i = 0; i < _pendingCommands.Count; i++)
            {
                var c = _pendingCommands[i];
                if (c.Tick == command.Tick && c.PlayerId == command.PlayerId)
                {
                    predicted = c;
                    break;
                }
            }

            if (predicted != null)
            {
                _inputPredictor.UpdateAccuracy(predicted, command);
                _pendingCommands.Remove(predicted);

                // Rollback if prediction was wrong
                if (predicted.CommandType != command.CommandType)
                {
                    Rollback(command.Tick);
                }
            }

            // Resume if was paused
            if (State == LockstepState.Paused && CanAdvanceTick())
            {
                State = LockstepState.Running;
            }
        }

        private void HandleNetworkDesync(int playerId, int tick, long localHash, long remoteHash)
        {
            LockstepLogger.Error($"Desync detected at tick {tick}! Player {playerId}: local={localHash}, remote={remoteHash}");
            OnDesyncDetected?.Invoke((int)localHash, (int)remoteHash);

            // Attempt rollback
            Rollback(tick);
        }

        private void HandleGameStart()
        {
            Start();
        }

        private void CleanupOldData()
        {
            int cleanupTick = CurrentTick - _config.MaxRollbackTicks - 10;
            if (cleanupTick > 0)
            {
                _inputBuffer.ClearBefore(cleanupTick);
                _networkService.ClearOldData(cleanupTick);

                // Remove old hashes (use cached list for GC prevention)
                _hashKeysToRemoveCache.Clear();
                foreach (var key in _localHashes.Keys)
                {
                    if (key < cleanupTick)
                        _hashKeysToRemoveCache.Add(key);
                }
                for (int i = 0; i < _hashKeysToRemoveCache.Count; i++)
                {
                    _localHashes.Remove(_hashKeysToRemoveCache[i]);
                }
            }
        }
    }
}
