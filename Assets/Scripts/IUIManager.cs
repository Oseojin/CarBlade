using CarBlade.Core;
using System.Collections.Generic;

namespace CarBlade.UI
{
    // UI �Ŵ��� �������̽�
    public interface IUIManager
    {
        void UpdateSpeedometer(float speed);
        void UpdateBoosterGauge(float boost);
        void ShowKillFeed(string killer, string victim);
        void UpdateScoreboard(List<PlayerScore> scores);
    }
}