using UnityEngine;

namespace Qaniva.Bridge
{
    /// <summary>
    /// Loads case JSON bundled under <c>Assets/Qaniva/Resources/Qaniva/Cases/&lt;caseId&gt;.json</c>.
    /// Fine for the MVP (all core assets bundled, blueprint §24). A remote/Addressables
    /// provider replaces this later without touching the controller.
    /// </summary>
    public sealed class ResourcesCaseProvider : ICaseProvider
    {
        public string GetCaseJson(string caseId, int caseVersion)
        {
            var asset = Resources.Load<TextAsset>($"Qaniva/Cases/{caseId}");
            if (asset == null)
            {
                throw new BridgeProtocolException(
                    $"Case resource \"Qaniva/Cases/{caseId}\" not found (requested v{caseVersion}).");
            }
            return asset.text;
        }
    }
}
