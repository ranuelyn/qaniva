using UnityEngine;
using UnityEngine.UIElements;

namespace Qaniva.Presentation
{
    /// <summary>
    /// Full-screen viewer for investigation result assets (the case's generic
    /// `resultAssets` — an ECG tracing today, X-ray/CT/ultrasound later).
    ///
    /// Presentation only: it renders a bundled image referenced by the engine's
    /// ActionOutcomeView.ResultAssetId. It never interprets the asset, never adds
    /// clinical text, and never touches simulation state. Pan is the ScrollView's
    /// native touch drag; zoom is deliberately simple (+/- buttons).
    ///
    /// Assets resolve from Resources/Qaniva/CaseAssets/&lt;assetId&gt; (bundled,
    /// mirroring ResourcesCaseProvider for case JSON).
    /// </summary>
    public sealed class ResultViewerPresenter
    {
        public const string AssetResourceFolder = "Qaniva/CaseAssets/";

        private const float MinZoom = 0.5f;
        private const float MaxZoom = 4f;
        private const float ZoomStep = 1.25f;

        /// <summary>Base on-panel width for a freshly opened asset (panel ref width 1206,
        /// minus the viewer's horizontal padding and the scroll surface inset).</summary>
        private const float FitWidth = 1150f;

        private readonly VisualElement _panel;
        private readonly Label _title;
        private readonly ScrollView _scroll;
        private readonly VisualElement _image;
        private readonly Label _note;
        private readonly VisualElement _actionArea;
        private readonly VisualElement _resultBanner;

        private Texture2D _texture;
        private float _zoom = 1f;
        private bool _bannerWasVisible;

        public ResultViewerPresenter(VisualElement root)
        {
            _panel = root.Q<VisualElement>("result-viewer");
            _title = root.Q<Label>("result-viewer-title");
            _scroll = root.Q<ScrollView>("result-viewer-scroll");
            _image = root.Q<VisualElement>("result-viewer-image");
            _note = root.Q<Label>("result-viewer-note");
            // The action drawer/result banner are later siblings (drawn above the
            // viewer) — the viewer hides them while open so the diagnostic asset
            // owns the whole screen.
            _actionArea = root.Q<VisualElement>("action-area");
            _resultBanner = root.Q<VisualElement>("result-banner");

            root.Q<Button>("result-viewer-close").clicked += Close;
            root.Q<Button>("result-zoom-in").clicked += () => SetZoom(_zoom * ZoomStep);
            root.Q<Button>("result-zoom-out").clicked += () => SetZoom(_zoom / ZoomStep);
        }

        public bool IsOpen => _panel != null && !_panel.ClassListContains("hidden");

        /// <summary>Opens the viewer for a result asset id. Missing assets fail loudly
        /// in the viewer itself (never a silent blank screen). A non-verified
        /// asset (provenance clinicalStatus != clinician_verified) shows a
        /// persistent provenance note so a placeholder can never be presented as
        /// verified diagnostic content.</summary>
        public void Open(string assetId, string label, string clinicalStatus = null)
        {
            if (_panel == null)
            {
                return;
            }

            _texture = Resources.Load<Texture2D>(AssetResourceFolder + assetId);
            _title.text = string.IsNullOrEmpty(label) ? assetId : label;

            if (_texture == null)
            {
                Debug.LogError($"[ResultViewer] asset \"{assetId}\" not found under Resources/{AssetResourceFolder}");
                _note.text = $"Result asset \"{assetId}\" is missing from this build.";
                _image.style.backgroundImage = new StyleBackground((Texture2D)null);
                _image.style.width = 0;
                _image.style.height = 0;
            }
            else
            {
                _note.text = clinicalStatus == "placeholder_replacement_required"
                    ? "Schematic training placeholder — NOT a verified diagnostic tracing (clinical verification pending)."
                    : clinicalStatus == "clinician_verified"
                        ? ""
                        : "Provenance not verified.";
                _image.style.backgroundImage = new StyleBackground(_texture);
                SetZoom(1f);
            }

            _bannerWasVisible = _resultBanner != null && !_resultBanner.ClassListContains("hidden");
            _actionArea?.AddToClassList("hidden");
            _resultBanner?.AddToClassList("hidden");
            _panel.RemoveFromClassList("hidden");
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }
            _panel.AddToClassList("hidden");
            _actionArea?.RemoveFromClassList("hidden");
            if (_bannerWasVisible)
            {
                _resultBanner?.RemoveFromClassList("hidden");
            }
            _bannerWasVisible = false;
        }

        private void SetZoom(float zoom)
        {
            _zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            if (_texture == null)
            {
                return;
            }
            float width = FitWidth * _zoom;
            _image.style.width = width;
            _image.style.height = width * ((float)_texture.height / _texture.width);
        }
    }
}
