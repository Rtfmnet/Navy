// Navy.Presentation.Game

using Cysharp.Threading.Tasks;
using Navy.Core.Models;
using UnityEngine;

namespace Navy.Presentation.Game
{
    /// <summary>
    /// Plays shot/hit/sunk animations using Unity Animator and UniTask delays.
    /// FR-UI-05: shot animation, hit flash, explosion on sunk.
    /// </summary>
    public sealed class ShotAnimator : MonoBehaviour
    {
        [SerializeField] private Animator _hitAnimator;
        [SerializeField] private Animator _sunkAnimator;
        [SerializeField] private float    _missDelay = 0.3f;
        [SerializeField] private float    _hitDelay  = 0.5f;
        [SerializeField] private float    _sunkDelay = 1.2f;

        private static readonly int TriggerHit  = Animator.StringToHash("Hit");
        private static readonly int TriggerSunk = Animator.StringToHash("Sunk");

        public async UniTask PlayAsync(ShotResult result, Vector3 worldPos)
        {
            switch (result)
            {
                case ShotResult.Miss:
                    await UniTask.Delay((int)(_missDelay * 1000f));
                    break;

                case ShotResult.Hit:
                    if (_hitAnimator != null)
                    {
                        _hitAnimator.transform.position = worldPos;
                        _hitAnimator.SetTrigger(TriggerHit);
                    }
                    await UniTask.Delay((int)(_hitDelay * 1000f));
                    break;

                case ShotResult.Sunk:
                    if (_sunkAnimator != null)
                    {
                        _sunkAnimator.transform.position = worldPos;
                        _sunkAnimator.SetTrigger(TriggerSunk);
                    }
                    await UniTask.Delay((int)(_sunkDelay * 1000f));
                    break;
            }
        }
    }
}
