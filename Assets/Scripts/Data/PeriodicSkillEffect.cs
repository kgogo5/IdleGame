using System.Collections;
using UnityEngine;
using IdleGame.Managers;

namespace IdleGame.Data
{
    public enum SkillDamageSource { AutoDamage, ClickDamage, Flat }

    public class PeriodicSkillEffect : SetEffect
    {
        public string           skillName;
        public string           particleEffectId;
        public float            intervalSeconds  = 4f;
        public SkillDamageSource damageSource    = SkillDamageSource.AutoDamage;
        public float            multiplier       = 2f;

        public override IEnumerator Run()
        {
            while (true)
            {
                yield return new WaitForSeconds(intervalSeconds);

                var monster = Core.MonsterManager.Instance?.CurrentMonster;
                if (monster == null) continue;

                var ps = PlayerStats.Instance;
                if (ps == null) continue;

                double damage = damageSource switch
                {
                    SkillDamageSource.AutoDamage  => ps.AutoDamage  * multiplier,
                    SkillDamageSource.ClickDamage => ps.ClickDamage * multiplier,
                    SkillDamageSource.Flat        => multiplier,
                    _                             => 0,
                };
                if (damage <= 0) continue;

                monster.TakeDamage(damage);

                if (!string.IsNullOrEmpty(particleEffectId))
                    ParticleManager.Instance?.Spawn(particleEffectId, monster.transform.position);
            }
        }
    }
}
