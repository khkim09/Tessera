using System;
using System.Collections.Generic;
using Tessera.Core;
using UnityEngine;

namespace Tessera.Data
{
    /// <summary>상대가 Round에서 사용할 Dice 세트를 정의하는 ScriptableObject다.</summary>
    [CreateAssetMenu(
        fileName = "EnemyDiceLoadoutDefinition",
        menuName = "Tessera/Stage/Enemy Dice Loadout Definition")]
    public class EnemyDiceLoadoutDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string loadoutId;
        [SerializeField] private string displayName = "Enemy Dice Loadout";

        [Header("Dice")]
        [SerializeField] private EnemyDiceFaceSetDefinition[] diceDefinitions;

        /// <summary>로드아웃 고유 ID.</summary>
        public string LoadoutId => string.IsNullOrWhiteSpace(loadoutId) ? name : loadoutId;

        /// <summary>표시 이름.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        /// <summary>주사위 정의 목록.</summary>
        public IReadOnlyList<EnemyDiceFaceSetDefinition> DiceDefinitions =>
            diceDefinitions ?? Array.Empty<EnemyDiceFaceSetDefinition>();

        /// <summary>로드아웃 정의와 fallback 개수를 기준으로 굴려진 DiceInstance 세트를 생성한다.</summary>
        public List<DiceInstance> CreateRolledDiceSet(int fallbackDiceCount, DiceRoller diceRoller)
        {
            if (diceRoller == null)
                throw new ArgumentNullException(nameof(diceRoller));

            int resolvedDiceCount = Mathf.Max(1, fallbackDiceCount);
            List<DiceInstance> result = new List<DiceInstance>(resolvedDiceCount);

            for (int i = 0; i < resolvedDiceCount; i++)
            {
                DiceInstance diceInstance = CreateDiceInstanceAtIndex(i);
                diceRoller.RollSingle(diceInstance);
                result.Add(diceInstance);
            }

            return result;
        }

        /// <summary>지정 인덱스의 주사위 정의를 생성한다. 없으면 기본 D6을 생성한다.</summary>
        private DiceInstance CreateDiceInstanceAtIndex(int index)
        {
            if (diceDefinitions == null ||
                index < 0 ||
                index >= diceDefinitions.Length ||
                diceDefinitions[index] == null)
            {
                return DiceInstance.CreateStandardD6();
            }

            return diceDefinitions[index].CreateDiceInstance();
        }
    }

    /// <summary>상대 Dice 하나의 숫자 면 구성을 정의한다.</summary>
    [Serializable]
    public class EnemyDiceFaceSetDefinition
    {
        [SerializeField] private int[] numberFaces = { 1, 2, 3, 4, 5, 6 };

        /// <summary>이 정의를 기반으로 DiceInstance를 생성한다.</summary>
        public DiceInstance CreateDiceInstance()
        {
            List<DiceFace> faces = new List<DiceFace>();

            if (numberFaces != null)
            {
                for (int i = 0; i < numberFaces.Length; i++)
                    faces.Add(DiceFace.Number(Mathf.Clamp(numberFaces[i], 1, 6)));
            }

            if (faces.Count <= 0)
            {
                faces.Add(DiceFace.Number(1));
                faces.Add(DiceFace.Number(2));
                faces.Add(DiceFace.Number(3));
                faces.Add(DiceFace.Number(4));
                faces.Add(DiceFace.Number(5));
                faces.Add(DiceFace.Number(6));
            }

            return new DiceInstance(faces, faces[0]);
        }
    }
}
