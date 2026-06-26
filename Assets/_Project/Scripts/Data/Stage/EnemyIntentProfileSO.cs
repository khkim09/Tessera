using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tessera.Data
{
    /// <summary>Bounty가 사용할 상대 Intent 후보 풀을 묶는 프로필이다.</summary>
    [CreateAssetMenu(
        fileName = "EnemyIntentProfile",
        menuName = "Tessera/Stage/Enemy Intent Profile")]
    public class EnemyIntentProfileSO : ScriptableObject
    {
        [Header("Intent Pool")]
        [SerializeField] private EnemyIntentDefinitionSO openingIntent;
        [SerializeField] private EnemyIntentDefinitionSO[] intentPool;

        /// <summary>첫 Attempt에서 우선 사용할 Intent 정의다.</summary>
        public EnemyIntentDefinitionSO OpeningIntent => openingIntent;

        /// <summary>Attempt 전환 시 선택할 Intent 후보 풀이다.</summary>
        public IReadOnlyList<EnemyIntentDefinitionSO> IntentPool => intentPool;
    }
}
