using System.Collections;
using TMPro;
using UnityEngine;

public class GameplayBehaviour : MonoBehaviour
{
    const int POOL_SIZE = 256;
    const float PLAYER_SPEED = 20f;

    const char WALL_ID = '1';
    const char PLAYER_ID = '2';
    const int CHECK_ID = 3;

    [SerializeField]
    private float stageWaitingTime;
    [SerializeField]
    private float touchDeltaDist = 5f;

    [SerializeField]
    private TextAsset[] stageMap;

    [SerializeField]
    private GameObject PlayerPrefab;
    [SerializeField]
    private GameObject WallPrefab;
    [SerializeField]
    private GameObject TileCheckPrefab;
    [SerializeField]
    private GameObject HitWallEffectPrefab;
    [SerializeField]
    private TextMeshProUGUI LevelLabel;
    [SerializeField]
    private CameraController cameraCtrl;

    private Transform _wallPool;
    private Transform _tilePool;
    private Transform _effects;
    private Transform _stageWalls;
    private Transform _stageTiles;
    private Transform _stagePlayers;
    private int[][] _stageMap;
    private Transform _player;
    private Vector2Int _playerPos;
    private Vector2Int _playerNextPos;
    private Vector2Int _playerMovement;
    private bool _isMoving;
    private bool _isStageClear;
    private int _stageIndex;

    private Vector2 _touchRefPos;

    // Start is called before the first frame update
    void Start()
    {
        _wallPool = CreateContainers("WallPool", true);
        _tilePool = CreateContainers("TilePool", true);

        _stageWalls = CreateContainers("Walls", false);
        _stageTiles = CreateContainers("Tiles", false);
        _stagePlayers = CreateContainers("Players", false);
        _effects = CreateContainers("Effects", false);

        CreatePool();
        LoadStage();
    }

    Transform CreateContainers(string name, bool isPool)
    {
        var transform = new GameObject(name).transform;
        transform.parent = this.transform;
        transform.gameObject.SetActive(!isPool);
        return transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_isStageClear)
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

        for (int i = 0; i < 5; i++)
        {
            var clone = Instantiate(HitWallEffectPrefab);
            clone.gameObject.name = "HitWallFx " + (i + 1);
            clone.gameObject.SetActive(false);
            clone.transform.parent = _effects;
        }
    }

    void LoadStage()
    {
        LevelLabel.text = "Level " + (_stageIndex + 1);
        var mapGrid = LoadStageFromFile();
        var maxX = mapGrid[0].Length - 1;//TODO
        var maxY = mapGrid.Length;
        Debug.Log("StageSize " + maxX + "x" + maxY);

        cameraCtrl.Focus(new Vector2(maxX, maxY));
        _stageMap = new int[maxX][];
        for (int i = 0; i < maxX; i++)
        {
            _stageMap[i] = new int[maxY];
            for (int l = 0; l < maxY; l++)
            {
                var itemId = mapGrid[maxY - l - 1][i];
                if (itemId == WALL_ID)
                {
                    _stageMap[i][l] = WALL_ID;
                    var wall = _wallPool.GetChild(0);
                    wall.parent = _stageWalls;
                    wall.localPosition = new Vector2(i, l);
                }
                else if (itemId == PLAYER_ID)
                {
                    if (_player == null)
                    {
                        _player = Instantiate(PlayerPrefab).transform;
                        _player.parent = _stagePlayers;
                    }
                    _player.localPosition = new Vector2(i, l);
                    _playerPos = new Vector2Int(i, l);
                    CheckTile(_playerPos);
                }
            }
        }
    }

    string[] LoadStageFromFile() => stageMap[_stageIndex % stageMap.Length].text.Split('\n');

    void HandlePlayerMovement()
    {
        if (Input.GetKeyDown(KeyCode.R)) 
        {
            ResetStage();
            LoadStage();
            return;
        }

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
        return TouchMovement();
    }

    Vector2 GetMouseTouchPosition()
    {
        if (Input.GetMouseButton(0))
            return Input.mousePosition;

        if (Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            return touch.position;
        }
        return Vector2.zero;
    }

    Vector2Int TouchMovement()
    {
        var touchPosition = GetMouseTouchPosition();
        if (touchPosition == Vector2.zero)
        {
            _touchRefPos = Vector2.zero;
            return Vector2Int.zero;
        }
        if (_touchRefPos == Vector2.zero)
        {
            _touchRefPos = touchPosition;
            return Vector2Int.zero;
        }

        var delta = touchPosition - _touchRefPos;
        if (delta.sqrMagnitude < touchDeltaDist)
            return Vector2Int.zero;

        return Snap(delta);
    }

    void ResetTouchReference()
    {
        _touchRefPos = GetMouseTouchPosition();
    }

    Vector2Int Snap(Vector2 dir)
    {
        float absX = Mathf.Abs(dir.x);
        float absY = Mathf.Abs(dir.y);

        if (absX > absY)
        {
            return dir.x > 0 ? Vector2Int.right : Vector2Int.left;
        }
        else if (absX < absY)
        {
            return dir.y > 0 ? Vector2Int.up : Vector2Int.down;
        }
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

        CreateHitWallEffect();
        ResetTouchReference();
        CheckWinningCodintion();
    }

    void CreateHitWallEffect()
    {
        var effect = _effects.GetChild(0);
        effect.gameObject.SetActive(false);
        effect.position = _player.transform.position + Vector3.back * 5;
        effect.SetAsLastSibling();
        effect.gameObject.SetActive(true);
    }

    void CheckTile(Vector2Int tilePos)
    {
        if (_stageMap[tilePos.x][tilePos.y] != 0)
            return;

        _stageMap[tilePos.x][tilePos.y] = CHECK_ID;
        var tile = _tilePool.GetChild(0);
        tile.parent = _stageTiles;
        tile.localPosition = (Vector2)tilePos;
    }

    void CheckWinningCodintion()
    {
        for (int x = 0; x < _stageMap.Length; x++)
            for (int y = 0; y < _stageMap[x].Length; y++)
                if (_stageMap[x][y] == 0)
                    return;


        StartCoroutine(OnStageClear());
    }

    IEnumerator OnStageClear()
    {
        Debug.Log("Stage Clear!");
        _isStageClear = true;
        yield return new WaitForSeconds(stageWaitingTime);
        ResetStage();
        _stageIndex++;
        LoadStage();
    }

    void ResetStage()
    {
        _isStageClear = false;
        for (int i = _stageWalls.childCount - 1; i >= 0; i--)
            _stageWalls.GetChild(i).parent = _wallPool;
        for (int i = _stageTiles.childCount - 1; i >= 0; i--)
            _stageTiles.GetChild(i).parent = _tilePool;
    }
}
