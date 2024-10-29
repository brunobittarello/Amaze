using UnityEngine;

public class GameplayBehaviour : MonoBehaviour
{
    const int POOL_SIZE = 50;
    const float PLAYER_SPEED = 10f;

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
    private Vector2Int _playerMovement;
    private bool _isMoving;

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
        Vector2 newPos = (Vector2)_player.localPosition + (Vector2)_playerMovement * PLAYER_SPEED * Time.deltaTime;
        Vector2 nextPosition = _playerPos + _playerMovement;
        if ((nextPosition - newPos).sqrMagnitude < 0.005f)
            PlayerReachedNewTile();
        //else 
            _player.localPosition = newPos;
    }

    void PlayerReachedNewTile()
    {
        _playerPos += _playerMovement;
        if (!CanPlayerMove(_playerMovement))
        {
            _isMoving = false;
            _playerMovement = Vector2Int.zero;
            //_player.localPosition = (Vector2)_playerPos;
        }
    }
}
