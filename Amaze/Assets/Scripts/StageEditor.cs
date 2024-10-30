using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(GameplayBehaviour))]
public class StageEditor : Editor
{
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
            if (CreateRandomStage(name, difficult))
                return true;
        }
        Debug.Log("Give up on " + name);
        return false;
    }

    bool CreateRandomStage(string name, StageDifficult difficult)
    {
        var settings = GetStageSettings(difficult);
        var stage = InitiateStage(Random.Range(settings.SizeMin, settings.SizeMax), Random.Range(settings.SizeMin, settings.SizeMax));
        stage[1][1] = 2;
        var limit = 0;
        var moves = Random.Range(settings.MovesMin, settings.MovesMax);
        Debug.Log("Size " + stage.Length + "x" + stage[0].Length + " Moves " + moves);

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(1, 1));
        for (int l = 0; l < moves && limit < 100;)
        {
            var pivot = queue.Dequeue();
            // if (Random.value < settings.BifurcationChance ) {
            //     Debug.Log("Bifurcation");
            //     queue.Enqueue(pivot);
            // }
            var size = Random.Range(1, stage.Length);
            var newPivot = AddRandomLine(stage, pivot, size);
            if (pivot != newPivot)
            {
                pivot = newPivot;
                l++;
                if (DEBUG_STAGE_CREATION)
                    SaveStage("Debug/" + name + "-" + l, StageIntoString(stage, true));
            }
            queue.Enqueue(pivot);
            limit++;
        }

        if (limit == 100)
        {
            Debug.LogWarning("failed to create");
            return false;
        }
        var stageStr = StageIntoString(stage, false);
        SaveStage(name, stageStr);
        AssetDatabase.Refresh();
        Debug.Log("Stage Created!");
        return true;
    }

    StageCreationSettings GetStageSettings(StageDifficult difficult) => difficult switch
    {
        StageDifficult.Easy => new StageCreationSettings()
        {
            SizeMin = 7,
            SizeMax = 9,
            MovesMin = 7,
            MovesMax = 9,
            BifurcationChance = 0
        },
        StageDifficult.Modarate => new StageCreationSettings()
        {
            SizeMin = 9,
            SizeMax = 13,
            MovesMin = 10,
            MovesMax = 15,
            BifurcationChance = 0.05f
        },
        StageDifficult.Hard => new StageCreationSettings()
        {
            SizeMin = 12,
            SizeMax = 15,
            MovesMin = 13,
            MovesMax = 18,
            BifurcationChance = 0.15f
        },
        StageDifficult.VeryHard => new StageCreationSettings()
        {
            SizeMin = 12,
            SizeMax = 16,
            MovesMin = 15,
            MovesMax = 25,
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