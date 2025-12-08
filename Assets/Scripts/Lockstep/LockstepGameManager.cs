#if UNITY_2021_1_OR_NEWER
using UnityEngine;
using Lockstep.Core;
using Lockstep.Core.Impl;
using Lockstep.Network;
using Lockstep.Network.Impl;
using Lockstep.Input.Impl;
using Lockstep.State.Impl;

namespace Lockstep
{
    /// <summary>
    /// Lockstep game manager
    /// Connects Unity MonoBehaviour with Lockstep system
    /// </summary>
    public class LockstepGameManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private LockstepConfig _config;

        [Header("Network")]
        [SerializeField] private string _serverAddress = "localhost";
        [SerializeField] private int _serverPort = 7777;

        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = true;

        // Components
        private LockstepEngine _engine;
        private Simulation _simulation;
        private LockstepNetworkService _networkService;
        private InputHandler _inputHandler;
        private CommandFactory _commandFactory;

        // State
        private bool _isInitialized;

        public static LockstepGameManager Instance { get; private set; }

        public LockstepEngine Engine => _engine;
        public Simulation Simulation => _simulation;
        public int CurrentTick => _engine?.CurrentTick ?? 0;
        public LockstepState State => _engine?.State ?? LockstepState.Idle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (_config == null)
            {
                _config = new LockstepConfig();
            }
        }

        /// <summary>
        /// Initialize Lockstep system
        /// </summary>
        public void Initialize(INetworkTransport transport)
        {
            // Create factory
            _commandFactory = new CommandFactory();

            // Create simulation
            _simulation = new Simulation(_config.TickIntervalMs);

            // Create network service
            _networkService = new LockstepNetworkService();
            _networkService.Initialize(transport, _commandFactory);

            // Create input handler
            _inputHandler = new InputHandler();

            // Create engine
            _engine = new LockstepEngine(_config);
            _engine.Initialize(_simulation, _networkService);

            // Connect events
            _engine.OnTickExecuted += OnTickExecuted;
            _engine.OnDesyncDetected += OnDesyncDetected;

            _isInitialized = true;

            Debug.Log("Lockstep system initialized");
        }

        /// <summary>
        /// Start game as host
        /// </summary>
        public void HostGame(INetworkTransport transport, int maxPlayers = 4)
        {
            Initialize(transport);
            _networkService.CreateRoom("Game", maxPlayers);
            _inputHandler.LocalPlayerId = _networkService.LocalPlayerId;
            _inputHandler.StartCapture();

            Debug.Log($"Hosting game as Player {_networkService.LocalPlayerId}");
        }

        /// <summary>
        /// Join game as client
        /// </summary>
        public void JoinGame(INetworkTransport transport, string roomName = "Game")
        {
            Initialize(transport);
            _networkService.JoinRoom(roomName);
            _inputHandler.LocalPlayerId = _networkService.LocalPlayerId;
            _inputHandler.StartCapture();

            Debug.Log($"Joined game as Player {_networkService.LocalPlayerId}");
        }

        /// <summary>
        /// Set ready status
        /// </summary>
        public void SetReady(bool ready)
        {
            _networkService?.SetReady(ready);
        }

        public void StartReplayFromFile(INetworkTransport transport, string filePath)
        {
            Initialize(transport);
            _engine.StartReplayFromFile(filePath);
        }

        private void Update()
        {
            if (!_isInitialized)
                return;

            // Capture input
            _inputHandler?.CaptureInput();

            // Update engine
            _engine?.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!_isInitialized || State != LockstepState.Running)
                return;

            // Convert input to command and send
            int inputTick = CurrentTick + _config.InputDelayTicks;
            var command = _inputHandler.CreateCommand(inputTick);

            if (command != null && !(command is EmptyCommand))
            {
                _engine.InputCommand(command);
            }
        }

        private void OnTickExecuted(int tick)
        {
            // Rendering update, etc.
            UpdateVisuals();
        }

        private void OnDesyncDetected(int localHash, int remoteHash)
        {
            Debug.LogError($"Desync detected! Local: {localHash}, Remote: {remoteHash}");
            // Implement resync logic here
        }

        /// <summary>
        /// Visual update (rendering)
        /// </summary>
        private void UpdateVisuals()
        {
            // Reflect simulation state to Unity GameObjects
            foreach (var entity in _simulation.Entities)
            {
                if (entity is EntityBase entityBase)
                {
                    // Update position/rotation if linked GameObject exists
                    // This part needs game-specific implementation
                }
            }
        }

        /// <summary>
        /// Spawn entity (synchronized)
        /// </summary>
        public T SpawnEntity<T>(int ownerId, Math.Impl.FPVector3 position) where T : EntityBase, new()
        {
            var entity = _simulation.SpawnEntity<T>(ownerId);
            entity.Position = position;
            return entity;
        }

        public void RemoveEntity(int entityId)
        {
            _simulation.RemoveEntity(entityId);
        }

        private void OnDestroy()
        {
            _engine?.Stop();
            _inputHandler?.StopCapture();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnGUI()
        {
            if (!_showDebugInfo || !_isInitialized)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"State: {State}");
            GUILayout.Label($"Tick: {CurrentTick}");
            GUILayout.Label($"Players: {_networkService?.PlayerCount ?? 0}");
            GUILayout.Label($"Entities: {_simulation?.Entities.Count ?? 0}");

            if (_networkService != null)
            {
                GUILayout.Label($"All Ready: {_networkService.AllPlayersReady}");
                GUILayout.Label($"Is Host: {_networkService.IsHost}");
            }

            GUILayout.EndArea();
        }
    }
}
#endif