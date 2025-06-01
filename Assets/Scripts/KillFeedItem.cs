using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using CarBlade.Core;
using CarBlade.Physics;
using CarBlade.Combat;

namespace CarBlade.UI
{
    public class KillFeedItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI killerText;
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private TextMeshProUGUI victimText;
        [SerializeField] private Image weaponIcon;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        private void Start()
        {
            // 페이드 인 애니메이션
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                StartCoroutine(FadeIn());
            }
        }

        public void SetKillInfo(string killer, string victim)
        {
            if (killerText != null) killerText.text = killer;
            if (actionText != null) actionText.text = "destroyed";
            if (victimText != null) victimText.text = victim;

            // 무기 아이콘 (블레이드)
            if (weaponIcon != null)
            {
                weaponIcon.sprite = GetBladeIcon();
            }
        }

        private Sprite GetBladeIcon()
        {
            // 실제 구현에서는 Resources나 AddressableAssets에서 로드
            return null;
        }

        private System.Collections.IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private void OnDestroy()
        {
            // 페이드 아웃은 별도 컴포넌트나 애니메이션으로 처리
        }
    }
}