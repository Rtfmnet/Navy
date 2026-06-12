// Navy.Data.Firebase
// Firebase SDK initialization bootstrap

using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase;
using UnityEngine;

namespace Navy.Data.Firebase
{
    /// <summary>
    /// Ensures Firebase dependencies are resolved before any Firebase API is used.
    /// Call FirebaseBootstrap.InitializeAsync() from AppBootstrap.
    /// </summary>
    public static class FirebaseBootstrap
    {
        public static bool IsInitialized { get; private set; }

        public static async UniTask InitializeAsync(CancellationToken ct = default)
        {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask().AttachExternalCancellation(ct);

            if (status == DependencyStatus.Available)
            {
                IsInitialized = true;
#if UNITY_EDITOR || DEBUG_BUILD
                Debug.Log("[FirebaseBootstrap] Firebase initialized successfully.");
#endif
            }
            else
            {
                Debug.LogError($"[FirebaseBootstrap] Firebase dependency check failed: {status}");
            }
        }
    }
}
