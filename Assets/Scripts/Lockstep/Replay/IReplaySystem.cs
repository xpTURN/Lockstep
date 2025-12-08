using System;
using System.Collections.Generic;
using Lockstep.Core;

namespace Lockstep.Replay
{
    /// <summary>
    /// Replay system state
    /// </summary>
    public enum ReplayState
    {
        /// <summary>Not recording or playing</summary>
        Idle,
        /// <summary>Recording gameplay</summary>
        Recording,
        /// <summary>Playing back replay</summary>
        Playing,
        /// <summary>Playback paused</summary>
        Paused,
        /// <summary>Replay finished</summary>
        Finished
    }

    /// <summary>
    /// Replay playback speed
    /// </summary>
    public enum ReplaySpeed
    {
        /// <summary>0.25x speed</summary>
        Quarter = 25,
        /// <summary>0.5x speed</summary>
        Half = 50,
        /// <summary>1x speed (normal)</summary>
        Normal = 100,
        /// <summary>2x speed</summary>
        Double = 200,
        /// <summary>4x speed</summary>
        Quadruple = 400
    }

    /// <summary>
    /// Replay metadata
    /// </summary>
    public interface IReplayMetadata
    {
        /// <summary>Replay format version</summary>
        int Version { get; }
        
        /// <summary>Game session ID</summary>
        string SessionId { get; }
        
        /// <summary>Recording date/time (UTC ticks)</summary>
        long RecordedAt { get; }
        
        /// <summary>Total duration in milliseconds</summary>
        long DurationMs { get; }
        
        /// <summary>Total tick count</summary>
        int TotalTicks { get; }
        
        /// <summary>Number of players</summary>
        int PlayerCount { get; }
        
        /// <summary>Tick interval in milliseconds</summary>
        int TickIntervalMs { get; }
        
        /// <summary>Random seed used for the game</summary>
        int RandomSeed { get; }
    }

    /// <summary>
    /// Replay data interface
    /// </summary>
    public interface IReplayData
    {
        /// <summary>Replay metadata</summary>
        IReplayMetadata Metadata { get; }
        
        /// <summary>Get commands for specific tick</summary>
        IReadOnlyList<ICommand> GetCommandsForTick(int tick);
        
        /// <summary>Get all commands</summary>
        IReadOnlyDictionary<int, List<ICommand>> GetAllCommands();
        
        /// <summary>Serialize replay to byte array</summary>
        byte[] Serialize();
        
        /// <summary>Deserialize replay from byte array</summary>
        void Deserialize(byte[] data);
    }

    /// <summary>
    /// Replay recorder interface
    /// </summary>
    public interface IReplayRecorder
    {
        /// <summary>Current recording state</summary>
        ReplayState State { get; }
        
        /// <summary>Current recording tick</summary>
        int CurrentTick { get; }
        
        /// <summary>Start recording</summary>
        void StartRecording(int playerCount, int tickIntervalMs, int randomSeed);
        
        /// <summary>Record commands for a tick</summary>
        void RecordTick(int tick, IEnumerable<ICommand> commands);
        
        /// <summary>Stop recording and get replay data</summary>
        IReplayData StopRecording(int totalTicks);
        
        /// <summary>Event fired when recording starts</summary>
        event Action OnRecordingStarted;
        
        /// <summary>Event fired when recording stops</summary>
        event Action<IReplayData> OnRecordingStopped;
    }

    /// <summary>
    /// Replay player interface
    /// </summary>
    public interface IReplayPlayer
    {
        /// <summary>Current playback state</summary>
        ReplayState State { get; }
        
        /// <summary>Current playback tick</summary>
        int CurrentTick { get; }
        
        /// <summary>Total ticks in replay</summary>
        int TotalTicks { get; }
        
        /// <summary>Current playback speed</summary>
        ReplaySpeed Speed { get; set; }
        
        /// <summary>Progress (0.0 to 1.0)</summary>
        float Progress { get; }
        
        /// <summary>Load replay data</summary>
        void Load(IReplayData replayData);
        
        /// <summary>Start playback</summary>
        void Play();
        
        /// <summary>Pause playback</summary>
        void Pause();
        
        /// <summary>Resume playback</summary>
        void Resume();
        
        /// <summary>Stop playback</summary>
        void Stop();
        
        /// <summary>Seek to specific tick</summary>
        void SeekToTick(int tick);
        
        /// <summary>Seek to progress (0.0 to 1.0)</summary>
        void SeekToProgress(float progress);
        
        /// <summary>Get commands for current tick and advance</summary>
        IReadOnlyList<ICommand> GetCurrentTickCommands();
        
        /// <summary>Update playback (call every frame)</summary>
        void Update(float deltaTime);
        
        /// <summary>Event fired when tick is played</summary>
        event Action<int, IReadOnlyList<ICommand>> OnTickPlayed;
        
        /// <summary>Event fired when playback finishes</summary>
        event Action OnPlaybackFinished;
        
        /// <summary>Event fired when seek completes</summary>
        event Action<int> OnSeekCompleted;
    }

    /// <summary>
    /// Complete replay system interface
    /// </summary>
    public interface IReplaySystem : IReplayRecorder, IReplayPlayer
    {
        /// <summary>Check if currently recording</summary>
        bool IsRecording { get; }
        
        /// <summary>Check if currently playing</summary>
        bool IsPlaying { get; }
        
        /// <summary>Save replay to file</summary>
        void SaveToFile(string filePath);
        
        /// <summary>Load replay from file</summary>
        void LoadFromFile(string filePath);
        
        /// <summary>Get current replay data (if recording or loaded)</summary>
        IReplayData CurrentReplayData { get; }
    }
}

