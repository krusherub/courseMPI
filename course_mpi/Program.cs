using System;
using System.Collections.Generic;
using System.Diagnostics;
using MPI;

public class Program
{
    static string pathToTestData = "D:\\data\\";
    public static void Main(string[] args)
    {
        using (new MPI.Environment(ref args))
        {
            int size = Communicator.world.Size;
            int rank = Communicator.world.Rank;
            int depth = 3;
            int childrens = 3;
            var path = pathToTestData + "tree depth - " + depth + ", childrens - " + childrens + ".txt";
            var treeArrayed = TreeSerializer.LoadFromJson(path);
            List<int> localVisitedNodes = null;
            Stopwatch watch = null;
            List<int> subtreeRoots = null;
        
            int root_node_id = 0;
            
            if (rank == 0)
            {
                // Запуск таймера
                watch = new Stopwatch();
                watch.Start();
                Console.WriteLine("The tree is loaded, starting test..");
                Console.WriteLine("Depth: " + depth + ", childrens: " + childrens);
                subtreeRoots = GetSubtreeRoots(treeArrayed);
                if (subtreeRoots.Count != size)
                {
                    Console.WriteLine("Not correct number of processes");
                    return;
                }
                for (int i = 1; i < size; i++)
                {
                    Communicator.world.Send<int>(subtreeRoots[i], i, i);
                }
        
                root_node_id = subtreeRoots[0];
            }
            else
            {
                root_node_id = Communicator.world.Receive<int>(0, rank); // Whole tree receive.
            }
            localVisitedNodes = RunBFS(treeArrayed, root_node_id);
            var results = Communicator.world.Gather(localVisitedNodes.ToArray(), 0);
            if (rank == 0)
            {
                var visitedNodesGlobal = new List<int>() {0};
                foreach (var result in results)
                {
                    visitedNodesGlobal.AddRange(result);
                }
                // Зупинка таймера
                watch.Stop();
                Console.WriteLine("Time taken for algo: " + watch.ElapsedTicks + " ticks");
                Console.WriteLine("Visited number: " + visitedNodesGlobal.Count);
                 PrintBFSQueue(visitedNodesGlobal);
                Console.WriteLine();
            }
        }
     //     List<(int depth, int childrens)> levelsAndChilds =
     //     new List<(int depth, int childrens)>(){
     //         (3, 3),
     //         (3, 5),
     //         (4,4),
     //         (4,6),
     //         (5,5),
     //         (5,7),
     //         (6,6),
     //         (6,8),
     //         // (7,7),
     //         // (7,9),
     //         // (8,8),
     //         // (8,9),
     //     };
     // //   GenerateTestData(levelsAndChilds, pathToTestData);
     //    TestConsistentAlgo(levelsAndChilds, pathToTestData);
     
    }

     static void TestConsistentAlgo(List<(int depth,int childrens)> levelsAndChilds, string pathToFolderFrom)
    {
        foreach (var data in levelsAndChilds)
        {
            var path = pathToFolderFrom + "tree depth - " + data.depth + ", childrens - " + data.childrens +
                       ".txt";

            var treeArrayed = TreeSerializer.LoadFromJson(path);
            Console.WriteLine("The tree is loaded, starting test..");
            Console.WriteLine("Depth: " + data.depth + ", childrens: " + data.childrens);
            var watch = new Stopwatch();
            watch.Start();
            var visitiedNodes = RunBFS(treeArrayed, 0);
            watch.Stop();
            Console.WriteLine("Time taken for algo: " + watch.ElapsedTicks + " ticks");
            Console.WriteLine("Visited number: " + visitiedNodes.Count);
                //   PrintBFSQueue(visitiedNodes);
            Console.WriteLine();

        }
    }
    public static List<int> GetSubtreeRoots(int[][] tree) 
    {
        // Список для зберігання індексів коренів піддерев
        List<int> roots = new List<int>();

        // Додавання дітей кореневого вузла до списку
        for (int i = 0; i < tree[0].Length; i++)
        {
            roots.Add(tree[0][i]);
        }

        return roots;
    }

    public static List<int> RunBFS(int[][] tree, int root)
    {
        bool[] visited = new bool[tree.Length];
        List<int> result = new List<int>();

        Queue<int> queue = new Queue<int>();
        queue.Enqueue(root);
        visited[root] = true;

        while(queue.Count > 0)
        {
            int node = queue.Dequeue();
            result.Add(node);

            foreach(int child in tree[node])
            {
                if(!visited[child])
                {
                    queue.Enqueue(child);
                    visited[child] = true;
                }
            }
        }

        return result;
    }
    
    public static void PrintBFSQueue(List<int> bfsQueue)
    {
        Console.Write("Вузли, відвідані в порядку BFS: ");
        for(int i = 0; i < bfsQueue.Count; i++)
        {
            Console.Write(bfsQueue[i]);
            if(i < bfsQueue.Count - 1)
            {
                Console.Write(" -> ");
            }
        }
        Console.WriteLine();
    }


    private static void GenerateTestData(List<(int depth,int childrens)> levelsAndChilds, string pathToFolderToSave)
    {
        foreach (var data in levelsAndChilds)
        {
            var tree = GenerateTree(data.depth, data.childrens);
            var treeArrayed = ConvertToArrays(tree);
            var a = RunBFS(treeArrayed, 0);
            //Console.WriteLine(a.Count);
            var path = pathToFolderToSave + "tree depth - " + data.depth + ", childrens - " + data.childrens +
                       ".txt";
            TreeSerializer.SaveToJson(treeArrayed, path);
        }
    }

    private static int[][] ConvertToArrays(List<List<int>> tree)
    {
        int[][] arrayTree = new int[tree.Count][];
        for (int i = 0; i < tree.Count; i++)
        {
            arrayTree[i] = tree[i].ToArray();
        }
        return arrayTree;
    }
    private static List<List<int>> GenerateTree(int maxDepth, int maxChildren)
    {
        int maxNodes = (int)(Math.Pow(maxChildren, maxDepth+1));
        var tree = new List<List<int>> { new List<int>() };
        var unusedNodes = new List<int>();
        for (int i = 1; i < maxNodes; ++i)
        {
            unusedNodes.Add(i);
        }

        GenerateSubtree(tree, 0, maxDepth, maxChildren, unusedNodes);
        return tree;
    }
    
    private static void GenerateSubtree(List<List<int>> tree, int node, int maxDepth, int maxChildren, List<int> unusedNodes)
    {
        if (maxDepth == 0 || unusedNodes.Count == 0)
        {
            return;
        }

        int childrenCount = Math.Min(unusedNodes.Count, maxChildren);
        for (int i = 0; i < childrenCount; ++i)
        {
            int randomIndex = new Random().Next(unusedNodes.Count);
            int child = unusedNodes[randomIndex];
            unusedNodes.RemoveAt(randomIndex);
            tree[node].Add(child);
            if (child >= tree.Count)
            {
                while (child >= tree.Count)
                {
                    tree.Add(new List<int>());
                }
            }

            GenerateSubtree(tree, child, maxDepth - 1, maxChildren, unusedNodes);
        }
    }
}