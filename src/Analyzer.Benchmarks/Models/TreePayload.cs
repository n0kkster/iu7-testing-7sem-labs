namespace Analyzer.Benchmarks.Models;

public record TreePayload(
    string Name,
    int Age
);

public record TreeNode(
    List<TreeNode> Siblings,
    TreeNode? Child,
    TreePayload Payload
);