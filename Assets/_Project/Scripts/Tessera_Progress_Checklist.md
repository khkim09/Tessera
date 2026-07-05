# Tessera 진행 체크리스트

> 1~9번 작업까지 이 파일을 기준으로 진행 상황을 공유한다. 범위가 완료되면 이 파일은 제거하고 다음 범위용 새 MD 파일을 만든다.

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
- [x] Unity Editor에서 특수 Face 검증 메뉴 실행 및 결과 확인.
  - 확인: Mirror/Blank/Wild 특수 Face 검증 PASS 로그 첨부 확인.

## 4. CastPower 조건 Device 후처리 연결

- [x] CastPower 확정 이후 Device 후처리 단계 설계.
  - 정책: SlotPair Score/Force/TruePower 계산으로 확정된 TableRule 적용 전 CastPower를 기준으로 `AddTrueImpactDamageIfCastPowerAtLeast`를 후처리한다.
- [x] `AddTrueImpactDamageIfCastPowerAtLeast` 활성화.
- [x] Preview/Submit parity 검증 추가.
- [ ] Unity Editor에서 Floating Text/전투 로그 표시 확인.
  - 보류: CastPower 후처리 Device SO를 실제 Shop/전투에 노출한 뒤 시각 표시를 확인한다. 현재는 자동 검증 PASS까지만 확인.

## 5. BrokenCast / Clash 후처리 Device 효과 구현

- [x] BrokenCast 판정 후 Overcharge 가산 Hook 구현.
- [x] Clash 패배/피해 적용 직전 IncomingDamage 감소 Hook 구현.
- [x] 중복 적용/다중 장착 정책 검증.
  - 정책: BrokenCast 후처리 Device는 해당 Device 슬롯에 Dice가 배치된 경우 `IntValue`를 슬롯별 합산한다.
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

## 공통. 디버그/검증 편의 기능

- [x] `1` 키 입력 시 Money/Overcharge를 지급하고 Workshop으로 즉시 진입하는 디버그 치트 추가.
- [x] 레거시 `Input.GetKeyDown`/`Update` 폴링 제거.
- [x] Unity New Input System `InputAction.performed`에서 Runtime 이벤트를 발행하고, Stage Flow는 해당 이벤트를 구독해 처리하도록 분리.
- [x] 디버그 치트 입력 경로 검증 메뉴 추가.
- [x] Unity Editor에서 `Tools/Tessera/Validation/Run Debug Shop Cheat Input Scenario Test v1` 실행 후 PASS 로그 확인.
  - 확인: 사용자 첨부 로그 기준 PASS.
- [x] Unity Editor Play Mode에서 `1` 키 입력 후 Workshop 즉시 진입, Console 로그, Money/Overcharge 갱신 확인.
  - 확인: 사용자 첨부 로그 기준 정상 동작.

## 8. DiceFaceUpgrade 적용 범위 기획 및 구현

- [ ] 단일 주사위 단면 개조 vs 5개 주사위 동일 단면 일괄 개조 중 최종 방향 결정.
- [ ] 결정된 정책에 맞춰 구매 UI/대상 선택/자동 적용 정책 재정의.
  - DiceFaceUpgrade 카드 구매 후 1개 Dice를 선택하는 단계 추가.
  - 선택한 Dice에서 1개 Face를 선택하는 단계 추가.
- [ ] 기존 RunSession FaceUpgrade 장착 모델을 최종 정책에 맞게 보정.
- [ ] Preview/Submit/PatternEvaluator 경로에 실제 장착 FaceUpgrade를 연결.
- [ ] Unity Editor에서 구매 후 실제 전투 Pattern 결과가 변경되는지 확인.

## 9. 라운드 승리 연출/카메라 연출 체크리스트

- [ ] 라운드 승리 시 해머 스윙 연출 트리거 추가.
- [ ] 해머 스윙 타이밍과 ImpactDamage 적용/표시 타이밍 동기화.
- [ ] 승리 시 카메라 무브먼트/줌/쉐이크 연출 설계.
- [ ] 연출 중 입력 잠금 및 UI 상태 전환 규칙 정리.
- [ ] Unity Editor에서 승리/패배/무승부 흐름별 연출 확인.

## 사용자 확인 요청 템플릿

- [ ] 새 Validation 메뉴가 추가되면 메뉴 경로와 기대 PASS 로그를 함께 공유한다.
- [ ] 직접 Play 확인이 필요하면 확인할 GameObject/UI/Console 로그와 첨부해야 할 스크린샷을 함께 공유한다.
- [ ] 확인 결과를 받으면 이 체크리스트에 PASS/보류/재작업 여부를 반영한다.
