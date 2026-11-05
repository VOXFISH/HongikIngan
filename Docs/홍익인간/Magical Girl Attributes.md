# 마법소녀 오브젝트 및 속성 정의

> 사용자 확정 규칙을 반영한 편집용 문서다. 필드명·클래스 구조와 명시적으로 임시 표시한 수치는 제안이며, 실제 코드 구현은 아니다.

관련 문서: [게임 시스템 기획서](Game%20System%20Design.md)

## 확정 규칙

- 같은 마법소녀는 중복 영입할 수 없다. 영입 시 `characterId`로 중복을 검사한다. 계약 해제 후 재영입 가능 여부는 별도 결정한다.
- 마법소녀는 1·2·3학년의 희귀도 등급을 가진다. 영입·보충 학습·판매(계약 해제) 가격은 모두 학년별 공통 가격표에서 조회한다.
- 역할군은 `Buff`, `Debuff`, `Dealer` 중 하나이며 유저 편의를 위한 표시 전용이다. 데미지 계산, 시너지, 트리거 조건에 사용하지 않는다.
- 스킬 강화 방식은 1회 강화 가능과 무한 강화 가능으로 구분한다. 강화 대상 변수를 지정하고, 강화 시 그 변수의 현재 값을 새 값으로 대체한다.
- 체력·방어력·치명타·속도 등의 캐릭터 전투 능력치는 게임에 존재하지 않는다. 보스 체력은 별도의 전투 판정 값이다.
- 큰 데미지의 저장·계산 자료형은 `float`다. 일반 표시는 소수점을 버린 정수 형태, 과학적 표시는 소수점 2자리로 한다.
- 액션 대상 선택은 별도 함수에서 전략 패턴으로 처리한다.
- 복수 키워드 스택 허용 여부는 해당 키워드 정의의 중첩 가능 여부로 결정한다.

## 1. 오브젝트 구성안

| 오브젝트 후보 | 책임 |
| --- | --- |
| `MagicalGirlDefinition` | 종류별 원본: ID, 이름, 학년, 역할군, 시너지, 스킬, 이미지 |
| `GradePriceDefinition` | 학년별 영입·학습·계약 해제 가격 |
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

`GradePriceDefinition`은 `grade: SchoolGrade`, `recruitCost: int`, `upgradeCost: int`, `sellPrice: int`를 가진다. 아래 수치는 수정 가능한 임시 값이며 확정 밸런스가 아니다.

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
| `hasResolvedSlotAction` | `bool` | `false` | 이번 턴 슬롯 액션 처리 여부. 반응 액션과 구별 |

원본·개체·표시 오브젝트 분리는 중복 영입을 허용한다는 의미가 아니다. 동일 `characterId`의 중복 영입을 차단한다. 이동 후 행동 순서와 턴 간 상태 초기화 범위는 별도로 정한다.

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

대체값 산출 방식은 아직 확정되지 않았다. 편집용 `replacementRule` 예시로 1회형은 100 → 180, 무한형은 다음 강화 횟수를 n이라 할 때 `100 + 80 × n`을 사용한다. 무한형은 100 → 180 → 260이 되며 매번 계산 결과를 대입한다. 기존 값에 새 대체값을 더하지 않는다. 두 규칙의 수치는 임시이고, 무한형에 유한한 값 목록만 두어 강화가 막히는 구조는 피한다.

## 5. 액션과 대상 선택

| 데이터 | 속성 후보 | 설명 |
| --- | --- | --- |
| `ActionRule` | `ruleId: string`, `trigger: ActionTrigger`, `condition: ActionCondition`, `effects: List<ActionEffect>` | 사건별 조건과 효과 |
| `ActionEffect` | `effectType: EffectType`, `valueVariableId: string`, `targetStrategyId: string`, `keywordId: string`, `keywordStacks: int` | 수치는 스킬 현재 변수에서 조회. 키워드 필드는 키워드 부여 효과에만 사용 |
| `ActionTrigger` | enum | 후보: `OnPlaced`, `OnSlotAction`, `OnOtherAllyAction`, `OnMoved` |
| `EffectType` | enum | 후보: `AddDamage`, `AddDamagePercentPoint`, `MultiplyDamage`, `AddKeyword`, `SwapPosition` |

대상 선택은 `SelectTargets(context)` 같은 별도 함수가 전략 객체에 위임한다. `targetStrategyId`의 예시는 `Self`, `OtherAlly`, `AllOtherAllies`, `Boss`, `TeamDamage`다. 전략별로 대상 후보를 선택하고 효과 실행부는 그 결과에 효과만 적용한다. 함수·인터페이스명과 구체적 선택 알고리즘은 구현 제안이며 미확정이다.

`OnOtherAllyAction`에서 자신은 제외한다. 역할군은 대상 전략의 전투 조건으로 사용하지 않는다. 동시 효과 순서인 유물 획득순 → 마법소녀 자리순은 기존 기획을 따른다.

## 6. 키워드 정의와 상태

| 데이터 | 속성 | 자료형 후보 | 의미 |
| --- | --- | --- | --- |
| `KeywordDefinition` | `keywordId` | `string` | 키워드 ID |
| `KeywordDefinition` | `canStack` | `bool` | 복수 스택 허용 여부의 기준 |
| `KeywordInstance` | `keywordId` | `string` | 정의 참조 ID |
| `KeywordInstance` | `stacks` | `int` | canStack=false이면 복수 스택을 보유하지 않음 |
| `KeywordInstance` | `duration` | `KeywordDuration` enum | 턴 종료·전투 종료까지 등의 수명은 임시 후보 |
| `KeywordInstance` | `sourceInstanceId` | `string` | 부여자 식별자 후보 |

효과가 여러 스택을 요청해도 `canStack=false`인 키워드에 복수 스택을 적용하지 않는다. 중첩 가능한 키워드의 합산 방식·상한, 중첩 불가능 키워드의 재부여 시 지속 시간 갱신·무시 여부는 별도 결정한다.

## 7. 데미지 자료형과 표시

- 내부 데미지와 데미지 계산 변수는 `float`를 사용한다. 표시를 위해 원본 값을 정수로 변경하지 않는다.
- 일반 표시는 소수점 이하를 버린 정수 형태로 하고 3자리마다 공백으로 구분한다. 예: `1234.75f` → `1 234`.
- 기존 기획의 10자리 초과 조건에서 과학적 표기를 사용하고 가수는 소수점 2자리까지 표시한다. 예: 약 2050억 → `2.05e11`, 약 1000억 → `1.00e11`.
- 'int 표기'는 정수처럼 표시한다는 뜻이다. 큰 float 값을 32비트 int로 강제 변환하지 않고 정수 형태 문자열로 포맷한다. 과학적 표기의 가수는 두 소수 자리로 포맷하며 일반 표시의 소수점 버림과 구분한다.
- float 정밀도로 이미 손실된 숫자는 표시 과정에서 복구하지 않는다. 계산 범위를 넘는 값의 처리와 과학적 표기 반올림의 경계 정책은 추후 정한다.

## 8. 표시 오브젝트와 예시

`MagicalGirlView`의 후보 속성은 `instance: MagicalGirlInstance`, `visualRoot: Transform`, `selectionIndicator: GameObject`, `isSelected: bool`, `isDragging: bool`다. 학년과 역할군은 원본에서 읽어 표시한다. 화면 좌표와 논리 슬롯은 구분한다.

아리의 편집용 예시: 1학년, Dealer, 영입 30·학습 20·계약 해제 15. 스킬은 Once이며 `damageAdd`가 100f에서 180f로 교체된다. 강화 후 두 번째 학습은 불가능하다. Unlimited로 설정한 별도 예시에서는 같은 변수에 180f, 260f 등을 차례로 대입한다. 모든 가격과 강화 수치는 임시 값이다.

## 남은 결정 항목

- 학년별 실제 가격, 강화 대체값 산출 규칙과 밸런스.
- 계약 해제 후 재영입 여부와 재영입 시 강화 상태 처리.
- 스킬·시너지 개수, 복수 규칙·효과의 허용 범위.
- 전략별 대상 선택 알고리즘, 위치 교환 후 행동 순서, 연쇄 제한.
- 중첩 키워드의 상한·재부여·수명, 턴 간 상태 유지.
- 이미지 리소스 구조와 숫자 범위 초과 처리.

전대 총 데미지, 유물 가방, 폴라로이드 수, 전투 턴, 보스 체력은 마법소녀 개체의 중복 속성으로 저장하지 않는 구성안이다.
