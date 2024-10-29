using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameplayBehaviour : MonoBehaviour
{
    const int POOL_SIZE = 50;

    [SerializeField]
    private TextAsset stageMap;

    [SerializeField]
    private GameObject PlayerPrefab;
    [SerializeField]
    private GameObject WallPrefab;

    private Transform _wallPool;
    private Transform _stage;

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
        Debug.Log(stageGrid[0]);
        LoadStage(stageGrid);
    }

    // Update is called once per frame
    void Update()
    {

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

    void LoadStage(string[] mapGrid)
    {
        for (int i = 0; i < mapGrid.Length; i++)
        {
            for (int l = 0; l < mapGrid[i].Length; l++)
            {
                var itemId = mapGrid[i][l];
                if (itemId == '1') {
                    var wall = _wallPool.GetChild(0);
                    wall.parent = _stage;
                    wall.localPosition = new Vector3(i, 0, l);
                } else if (itemId == '1') {
                    var player = Instantiate(PlayerPrefab).transform;
                    player.parent = _stage;
                    player.localPosition = new Vector3(i, 0, l);
                }
            }
        }
    }
}
