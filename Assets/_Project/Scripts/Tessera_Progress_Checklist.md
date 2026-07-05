# Tessera 진행 체크리스트

> 7번 작업까지 완료할 때까지 이 파일을 기준으로 진행 상황을 공유한다. 7번까지 완료되면 이 파일은 제거하고 다음 범위용 새 MD 파일을 만든다.

## 진행 규칙

- [ ] 작업 시작 전/후로 이 체크리스트를 갱신한다.
- [ ] 코드 변경이 있으면 자동 검증 가능한 Editor 검증 코드 또는 기존 검증 클래스를 함께 갱신한다.
- [ ] Unity Editor에서 직접 확인해야 하는 Inspector/Prefab/Scene 항목은 별도 체크리스트로 남긴다.
- [ ] `main`에 직접 병합하거나 push하지 않고 클라우드 작업 브랜치에서만 작업한다.

## 1. SingleDice / DiceTypeUpgrade 개별 적용 런타임 로직 + 검증

- [x] 현재 막혀 있는 `SingleDice`, `DiceTypeUpgrade` 구매 차단 경로 확인.
- [x] 대상 선택 UI가 없어도 검증 가능한 런타임 기본 적용 정책 정의.
  - 정책: 구매한 DiceType과 다른 첫 번째 플레이어 Dice 슬롯에 적용하고, 모두 동일하면 1번 슬롯에 재적용한다.
- [x] `TesseraRunSession`에 개별 DiceType 구매 적용 API 추가.
- [x] Shop 구매 가능 여부에서 `SingleDice`, `DiceTypeUpgrade` 차단 제거.
- [x] Shop 구매 적용 경로에서 `SingleDice`, `DiceTypeUpgrade` 실제 적용.
- [x] Shop 카드 버튼의 구매 가능 여부에서도 동일 차단 제거.
- [x] 기존 DiceType intrinsic 검증 클래스에 SingleDice/DiceTypeUpgrade 분리 검증 시나리오 추가.
- [x] Unity Editor에서 Shop 카드 클릭 및 DiceType 표시 갱신 확인.
  - 확인: DiceType visual과 표시 UI 갱신 로그 PASS 확인.

## 2. DiceFaceUpgrade 구매 placeholder 제거 + 실제 보유/적용 상태 모델 추가

- [x] 현재 `DiceFaceUpgrade` 구매가 placeholder로만 처리되는 경로 확인.
- [x] FaceUpgrade 보유/장착 상태 모델 확정.
  - 정책: Upgrade의 대상 숫자 또는 교체 숫자에 해당하는 FaceIndex를 고르고, 비어 있거나 다른 Upgrade가 장착된 첫 번째 Dice 슬롯에 적용한다.
- [x] DiceFaceUpgrade 구매 시 RunSession 장착 상태에 실제 적용하도록 연결.
- [x] FaceUpgrade 구매/적용 검증 클래스 추가.
- [x] Unity Editor에서 FaceUpgrade 상품 카드, 구매 메시지, 적용 UI 확인.
  - 확인: DiceFaceUpgrade 구매/적용 검증 PASS 및 실제 구매 로그 확인.

## 3. Mirror / Blank / Wild Face의 PatternEvaluator 전처리 설계 및 구현

- [x] Pattern 평가용 effective dice face/value 모델 설계.
  - 정책: Number는 1~6, Blank는 0으로 기여 제외, Mirror는 왼쪽 평가값 복제, Wild는 1~6 후보를 전부 평가해 Cast별 최선 결과를 선택한다.
- [x] Mirror Face 동작 규칙 구현.
- [x] Blank Face 동작 규칙 구현.
- [x] Wild Face 최적값 선택 규칙 구현.
- [x] 특수 Face PatternEvaluator 검증 시나리오 추가.
- [ ] Unity Editor에서 특수 Face 검증 메뉴 실행 및 결과 확인.

## 4. CastPower 조건 Device 후처리 연결

- [ ] CastPower 확정 이후 Device 후처리 단계 설계.
- [ ] `AddTrueImpactDamageIfCastPowerAtLeast` 활성화.
- [ ] Preview/Submit parity 검증 추가.
- [ ] Unity Editor에서 Floating Text/전투 로그 표시 확인.

## 5. BrokenCast / Clash 후처리 Device 효과 구현

- [ ] BrokenCast 판정 후 Overcharge 가산 Hook 구현.
- [ ] Clash 패배/피해 적용 직전 IncomingDamage 감소 Hook 구현.
- [ ] 중복 적용/다중 장착 정책 검증.
- [ ] Unity Editor에서 BrokenCast/Clash 후처리 피드백 확인.

## 6. DiceSynergy 효과 enum 및 런타임 계산 연결

- [ ] 보류된 DiceSynergy 목록 산출.
- [ ] 필요한 `DiceSynergyEffectType` enum 확장.
- [ ] DiceSynergy Evaluator 또는 기존 계산기 통합 경로 구현.
- [ ] DiceType intrinsic과 중첩 순서 검증.
- [ ] Unity Editor에서 Synergy 표시/효과 적용 확인.

## 7. 상점 시각/상품군 확장

- [ ] `ShopProductDefinitionSO` 카드 배경 Sprite 필드 추가.
- [ ] `ShopProductCardView` 배경 Sprite 할당 구현.
- [ ] Consumable/PermanentUpgrade/HPRepair 정식 SO 설계.
- [ ] 각 상품 타입별 구매 적용 로직 구현.
- [ ] Unity Editor에서 카드 배경, Tooltip, 상품 타입별 UI 확인.
