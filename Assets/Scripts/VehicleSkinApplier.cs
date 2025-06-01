using UnityEngine;

namespace CarBlade.Customization
{
    public class VehicleSkinApplier : MonoBehaviour
    {
        [Header("Vehicle Parts")]
        [SerializeField] private MeshRenderer bodyRenderer;
        [SerializeField] private MeshRenderer[] wheelRenderers;
        [SerializeField] private Transform boosterEffectPoint;
        [SerializeField] private Transform bladeTrailPoint;

        private SkinData currentSkin;
        private GameObject currentBoosterEffect;
        private GameObject currentBladeTrail;

        public void ApplySkin(SkinData skin, CustomizationDatabase database)
        {
            currentSkin = skin;

            // 차량 도색 적용
            ApplyPaint(skin.paintId, database);

            // 데칼 적용
            ApplyDecal(skin.decalId, database);

            // 휠 적용
            ApplyWheels(skin.wheelId, database);

            // 부스터 이펙트 적용
            ApplyBoosterEffect(skin.boosterEffectId, database);

            // 블레이드 트레일 적용
            ApplyBladeTrail(skin.bladeTrailId, database);

            // 경적 설정
            SetHorn(skin.hornId, database);
        }

        private void ApplyPaint(int paintId, CustomizationDatabase database)
        {
            var paintItem = database.GetItem(paintId);
            if (paintItem != null && bodyRenderer != null)
            {
                if (paintItem.material != null)
                {
                    bodyRenderer.material = paintItem.material;
                }
                else
                {
                    // 색상만 변경
                    MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                    bodyRenderer.GetPropertyBlock(mpb);
                    mpb.SetColor("_Color", paintItem.primaryColor);
                    mpb.SetColor("_EmissionColor", paintItem.secondaryColor);
                    bodyRenderer.SetPropertyBlock(mpb);
                }
            }
        }

        private void ApplyDecal(int decalId, CustomizationDatabase database)
        {
            var decalItem = database.GetItem(decalId);
            if (decalItem != null && bodyRenderer != null)
            {
                // 데칼 텍스처 적용 로직
                // 실제 구현에서는 데칼 시스템 사용
            }
        }

        private void ApplyWheels(int wheelId, CustomizationDatabase database)
        {
            var wheelItem = database.GetItem(wheelId);
            if (wheelItem != null && wheelItem.prefab != null && wheelRenderers != null)
            {
                // 휠 교체 로직
                // 실제 구현에서는 휠 메시 교체
            }
        }

        private void ApplyBoosterEffect(int effectId, CustomizationDatabase database)
        {
            // 기존 이펙트 제거
            if (currentBoosterEffect != null)
            {
                Destroy(currentBoosterEffect);
            }

            var effectItem = database.GetItem(effectId);
            if (effectItem != null && effectItem.prefab != null && boosterEffectPoint != null)
            {
                currentBoosterEffect = Instantiate(effectItem.prefab, boosterEffectPoint);
                currentBoosterEffect.transform.localPosition = Vector3.zero;
                currentBoosterEffect.SetActive(false); // 부스터 사용 시 활성화
            }
        }

        private void ApplyBladeTrail(int trailId, CustomizationDatabase database)
        {
            // 기존 트레일 제거
            if (currentBladeTrail != null)
            {
                Destroy(currentBladeTrail);
            }

            var trailItem = database.GetItem(trailId);
            if (trailItem != null && trailItem.prefab != null && bladeTrailPoint != null)
            {
                currentBladeTrail = Instantiate(trailItem.prefab, bladeTrailPoint);
                currentBladeTrail.transform.localPosition = Vector3.zero;
            }
        }

        private void SetHorn(int hornId, CustomizationDatabase database)
        {
            var hornItem = database.GetItem(hornId);
            if (hornItem != null && hornItem.audioClip != null)
            {
                // AudioManager에 경적 소리 설정
                // 실제 구현에서는 Audio 시스템과 연동
            }
        }

        // 부스터 이펙트 활성화/비활성화
        public void ToggleBoosterEffect(bool active)
        {
            if (currentBoosterEffect != null)
            {
                currentBoosterEffect.SetActive(active);
            }
        }
    }
}