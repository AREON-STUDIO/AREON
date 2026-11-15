// Assets/AREON/Editor/Stage10DependencyAudit.cs
// Stage 10 - Tasks 49 and 50. READ-ONLY audit (never deletes, moves or edits an asset).
// The only write is the optional package export, which writes a .unitypackage OUTSIDE Assets/.
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AREON.EditorTools
{
    public static class Stage10DependencyAudit
    {
        private const string Root = "Assets/AREON";
        private const string ScenePath = "Assets/AREON/Scenes/AREON_Prototype.unity";

        /// <summary>
        /// External (non-AREON) assets the team has REVIEWED AND APPROVED for the package, e.g. an input actions asset.
        /// Leave empty until the audit report proves one is required. Never add unrelated content.
        /// </summary>
        private static readonly string[] ApprovedExternalAssets = new[]
        {
            "Assets/InputSystem_Actions.inputactions"
        };

        [MenuItem("AREON/Stage 10/Run Dependency And Isolation Audit")]
        public static void RunAudit()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== AREON Stage 10 audit ===");

            // 1. Everything the prototype scene needs (transitive).
            string[] deps = AssetDatabase.GetDependencies(ScenePath, true);

            List<string> inside = deps.Where(p => p.StartsWith(Root + "/")).OrderBy(p => p).ToList();
            List<string> outsideAssets = deps.Where(p => p.StartsWith("Assets/") && !p.StartsWith(Root + "/")).OrderBy(p => p).ToList();
            List<string> packages = deps.Where(p => p.StartsWith("Packages/")).OrderBy(p => p).ToList();
            sb.AppendLine("\\n[1] Scene dependencies inside AREON: " + inside.Count);
            sb.AppendLine("[1] Scene dependencies OUTSIDE Assets/AREON (must be justified in the handoff): " + outsideAssets.Count);
            foreach (string p in outsideAssets) sb.AppendLine("    EXTERNAL  " + p);
            sb.AppendLine("[1] Package-provided dependencies (informational): " + packages.Count);
            foreach (string p in packages.Take(40)) sb.AppendLine("    PACKAGE   " + p);

            // 2. AREON-looking assets outside the AREON folder (isolation check).
            sb.AppendLine("\\n[2] Assets whose NAME contains 'AREON' but live outside Assets/AREON:");
            int stray = 0;
            foreach (string guid in AssetDatabase.FindAssets("AREON"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Assets/") && !path.StartsWith(Root + "/")) { sb.AppendLine("    STRAY     " + path); stray++; }
            }
            if (stray == 0) sb.AppendLine("    none");

            // 3. Scripts in the project that define AREON types but live outside AREON.
            sb.AppendLine("\\n[3] MonoScripts in namespace AREON outside Assets/AREON:");
            int strayScripts = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/") || path.StartsWith(Root + "/")) continue;
                MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                System.Type t = ms != null ? ms.GetClass() : null;
                if (t != null && t.Namespace != null && t.Namespace.StartsWith("AREON")) { sb.AppendLine("    STRAY     " + path); strayScripts++; }
            }
            if (strayScripts == 0) sb.AppendLine("    none");

            // 4. Unused-candidate report: AREON assets that nothing in the scene depends on.
            HashSet<string> used = new HashSet<string>(deps);
            sb.AppendLine("\\n[4] AREON assets NOT reachable from the scene (CANDIDATES ONLY - verify manually before any removal):");
            int candidates = 0;
            foreach (string guid in AssetDatabase.FindAssets("", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path) || used.Contains(path)) continue;
                string kind = path.Contains("/Editor/") ? "editor-tool" : path.EndsWith(".cs") ? "script" : "asset";
                sb.AppendLine("    " + kind.PadRight(11) + path);
                candidates++;
            }
            if (candidates == 0) sb.AppendLine("    none");
            sb.AppendLine("    Note: editor tools, interfaces and test harness scripts normally appear here. That is expected, not proof they are unused.");

            // 5. Reverse references: which scene objects use harness components.
            sb.AppendLine("\\n[5] Test-harness components currently in the open scene:");
            foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb != null && mb.GetType().Name.EndsWith("TestHarness"))
                    sb.AppendLine("    " + mb.GetType().Name + "  on  " + HierarchyPath(mb.transform) + "  (enabled=" + mb.enabled + ", tag=" + mb.gameObject.tag + ")");
            }

            Debug.Log(sb.ToString());
        }

        [MenuItem("AREON/Stage 10/Export AREON Package (explicit list)")]
        public static void ExportPackage()
        {
            List<string> toExport = new List<string> { Root };
            toExport.AddRange(ApprovedExternalAssets);

            // Do NOT use ExportPackageOptions.IncludeDependencies: it can silently pull unrelated project content.
            string target = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "..", "AREON_MarsSTEM_v1.0.unitypackage"));
            AssetDatabase.ExportPackage(toExport.ToArray(), target, ExportPackageOptions.Recurse);
            Debug.Log("[Stage10Audit] Exported: " + target + "\\nIncluded roots: " + string.Join(", ", toExport));
        }

        private static string HierarchyPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }
    }
}
#endif
