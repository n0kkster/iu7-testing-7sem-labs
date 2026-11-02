namespace Analyzer.Benchmarks.Scenarios;

using System.Text.Json;
using Analyzer.Benchmarks.Data;
using Analyzer.Benchmarks.Models;
using Newtonsoft.Json;

public class JsonBenchmarkScenarios
{
    public static volatile object? Consumer;

    private readonly SystemTopologyPayload _payload;
    private readonly string _stjJsonString;
    private readonly string _newtonsoftJsonString;

    private static readonly JsonSerializerOptions StjOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerSettings NewtonsoftSettings = new()
    {
        Formatting = Formatting.None
    };

    public JsonBenchmarkScenarios(int componentCount = 100, int linkCount = 150)
    {
        _payload = TopologyGenerator.Generate(componentCount, linkCount);

        _stjJsonString = System.Text.Json.JsonSerializer.Serialize(_payload, StjOptions);
        _newtonsoftJsonString = JsonConvert.SerializeObject(_payload, NewtonsoftSettings);

        File.WriteAllText($"results/comp{componentCount}_link{linkCount}.json", JsonConvert.SerializeObject(_payload, Formatting.Indented));
    }

    // =========================================================================
    // 1. WRITE
    // =========================================================================
    public void SystemTextJson_Write()
    {
        Consumer = System.Text.Json.JsonSerializer.Serialize(_payload, StjOptions);
    }

    public void Newtonsoft_Write()
    {
        Consumer = JsonConvert.SerializeObject(_payload, NewtonsoftSettings);
    }

    // =========================================================================
    // 2. READ
    // =========================================================================
    public void SystemTextJson_Read()
    {
        Consumer = System.Text.Json.JsonSerializer.Deserialize<SystemTopologyPayload>(_stjJsonString, StjOptions);
    }

    public void Newtonsoft_Read()
    {
        Consumer = JsonConvert.DeserializeObject<SystemTopologyPayload>(_newtonsoftJsonString, NewtonsoftSettings);
    }

    // =========================================================================
    // 3. READ + WRITE
    // =========================================================================
    public void SystemTextJson_Roundtrip()
    {
        var obj = System.Text.Json.JsonSerializer.Deserialize<SystemTopologyPayload>(_stjJsonString, StjOptions);
        Consumer = System.Text.Json.JsonSerializer.Serialize(obj, StjOptions);
    }

    public void Newtonsoft_Roundtrip()
    {
        var obj = JsonConvert.DeserializeObject<SystemTopologyPayload>(_newtonsoftJsonString, NewtonsoftSettings);
        Consumer = JsonConvert.SerializeObject(obj, NewtonsoftSettings);
    }
}