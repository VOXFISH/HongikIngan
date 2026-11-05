# Girl%d — 이름

> 이 문서를 `Girl1.md`, `Girl2.md`처럼 복사하고 제목과 ID의 `%d`를 같은 정수로 바꾼다.
> 위쪽은 JSON 모양으로 정보를 정리하고, 아래쪽은 설정·스킬 동작·기타 내용을 자유롭게 적는다. 엄격한 JSON 문법이나 고정 스키마를 맞출 필요는 없다.
> AI 구현 시 코드 블록과 아래 설명을 모두 읽는다. 서로 모순되거나 미정인 내용은 임의로 확정하지 않는다.

## 기본 정보

```json
{
  "schemaVersion": 1,
  "girlId": "Girl1",
  "status": "draft", // draft 또는 ready. 미정 항목을 해결한 뒤 ready로 변경
  "displayName": "",
  "description": "", // 성격, 배경, 전투 컨셉을 자유롭게 설명
  "grade": null, // 1, 2, 3 중 하나. 가격은 공통 EconomySettings 사용
  "role": "", // Buff / Debuff / Dealer. 표시 전용, 효과 조건에 사용하지 않음
  "synergies": {
    "club": "", // [[Girl 0]]부활동
    "personality": "", // 성격
    "special": [], // 특수 시너지 ID/이름. 없으면 []
    "notes": "" // 시너지의 고유 효과나 기존 정의와 차이가 있으면 설명
  },
  "skill": {
    "skillId": "Girl%d_Skill",
    "displayName": "",
    "descriptionTemplate": "", // 현재 변수는 {value}처럼 표기
    "intent": "", // 어떤 조합과 배치에서 어떤 플레이를 유도하는지
    "variables": [
      { "id": "value", "initialValue": null, "unit": "", "description": "" }
    ],
    "upgrade": {
      "mode": "", // Once 또는 Unlimited
      "targetVariableId": "value", // 스킬당 하나만 강화. variables 안에 존재해야 함
      "replacementRule": "", // n=강화 후 횟수, old=현재 값. 산출값으로 대체(누적 가산 아님)
      "examples": [
        { "upgradeCount": 0, "expectedValue": null },
        { "upgradeCount": 1, "expectedValue": null },
        { "upgradeCount": 2, "expectedValue": null }
      ] // Once이면 2회 항목 삭제. Unlimited이면 모든 n의 규칙 설명
    },
    "rules": [
      {
        "ruleId": "Girl%d_Rule1",
        "trigger": "", // 예: 배치 시 / 자기 슬롯 액션 / 다른 아군 액션 / 이동 시
        "condition": "", // 무조건이면 '없음'. 키워드/이동 등 조건의 판정 시점도 명시
        "effectsInOrder": [
          {
            "description": "", // 효과 순서가 중요. 필요하면 효과 항목을 복사
            "operation": "", // 예: 합동 데미지 더하기 / %p / 곱하기 / 키워드 부여 / 위치 교환 / 재화 획득
            "valueExpression": "value", // 현재 스킬 변수로 계산. 상수/수식이면 의미도 설명
            "target": {
              "strategy": "", // 예: 자신 / 다음 아군 / 다른 아군 전체 / 보스 / 합동 데미지
              "selectionTiming": "", // 액션 실행 직전 등
              "tieBreaker": "", // 여러 후보 중 선택 순서. 무관하면 '해당 없음'
              "ifEmpty": "" // 대상이 없을 때. 임의 규칙을 만들지 않도록 명시
            },
            "keywordId": null, // 키워드 부여일 때만 입력
            "keywordStacks": null // 키워드 부여일 때만 입력
          }
        ],
        "reactionNotes": "" // 자기 반응 제외, 반응 연쇄/교환 재행동과의 관계, 무한 콤보 가능성
      }
    ]
  },
  "keywordDefinitions": [
    // 새 키워드가 없으면 이 항목을 삭제하고 []로 둔다. 기존 키워드 참조는 ID만 사용.
    {
      "keywordId": "",
      "displayName": "",
      "description": "",
      "consumable": null,
      "stackable": null,
      "isVolatile": null,
      "applicationTiming": "",
      "effect": ""
    }
  ],
  "visuals": {
    "portraitAsset": null, // 기존 에셋 경로 또는 제작 예정. 없으면 null
    "battleAsset": null,
    "themeColor": null, // 예: #FFD76AFF
    "presentationNotes": "" // 게임 효과와 순수 연출을 구별
  },
  "acceptanceExamples": [
    {
      "name": "기본 액션",
      "given": "", // 배치, 강화 횟수, 현재 데미지, 키워드 등
      "when": "",
      "then": "" // 기대 수치·대상·키워드·슬롯 변화
    },
    { "name": "강화 후 액션", "given": "", "when": "", "then": "" },
    { "name": "대상 없음 또는 조건 불충족", "given": "", "when": "", "then": "" }
  ],
  "openQuestions": [], // 미정 사항을 구체적으로 작성. 있으면 ready로 바꾸지 않음
  "implementationNotes": "" // 기존 프로토타입의 제약을 포함한 구현자 전달 사항
}
```

## 캐릭터 설정

<!-- 배경, 성격, 관계, 말투, 컨셉 등 자유롭게 작성. 게임 규칙과 구별한다. -->

## 스킬 동작 상세

<!-- 발동 계기, 조건, 대상 선택, 효과 순서, 예외 상황을 자연어로 설명. 위 필드에 담기 어려운 내용도 여기에 작성한다. -->

## 강화와 수치 설명

<!-- 강화 전후 예시, 계산 공식의 의미, 바뀌는 변수, 밸런스 의도 등. -->

## 연출과 표시

<!-- 모션, 이펙트, 대사, UI 설명, 이미지 참고 등. 실제 게임 효과와 순수 연출을 구별한다. -->

## 동작 예시

<!-- 특정 배치와 상황에서 어떤 결과가 나와야 하는지 자유롭게 작성한다. -->

## 기타 메모와 미정 사항

<!-- 잡다한 아이디어, 참고 자료, 추후 결정할 내용. 필요한 제목과 항목을 자유롭게 추가한다. -->
