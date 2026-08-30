using System.IO;
using Qaniva.Clinical.Core.Serialization;
using Xunit;

namespace Qaniva.Clinical.Tests;

public class CaseLoaderTests
{
    [Fact]
    public void LoadsTheDemoFixture()
    {
        var c = TestData.DemoCase();
        Assert.Equal("demo_sync_bradycardia_001", c.Id);
        Assert.Equal(1, c.SchemaVersion);
        Assert.True(c.Metadata.Fictional);
        Assert.NotEmpty(c.AvailableActions);
        Assert.NotEmpty(c.ScoringCriteria);
        Assert.NotEmpty(c.TerminalStates);
    }

    [Fact]
    public void RejectsEmptyJson()
    {
        Assert.Throws<System.ArgumentException>(() => CaseLoader.FromJson("  "));
    }

    [Fact]
    public void RejectsNonFictionalCase()
    {
        var json = File.ReadAllText(TestData.CasePath).Replace("\"fictional\": true", "\"fictional\": false");
        var ex = Assert.Throws<CaseLoadException>(() => CaseLoader.FromJson(json));
        Assert.Contains("fictional", ex.Message);
    }

    [Fact]
    public void RejectsUnsupportedSchemaVersion()
    {
        var json = File.ReadAllText(TestData.CasePath).Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2");
        Assert.Throws<CaseLoadException>(() => CaseLoader.FromJson(json));
    }
}
