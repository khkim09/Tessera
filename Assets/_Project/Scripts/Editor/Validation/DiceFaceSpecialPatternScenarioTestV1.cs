#if UNITY_EDITOR
using System.Collections.Generic;
using Tessera.Core;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Validation
{
    /// <summary>Mirror/Blank/Wild Face의 PatternEvaluator 전처리 결과를 결정적 시나리오로 검증한다.</summary>
    public static class DiceFaceSpecialPatternScenarioTestV1
    {
        /// <summary>Editor 메뉴에서 특수 Face Pattern 평가 검증을 실행한다.</summary>
        [MenuItem("Tools/Tessera/Validation/Run DiceFace Special Pattern Scenario Test v1")]
        public static void Run()
        {
            int failures = 0;
            PatternEvaluator evaluator = PatternEvaluator.CreateDefault();

            ValidateMirrorCopiesLeftValue(evaluator, ref failures);
            ValidateBlankDoesNotContribute(evaluator, ref failures);
            ValidateWildChoosesLargeStraightValue(evaluator, ref failures);
            ValidateWildChoosesTesseraValue(evaluator, ref failures);

            if (failures > 0)
            {
                Debug.LogError($"[DiceFaceSpecialPatternScenarioTest] FAIL Count={failures}");
                return;
            }

            Debug.Log("[DiceFaceSpecialPatternScenarioTest] PASS All DiceFace special pattern scenario checks v1");
        }

        /// <summary>Mirror Face가 왼쪽 평가값을 복제해 ThreeOfAKind를 만드는지 검증한다.</summary>
        private static void ValidateMirrorCopiesLeftValue(PatternEvaluator evaluator, ref int failures)
        {
            List<DiceFace> faces = new List<DiceFace>
            {
                DiceFace.Number(2),
                new DiceFace(DiceFaceType.Mirror, 1),
                DiceFace.Number(2),
                DiceFace.Number(3),
                DiceFace.Number(4)
            };

            if (!evaluator.TryEvaluateSpecificPatternFaces(faces, RollPatternType.ThreeOfAKind, out PatternResult result) || result.RawCastScore != 13)
                Fail(ref failures, "Mirror", "ThreeOfAKind", "RawCastScore 13", result != null ? result.ToString() : "null");
            else
                Debug.Log("[DiceFaceSpecialPatternScenarioTest] PASS Mirror ThreeOfAKind");
        }

        /// <summary>Blank Face가 Chance 및 같은 눈금 Cast 점수에서 제외되는지 검증한다.</summary>
        private static void ValidateBlankDoesNotContribute(PatternEvaluator evaluator, ref int failures)
        {
            List<DiceFace> faces = new List<DiceFace>
            {
                DiceFace.Number(6),
                DiceFace.Number(6),
                new DiceFace(DiceFaceType.Blank, 0),
                DiceFace.Number(1),
                DiceFace.Number(2)
            };

            bool chanceOk = evaluator.TryEvaluateSpecificPatternFaces(faces, RollPatternType.Chance, out PatternResult chanceResult) && chanceResult.RawCastScore == 15;
            bool sixesOk = evaluator.TryEvaluateSpecificPatternFaces(faces, RollPatternType.Sixes, out PatternResult sixesResult) && sixesResult.RawCastScore == 12;

            if (!chanceOk || !sixesOk)
                Fail(ref failures, "Blank", "ExcludedScore", "Chance 15 / Sixes 12", $"Chance={FormatResult(chanceResult)}, Sixes={FormatResult(sixesResult)}");
            else
                Debug.Log("[DiceFaceSpecialPatternScenarioTest] PASS Blank ExcludedScore");
        }

        /// <summary>Wild Face가 LargeStraight에 유리한 값으로 평가되는지 검증한다.</summary>
        private static void ValidateWildChoosesLargeStraightValue(PatternEvaluator evaluator, ref int failures)
        {
            List<DiceFace> faces = new List<DiceFace>
            {
                DiceFace.Number(1),
                DiceFace.Number(2),
                DiceFace.Number(3),
                DiceFace.Number(4),
                new DiceFace(DiceFaceType.Wild, 0)
            };

            if (!evaluator.TryEvaluateSpecificPatternFaces(faces, RollPatternType.LargeStraight, out PatternResult result) || result.RawCastScore != 40)
                Fail(ref failures, "Wild", "LargeStraight", "RawCastScore 40", result != null ? result.ToString() : "null");
            else
                Debug.Log("[DiceFaceSpecialPatternScenarioTest] PASS Wild LargeStraight");
        }

        /// <summary>Wild Face가 Tessera에 유리한 값으로 평가되는지 검증한다.</summary>
        private static void ValidateWildChoosesTesseraValue(PatternEvaluator evaluator, ref int failures)
        {
            List<DiceFace> faces = new List<DiceFace>
            {
                DiceFace.Number(6),
                DiceFace.Number(6),
                DiceFace.Number(6),
                DiceFace.Number(6),
                new DiceFace(DiceFaceType.Wild, 0)
            };

            if (!evaluator.TryEvaluateSpecificPatternFaces(faces, RollPatternType.Tessera, out PatternResult result) || result.RawCastScore != 50)
                Fail(ref failures, "Wild", "Tessera", "RawCastScore 50", result != null ? result.ToString() : "null");
            else
                Debug.Log("[DiceFaceSpecialPatternScenarioTest] PASS Wild Tessera");
        }

        /// <summary>검증 실패를 누적하고 명확한 오류 로그를 출력한다.</summary>
        private static void Fail(ref int failures, string faceType, string location, string expected, string actual)
        {
            failures++;
            Debug.LogError($"[DiceFaceSpecialPatternScenarioTest] FAIL FaceType={faceType} Location={location} Expected={expected} Actual={actual}");
        }

        /// <summary>PatternResult를 로그용 문자열로 변환한다.</summary>
        private static string FormatResult(PatternResult result)
        {
            return result != null ? result.ToString() : "null";
        }
    }
}
#endif
