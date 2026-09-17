namespace Analyzer.Infrastructure.Queries;

public static class CypherQueryFactory
{
    #region Компоненты

    public static string GetComponentsBySystemId() => @"
        MATCH (n) 
        WHERE n.system_id = $SystemId 
        RETURN n.name AS Name, 
               n.id AS Id, 
               n.desc AS Desc,
               n.system_id AS SystemId, 
               labels(n) AS Type";

    public static string GetComponentById() => @"
        MATCH (n) 
        WHERE n.id = $Id 
        RETURN n.name AS Name, 
               n.desc AS Desc, 
               n.system_id AS SystemId, 
               labels(n) AS Type";

    public static string AddComponent(string componentType) => $@"
        CREATE (:{componentType} {{
            name: $Name, 
            id: $Guid, 
            desc: $Description,
            system_id: $SystemId
        }})";

    public static string UpdateComponent() => @"
        MATCH (c {id: $Id})
        SET c.name = $Name, 
            c.desc = $Description";

    public static string DeleteComponent() => @"
        MATCH (c {id: $Id})
        DETACH DELETE c";

    #endregion

    #region Связи

    public static string AddLink() => @"
        MATCH (s { id: $SourceId })
        MATCH (t { id: $TargetId })
        CREATE (s)-[:DEPENDS_ON {id: $Id, severity: $Severity, protocol: $Protocol}]->(t)";

    public static string GetLinksBySystemId() => @"
        MATCH (source)-[r:DEPENDS_ON]->(target)
        WHERE source.system_id = $SystemId AND target.system_id = $SystemId
        RETURN source.id AS SourceId, target.id AS TargetId, 
               r.severity AS Severity, r.protocol AS Protocol, r.id AS Id";

    public static string DeleteLink() => @"
        MATCH ()-[r:DEPENDS_ON]->()
        WHERE r.id = $Id
        DELETE r";

    #endregion

    #region Аналитика отказоустойчивости

    public static string GetCascadingFailureImpact() => @"
        MATCH (failed {id: $FailedId})
        CALL apoc.path.subgraphNodes(failed, {
            relationshipFilter: '<DEPENDS_ON',
            minLevel: 1
        }) YIELD node AS affected
        RETURN affected.id AS Id";

    public static string GetCyclicDependencies() => @"
        MATCH (c {system_id: $SystemId})
        CALL apoc.path.expandConfig(c, {
            relationshipFilter: 'DEPENDS_ON>',
            terminatorNodes: [c],
            minLevel: 1,
            uniqueness: 'RELATIONSHIP_PATH'
        }) YIELD path
        WITH [node in nodes(path)[0..-1] | node.id] AS cycleMembers
        WITH apoc.coll.sort(cycleMembers) AS sortedCycle
        RETURN DISTINCT sortedCycle AS CycleIds";

    public static string GetSinglePointsOfFailure() => @"
        MATCH (c {system_id: $SystemId})
        CALL apoc.path.subgraphNodes(c, {
            relationshipFilter: '<DEPENDS_ON',
            minLevel: 1
        }) YIELD node AS dependent
        WITH c, count(dependent) AS ImpactCount
        WHERE ImpactCount >= $Threshold
        RETURN c.id AS Id, ImpactCount";

    public static string GetDecommissioningImpact() => @"
        MATCH (target {id: $TargetId})
        CALL apoc.path.subgraphNodes(target, {
            relationshipFilter: '<DEPENDS_ON',
            minLevel: 1
        }) YIELD node AS impacted
        RETURN impacted.id AS Id";

    public static string GetDeploymentRiskPaths() => @"
        MATCH (target {id: $TargetId})
        CALL apoc.path.expandConfig(target, {
            relationshipFilter: '<DEPENDS_ON',
            minLevel: 1,
            uniqueness: 'NODE_PATH'
        }) YIELD path
        RETURN [node in reverse(nodes(path)) | node.id] AS PathIds";

    #endregion
}