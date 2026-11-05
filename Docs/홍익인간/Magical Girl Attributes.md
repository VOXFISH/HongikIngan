# 마법소녀 오브젝트 및 속성 정의 초안

> 상태: 편집용 초안. 아래의 오브젝트명·필드명·자료형·예시 값은 수정할 수 있도록 임의로 채운 제안이다. 실제 C# 클래스나 공용 API를 확정하거나 구현한 문서가 아니다.

관련 문서: [게임 시스템 기획서](Game%20System%20Design.md)

## 작성과 수정 기준

- 표의 `속성명`, `자료형 후보`, `예시 / 초깃값`, `의미와 규칙`을 직접 수정한다. 필요 없는 행은 삭제하고 필요한 행은 추가한다.
- 자료형은 C# 표기를 사용한다. `List<T>`는 목록, `enum`은 선택 가능한 종류, 그 외 별도 정의 이름은 이 문서에서 제안한 사용자 정의 자료형이다.
- **기획 근거가 있는 개념:** 배치 슬롯, 지역·직업·특수 시너지, 키워드, 액션 트리거, 액션 효과, 다른 아군의 액션에 대한 반응.
- **이번 문서에서 제안하는 부분:** 데이터 분리 방식, ID 형식, 목록 개수, 키워드 중첩·수명 구조, 구체적인 캐릭터·스킬 이름과 수치. 예시는 밸런스 확정값이 아니다.
- 기획서의 본문에는 동시 효과 처리 순서와 영창 정렬 규칙이 구체화되어 있지만 하단 미정 목록에도 남아 있다. 이 초안은 본문의 규칙을 근거로 삼으며, 기존 문서 자체는 변경하지 않는다.

## 1. 오브젝트 구성안

| 오브젝트 후보 | 역할 | 포함하는 데이터 |
| --- | --- | --- |
| `MagicalGirlDefinition` | 캐릭터 종류별 공통 원본 | 이름, 소개, 시너지, 기본·강화 스킬, 영입 가격, 표시 리소스 |
| `MagicalGirlInstance` | 실제 영입한 개체의 현재 상태 | 개체 ID, 원본 참조, 강화 상태, 현재 슬롯, 부여된 키워드 |
| `MagicalGirlView` | 화면에 표시되는 오브젝트 | 개체 참조, 이미지·선택 표시, 드래그와 연출용 참조 |

같은 종류의 캐릭터를 여러 번 사용할 수 있게 되는 경우에도, 원본 데이터와 개체 상태를 분리하면 키워드와 슬롯 상태가 서로 섞이지 않는다. 동일 캐릭터의 중복 영입 허용 여부는 별도 결정한다. 원본의 Unity 저장 형태는 `ScriptableObject`를 우선 후보로 두되, 확정 전 변경할 수 있다.

## 2. 캐릭터 원본 속성

대상: `MagicalGirlDefinition`. 플레이 중 원본 값을 직접 변경하지 않는 구성을 제안한다.

| 속성명 | 자료형 후보 | 예시 / 초깃값 | 의미와 규칙 |
| --- | --- | --- | --- |
| `characterId` | `string` | `mg_starlight_001` | 종류를 구별하는 고유 ID. 표시 이름이 바뀌어도 유지한다. |
| `displayName` | `string` | `별빛 소녀 아리` | 화면에 표시할 이름 |
| `description` | `string` | `별빛을 모아 동료의 필살기를 돕는 마법소녀.` | 캐릭터 소개 |
| `recruitCost` | `int` | `30` | 미라클 에너지 기준 기본 영입 가격 후보. 실제 지불 가격의 할인 계산은 별도 처리한다. |
| `upgradeCost` | `int` | `20` | 보충 학습 가격 후보 |
| `synergies` | `List<SynergyRef>` | `Region: starlight_city`, `Job: student`, `Special: arcana` | 보유 시너지 목록. 카테고리당 개수 제한은 미정이다. |
| `baseSkill` | `SkillDefinition` | `skill_starlight`, 아래 예시 참고 | 기본 스킬 데이터 |
| `upgradedSkill` | `SkillDefinition` | `skill_starlight_plus` | 보충 학습 후 사용할 스킬 후보. 초안은 강화 1단계로 채웠으며 횟수·방식은 수정 가능하다. |
| `portrait` | `Sprite` | `Portrait_Ari` | 카드·상세 UI용 이미지 참조 후보. 예시 이름에 해당하는 실제 에셋은 아직 없다. |
| `battleSprite` | `Sprite` | `Battle_Ari` | 전투 표시용 이미지 참조 후보. 실제 에셋은 아직 없다. |
| `themeColor` | `Color` | `#FFD76AFF` | 이름·카드 장식 등에 사용할 테마 색상 |

`SynergyRef`의 속성 후보는 다음과 같다. 시너지 활성화 인원과 전대 효과는 시너지 데이터에서 정의하고 캐릭터마다 복사하지 않는 안이다.

| 속성명 | 자료형 후보 | 예시 | 의미 |
| --- | --- | --- | --- |
| `category` | `SynergyCategory` enum | `Region` | 후보 값: `Region`, `Job`, `Special` |
| `synergyId` | `string` | `starlight_city` | 별도 시너지 정의를 찾는 ID |

## 3. 영입 개체와 전투 상태 속성

대상: `MagicalGirlInstance`. 전투 중 바뀌는 값은 원본이 아니라 이 개체에서 관리하는 안이다.

| 속성명 | 자료형 후보 | 예시 / 초깃값 | 의미와 규칙 |
| --- | --- | --- | --- |
| `instanceId` | `string` | `girl_0001` | 영입 개체의 고유 ID. `characterId`와 구분한다. |
| `definition` | `MagicalGirlDefinition` | `mg_starlight_001`의 원본 참조 | 종류별 공통 데이터. 표의 ID는 사람이 읽기 위한 예시다. |
| `isUpgraded` | `bool` | `false` | 초안의 강화 여부. 다단계 강화로 바꾸면 정수 단계로 대체할 수 있다. |
| `slotIndex` | `int` | `0` | 왼쪽부터 0번. 미배치는 `-1`로 두는 임시 규칙이다. |
| `keywords` | `List<KeywordInstance>` | 빈 목록 `[]` | 현재 개체에 부여된 키워드 |
| `hasResolvedSlotAction` | `bool` | `false` | 이번 턴의 슬롯 순회 액션 처리 여부. 반응 액션의 실행 여부와 구별한다. |

`currentSkill`은 `isUpgraded`에 따라 원본의 `baseSkill` 또는 `upgradedSkill`을 조회하는 파생값으로 제안한다. 별도의 수정 가능한 스킬 복사본을 동시에 보관하지 않는다.

턴 시작 때 `hasResolvedSlotAction`을 초기화하는 안을 제안한다. 이동 후 재행동 여부, 슬롯 처리 도중 교환된 개체의 다음 순서, 턴을 넘기는 키워드·버프의 유지 범위는 별도 결정해야 한다. 이 필드만으로 재행동 금지 규칙을 확정하지 않는다.

## 4. 스킬, 트리거와 효과 속성

### SkillDefinition

| 속성명 | 자료형 후보 | 예시 / 초깃값 | 의미 |
| --- | --- | --- | --- |
| `skillId` | `string` | `skill_starlight` | 스킬 고유 ID |
| `displayName` | `string` | `별빛 모으기` | 표시 이름 |
| `description` | `string` | `자신의 슬롯 액션 시 합동 데미지에 100을 더한다.` | 설명. 효과 수치를 바꾸면 함께 수정한다. |
| `rules` | `List<ActionRule>` | 아래 규칙 1개 | 반응할 사건·조건·효과의 묶음. 복수 규칙 허용은 임시 제안이다. |

### ActionRule

| 속성명 | 자료형 후보 | 예시 / 초깃값 | 의미 |
| --- | --- | --- | --- |
| `ruleId` | `string` | `rule_starlight_slot` | 같은 규칙을 구별하기 위한 ID |
| `trigger` | `ActionTrigger` enum | `OnSlotAction` | 후보 값: `OnPlaced`, `OnSlotAction`, `OnOtherAllyAction`, `OnMoved` |
| `condition` | `ActionCondition` enum | `None` | 후보 값: `None`, `SelfHasKeyword`, `AnyAllyHasKeyword`. 아군 전체 키워드 조건을 '누군가 보유'로 해석한 임시안이다. |
| `requiredKeywordId` | `string` | 빈 문자열 `""` | 키워드 조건에서 검사할 ID. `None`이면 사용하지 않는다. |
| `effects` | `List<ActionEffect>` | `AddDamage / TeamDamage / 100` 1개 | 조건을 만족하면 실행할 효과 목록 |

트리거와 조건을 분리하는 것은 데이터 표현 제안이다. 기획서의 event 구독 기반 처리를 따르되, 같은 규칙을 중복 구독하지 않도록 한다. `OnOtherAllyAction`은 본인의 액션을 제외한다. 원본 기획의 동시 발동 순서인 **유물 획득순 → 마법소녀 자리순**은 공통 처리 규칙으로 두고, 캐릭터별 우선순위 속성으로 덮어쓰지 않는다.

### ActionEffect

| 속성명 | 자료형 후보 | 예시 / 초깃값 | 의미 |
| --- | --- | --- | --- |
| `effectType` | `EffectType` enum | `AddDamage` | 후보 값: `AddDamage`, `AddDamagePercentPoint`, `MultiplyDamage`, `AddKeyword`, `SwapPosition` |
| `target` | `EffectTarget` enum | `TeamDamage` | 후보 값: `TeamDamage`, `Self`, `OtherAlly`, `AllOtherAllies`, `Boss`. 아군 대상 선택 방식은 미정이다. |
| `value` | `double` | `100.0` | 데미지 증가·%p·배율의 수치 후보. `AddDamagePercentPoint`의 `20`은 +20%p, `MultiplyDamage`의 `1.5`는 1.5배로 읽는다. |
| `keywordId` | `string` | 빈 문자열 `""` | `AddKeyword`일 때 부여할 키워드 ID |
| `keywordStacks` | `int` | `1` | `AddKeyword`일 때 부여할 중첩 수 후보 |

`value`는 `AddDamage`, `AddDamagePercentPoint`, `MultiplyDamage`에서만 사용한다. `keywordId`와 `keywordStacks`는 `AddKeyword`에서만 사용하고, `SwapPosition`은 교환 대상만 사용한다. 효과별 필수 속성은 나중에 별도 자료형으로 나눌 수 있다.

기획서에는 `2.05e11`처럼 큰 데미지 표기가 있으므로 데미지 관련 값을 모두 `int`로 고정하지 않는다. 여기서는 수치 파라미터에 `double`을 임시로 사용한다. 실제 합동 데미지의 저장 타입, 정밀도·반올림·오버플로 정책과 %p 누적 공식은 별도 결정하며, 화면의 정수 표기와 내부 계산 타입은 구분한다.

## 5. 부여된 키워드 속성

대상: `KeywordInstance`. 키워드 효과 자체의 정의는 ID로 참조하고, 부여 상태만 담는 안이다.

| 속성명 | 자료형 후보 | 예시 / 초깃값 | 의미 |
| --- | --- | --- | --- |
| `keywordId` | `string` | `starlight` | 별도 키워드 정의의 ID |
| `stacks` | `int` | `1` | 중첩 수 후보. 동일 키워드의 합산·갱신 방식은 미정이다. |
| `duration` | `KeywordDuration` enum | `UntilTurnEnd` | 후보 값: `UntilTurnEnd`, `UntilBattleEnd`. 임시로 채운 수명 규칙이다. |
| `sourceInstanceId` | `string` | `girl_0002` | 부여한 마법소녀의 개체 ID. 마법소녀 외 출처는 이 값을 비우고 별도 출처 표현을 결정한다. |

이 표의 중첩과 수명은 기존 기획의 확정 규칙이 아니다. 키워드마다 지속 시간을 다르게 할지, 부여 효과가 지속 시간을 결정할지부터 확정해야 한다.

## 6. 표시 오브젝트 속성

대상: `MagicalGirlView`. 다음은 화면에 표시할 때 필요한 참조의 예시이며, 게임 규칙을 저장하는 장소로 사용하지 않는 안이다.

| 속성명 | 자료형 후보 | 예시 / 초깃값 | 의미 |
| --- | --- | --- | --- |
| `instance` | `MagicalGirlInstance` | `girl_0001` 개체 참조 | 표시할 영입 개체 |
| `visualRoot` | `Transform` | `VisualRoot` | 배치·드래그·연출용 표시 위치 참조 |
| `selectionIndicator` | `GameObject` | `SelectionOutline` | 선택 표시 오브젝트 참조 |
| `isSelected` | `bool` | `false` | 현재 선택 상태 |
| `isDragging` | `bool` | `false` | 현재 드래그 상태 |

위 오브젝트명도 예시이며 실제 Scene·Prefab은 생성하지 않았다. UI 카드와 전투 캐릭터에 같은 View를 쓸지 분리할지는 화면 구성에 맞춰 결정한다. 화면 좌표를 곧바로 슬롯 번호로 사용하지 않고, 슬롯 데이터와 표시 위치를 구분한다.

## 7. 한 캐릭터를 채운 예시

| 항목 | 임시 내용 |
| --- | --- |
| 종류 / 개체 | `mg_starlight_001` / `girl_0001` |
| 이름 / 가격 | 별빛 소녀 아리 / 영입 30 / 보충 학습 20 |
| 시너지 | 지역 `starlight_city`, 직업 `student`, 특수 `arcana` |
| 기본 스킬 | `skill_starlight`: `OnSlotAction` + `None` → `AddDamage`, 대상 `TeamDamage`, 값 `100.0` |
| 강화 스킬 | `skill_starlight_plus`: 같은 트리거·조건·대상, 값 `180.0`; 표시 설명도 180으로 변경 |
| 초기 상태 | `isUpgraded=false`, `slotIndex=0`, `keywords=[]`, `hasResolvedSlotAction=false` |
| 키워드 부여 후 상태 예시 | `keywords`에 `starlight / stacks=1 / UntilTurnEnd / source=girl_0002` 추가. 아리의 스킬이 이 키워드를 부여한다는 뜻은 아니다. |

## 8. 속성 확정 전에 결정할 항목

- [ ] 원본·영입 개체·표시 오브젝트를 위와 같이 나눌지 결정한다.
- [ ] 영입 가격과 보충 학습 가격을 캐릭터별로 둘지, 공통 계산으로 둘지 정한다.
- [ ] 지역·직업·특수 시너지별 보유 개수와 중복 가능 여부를 정한다.
- [ ] 스킬 수, 강화 횟수, 복수 규칙·효과 허용 여부를 정한다.
- [ ] 키워드 중첩·지속 시간·출처 표현과 턴 사이 유지 범위를 정한다.
- [ ] 액션 대상 선택, 이동 후 행동 순서, 반응 연쇄 제한을 정한다.
- [ ] 큰 데미지를 다루는 숫자 자료형과 계산·표시 규칙을 정한다.
- [ ] 이미지 참조 방식과 UI 카드·전투 View의 분리 여부를 정한다.

캐릭터별 체력·방어력·치명타·속도는 현재 기획에서 사용 근거가 없으므로 기본 속성에 넣지 않았다. 전대의 총 데미지, 유물 가방, 폴라로이드 보유 수, 전투 턴 수, 보스 체력은 캐릭터 개체마다 중복 저장하지 않고 각각의 관리 대상에서 소유하는 안이다. 해당 시스템이 추가되거나 소유권을 바꾸기로 하면 이 문서를 수정한다.
