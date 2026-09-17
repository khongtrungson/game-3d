using UnityEngine;

namespace NullProtocol.UI
{
    /// <summary>
    /// FR-36: Enforces 100% diegetic viewport clarity.
    /// Ensures viewport remains strictly free of traditional 2D HUD overlays
    /// (no floating mini-maps, screen-edge health bars, or floating ammo counts).
    /// Disables any non-diegetic HUD canvases while preserving in-world UI, pause menus, and debrief screens.
    /// </summary>
    [DisallowMultipleComponent]
    public class DiegeticViewportEnforcer : MonoBehaviour
    {
        [Header("Non-Diegetic HUD Detection & Suppression")]
        [Tooltip("When enabled, searches for and suppresses floating 2D HUD canvases on Awake/Start")]
        [SerializeField] private bool _autoSuppressFloatingHUDs = true;

        [Tooltip("Layer or Tag names associated with floating HUD overlays")]
        [SerializeField] private string[] _hudTagsToSuppress = new string[] { "FloatingHUD", "ScreenOverlayHUD", "MiniMap" };

        [Tooltip("Specific GameObjects to disable to maintain 100% diegetic clarity")]
        [SerializeField] private GameObject[] _floatingHUDObjectsToDisable;

        public bool AutoSuppressFloatingHUDs => _autoSuppressFloatingHUDs;
        public int SuppressedCount { get; private set; }

        private void Awake()
        {
            EnforceDiegeticViewport();
        }

        private void Start()
        {
            EnforceDiegeticViewport();
        }

        /// <summary>
        /// Validates that no floating ScreenSpaceOverlay canvases exist for HUD elements.
        /// Retains WorldSpace canvases and tactical debrief / pause menus.
        /// </summary>
        public void EnforceDiegeticViewport()
        {
            int count = 0;

            if (_floatingHUDObjectsToDisable != null)
            {
                for (int i = 0; i < _floatingHUDObjectsToDisable.Length; i++)
                {
                    if (_floatingHUDObjectsToDisable[i] != null && _floatingHUDObjectsToDisable[i].activeSelf)
                    {
                        _floatingHUDObjectsToDisable[i].SetActive(false);
                        count++;
                    }
                }
            }

            if (_autoSuppressFloatingHUDs)
            {
                var allCanvases = FindObjectsByType<Canvas>();
                for (int i = 0; i < allCanvases.Length; i++)
                {
                    var canvas = allCanvases[i];
                    if (canvas == null) continue;

                    // WorldSpace canvases are diegetic (e.g. wrist display, terminal screens) -> keep active
                    if (canvas.renderMode == RenderMode.WorldSpace)
                    {
                        continue;
                    }

                    // Check if canvas represents a forbidden 2D HUD overlay
                    string canvasName = canvas.gameObject.name.ToLowerInvariant();
                    bool isForbiddenHUD = canvasName.Contains("minimap") ||
                                          canvasName.Contains("ammohud") ||
                                          canvasName.Contains("healthbar") ||
                                          canvasName.Contains("floatinghud") ||
                                          canvasName.Contains("screenhud") ||
                                          canvasName.Contains("overlayhud");

                    // Check tags
                    if (!isForbiddenHUD && _hudTagsToSuppress != null)
                    {
                        for (int t = 0; t < _hudTagsToSuppress.Length; t++)
                        {
                            if (!string.IsNullOrEmpty(_hudTagsToSuppress[t]) && canvas.CompareTag(_hudTagsToSuppress[t]))
                            {
                                isForbiddenHUD = true;
                                break;
                            }
                        }
                    }

                    if (isForbiddenHUD && canvas.gameObject.activeSelf)
                    {
                        canvas.gameObject.SetActive(false);
                        count++;
                    }
                }
            }

            SuppressedCount = count;
        }

        /// <summary>
        /// Checks if a canvas is compliant with FR-36 diegetic design rules.
        /// </summary>
        public static bool IsCanvasCompliant(Canvas canvas)
        {
            if (canvas == null) return true;
            if (canvas.renderMode == RenderMode.WorldSpace) return true;

            string name = canvas.gameObject.name.ToLowerInvariant();
            if (name.Contains("minimap") || name.Contains("ammohud") || name.Contains("healthbar") || name.Contains("floatinghud"))
            {
                return false;
            }

            return true;
        }
    }
}
