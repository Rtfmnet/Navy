// Navy.Data.Firebase
// Anonymous authentication service

using System.Threading;
using Cysharp.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;

namespace Navy.Data.Firebase
{
    /// <summary>
    /// Handles Firebase Anonymous Authentication.
    /// After sign-in, UID is stable for the device until app data is cleared.
    /// </summary>
    public sealed class FirebaseAuthService
    {
        private FirebaseAuth _auth;
        private FirebaseUser _currentUser;

        public string Uid => _currentUser?.UserId;

        public async UniTask<string> SignInAnonymouslyAsync(CancellationToken ct = default)
        {
            _auth = FirebaseAuth.DefaultInstance;

            if (_auth.CurrentUser != null)
            {
                _currentUser = _auth.CurrentUser;
                return _currentUser.UserId;
            }

            // Modern Firebase Unity SDK (>= 11.x) returns AuthResult.
            var authResult = await _auth.SignInAnonymouslyAsync()
                .AsUniTask().AttachExternalCancellation(ct);
            _currentUser = authResult.User;

#if UNITY_EDITOR || DEBUG_BUILD
            Debug.Log($"[FirebaseAuthService] Signed in anonymously. UID: {_currentUser.UserId}");
#endif
            return _currentUser.UserId;
        }
    }
}
