using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GaussianSplatting.Editor
{
    [InitializeOnLoad]
    internal static class ProjectAutoConfigurator
    {
        static ProjectAutoConfigurator()
        {
            EnsureProjectSettings();
        }

        private static void EnsureProjectSettings()
        {
            bool changed = false;

            // Ensure 'unsafe' code is allowed at project level (in addition to asmdef flags)
            try
            {
                if (!PlayerSettings.allowUnsafeCode)
                {
                    PlayerSettings.allowUnsafeCode = true;
                    changed = true;
                }
            }
            catch
            {
                // Some Unity versions may not expose allowUnsafeCode; ignore safely
            }

            // Allow downloads over HTTP (non-HTTPS) to match gateway requirements when needed
            try
            {
                if (PlayerSettings.insecureHttpOption != InsecureHttpOption.AlwaysAllowed)
                {
                    PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
                    changed = true;
                }
            }
            catch
            {
                // API not available on some editor versions
            }

            // Prefer Vulkan on Linux, Direct3D12 on Windows
            try
            {
                // Linux
                var linuxTarget = BuildTarget.StandaloneLinux64;
                var linuxDesired = new[] { GraphicsDeviceType.Vulkan };
                if (PlayerSettings.GetUseDefaultGraphicsAPIs(linuxTarget) ||
                    !GraphicsApisEqual(PlayerSettings.GetGraphicsAPIs(linuxTarget), linuxDesired))
                {
                    PlayerSettings.SetUseDefaultGraphicsAPIs(linuxTarget, false);
                    PlayerSettings.SetGraphicsAPIs(linuxTarget, linuxDesired);
                    changed = true;
                }
            }
            catch { }
            try
            {
                // Windows
                var winTarget = BuildTarget.StandaloneWindows64;
                var winDesired = new[] { GraphicsDeviceType.Direct3D12 };
                if (PlayerSettings.GetUseDefaultGraphicsAPIs(winTarget) ||
                    !GraphicsApisEqual(PlayerSettings.GetGraphicsAPIs(winTarget), winDesired))
                {
                    PlayerSettings.SetUseDefaultGraphicsAPIs(winTarget, false);
                    PlayerSettings.SetGraphicsAPIs(winTarget, winDesired);
                    changed = true;
                }
            }
            catch { }

            if (changed)
            {
                Debug.Log("[404-GEN] Project settings updated: enabled unsafe code and allowed HTTP downloads.");
                AssetDatabase.SaveAssets();
            }
        }

        private static bool GraphicsApisEqual(GraphicsDeviceType[] a, GraphicsDeviceType[] b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}


