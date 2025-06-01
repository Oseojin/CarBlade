using System.Collections.Generic;

namespace CarBlade.Customization
{
    // 커스터마이징 시스템 인터페이스
    public interface ICustomizationSystem
    {
        void ApplySkin(int vehicleId, SkinData skin);
        void PurchaseItem(int itemId);
        List<CustomizationItem> GetOwnedItems();
    }
}