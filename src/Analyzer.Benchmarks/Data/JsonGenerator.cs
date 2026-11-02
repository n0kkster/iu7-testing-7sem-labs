namespace Analyzer.Benchmarks.Data;

using Analyzer.Benchmarks.Models;

public static class JsonGenerator
{
    private static TreeNode GenerateTreeNode(int depthLevel)
    {
        if (depthLevel == 1)
        {
            return new(
                Siblings: [],
                Child: null,
                Payload: new TreePayload("aboba", 42)
            );
        }

        return new(
            Siblings: [],
            Child: GenerateTreeNode(depthLevel - 1),
            Payload: new TreePayload("aboba", 42)
        );
    }

    private static List<TreeNode> GenerateNodes(int nodesCount, int depthLevel)
    {
        List<TreeNode> nodes = [];
        for (int i = 0; i < nodesCount; i++)
            nodes.Add(GenerateTreeNode(depthLevel));

        return nodes;
    }

    public static TreeNode Generate(int nodesCount = 100, int depthLevel = 5)
    {
        return new(
            Siblings: GenerateNodes(nodesCount, 1),
            Child: GenerateTreeNode(depthLevel - 1),
            Payload: new TreePayload("root", 1337)
        );
    }
}