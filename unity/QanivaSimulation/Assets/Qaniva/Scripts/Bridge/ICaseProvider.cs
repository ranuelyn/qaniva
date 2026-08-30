namespace Qaniva.Bridge
{
    /// <summary>
    /// Supplies the schema-validated case JSON for a given case id/version. The
    /// START_SIMULATION message only carries the id + version; the runtime resolves
    /// the actual document through this so the transport payload stays small.
    /// </summary>
    public interface ICaseProvider
    {
        string GetCaseJson(string caseId, int caseVersion);
    }
}
