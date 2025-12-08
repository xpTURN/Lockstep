using System.Collections.Generic;
using Lockstep.Core;
using Lockstep.Core.Impl;

namespace Lockstep.Input.Impl
{
    /// <summary>
    /// Input buffer implementation
    /// </summary>
    public class InputBuffer : IInputBuffer
    {
        // tick -> playerId -> command
        private readonly Dictionary<int, Dictionary<int, ICommand>> _commands
            = new Dictionary<int, Dictionary<int, ICommand>>();

        private int _oldestTick = int.MaxValue;
        private int _newestTick = int.MinValue;
        
        // Cached lists for object pooling (GC prevention)
        private readonly List<ICommand> _commandListCache = new List<ICommand>();
        private readonly List<int> _ticksToRemoveCache = new List<int>();

        public int Count
        {
            get
            {
                int count = 0;
                foreach (var tickCommands in _commands.Values)
                    count += tickCommands.Count;
                return count;
            }
        }

        public int OldestTick => _oldestTick == int.MaxValue ? 0 : _oldestTick;
        public int NewestTick => _newestTick == int.MinValue ? 0 : _newestTick;

        public void AddCommand(ICommand command)
        {
            if (command == null)
                return;

            int tick = command.Tick;
            int playerId = command.PlayerId;

            if (!_commands.TryGetValue(tick, out var tickCommands))
            {
                // Get from Dictionary pool (GC prevention)
                tickCommands = DictionaryPoolHelper.GetIntDictionary<ICommand>();
                _commands[tick] = tickCommands;
            }

            tickCommands[playerId] = command;

            // Update bounds
            if (tick < _oldestTick)
                _oldestTick = tick;
            if (tick > _newestTick)
                _newestTick = tick;
        }

        public IEnumerable<ICommand> GetCommands(int tick)
        {
            if (_commands.TryGetValue(tick, out var tickCommands))
            {
                return tickCommands.Values;
            }
            return System.Array.Empty<ICommand>();
        }

        public ICommand GetCommand(int tick, int playerId)
        {
            if (_commands.TryGetValue(tick, out var tickCommands))
            {
                if (tickCommands.TryGetValue(playerId, out var command))
                    return command;
            }
            return null;
        }

        public bool HasCommandForTick(int tick)
        {
            return _commands.ContainsKey(tick) && _commands[tick].Count > 0;
        }

        public bool HasCommandForTick(int tick, int playerId)
        {
            if (_commands.TryGetValue(tick, out var tickCommands))
            {
                return tickCommands.ContainsKey(playerId);
            }
            return false;
        }

        /// <summary>
        /// Check if all player commands have arrived
        /// </summary>
        public bool HasAllCommands(int tick, int playerCount)
        {
            if (!_commands.TryGetValue(tick, out var tickCommands))
                return false;

            return tickCommands.Count >= playerCount;
        }

        public void ClearBefore(int tick)
        {
            // Use cached list (GC prevention)
            _ticksToRemoveCache.Clear();

            foreach (var t in _commands.Keys)
            {
                if (t < tick)
                    _ticksToRemoveCache.Add(t);
            }

            for (int i = 0; i < _ticksToRemoveCache.Count; i++)
            {
                int t = _ticksToRemoveCache[i];
                // Return Dictionary to pool
                if (_commands.TryGetValue(t, out var dict))
                {
                    DictionaryPoolHelper.ReturnIntDictionary(dict);
                }
                _commands.Remove(t);
            }

            // Recalculate bounds
            RecalculateBounds();
        }

        public void ClearAfter(int tick)
        {
            // Use cached list (GC prevention)
            _ticksToRemoveCache.Clear();

            foreach (var t in _commands.Keys)
            {
                if (t > tick)
                    _ticksToRemoveCache.Add(t);
            }

            for (int i = 0; i < _ticksToRemoveCache.Count; i++)
            {
                int t = _ticksToRemoveCache[i];
                // Return Dictionary to pool
                if (_commands.TryGetValue(t, out var dict))
                {
                    DictionaryPoolHelper.ReturnIntDictionary(dict);
                }
                _commands.Remove(t);
            }

            RecalculateBounds();
        }

        public void Clear()
        {
            // Return all Dictionaries to pool
            foreach (var dict in _commands.Values)
            {
                DictionaryPoolHelper.ReturnIntDictionary(dict);
            }
            _commands.Clear();
            _oldestTick = int.MaxValue;
            _newestTick = int.MinValue;
        }

        private void RecalculateBounds()
        {
            _oldestTick = int.MaxValue;
            _newestTick = int.MinValue;

            foreach (var tick in _commands.Keys)
            {
                if (tick < _oldestTick)
                    _oldestTick = tick;
                if (tick > _newestTick)
                    _newestTick = tick;
            }
        }

        /// <summary>
        /// Fill cached list with commands for specific tick (GC-Free)
        /// Note: Returned list contents will change on next call
        /// </summary>
        public List<ICommand> GetCommandList(int tick)
        {
            _commandListCache.Clear();
            if (_commands.TryGetValue(tick, out var tickCommands))
            {
                foreach (var cmd in tickCommands.Values)
                {
                    _commandListCache.Add(cmd);
                }
            }
            return _commandListCache;
        }
    }

    /// <summary>
    /// Simple input predictor implementation (repeats last input)
    /// </summary>
    public class SimpleInputPredictor : IInputPredictor
    {
        private int _correctPredictions;
        private int _totalPredictions;
        
        // Cached CommandFactory (GC prevention)
        private readonly Core.Impl.CommandFactory _commandFactory = new Core.Impl.CommandFactory();
        
        // Cached EmptyCommand (GC prevention) - reused per playerId
        private Core.Impl.EmptyCommand _emptyCommandCache;

        public float Accuracy => _totalPredictions > 0
            ? (float)_correctPredictions / _totalPredictions
            : 1.0f;

        public ICommand PredictInput(int playerId, int tick, IEnumerable<ICommand> previousCommands)
        {
            // Clone and return most recent command
            ICommand lastCommand = null;

            foreach (var cmd in previousCommands)
            {
                if (cmd.PlayerId == playerId)
                {
                    if (lastCommand == null || cmd.Tick > lastCommand.Tick)
                        lastCommand = cmd;
                }
            }

            if (lastCommand != null)
            {
                // Clone and update tick (using cached factory)
                byte[] data = lastCommand.Serialize();
                var predicted = _commandFactory.DeserializeCommand(data);

                // Update tick (if CommandBase)
                if (predicted is Core.Impl.CommandBase cmdBase)
                {
                    cmdBase.Tick = tick;
                }

                return predicted;
            }

            // Return cached empty command if no previous command
            if (_emptyCommandCache == null)
            {
                _emptyCommandCache = new Core.Impl.EmptyCommand(playerId, tick);
            }
            else
            {
                _emptyCommandCache.PlayerId = playerId;
                _emptyCommandCache.Tick = tick;
            }
            return _emptyCommandCache;
        }

        public void UpdateAccuracy(ICommand predicted, ICommand actual)
        {
            _totalPredictions++;

            // Simple comparison: consider accurate if same type
            if (predicted.CommandType == actual.CommandType)
            {
                _correctPredictions++;
            }
        }
    }
}
