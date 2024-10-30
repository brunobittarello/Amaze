using System.IO;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(GameplayBehaviour))]
public class StageEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.HelpBox("This is a help box", MessageType.Info);

        if (GUILayout.Button("Create Random Stage"))
        {
            CreateRandomStage();
        }
    }

    void CreateRandomStage()
    {
        var stage = InitiateStage(9, 9);
        stage[1][1] = 2;
        var pivot = new Vector2Int(1, 1);
        var limit = 0;
        for (int l = 0; l < 10 && limit < 100;)
        {
            var newPivot = AddRandomLine(stage, pivot);
            if (pivot != newPivot)
            {
                pivot = newPivot;
                l++;
                SaveStage("teste-debug"+l, StageIntoString(stage));
            }
            limit++;
        }

        if (limit == 100) {
            Debug.LogWarning("failed to create");
            return;
        }
        var stageStr = StageIntoString(stage);
        SaveStage("teste", stageStr);
        Debug.Log("Stage Created!");
    }

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
        if (final.x <= 0 || final.y <= 0 || final.x >= stage.Length - 1 || final.y >= stage[0].Length - 1) {
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
        
        return true;
    }

    Vector2Int AddRandomLine(int[][] stage, Vector2Int pivot)
    {
        var size = Random.Range(1, 10);
        var dir = RandomDirection();
        if (!IsValid(stage, pivot, dir, size))
            return pivot;

        var newPivot = pivot;
        for (int s = 1; s <= size; s++)
        {
            var target = pivot + dir * s;
            if (target.x == 0 || target.y == 0 || target.x == stage.Length - 1 || target.y == stage[0].Length - 1) {
                AddFixedWall(stage, target);
                return newPivot;
            }

            if (stage[target.x][target.y] == 9)
                return newPivot;

            if (stage[target.x][target.y] == 1)
            {
                stage[target.x][target.y] = 0;
                newPivot = target;
            }
        }
        if (pivot != newPivot)
            AddFixedWall(stage, pivot + dir * (size + 1));
        return newPivot;
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

    string[] StageIntoString(int[][] stage)
    {
        var data = new string[stage.Length];
        for (int y = stage.Length - 1; y >= 0; y--)
        {
            var line = "";
            for (int x = 0; x < stage[0].Length; x++)
                line += stage[y][x].ToString().Replace('1','1');
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