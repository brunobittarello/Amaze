using UnityEngine;

public class GameplayBehaviour : MonoBehaviour
{
    const int POOL_SIZE = 50;
    const float PLAYER_SPEED = 10f;

    const char WALL_ID = '1';
    const char PLAYER_ID = '2';
    const int CHECK_ID = 3;

    [SerializeField]
    private TextAsset stageMap;

    [SerializeField]
    private GameObject PlayerPrefab;
    [SerializeField]
    private GameObject WallPrefab;
    [SerializeField]
    private GameObject TileCheckPrefab;

    private Transform _wallPool;
    private Transform _tilePool;
    private Transform _stage;
    private int[][] _stageMap;
    private Transform _player;
    private Vector2Int _playerPos;
    private Vector2Int _playerNextPos;
    private Vector2Int _playerMovement;
    private bool _isMoving;

    // Start is called before the first frame update
    void Start()
    {
        _wallPool = new GameObject("WallPool").transform;
        _wallPool.parent = this.transform;
        _wallPool.gameObject.SetActive(false);

        _tilePool = new GameObject("TilePool").transform;
        _tilePool.parent = this.transform;
        _tilePool.gameObject.SetActive(false);

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

        for (int i = 0; i < POOL_SIZE; i++)
        {
            var clone = Instantiate(TileCheckPrefab);
            clone.gameObject.name = "Tile " + (i + 1);
            clone.transform.parent = _tilePool;
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
                    CheckTile(_playerPos);
                }
            }
        }
    }

    void HandlePlayerMovement()
    {
        if (_isMoving)
            Move();
        else
            DetectNewMovement();
    }

    void DetectNewMovement()
    {
        var movement = PlayerMovement();
        if (movement == Vector2Int.zero)
            return;

        if (CanPlayerMove(movement))
        {
            _isMoving = true;
            _playerMovement = movement;
            _playerNextPos = _playerPos + movement;
        }
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

    void Move()
    {
        var delta = PLAYER_SPEED * Time.deltaTime;
        Vector2 newPos = (Vector2)_player.localPosition + (Vector2)_playerMovement * delta;
        if ((_playerNextPos - newPos).sqrMagnitude < delta * 0.5f)
            PlayerReachedNewTile();
        else
            _player.localPosition = newPos;
    }

    void PlayerReachedNewTile()
    {
        _playerPos += _playerMovement;
        CheckTile(_playerPos);
        if (CanPlayerMove(_playerMovement))
        {
            _playerNextPos = _playerPos + _playerMovement;
            return;
        }

        _isMoving = false;
        _playerMovement = Vector2Int.zero;
        _player.localPosition = (Vector2)_playerPos;
    }

    void CheckTile(Vector2Int tilePos)
    {
        if (_stageMap[tilePos.x][tilePos.y] != 0)
            return;

        _stageMap[tilePos.x][tilePos.y] = CHECK_ID;
        var tile = _tilePool.GetChild(0);
        tile.parent = _stage;
        tile.localPosition = (Vector2)tilePos;
    }
}
