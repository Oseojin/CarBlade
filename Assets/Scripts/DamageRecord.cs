using UnityEngine;

namespace CarBlade.Core
{
    public class DamageRecord
    {
        public int attackerId;
        public int targetId;
        public float damage;
        public float timestamp;

        public DamageRecord(int attacker, int target, float dmg)
        {
            attackerId = attacker;
            targetId = target;
            damage = dmg;
            timestamp = Time.time;
        }
    }
}