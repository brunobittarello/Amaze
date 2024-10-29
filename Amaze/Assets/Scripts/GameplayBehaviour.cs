using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class GameplayBehaviour : MonoBehaviour
{
    const int POOL_SIZE = 50;

    const char WALL_ID = '1';
    const char PLAYER_ID = '2';

    [SerializeField]
    private TextAsset stageMap;

    [SerializeField]
    private GameObject PlayerPrefab;
    [SerializeField]
    private GameObject WallPrefab;

    private Transform _wallPool;
    private Transform _stage;
    private int[][] _stageMap;
    private Transform _player;
    private Vector2Int _playerPos;

    // Start is called before the first frame update
    void Start()
    {
        _wallPool = new GameObject("WallPool").transform;
        _wallPool.parent = this.transform;
        _wallPool.gameObject.SetActive(false);

        _stage = new GameObject("Stage").transform;
        _stage.parent = this.transform;

        CreatePool();

        var stageGrid = stageMap.text.Split('\n');
        LoadStage(stageGrid, stageGrid[0].Length - 1, stageGrid.Length);
    }

    // Update is called once per frame
    void Update()
    {
        HandlePlayerMovement();
    }

    void CreatePool()
    {
        for (int i = 0; i < POOL_SIZE; i++)
        {
            var clone = Instantiate(WallPrefab);
            clone.gameObject.name = "Wall " + (i + 1);
            clone.transform.parent = _wallPool;
        }
    }

    void LoadStage(string[] mapGrid, int maxX, int maxY)
    {
        _stageMap = new int[maxX][];
        for (int i = 0; i < maxX; i++)
        {
            _stageMap[i] = new int[maxY];
            for (int l = 0; l < maxY; l++)
            {
                var itemId = mapGrid[maxY - i - 1][l];
                if (itemId == WALL_ID)
                {
                    _stageMap[i][l] = WALL_ID;
                    var wall = _wallPool.GetChild(0);
                    wall.parent = _stage;
                    wall.localPosition = new Vector2(i, l);
                }
                else if (itemId == PLAYER_ID)
                {
                    _player = Instantiate(PlayerPrefab).transform;
                    _player.parent = _stage;
                    _player.localPosition = new Vector2(i, l);
                    _playerPos = new Vector2Int(i, l);
                }
            }
        }
    }

    void HandlePlayerMovement()
    {
        var movement = PlayerMovement();
        if (movement == Vector2Int.zero)
            return;

        if (CanPlayerMove(movement))
            Move(movement);
    }

    Vector2Int PlayerMovement()
    {
        if (Input.GetKeyDown(KeyCode.W)) return Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.S)) return Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.A)) return Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.D)) return Vector2Int.right;
        return Vector2Int.zero;
    }

    bool CanPlayerMove(Vector2Int movement)
    {
        return _stageMap[_playerPos.x + movement.x][_playerPos.y + movement.y] != WALL_ID;
    }

    void Move(Vector2Int movement)
    {
        _playerPos += movement;
        _player.localPosition = (Vector3Int)_playerPos;
    }
}
