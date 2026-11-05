# 마법소녀 오브젝트 및 속성 정의

> 사용자 확정 규칙을 반영한 편집용 문서다. 필드명·클래스 구조와 명시적으로 임시 표시한 수치는 제안이며, 실제 코드 구현은 아니다.

관련 문서: [게임 시스템 기획서](Game%20System%20Design.md)

## 확정 규칙

- 같은 마법소녀는 중복 영입할 수 없다. 영입 시 `characterId`로 중복을 검사한다. 계약 해제 후에는 재영입할 수 있으며, 강화 횟수는 0으로, 강화 변수는 원본 초기값으로 돌아간다.
- 마법소녀는 1·2·3학년의 희귀도 등급을 가진다. 영입·보충 학습·판매(계약 해제) 가격은 모두 학년별 공통 가격표에서 조회한다.
- 역할군은 `Buff`, `Debuff`, `Dealer` 중 하나이며 유저 편의를 위한 표시 전용이다. 데미지 계산, 시너지, 트리거 조건에 사용하지 않는다.
- 스킬 강화 방식은 1회 강화 가능과 무한 강화 가능으로 구분한다. 강화 대상 변수는 스킬당 하나이며, 강화값 산출 로직은 마법소녀마다 다르다. 강화 대상 변수를 지정하고, 강화 시 그 변수의 현재 값을 새 값으로 대체한다.
- 체력·방어력·치명타·속도 등의 캐릭터 전투 능력치는 게임에 존재하지 않는다. 보스 체력은 별도의 전투 판정 값이다.
- 큰 데미지의 저장·계산 자료형은 `float`다. 일반 표시는 소수점을 버린 정수 형태, 과학적 표시는 소수점 2자리로 한다.
- 액션 대상 선택은 별도 함수에서 전략 패턴으로 처리한다.
- 복수 키워드 스택 허용 여부는 해당 키워드 정의의 중첩 가능 여부로 결정한다.

## 1. 오브젝트 구성안

| 오브젝트 후보 | 책임 |
| --- | --- |
| `MagicalGirlDefinition` | 종류별 원본: ID, 이름, 학년, 역할군, 시너지, 스킬, 이미지 |
| `EconomySettings` | 모든 경제 관련 값을 모아 관리하는 단일 ScriptableObject. 학년별 가격표 포함 |
| `MagicalGirlInstance` | 영입 개체의 슬롯, 강화 횟수와 현재 변수 값, 부여된 키워드 |
| `MagicalGirlView` | 개체를 표시하고 선택·드래그·연출을 처리하는 화면 오브젝트 |

원본을 `ScriptableObject`로 보관하는 것은 후보안이다. 플레이 중 강화는 개체의 변수 값을 변경하며 공유 원본은 수정하지 않는다.

## 2. 캐릭터 원본 속성

| 속성명 | 자료형 후보 | 예시 | 의미 |
| --- | --- | --- | --- |
| `characterId` | `string` | `mg_starlight_001` | 고유 ID와 중복 영입 검사 기준 |
| `displayName` | `string` | `별빛 소녀 아리` | 표시 이름 |
| `description` | `string` | `별빛을 모아 필살기를 강화한다.` | 소개 |
| `grade` | `SchoolGrade` enum | `Grade1` | 값: `Grade1=1`, `Grade2=2`, `Grade3=3`. 희귀도 등급 |
| `role` | `MagicalGirlRole` enum | `Dealer` | 값: `Buff`, `Debuff`, `Dealer`. 표시 전용 |
| `synergies` | `List<SynergyRef>` | `Region: starlight_city` | 지역·직업·특수 시너지의 카테고리와 ID 목록 |
| `skill` | `SkillDefinition` | `skill_starlight` | 스킬 원본 |
| `portrait` | `Sprite` | `Portrait_Ari` | UI 이미지 참조 후보. 실제 에셋은 아직 없음 |
| `battleSprite` | `Sprite` | `Battle_Ari` | 전투 이미지 참조 후보. 실제 에셋은 아직 없음 |
| `themeColor` | `Color` | `#FFD76AFF` | 표시용 색상 |

### 학년별 공통 가격

경제 관련 값은 하나의 `EconomySettings` ScriptableObject에서 관리한다. 이름은 후보이며 단일 에셋 관리 방식은 확정이다. 그 안의 학년별 가격 행 `GradePriceDefinition`은 `grade: SchoolGrade`, `recruitCost: int`, `upgradeCost: int`, `sellPrice: int`를 가진다. 아래 수치는 수정 가능한 임시 값이며 확정 밸런스가 아니다.

| 학년 | 영입 가격 | 보충 학습 가격 | 판매(계약 해제) 가격 |
| --- | --- | --- | --- |
| 1학년 | 30 | 20 | 15 |
| 2학년 | 60 | 40 | 30 |
| 3학년 | 90 | 60 | 45 |

각 마법소녀의 세 가격은 `grade`로 조회하는 파생값이다. 캐릭터 원본에 같은 가격을 중복 입력하지 않는다. 무한 강화도 해당 학년의 보충 학습 가격을 사용하며 강화 횟수별 가격 상승은 추가하지 않는다.

## 3. 영입 개체 속성

| 속성명 | 자료형 후보 | 초기 예시 | 의미 |
| --- | --- | --- | --- |
| `instanceId` | `string` | `girl_0001` | 영입 개체 식별자 |
| `definition` | `MagicalGirlDefinition` | 아리 원본 참조 | 공유 원본 |
| `upgradeCount` | `int` | `0` | 완료한 강화 횟수. 1회 강화 스킬은 0 또는 1 |
| `skillVariables` | `List<SkillVariableValue>` | `damageAdd=100f` | 변수 ID와 현재 float 값. 원본 초기값으로 생성 |
| `slotIndex` | `int` | `0` | 왼쪽부터 0번, 미배치 -1은 임시안 |
| `keywords` | `List<KeywordInstance>` | `[]` | 현재 키워드 상태 |
| `hasResolvedSlotAction` | `bool` | `false` | 이번 턴 슬롯 액션 처리 기록. 위치 교환에 따른 재행동을 막는 조건으로 쓰지 않음 |

원본·개체·표시 오브젝트 분리는 중복 영입을 허용한다는 의미가 아니다. 동일 `characterId`의 중복 영입을 차단한다. 두 소녀가 위치를 교환하면 교환 후 슬롯 번호가 작은 소녀부터 행동하고, 번호가 큰 소녀가 다시 행동한다. 기존 행동 완료 여부만으로 이 재행동을 차단하지 않는다. 합동 데미지는 매 턴 0으로 초기화하며 키워드는 아래 소모·휘발 규칙을 따른다.

## 4. 스킬과 강화 변수

| 스킬 속성 | 자료형 후보 | 예시 | 의미 |
| --- | --- | --- | --- |
| `skillId` | `string` | `skill_starlight` | 스킬 ID |
| `displayName` | `string` | `별빛 모으기` | 표시 이름 |
| `descriptionTemplate` | `string` | `합동 데미지에 {damageAdd}를 더한다.` | 현재 변수 값으로 표시 설명 갱신 |
| `variables` | `List<SkillVariableDefinition>` | `damageAdd: 100f` | 각 항목은 `variableId: string`, `initialValue: float` |
| `upgradeMode` | `SkillUpgradeMode` enum | `Once` | `Once`: 1회 강화, `Unlimited`: 횟수 제한 없이 강화 |
| `upgradeTargetVariableId` | `string` | `damageAdd` | 강화 시 교체할 변수 ID. variables에 존재해야 함 |
| `replacementRule` | `UpgradeReplacementRule` | 아래 임시 예시 | 다음 강화 횟수와 현재 값으로 새 값을 구하는 규칙 후보 |
| `rules` | `List<ActionRule>` | 슬롯 액션 규칙 1개 | 트리거·조건·효과 묶음 |

강화 처리 순서는 횟수 제한 확인 → 학년별 비용 확인 → 대체값 계산 → 비용 지불 및 대상 변수 교체 → 횟수 증가와 설명 갱신이다. 제한 초과나 비용 부족이면 비용·변수·횟수를 변경하지 않는다. 액션은 원본 초기값이 아니라 개체의 현재 변수 값을 읽는다.

대체값 산출 로직은 마법소녀별로 지정한다. 모든 소녀에게 하나의 공통 공식을 강제하지 않는다. 편집용 `replacementRule` 예시로 1회형은 100 → 180, 무한형은 다음 강화 횟수를 n이라 할 때 `100 + 80 × n`을 사용한다. 무한형은 100 → 180 → 260이 되며 매번 계산 결과를 대입한다. 기존 값에 새 대체값을 더하지 않는다. 두 규칙의 수치는 임시이고, 무한형에 유한한 값 목록만 두어 강화가 막히는 구조는 피한다.

## 5. 액션과 대상 선택

| 데이터 | 속성 후보 | 설명 |
| --- | --- | --- |
| `ActionRule` | `ruleId: string`, `trigger: ActionTrigger`, `condition: ActionCondition`, `effects: List<ActionEffect>` | 사건별 조건과 효과 |
| `ActionEffect` | `effectType: EffectType`, `valueVariableId: string`, `targetStrategyId: string`, `keywordId: string`, `keywordStacks: int` | 수치는 스킬 현재 변수에서 조회. 키워드 필드는 키워드 부여 효과에만 사용 |
| `ActionTrigger` | enum | 후보: `OnPlaced`, `OnSlotAction`, `OnOtherAllyAction`, `OnMoved` |
| `EffectType` | enum | 후보: `AddDamage`, `AddDamagePercentPoint`, `MultiplyDamage`, `AddKeyword`, `SwapPosition` |

대상 선택은 `SelectTargets(context)` 같은 별도 함수가 전략 객체에 위임한다. `targetStrategyId`의 예시는 `Self`, `OtherAlly`, `AllOtherAllies`, `Boss`, `TeamDamage`다. 전략별로 대상 후보를 선택하고 효과 실행부는 그 결과에 효과만 적용한다. 함수·인터페이스명과 구체적 선택 알고리즘은 구현 제안이며 미확정이다.

`OnOtherAllyAction`에서 자신은 제외한다. 역할군은 대상 전략의 전투 조건으로 사용하지 않는다. 동시 효과 순서인 유물 획득순 → 마법소녀 자리순은 기존 기획을 따른다. 반응으로 발생한 액션도 다른 아군의 반응을 발생시킨다. 현재 기획에서는 연쇄 횟수 제한을 추가하지 않으며, 무한 콤보는 발생 사례를 보고 사후 조치한다.

## 6. 키워드 정의와 상태

| 데이터 | 속성 | 자료형 후보 | 의미 |
| --- | --- | --- | --- |
| `KeywordDefinition` | `keywordId` | `string` | 키워드 ID |
| `KeywordDefinition` | `consumable` | `bool` | 적용 시 소모되는지 |
| `KeywordDefinition` | `stackable` | `bool` | 복수 스택으로 쌓이는지. 기획상 상한 없음 |
| `KeywordDefinition` | `volatile` | `bool` | 턴 종료 시 사라지는지. C# 실제 필드명은 예약어를 피해 `isVolatile` 등으로 구현 |
| `KeywordInstance` | `keywordId` | `string` | 정의 참조 ID |
| `KeywordInstance` | `stacks` | `int` 후보 | 현재 스택 수. 기획상 무제한과 저장 타입의 표현 한계는 구분 |
| `KeywordInstance` | `sourceInstanceId` | `string` | 부여자 식별자 후보 |

세 속성은 독립적이다. 기존 duration 열거형 대신 휘발성 여부로 턴 종료 소멸을 정의한다.

| 상황 | 처리 |
| --- | --- |
| 중첩 가능 키워드 재부여 | 스택을 쌓는다. 기획상 스택 상한 없음 |
| 이미 있는 중첩 불가 키워드 재부여 | 무시한다 |
| 소모성 + 중첩 가능 키워드 적용 | 1스택 소모. 남은 스택이 0이면 제거 |
| 소모성 + 중첩 불가 키워드 적용 | 해당 키워드 제거 |
| 비소모성 키워드 적용 | 적용만으로 소모하지 않음 |
| 휘발성 키워드의 턴 종료 | 남은 스택 전체와 키워드 제거 |
| 비휘발성 키워드의 턴 종료 | 턴 종료만으로 제거하지 않음 |

소모성과 휘발성이 모두 참이면 적용할 때 소모하고, 턴 종료 때 남은 스택도 모두 제거한다. 소모·휘발 규칙은 키워드 정의를 따르며 아군·보스에 부여된 키워드에 공통 적용한다. 전투 종료 후 상태 정리 정책은 별도 결정한다.

## 7. 데미지 자료형과 표시

- 합동 데미지는 매 턴 `0f`부터 시작한다. 합연산·%p·곱연산을 발생 순서대로 적용하고, 값이 변경될 때마다 합동 공격 데미지 표시를 갱신한다. 초기화 시 표시도 0으로 갱신한다.
- 내부 데미지와 데미지 계산 변수는 `float`를 사용한다. 표시를 위해 원본 값을 정수로 변경하지 않는다.
- 일반 표시는 소수점 이하를 버린 정수 형태로 하고 3자리마다 공백으로 구분한다. 예: `1234.75f` → `1 234`.
- 기존 기획의 10자리 초과 조건에서 과학적 표기를 사용하고 가수는 소수점 2자리까지 표시한다. 예: 약 2050억 → `2.05e11`, 약 1000억 → `1.00e11`.
- 'int 표기'는 정수처럼 표시한다는 뜻이다. 큰 float 값을 32비트 int로 강제 변환하지 않고 정수 형태 문자열로 포맷한다. 과학적 표기의 가수는 두 소수 자리로 포맷하며 일반 표시의 소수점 버림과 구분한다.
- float 정밀도로 이미 손실된 숫자는 표시 과정에서 복구하지 않는다. 계산 범위를 넘는 값의 처리와 과학적 표기 반올림의 경계 정책은 추후 정한다.

## 8. 표시 오브젝트와 예시

`MagicalGirlView`의 후보 속성은 `instance: MagicalGirlInstance`, `visualRoot: Transform`, `selectionIndicator: GameObject`, `isSelected: bool`, `isDragging: bool`다. 학년과 역할군은 원본에서 읽어 표시한다. 화면 좌표와 논리 슬롯은 구분한다.

아리의 편집용 예시: 1학년, Dealer, 영입 30·학습 20·계약 해제 15. 스킬은 Once이며 `damageAdd`가 100f에서 180f로 교체된다. 강화 후 두 번째 학습은 불가능하다. Unlimited로 설정한 별도 예시에서는 같은 변수에 해당 마법소녀의 로직이 산출한 180f, 260f 등의 값을 차례로 대입한다. 모든 가격과 강화 수치는 임시 값이다.

## 슬롯 규칙

기본 슬롯은 3개, 일반 확장 최대치는 7개다. 슬롯 증가 유물이 있으면 전체 슬롯은 최대 9개까지 증가한다. 유물 증가 슬롯과 기존 슬롯은 연출용으로만 구분하며 액션·키워드·대상 선택에서 별도 종류로 취급하지 않는다.

## 남은 결정 항목

- 학년별 실제 가격과 각 마법소녀의 강화 산출 로직·수치. 현재는 임시 값 사용.
- 스킬·시너지 개수, 복수 규칙·효과의 허용 범위.
- 전략별 구체적인 대상 선택과 빈 대상 처리, 추가 슬롯 인원 선택.
- 전투 종료 시 키워드 정리, 키워드 외 버프의 유지 범위.
- 이미지 리소스 구조, 숫자·스택 저장 타입의 표현 범위 초과 처리.

전대 총 데미지, 유물 가방, 폴라로이드 수, 전투 턴, 보스 체력은 마법소녀 개체의 중복 속성으로 저장하지 않는 구성안이다.
