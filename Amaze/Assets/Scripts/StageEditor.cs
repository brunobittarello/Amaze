#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(GameplayBehaviour))]
public class StageEditor : Editor
{
    const int RANDOM_LINE_TRY_LIMIT = 300;
    readonly bool DEBUG_STAGE_CREATION = true;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.HelpBox("This is a help box", MessageType.Info);

        if (GUILayout.Button("Create Random Stage"))
        {
            CreateRandomStages();
            // var serializedStageMap = serializedObject.FindProperty("stageMap");
        }
    }

    void CreateRandomStages()
    {
        var stageList = new Queue<StageDifficult>();
        stageList.Enqueue(StageDifficult.Easy);
        stageList.Enqueue(StageDifficult.Easy);
        stageList.Enqueue(StageDifficult.Easy);
        stageList.Enqueue(StageDifficult.Modarate);
        stageList.Enqueue(StageDifficult.Modarate);
        stageList.Enqueue(StageDifficult.Modarate);
        stageList.Enqueue(StageDifficult.Hard);
        stageList.Enqueue(StageDifficult.Hard);
        stageList.Enqueue(StageDifficult.Hard);
        stageList.Enqueue(StageDifficult.VeryHard);
        stageList.Enqueue(StageDifficult.VeryHard);
        stageList.Enqueue(StageDifficult.VeryHard);

        var index = 0;
        while (stageList.Count != 0)
        {
            var difficult = stageList.Dequeue();
            TryToCreateRandomStage("RandomStage-" + index, difficult);
            index++;
        }
    }

    bool TryToCreateRandomStage(string name, StageDifficult difficult)
    {
        for (int i = 0; i < 5; i++)
        {
            if (CreateRandomStage(name, difficult, i))
                return true;
        }
        Debug.Log("Give up on " + name);
        return false;
    }

    bool CreateRandomStage(string name, StageDifficult difficult, int tentative)
    {
        var settings = GetStageSettings(difficult);
        var stageSize = new Vector2Int(Random.Range(settings.SizeMin, settings.SizeMax), Random.Range(settings.SizeMin, settings.SizeMax));
        var stage = InitiateStage(stageSize.x, stageSize.y);
        var moves = Random.Range(settings.MovesMin, settings.MovesMax);
        int maxSize = (int)(stage.Length * 0.7f);

        Debug.Log("Size " + stageSize.x + "x" + stageSize.y + " Moves " + moves);

        if (!CreateMovements(name, stage, stageSize, maxSize, moves, settings.MovesMin, tentative))
            return false;

        // CreateStartPoint(stage, stageSize);
        var stageStr = StageIntoString(stage, false);
        SaveStage(name, stageStr);
        AssetDatabase.Refresh();
        Debug.Log("Stage Created!");
        return true;
    }

    bool CreateMovements(string name, int[][] stage, Vector2Int stageSize, int maxSize, int moves, int minMoves, int tentative)
    {
        var limit = 0;
        int lines;
        var queue = new Queue<Vector2Int>();
        // var start = new Vector2Int(Random.Range(1, stageSize.x - 1), Random.Range(1, stageSize.y - 1));
        var start = new Vector2Int(1, 1);//TODO
        stage[start.y][start.x] = 2;

        queue.Enqueue(start);
        for (lines = 0; lines < moves && limit < RANDOM_LINE_TRY_LIMIT;)
        {
            var pivot = queue.Dequeue();
            // if (Random.value < settings.BifurcationChance ) {
            //     Debug.Log("Bifurcation");
            //     queue.Enqueue(pivot);
            // }
            var size = Random.Range(1, maxSize);
            var newPivot = AddRandomLine(stage, pivot, size);
            if (pivot != newPivot)
            {
                queue.Enqueue(newPivot);
                lines++;
                if (DEBUG_STAGE_CREATION)
                    SaveStage("Debug/" + name + "-" + tentative + "-" + lines, StageIntoString(stage, true));
            }
            else if (queue.Count == 0)
                queue.Enqueue(pivot);
            limit++;
        }

        if (limit == RANDOM_LINE_TRY_LIMIT && lines < minMoves)
        {
            Debug.LogWarning("failed to create");
            return false;
        }
        return true;
    }

    StageCreationSettings GetStageSettings(StageDifficult difficult) => difficult switch
    {
        StageDifficult.Easy => new StageCreationSettings()
        {
            SizeMin = 7,
            SizeMax = 9,
            MovesMin = 7,
            MovesMax = 10,
            BifurcationChance = 0
        },
        StageDifficult.Modarate => new StageCreationSettings()
        {
            SizeMin = 9,
            SizeMax = 13,
            MovesMin = 12,
            MovesMax = 17,
            BifurcationChance = 0.05f
        },
        StageDifficult.Hard => new StageCreationSettings()
        {
            SizeMin = 12,
            SizeMax = 15,
            MovesMin = 20,
            MovesMax = 25,
            BifurcationChance = 0.15f
        },
        StageDifficult.VeryHard => new StageCreationSettings()
        {
            SizeMin = 12,
            SizeMax = 16,
            MovesMin = 25,
            MovesMax = 35,
            BifurcationChance = 0.2f
        },
        _ => null,
    };

    int[][] InitiateStage(int sizeX, int sizeY)
    {
        var stage = new int[sizeY][];
        for (int y = 0; y < sizeY; y++)
        {
            stage[y] = new int[sizeX];
            for (int x = 0; x < sizeX; x++)
                stage[y][x] = 1;
        }
        return stage;
    }

    bool IsValid(int[][] stage, Vector2Int pivot, Vector2Int dir, int size)
    {
        var final = pivot + dir * size;
        if (final.x <= 0 || final.y <= 0 || final.x >= stage.Length - 1 || final.y >= stage[0].Length - 1)
        {
            Debug.Log("Out of bounds " + final);
            return false;
        }

        var finalFixedWall = final + dir;
        var finalId = stage[finalFixedWall.x][finalFixedWall.y];
        if (stage[final.x][final.y] != 1 || (finalId != 1 && finalId != 9))
        {
            Debug.Log("Invalid end");
            return false;
        }

        for (int s = 1; s <= size; s++)
        {
            var target = pivot + dir * s;
            if (stage[target.x][target.y] == 9)
                return false;
        }

        return true;
    }

    Vector2Int AddRandomLine(int[][] stage, Vector2Int pivot, int size)
    {
        var dir = RandomDirection();
        if (!IsValid(stage, pivot, dir, size))
            return pivot;

        for (int s = 1; s <= size; s++)
        {
            var target = pivot + dir * s;
            if (stage[target.x][target.y] == 1)
                stage[target.x][target.y] = 0;
        }
        AddFixedWall(stage, pivot + dir * (size + 1));
        return pivot + dir * size;
    }

    void AddFixedWall(int[][] stage, Vector2Int point)
    {
        stage[point.x][point.y] = 9;
        Debug.Log("FIXED WALL " + point.x + " x " + point.y);
    }

    Vector2Int RandomDirection()
    {
        var r = Random.Range(0, 4);
        switch (r)
        {
            case 0: return Vector2Int.up;
            case 1: return Vector2Int.down;
            case 2: return Vector2Int.left;
            default:
                return Vector2Int.right;
        }
    }

    void CreateStartPoint(int[][] stage, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                if (stage[y][x] == 0)
                {
                    stage[y][x] = 2;
                    return;
                }
    }

    string[] StageIntoString(int[][] stage, bool isDebug)
    {
        var data = new string[stage.Length];
        for (int y = stage.Length - 1; y >= 0; y--)
        {
            var line = "";
            for (int x = 0; x < stage[0].Length; x++)
                line += stage[y][x].ToString();

            if (!isDebug)
                line = line.Replace('9', '1');
            data[stage.Length - 1 - y] = line;
        }
        return data;
    }

    void SaveStage(string name, string[] lines)
    {
        string path = Application.dataPath + "/Stages/" + name + ".txt";
        StreamWriter writer = new StreamWriter(path, false);
        for (int i = 0; i < lines.Length; i++)
            if (i + 1 == lines.Length)
                writer.Write(lines[i]);
            else
                writer.WriteLine(lines[i]);
        writer.Close();
    }
}
#endif