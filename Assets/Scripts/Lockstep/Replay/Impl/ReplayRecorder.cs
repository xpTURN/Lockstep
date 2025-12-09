using System;
using System.Collections.Generic;
using xpTURN.Lockstep.Core;

namespace xpTURN.Lockstep.Replay.Impl
{
    /// <summary>
    /// Replay recorder implementation
    /// Records all commands during gameplay for later replay
    /// </summary>
    public class ReplayRecorder : IReplayRecorder
    {
        private ReplayData _replayData;
        private ReplayState _state = ReplayState.Idle;
        private int _currentTick;
        private readonly ICommandFactory _commandFactory;

        public ReplayState State => _state;
        public int CurrentTick => _currentTick;

        public event Action OnRecordingStarted;
        public event Action<IReplayData> OnRecordingStopped;

        public ReplayRecorder() : this(new Core.Impl.CommandFactory())
        {
        }

        public ReplayRecorder(ICommandFactory commandFactory)
        {
            _commandFactory = commandFactory;
        }

        public void StartRecording(int playerCount, int tickIntervalMs, int randomSeed)
        {
            if (_state == ReplayState.Recording)
            {
                LockstepLogger.Warning("[ReplayRecorder] Already recording");
                return;
            }

            _replayData = new ReplayData(_commandFactory);
            _replayData.Initialize(playerCount, tickIntervalMs, randomSeed);
            
            _currentTick = 0;
            _state = ReplayState.Recording;
            
            LockstepLogger.Info($"[ReplayRecorder] Recording started - Players: {playerCount}, TickInterval: {tickIntervalMs}ms, Seed: {randomSeed}");
            
            OnRecordingStarted?.Invoke();
        }

        public void RecordTick(int tick, IEnumerable<ICommand> commands)
        {
            if (_state != ReplayState.Recording)
            {
                return;
            }

            // Clone commands to preserve state at recording time
            var clonedCommands = CloneCommands(commands);
            _replayData.AddCommands(tick, clonedCommands);
            
            _currentTick = tick;
        }

        public IReplayData StopRecording(int totalTicks)
        {
            if (_state != ReplayState.Recording)
            {
                LockstepLogger.Warning("[ReplayRecorder] Not recording");
                return null;
            }

            _replayData.FinalizeRecording(totalTicks);
            _state = ReplayState.Idle;
            
            var result = _replayData;
            
            LockstepLogger.Info($"[ReplayRecorder] Recording stopped - Total ticks: {result.Metadata.TotalTicks}, Duration: {result.Metadata.DurationMs}ms");
            
            OnRecordingStopped?.Invoke(result);
            
            return result;
        }

        /// <summary>
        /// Clone commands to preserve their state
        /// </summary>
        private List<ICommand> CloneCommands(IEnumerable<ICommand> commands)
        {
            var cloned = Core.Impl.ListPoolHelper.GetCommandList();
            
            foreach (var cmd in commands)
            {
                // Serialize and deserialize to create a deep copy
                byte[] data = cmd.Serialize();
                ICommand clone = _commandFactory.DeserializeCommand(data);
                if (clone != null)
                {
                    cloned.Add(clone);
                }
            }
            
            return cloned;
        }

        /// <summary>
        /// Get current replay data (while recording)
        /// </summary>
        public IReplayData GetCurrentReplayData()
        {
            return _replayData;
        }

        /// <summary>
        /// Cancel recording and discard data
        /// </summary>
        public void CancelRecording()
        {
            if (_state != ReplayState.Recording)
                return;

            _replayData?.Clear();
            _replayData = null;
            _state = ReplayState.Idle;
            _currentTick = 0;
            
            LockstepLogger.Info("[ReplayRecorder] Recording cancelled");
        }
    }
}
