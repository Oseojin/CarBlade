using System.Collections.Generic;

namespace CarBlade.Customization
{
    // Ŀ���͸���¡ �ý��� �������̽�
    public interface ICustomizationSystem
    {
        void ApplySkin(int vehicleId, SkinData skin);
        void PurchaseItem(int itemId);
        List<CustomizationItem> GetOwnedItems();
    }
}