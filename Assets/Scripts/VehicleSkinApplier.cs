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

            // ���� ���� ����
            ApplyPaint(skin.paintId, database);

            // ��Į ����
            ApplyDecal(skin.decalId, database);

            // �� ����
            ApplyWheels(skin.wheelId, database);

            // �ν��� ����Ʈ ����
            ApplyBoosterEffect(skin.boosterEffectId, database);

            // ���̵� Ʈ���� ����
            ApplyBladeTrail(skin.bladeTrailId, database);

            // ���� ����
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
                    // ���� ����
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
                // ��Į �ؽ�ó ���� ����
                // ���� ���������� ��Į �ý��� ���
            }
        }

        private void ApplyWheels(int wheelId, CustomizationDatabase database)
        {
            var wheelItem = database.GetItem(wheelId);
            if (wheelItem != null && wheelItem.prefab != null && wheelRenderers != null)
            {
                // �� ��ü ����
                // ���� ���������� �� �޽� ��ü
            }
        }

        private void ApplyBoosterEffect(int effectId, CustomizationDatabase database)
        {
            // ���� ����Ʈ ����
            if (currentBoosterEffect != null)
            {
                Destroy(currentBoosterEffect);
            }

            var effectItem = database.GetItem(effectId);
            if (effectItem != null && effectItem.prefab != null && boosterEffectPoint != null)
            {
                currentBoosterEffect = Instantiate(effectItem.prefab, boosterEffectPoint);
                currentBoosterEffect.transform.localPosition = Vector3.zero;
                currentBoosterEffect.SetActive(false); // �ν��� ��� �� Ȱ��ȭ
            }
        }

        private void ApplyBladeTrail(int trailId, CustomizationDatabase database)
        {
            // ���� Ʈ���� ����
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
                // AudioManager�� ���� �Ҹ� ����
                // ���� ���������� Audio �ý��۰� ����
            }
        }

        // �ν��� ����Ʈ Ȱ��ȭ/��Ȱ��ȭ
        public void ToggleBoosterEffect(bool active)
        {
            if (currentBoosterEffect != null)
            {
                currentBoosterEffect.SetActive(active);
            }
        }
    }
}