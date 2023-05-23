using System.Text.Json;
using System.IO;

public class TreeSerializer
{
    public static void SaveToJson(int[][] tree, string filePath)
    {
        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        string jsonString = JsonSerializer.Serialize(tree, options);
        File.WriteAllText(filePath, jsonString);
    }

    public static int[][] LoadFromJson(string filePath)
    {
        string jsonString = File.ReadAllText(filePath);
        int[][] tree = JsonSerializer.Deserialize<int[][]>(jsonString);
        return tree;
    }
}