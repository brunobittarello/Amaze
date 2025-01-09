#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

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
        var settings = GetStageSettings(difficult);
        for (int i = 0; i < 5; i++)
        {
            if (CreateRandomStage(name, settings, i))
                return true;
        }
        Debug.Log("Give up on " + name);
        return false;
    }

    bool CreateRandomStage(string name, StageCreationSettings settings, int tentative)
    {
        var stageSize = new Vector2Int(Random.Range(settings.SizeMin, settings.SizeMax), Random.Range(settings.SizeMin, settings.SizeMax));
        var stage = InitiateStage(stageSize.x, stageSize.y);
        var moves = Random.Range(settings.MovesMin, settings.MovesMax);
        int maxSize = (int)(stage.Length * 0.7f);

        Debug.Log("Size " + stageSize.x + "x" + stageSize.y + " Moves " + moves);

        if (!CreateMovements(name, stage, stageSize, maxSize, moves, settings.MovesMin, tentative))
            return false;

        // CreateStartPoint(stage, stageSize);
        var stageStr = StageIntoString2(stage);
        SaveStage(name, stageStr);
        AssetDatabase.Refresh();
        Debug.Log("Stage Created!");
        return true;
    }

    bool CreateMovements(string name, int[][] stage, Vector2Int stageSize, int maxSize, int moves, int minMoves, int tentative)
    {
        breakAvailable = 3;
        var connections = Connections(stageSize);
        var start = connections[0].Item1;
        stage[start.x][start.y] = 2;

        for (int i = 0; i < connections.Count; i++)
        {
            var result = ConnectTwoPoints(stage, connections[i].Item1, connections[i].Item2);
            SaveStage("Debug/" + name + "-" + tentative + "-" + i, StageIntoString(stage, true));
            if (result == false) return false;
        }
        return true;
    }

    List<Tuple<Vector2Int, Vector2Int>> Connections(Vector2Int stageSize)
    {
        var connections = new List<Tuple<Vector2Int, Vector2Int>>();
        var halfSize = stageSize / 3;
        var randomPos = new Vector2Int(Random.Range(1, halfSize.x), Random.Range(1, halfSize.y));
        var start = randomPos;
        randomPos = new Vector2Int(Random.Range(1, halfSize.x), Random.Range(1, halfSize.y));
        var p1 = new Vector2Int(stageSize.x - randomPos.x - 1, randomPos.y);
        randomPos = new Vector2Int(Random.Range(1, halfSize.x), Random.Range(1, halfSize.y));
        var p2 = new Vector2Int(randomPos.x, stageSize.y - randomPos.y - 1);
        randomPos = new Vector2Int(Random.Range(1, halfSize.x), Random.Range(1, halfSize.y));
        var p3 = new Vector2Int(stageSize.x - randomPos.x - 1, stageSize.y - randomPos.y - 1);

        connections.Add(new Tuple<Vector2Int, Vector2Int>(start, p1));
        connections.Add(new Tuple<Vector2Int, Vector2Int>(start, p2));
        connections.Add(new Tuple<Vector2Int, Vector2Int>(p1, p3));
        connections.Add(new Tuple<Vector2Int, Vector2Int>(p2, p3));
        return connections;
    }

    StageCreationSettings GetStageSettings(StageDifficult difficult) => difficult switch
    {
        StageDifficult.Easy => new StageCreationSettings()
        {
            SizeMin = 9,
            SizeMax = 13,
            MovesMin = 12,
            MovesMax = 17,
            BifurcationChance = 0.05f
        },
        StageDifficult.Modarate => new StageCreationSettings()
        {
            SizeMin = 12,
            SizeMax = 15,
            MovesMin = 20,
            MovesMax = 25,
            BifurcationChance = 0.15f
        },
        StageDifficult.Hard => new StageCreationSettings()
        {
            SizeMin = 12,
            SizeMax = 16,
            MovesMin = 25,
            MovesMax = 35,
            BifurcationChance = 0.2f
        },
        StageDifficult.VeryHard => new StageCreationSettings()
        {
            SizeMin = 16,
            SizeMax = 20,
            MovesMin = 25,
            MovesMax = 35,
            BifurcationChance = 0.2f
        },
        _ => null,
    };

    int[][] InitiateStage(int sizeX, int sizeY)
    {
        var stage = new int[sizeX][];
        for (int x = 0; x < sizeX; x++)
        {
            stage[x] = new int[sizeY];
            for (int y = 0; y < sizeY; y++)
                stage[x][y] = 1;
        }
        return stage;
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

    Vector2Int AddRectangle(int[][] stage, Vector2Int pivot)
    {
        var height = stage[0].Length;
        var randomPoint = RandomSecondRectPoint(pivot, stage.Length, height);
        if (randomPoint == pivot)
        {
            Debug.LogWarning("What is the odds?!");
            return pivot;
        }

        var pivot1 = new Vector2Int(pivot.x, randomPoint.y);
        var pivot2 = new Vector2Int(randomPoint.x, pivot.y);

        if (IsValid2(stage, pivot, pivot1) && IsValid2(stage, pivot, pivot2) && IsValid2(stage, pivot1, randomPoint) && IsValid2(stage, pivot2, randomPoint))
        {
            WriteRectangle(stage, pivot, pivot1, pivot2, randomPoint);
            return randomPoint;
        }

        return randomPoint;
    }

    int breakAvailable;
    bool ConnectTwoPoints(int[][] stage, Vector2Int p1, Vector2Int p2, bool isABreak = false)
    {
        Debug.Log($"Points from {p1} to {p2}");
        var dx = Mathf.Abs(p1.x - p2.x);
        var dy = Mathf.Abs(p1.y - p2.y);
        Debug.Log($"Points {dx} to {dy}");
        if (dx == 0 || dy == 0)
        {
            if (IsValid3(stage, p1, p2) && IsValid3(stage, p2, p1))
            {
                WriteLine(stage, p1, p2);
                return true;
            }
            return false;
        }

        var p3 = new Vector2Int(p1.x, p2.y);
        var p4 = new Vector2Int(p2.x, p1.y);
        var order = new Vector2Int[2];

        if (dx > dy)
        {
            Debug.Log($"ConnectTwoPoints A");
            order[0] = p3;
            order[1] = p4;
        }
        else
        {
            Debug.Log($"ConnectTwoPoints B");
            order[0] = p4;
            order[1] = p3;
        }

        if (!isABreak && breakAvailable > 0 && Random.value < 0.8f)
        {
            breakAvailable--;
            Vector2Int px;
            if (Random.value < 0.5f)
                px = new Vector2Int((p1.x + p2.x) / 2, p1.y);
            else
                px = new Vector2Int(p1.x, (p1.y + p2.y) / 2);

            Debug.Log($"Break added");
            var result = ConnectTwoPoints(stage, p1, px, true) && ConnectTwoPoints(stage, px, p2, true);
            if (!result) return false;
            if (Random.value < 0.5f) return true;
        }

        foreach (var px in order)
            if (IsValid3(stage, p1, px) && IsValid3(stage, p2, px))
            {
                WriteLine(stage, p1, px);
                WriteLine(stage, px, p2);
                return true;
            }
        Debug.LogError($"FAILED");
        return false;
    }

    Vector2Int RandomSecondRectPoint(Vector2Int pivot, int width, int height)
    {
        for (int i = 0; i < 99; i++)
        {
            var randomPoint = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
            if (randomPoint.x != pivot.x && randomPoint.y != pivot.y)
                return randomPoint;
        }
        return pivot;
    }

    bool IsValid3(int[][] stage, Vector2Int from, Vector2Int to)
    {
        var dir = new Vector2Int(MathF.Sign(to.x - from.x), Math.Sign(to.y - from.y));
        var next = from + dir;
        if (stage[next.x][next.y] == 0)
            return false;
        return true;
    }

    bool IsValid2(int[][] stage, Vector2Int from, Vector2Int to)
    {
        var dir = new Vector2Int(MathF.Sign(to.x - from.x), Math.Sign(to.y - from.y));
        var size = Math.Max(Math.Abs(to.x - from.x), Math.Abs(to.y - from.y));
        return IsValid(stage, from, dir, size);
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

    void WriteRectangle(int[][] stage, Vector2Int p1, Vector2Int p2, Vector2Int p3, Vector2Int p4)
    {
        WriteLine(stage, p1, p2);
        WriteLine(stage, p1, p3);
        WriteLine(stage, p2, p4);
        WriteLine(stage, p3, p4);
    }

    void WriteLine(int[][] stage, Vector2Int p1, Vector2Int p2)
    {
        Debug.Log($"Line from {p1} to {p2}");
        var diff = p2 - p1;
        var dir = new Vector2Int(MathF.Sign(diff.x), Math.Sign(diff.y));
        var size = Math.Max(Math.Abs(diff.x), Math.Abs(diff.y));
        WriteLine(stage, p1, dir, size);
    }

    void WriteLine(int[][] stage, Vector2Int pivot, Vector2Int dir, int size)
    {
        for (int s = 1; s <= size; s++)
        {
            var target = pivot + dir * s;
            if (stage[target.x][target.y] == 1)
                stage[target.x][target.y] = 0;
        }
        //AddFixedWall(stage, pivot + dir * (size + 1));
    }

    void AddFixedWall(int[][] stage, Vector2Int point)
    {
        Debug.Log("FIXED WALL " + point.x + " x " + point.y);
        stage[point.x][point.y] = 9;
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

    bool HasAnyEmptySpace(int[][] stage, int x)
    {
        for (int y = 0; y < stage[x].Length; y++)
        {
            if (stage[x][y] != 1 || stage[x][y] != 9)
                return true;
        }
        return false;
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

    string[] StageIntoString2(int[][] stage)
    {
        var data = new List<string>();
        for (int x = 0; x < stage.Length; x++)
        {
            var line = "";
            var hasEmptySpace = false;
            for (int y = 0; y < stage[0].Length; y++)
            {
                if (stage[x][y] == 0 || stage[x][y] == 2) hasEmptySpace = true;
                line += stage[x][y].ToString();
            }

            line = line.Replace('9', '1');

            if (x != 0 && x != stage.Length - 1 && !hasEmptySpace)
                continue;

            data.Add(line);
        }

        var data2 = new List<string>();
        for (int i = data[0].Length - 1; i >= 0 ; i--)
        {
            var line = "";
            var hasEmptySpace = false;
            for (int l = 0; l < data.Count; l++)
            {
                if (data[l][i] == '0' || data[l][i] == '2') hasEmptySpace = true;
                line += data[l][i];
            }

            if (i != 0 && i != data[0].Length - 1 && !hasEmptySpace)
                continue;

            data2.Add(line);
        }
        return data2.ToArray();
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