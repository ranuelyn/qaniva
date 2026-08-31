using System.Collections.Generic;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Maps the case's presentation keys (case.json `presentationProfile`) to the
    /// Resources paths of reusable presentation prefabs. This is the whole
    /// "content scale" boundary: shipping a new environment or patient visual is
    /// one prefab + one entry here — no gameplay code, no new scene.
    ///
    /// Unknown keys resolve to null; callers must fail loudly (no silent fallback
    /// to a wrong room).
    /// </summary>
    public static class PresentationRegistry
    {
        private static readonly Dictionary<string, string> Environments = new()
        {
            ["ed_resus_v1"] = "Qaniva/Environments/ed_resus_v1",
        };

        private static readonly Dictionary<string, string> Patients = new()
        {
            ["adult_neutral_v1"] = "Qaniva/Patients/adult_neutral_v1",
            ["adult_rigged_v1"] = "Qaniva/Patients/adult_rigged_v1",
        };

        public static string ResolveEnvironment(string roomKey) =>
            roomKey != null && Environments.TryGetValue(roomKey, out var path) ? path : null;

        public static string ResolvePatient(string patientVariant) =>
            patientVariant != null && Patients.TryGetValue(patientVariant, out var path) ? path : null;
    }
}
