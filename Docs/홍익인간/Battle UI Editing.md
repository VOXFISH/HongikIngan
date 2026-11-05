# 전투 UI 편집

`UnityProject/Assets/Scenes/Battle.unity`를 Unity 6000.3.7f1에서 연다. `BattleCanvas/Layout`의 자식은 씬에 저장된 uGUI 오브젝트이며 Play를 누르지 않아도 선택할 수 있다.

| 오브젝트 | 편집 내용 |
| --- | --- |
| Header | 제목, 진행 요약, 사운드 버튼 |
| Turn / Damage | 턴 안내와 합동 데미지 |
| BossPanel | 보스 이름, HP 텍스트, 체력 바 |
| Formation / Slot01…Slot09 | 개별 학생 슬롯과 얼굴·이름·학년·시너지·스킬 |
| CurrentStep / BattleLog | 현재 연출 메시지와 최근 4개 기록 |
| ActionButton | 공격, 승리 복귀, 게임 오버 재시작 |
| Presentation / Beam | 빔 표시 오브젝트 |
| SoundModal / Dialog | BGM·SFX·Voice 볼륨/음소거와 닫기 버튼 |

Rect Tool 또는 Inspector의 `RectTransform`에서 위치·크기·앵커를 변경하고 씬을 저장한다. `Formation`을 옮기면 슬롯 전체가, 각 `Slot`을 옮기면 해당 슬롯만 이동한다. 자동 LayoutGroup이나 실행 중 위치 초기화는 없다. CanvasScaler 기준은 1440×900이며 Expand 방식으로 화면 안에 전체 레이아웃을 맞춘다. Layout 루트는 중앙에 고정된다. 학생 수·확장 슬롯 수에 따라 표시 여부만 바뀌며 슬롯 배치는 유지된다. 런타임에서 바뀌는 텍스트 값은 상태에서 갱신되므로 제목 외의 동적 문구는 해당 뷰에서 변경한다.

`BattleFormationView.slots` 배열 순서가 논리적인 공격 순서다. 시각 위치 변경으로 공격 순서가 바뀌지 않는다. 실제 플레이에서는 학생을 다른 학생 위에 드래그하면 순서가 교환된다. 빈 슬롯으로의 이동은 기존 전투 모델이 지원하지 않아 교환하지 않는다. 잘못된 위치에 놓으면 원래 자리로 돌아간다. 공격 중, 결과 화면, 사운드 창에서는 드래그와 공격 재입력이 잠긴다.

## 책임

- `BattleScreen`: 진행 상태 구독, 입력 명령, 턴 코루틴과 씬 전환 연결.
- `BattleHud`: 헤더·데미지·HP·결과 버튼·기록의 텍스트/값 표시.
- `BattleFormationView` / `BattleStudentSlot`: 학생 표시와 드래그·교환 요청.
- `BattlePresentation`: 빔과 전투 사운드, 단계별 표시 시간.
- `BattleAudioPanel`: 사운드 설정과 모달 입력 차단.
- `BattleUiFont`: 기존 프로토타입의 OS 폰트 대체. Inspector의 Font를 지정하면 해당 폰트 사용.
- `CombatEngine` / `PrototypeRun`: 기존 전투 계산·거래·승패 규칙. UI 분리로 변경하지 않는다. `BattleStep.Actor`·`Target`은 어느 슬롯을 강조할지 알려 주는 연출용 정보이며 규칙에 쓰지 않는다.

기존 `BattleScreen` GUID와 `FormatDamage` 호출 호환성을 보존한다. 전투 진행 중 컨트롤러가 비활성화되면 중간 효과가 적용된 턴을 재실행하지 않고 재시작을 안내한다.

## 생성과 검증

메뉴 `HongikIngan/Apply editable Battle layout`은 레거시 씬을 한 번 변환한다. 이미 연결된 UI가 있으면 참조만 검사하고 수동 배치를 덮어쓰지 않는다. `Create prototype scenes`도 이 변환을 사용한다. 씬과 새 스크립트 `.meta`는 Unity가 생성한다. UI만 별도 프리팹으로 추출할 경우 에셋은 `Assets/UI/` 아래에 둔다.

배치 실행 진입점은 `HongikIngan.Editor.BattleLayoutBuilder.Apply`, 기존 규칙 검사는 `HongikIngan.PrototypeChecks.Run`이다. 실제 플레이 검사에는 `--battle-ui-smoke --smoke-output <폴더>`를 사용한다. 검사 결과는 출력 폴더의 `battle-ui-result.txt`에 기록한다.

2026-11-05 검증: Unity 6000.3.7f1 컴파일·씬 참조·Missing Script 검사, 기존 2,219개 규칙 검사, Windows 실행 파일에서 26개 UI/씬 전환 검사를 통과했다. 1440×900과 1280×720 캡처에서 9개 슬롯, 보스 체력 바, 하단 버튼 표시를 확인했다. 로그와 캡처는 로컬 `UnityProject/Logs/BattleLayout.log`, `BattleLayoutFix.log`, `BattlePlayerFinal.log`, `UnityProject/Builds/BattleUiSmokeFinal/`에 있으며 버전 관리에서 제외한다. 입력 검사는 실제 UI 이벤트 핸들러를 호출하는 자동 검사이며 사람의 수동 플레이와 FMOD 실제 재생은 포함하지 않는다.
