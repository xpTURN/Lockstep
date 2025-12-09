using UnityEngine;
using xpTURN.Lockstep.Core;
using xpTURN.Lockstep.Math.Impl;

namespace xpTURN.Lockstep.Sample
{
    /// <summary>
    /// Sample game controller using the Lockstep system
    /// </summary>
    public class SampleGameController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _isHost = true;
        [SerializeField] private int _maxPlayers = 2;

        [Header("Prefabs")]
        [SerializeField] private GameObject _playerUnitPrefab;

        private LockstepGameManager _gameManager;
        private LocalTestTransport _transport;
        private PlayerUnit _myUnit;
        private Camera _mainCamera;

        private string _replayPath = Application.dataPath + "/../Replays/001.rply";

        private void Start()
        {
            _mainCamera = Camera.main;

            // Find Lockstep game manager
            _gameManager = LockstepGameManager.Instance;
            if (_gameManager == null)
            {
                // Create manager if not exists
                var go = new GameObject("LockstepGameManager");
                _gameManager = go.AddComponent<LockstepGameManager>();
            }

            // Create local test transport
            _transport = new LocalTestTransport();
        }

        private void StartGame()
        {
            DespawnMyUnit();
            
            // Start game
            if (_isHost)
            {
                StartHost();
            }
            else
            {
                JoinGame();
            }
        }

        private void StopGame()
        {
            _gameManager.Engine.Stop();
            _gameManager.Engine.SaveReplayToFile(_replayPath);
            DespawnMyUnit();
        }

        private void StartReplay()
        {
            _gameManager.StartReplayFromFile(_transport, _replayPath);
        }

        private void StartHost()
        {
            _gameManager.HostGame(_transport, _maxPlayers);
            Debug.Log("Started game as host");

            // Host ready
            _gameManager.SetReady(true);
        }

        private void JoinGame()
        {
            _gameManager.JoinGame(_transport, "Game");
            Debug.Log("Joined game");

            // Client ready
            _gameManager.SetReady(true);
        }

        private void Update()
        {
            if (_gameManager == null)
            {
                return;
            }

            // Process transport events
            _transport?.PollEvents();

            // Only process when game is running
            if (_gameManager.State != LockstepState.Running)
                return;

            // Spawn my unit if not exists
            if (_myUnit == null)
            {
                SpawnMyUnit();
            }

            // Handle input (direct processing without separate input system)
            HandleInput();

            // Visual update
            UpdateVisuals();
        }

        private void SpawnMyUnit()
        {
            // Create unit in simulation
            int playerId = _gameManager.Engine.LocalPlayerId;
            FPVector3 spawnPos = new FPVector3(
                FP64.FromInt(playerId * 3),
                FP64.Zero,
                FP64.Zero
            );

            _myUnit = _gameManager.SpawnEntity<PlayerUnit>(playerId, spawnPos);

            // Create and link Unity GameObject
            if (_playerUnitPrefab != null)
            {
                _myUnit.GameObject = Instantiate(
                    _playerUnitPrefab,
                    new Vector3(spawnPos.x.ToFloat(), spawnPos.y.ToFloat(), spawnPos.z.ToFloat()),
                    Quaternion.identity
                );
            }
            else
            {
                // Create default cube if no prefab
                _myUnit.GameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _myUnit.GameObject.transform.position = new Vector3(
                    spawnPos.x.ToFloat(),
                    spawnPos.y.ToFloat(),
                    spawnPos.z.ToFloat()
                );

                // Set color (different per player)
                var renderer = _myUnit.GameObject.GetComponent<Renderer>();
                renderer.material.color = playerId == 0 ? Color.blue : Color.red;
            }

            _myUnit.GameObject.name = $"PlayerUnit_{playerId}";
        }

        private void DespawnMyUnit()
        {
            if (_myUnit != null)
            {
                _gameManager.RemoveEntity(_myUnit.EntityId);
                Destroy(_myUnit.GameObject);
                _myUnit = null;
            }
        }

        private void HandleInput()
        {
            // Right click: move command
            if (UnityEngine.Input.GetMouseButton(1) && _mainCamera != null)
            {
                Ray ray = _mainCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    // Create move command
                    int targetTick = _gameManager.CurrentTick + 2; // Input delay
                    var moveCmd = new Core.Impl.MoveCommand(
                        _gameManager.Engine.LocalPlayerId,
                        targetTick,
                        FP64.FromFloat(hit.point.x).RawValue,
                        FP64.FromFloat(hit.point.y).RawValue,
                        FP64.FromFloat(hit.point.z).RawValue
                    );

                    _gameManager.Engine.InputCommand(moveCmd);
                }
            }

            // WASD: directional movement
            float h = UnityEngine.Input.GetAxisRaw("Horizontal");
            float v = UnityEngine.Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
            {
                // Convert direction to movement target
                Vector3 currentPos = _myUnit?.GameObject?.transform.position ?? Vector3.zero;
                Vector3 direction = new Vector3(h, 0, v).normalized;
                Vector3 targetPos = currentPos + direction * 5f; // 5 units ahead

                int targetTick = _gameManager.CurrentTick + 2;
                var moveCmd = new Core.Impl.MoveCommand(
                    _gameManager.Engine.LocalPlayerId,
                    targetTick,
                    FP64.FromFloat(targetPos.x).RawValue,
                    FP64.FromFloat(targetPos.y).RawValue,
                    FP64.FromFloat(targetPos.z).RawValue
                );

                _gameManager.Engine.InputCommand(moveCmd);
            }
        }

        private void UpdateVisuals()
        {
            // Update visual state of all entities
            foreach (var entity in _gameManager.Simulation.Entities)
            {
                if (entity is PlayerUnit unit && unit.GameObject != null)
                {
                    // Position interpolation (visual smoothness)
                    Vector3 targetPos = new Vector3(
                        unit.Position.x.ToFloat(),
                        unit.Position.y.ToFloat(),
                        unit.Position.z.ToFloat()
                    );

                    unit.GameObject.transform.position = Vector3.Lerp(
                        unit.GameObject.transform.position,
                        targetPos,
                        Time.deltaTime * 15f
                    );

                    // Rotation update
                    unit.GameObject.transform.rotation = Quaternion.Euler(
                        0,
                        unit.Rotation.ToFloat(),
                        0
                    );
                }
            }
        }

        private void OnDestroy()
        {
            LocalTestTransport.Reset();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 210, 10, 200, 220));

            GUILayout.Label($"<b>Sample Game</b>");
            GUILayout.Label($"Mode: {(_isHost ? "Host" : "Client")}");
            GUILayout.Label($"Tick: {_gameManager?.CurrentTick ?? 0}");
            GUILayout.Label($"State: {_gameManager?.State}");

            if (_myUnit != null)
            {
                GUILayout.Label($"HP: {_myUnit.CurrentHealth}/{_myUnit.MaxHealth}");
                GUILayout.Label($"Pos: {_myUnit.Position}");
            }

            if (_gameManager.State != LockstepState.Running)
            {
                if (GUILayout.Button("Start Game"))
                {
                    StartGame();
                }
                if (GUILayout.Button("Start Replay"))
                {
                    StartReplay();
                }
            }
            else
            {
                if (GUILayout.Button("Stop Game"))
                {
                    StopGame();
                }
            }

            GUILayout.EndArea();
        }
    }
}
