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

Rect Tool 또는 Inspector의 `RectTransform`에서 위치·크기·앵커를 변경하고 씬을 저장한다. `Formation`을 옮기면 슬롯 전체가, 각 `Slot`을 옮기면 해당 슬롯만 이동한다. 자동 LayoutGroup이나 실행 중 위치 초기화는 없다. CanvasScaler 기준은 1440×900이며 Expand 방식으로 화면 안에 전체 레이아웃을 맞춘다. Layout 루트는 중앙에 고정된다. 학생 수·확장 슬롯 수에 따라 표시 여부만 바뀌며 슬롯 배치는 유지된다. 현재 배치는 HTML 목업에 따라 왼쪽 부유 슬롯, 중앙 원기옥·보너스 로그, 오른쪽 괴수·HP로 구성한다. 런타임에서 바뀌는 텍스트 값은 상태에서 갱신되므로 제목 외의 동적 문구는 해당 뷰에서 변경한다.

`BattleFormationView.slots` 배열 순서가 논리적인 공격 순서다. 시각 위치 변경으로 공격 순서가 바뀌지 않는다. 실제 플레이에서는 학생을 다른 학생 위에 드래그하면 순서가 교환된다. 빈 슬롯으로의 이동은 기존 전투 모델이 지원하지 않아 교환하지 않는다. 잘못된 위치에 놓으면 원래 자리로 돌아간다. 공격 중, 결과 화면, 사운드 창에서는 드래그와 공격 재입력이 잠긴다.

## 책임

- `BattleScreen`: 진행 상태 구독, 입력 명령, 턴 코루틴과 씬 전환 연결.
- `BattleHud`: 헤더·데미지·HP·결과 버튼·기록의 텍스트/값 표시.
- `BattleFormationView` / `BattleStudentSlot`: 학생 표시와 드래그·교환 요청.
- `BattlePresentation`: 빔과 전투 사운드, 단계별 표시 시간.
- `BattleAudioPanel`: 사운드 설정과 모달 입력 차단.
- `BattleUiFont`: TMP 폰트 선택. 기본적으로 각 텍스트에 저장된 폰트를 유지하고, Inspector의 Font에 TMP Font Asset을 지정하면 하위 텍스트에 적용한다.
- `CombatEngine` / `PrototypeRun`: 기존 전투 계산·거래·승패 규칙. UI 분리로 변경하지 않는다. `BattleStep.Actor`·`Target`은 어느 슬롯을 강조할지 알려 주는 연출용 정보이며 규칙에 쓰지 않는다.

기존 `BattleScreen` GUID와 `FormatDamage` 호출 호환성을 보존한다. 전투 진행 중 컨트롤러가 비활성화되면 중간 효과가 적용된 턴을 재실행하지 않고 재시작을 안내한다.

## 생성과 검증

메뉴 `HongikIngan/Apply editable Battle layout`은 레거시 씬을 한 번 변환한다. 이미 연결된 UI가 있으면 참조만 검사하고 수동 배치를 덮어쓰지 않는다. `Create prototype scenes`도 이 변환을 사용한다. 씬과 새 스크립트 `.meta`는 Unity가 생성한다. UI만 별도 프리팹으로 추출할 경우 에셋은 `Assets/UI/` 아래에 둔다.

배치 실행 진입점은 `HongikIngan.Editor.BattleLayoutBuilder.Apply`, 기존 규칙 검사는 `HongikIngan.PrototypeChecks.Run`이다. 실제 플레이 검사에는 `--battle-ui-smoke --smoke-output <폴더>`를 사용한다. 검사 결과는 출력 폴더의 `battle-ui-result.txt`에 기록한다.

2026-11-05 검증: Unity 6000.3.7f1 컴파일·씬 참조·Missing Script 검사, 기존 2,219개 규칙 검사, Windows 실행 파일에서 26개 UI/씬 전환 검사를 통과했다. 1440×900과 1280×720 캡처에서 9개 슬롯, 보스 체력 바, 하단 버튼 표시를 확인했다. 로그와 캡처는 로컬 `UnityProject/Logs/BattleLayout.log`, `BattleLayoutFix.log`, `BattlePlayerFinal.log`, `UnityProject/Builds/BattleUiSmokeFinal/`에 있으며 버전 관리에서 제외한다. 입력 검사는 실제 UI 이벤트 핸들러를 호출하는 자동 검사이며 사람의 수동 플레이와 FMOD 실제 재생은 포함하지 않는다.

## HTML 목업 배치와 전환 오브젝트 (v4)

현재 전체 구조는 [목업 구현 v4](UI%20Mockup/UI%20Mockup%20Implementation%20v4.md)를 따른다. `Presentation/Orb`, `Actor`, `Chant`, `DamagePop`과 `BonusPanel`, `CityBack/BossVisual`도 독립적으로 편집할 수 있다.

`BattleCanvas/Layout/TurnTransition/Band/TurnTitle`이 매 턴 준비 전 `Turn x`를 표시한다. `BattleScreen`의 `TurnTransitionView` 참조를 통해 연결되며 표시 시간은 해당 컴포넌트의 `duration`이다. 표시 중에는 공격·드래그를 잠그고, 승리·패배가 확정되면 다음 턴을 표시하지 않는다.

별도 루트 `SceneTransitionOverlay/Curtain/Band`는 출동·학교 복귀·새 게임 전환을 표시한다. 위치·크기는 RectTransform으로 조절하고, `SceneTransitionOverlay`의 `fadeDuration`, `holdDuration`으로 시간을 조절한다. 실행 중 하나만 유지하며 씬 로드 동안 화면을 덮는다. 기본 배치를 수정할 때는 School과 Battle에 저장된 전환 오브젝트를 함께 맞춘다.

학교 팝업과 책상 위치도 씬에 저장되며 UI 갱신에서 강제 재배치하지 않는다. 이전 test 전용 디버그/중도 복귀 오브젝트는 공통 씬에 존재하지 않는다.
## TextMeshPro 텍스트 (2026-11-06)

프로젝트의 Main(6개), School(133개), Battle(90개) 텍스트를 모두 `TextMeshProUGUI`로 전환했다. SampleScene과 URP2DSceneTemplate에는 변환 대상이 없다. 문구·RectTransform·컴포넌트 fileID를 보존하고, UI 스크립트의 참조 타입은 `TMP_Text`로 변경했다. 기존 버튼, 학생 드래그, 상태 갱신 연결은 같은 객체를 참조한다.

기본 폰트는 기존 `Assets/Resources/GmarketSansTTFMedium SDF.asset`이며 TMP Settings에도 지정했다. Main·School·씬 전환에서 실행 시 OS 폰트로 덮어쓰던 처리를 제거했으므로 Inspector에서 지정한 TMP 폰트가 유지된다. 레이아웃 생성 메뉴도 TMP 텍스트를 생성한다. `Tools/Bulk Replace TMP Fonts`는 열린 씬의 TMP Font Asset을 교체하며, 변경 후 씬을 저장해야 한다.

파일 비교로 텍스트와 참조 식별자, 텍스트 이외 씬 블록의 보존을 확인했다. 사용자 요청에 따라 Unity 컴파일·플레이·시각 검사는 실행하지 않았다. 인수 확인 시 세 씬의 한글, 긴 문구 잘림, 버튼 라벨, 팝업, 턴·씬 전환 표시를 확인한다.

## School 배경 이미지 (2026-11-06)

School 씬의 교실 배경, 책상, 의자, 문, 사물함과 별 장식은 `Assets/sprite/background`에 있는 기존 스프라이트를 직접 참조한다. 배경 그림에 포함된 창문·시계·달력·책장 및 기존 벽·바닥 도형은 중복되지 않도록 비활성화했다. 책상 이미지는 학생 앞, 시너지 책 뒤에 표시되며 드래그용 슬롯과 학생 참조는 유지한다. 학생·펫·기능 버튼은 대응 이미지가 없어 기존 표시를 유지한다. 이미지 생성이나 실행 검증은 수행하지 않았다.

후속 정리: 배경 교체로 비활성화한 중복 장식과 이전 UI 잔여물을 씬에서 제거했다. 개별 경로와 이유는 [School 씬 삭제 목록](School%20Scene%20Cleanup.md)에 기록했다. 드래그 레이어, 배치 적용 표식, 팝업과 추가 학생 슬롯은 유지한다.

## School 공통 책상 프리팹 (2026-11-06)

`Assets/UI/Prefabs/SchoolDeskSlot.prefab`은 School의 DeskSlot0 구성을 기준으로 만든 공통 프리팹이다. `SchoolCanvas/Layout/StudentDesks`의 DeskSlot0~8은 모두 이 프리팹의 인스턴스다. 공통 학생 표시·의자·책상·시너지 책의 내부 위치와 크기는 Prefab Mode에서 편집한다. 각 인스턴스의 이름, 배치 위치·회전·크기 배율, 활성 상태, 공격 순서 문구는 씬별 설정으로 유지한다. SchoolScreen.slots 및 기존 외부 참조는 새 인스턴스로 연결했다.

최초 변환 메뉴는 `HongikIngan > Use DeskSlot0 prefab in School`이다. 이미 전체 슬롯이 이 프리팹을 사용하면 재생성하지 않는다. 기존 프리팹이 있는 부분 변환 상태에서는 사용자 편집을 보호하기 위해 중단한다. 생성 도구는 `SchoolDeskPrefabBuilder.cs`이며 Unity가 프리팹·폴더·스크립트의 .meta를 생성했다. Unity Editor에서 9개 인스턴스와 씬 저장을 완료했으며 플레이·드래그 테스트는 사용자가 수행한다.

School의 `DragLayer`는 책상 프리팹 밖의 `SchoolCanvas/Layout` 아래에 유지해야 한다. `SchoolScreen.Awake`는 참조가 비어 있으면 기존 레이어를 연결하고, 레이어도 없으면 전체 Layout 크기의 RectTransform을 생성한다. 편집 상태에서 복구·저장하려면 `HongikIngan > Restore School drag layer` 메뉴를 사용한다. 이번 수정에서 씬의 누락된 레이어와 참조를 저장했으며 플레이 테스트는 수행하지 않았다.

텍스트 구분자: 폰트에 없는 U+00B7 대신 모든 씬, 공통 책상 프리팹, 런타임/Editor 문구 생성 코드에서 ASCII 하이픈(-)을 사용한다. 2026-11-06에 로컬 스크립트 UnityProject/Logs/ReplaceMiddleDot.ps1로 124곳을 치환했으며 원본은 UnityProject/Logs/MiddleDotBackup-20261106-043514-997/에 보관했다. Logs의 스크립트와 백업은 버전 관리 대상이 아니다. 씬 5개 중 SampleScene과 URP2DSceneTemplate에는 해당 문자가 없어 변경하지 않았다.


## 전투 보상 팝업

`BattleCanvas/Layout/RewardPopup/Dialog`는 전투 종료 후 보상을 수령하는 임시 네모 창이다. `Title`, `RewardDetails`, `ClaimButton`을 Inspector의 RectTransform에서 조절한다. `BattleRewardPopup`은 표시 내용과 활성 상태만 갱신하며 배치를 덮어쓰지 않는다. 생성 메뉴는 `HongikIngan/Add battle reward popup`이며 이미 연결된 팝업은 재생성하지 않는다.

보스 처치가 확정되면 정산 전 소유 에너지와 남은 턴을 저장한다. 기본 보상은 `50 + 남은 턴 × 100 + 정산 전 소유 에너지의 10%`이며 정수 에너지의 소수 부분은 버린다. 기존 선도부 획득 배율과 유물 추가 보상을 유지한다. 전투 연출이 끝난 뒤 팝업을 표시하고 수령 버튼을 누를 때 한 번만 지급한 뒤 학교로 돌아간다. 게임오버에는 지급·표시하지 않는다.

학교 모집 창의 Replace·ReplacementPanel을 삭제했다. 정원 초과 영입 후 학교 학생 상세에서 계약을 해제할 수 있다. SchoolCanvas/Layout/RosterPages의 Previous·Page·Next는 보유 학생 9명 단위의 페이지를 바꾸는 독립 UI이며 Inspector에서 위치를 조절한다. 출전 정원과 보유 인원은 별도로 검사한다.

책상 프리팹의 표시용 AttackOrder 라벨은 사용자 요청으로 제거했다. 슬롯별 텍스트 오버라이드와 생성 코드도 제거했으며, 실제 학생 배치 및 공격 순서 처리에는 변경이 없다.

후속 요청으로 AttackOrder 라벨을 복구했다. 현재 표시는 설명 문구 없이 슬롯별 숫자 1~9만 사용하며, 생성 코드도 같은 형식을 적용한다.

## 시너지 책 프리팹

Assets/UI/Prefabs/SchoolSynergyBook.prefab이 책 한 권의 공통 배치다. SchoolDeskSlot 안의 SynergyBook0~2는 이 프리팹의 중첩 인스턴스이며, 모든 학교 책상에 적용된다. 책의 공통 구조는 책 프리팹에서 편집하고, 쌓인 위치와 가운데 책의 색상은 책상 프리팹의 인스턴스 설정으로 유지한다. SchoolStudentSlot.books와 bookLabels는 각 책 및 시너지 텍스트를 참조하며 실제 문구와 표시 여부는 기존 Bind가 담당한다. 최초 변환 메뉴는 HongikIngan > Use synergy book prefab in School desks이며 이미 연결된 경우 재생성하지 않는다.


전투 카드의 Name 텍스트는 긴 이름(드미트리우스 등)을 위해 TMP Auto Size를 사용한다. Inspector에서 최소 10, 최대 17로 설정되어 있으며, RectTransform 위치와 크기는 그대로 편집할 수 있다. 런타임 코드는 이름만 갱신한다.

## Battle 공통 프리팹

Assets/UI/Prefabs/BattleStudentSlot.prefab은 Slot01 기반의 학생 슬롯이다. Formation/Slot01~09가 이를 사용하며 BattleFormationView.slots 배열과 드래그 연결을 유지한다. Assets/UI/Prefabs/BattleBuilding.prefab은 Building0 기반 배경 건물이며 CityBack/Building0~13에 적용된다. 기존 개별 위치·크기·색상과 텍스트 차이는 인스턴스 오버라이드로 보존했다. 공통 변경이 특정 인스턴스의 오버라이드와 겹치면 해당 인스턴스 설정이 우선한다. 생성 메뉴는 HongikIngan > Use Battle slot and building prefabs이다. 이미 연결된 프리팹은 재생성하지 않는다. 건물은 사용자 추가 승인 후 변환했으며 SoundModal과 SceneTransitionOverlay는 이번 프리팹화 범위에 포함하지 않았다.

Girl35 확장 통합에서는 main의 이 프리팹 구조를 보존했다. 학생 이름의 Auto Size 설정은 공통 BattleStudentSlot 프리팹에 두며, 보상 팝업은 Battle 씬의 독립 오브젝트다. HongikIngan.Editor.PrototypeBuilder.BuildExpansion은 필요한 팝업·모집 UI 연결과 규칙 검사를 마친 뒤 Windows 빌드를 생성한다.

## School 학생 초상화

SchoolStudentSlot.Bind가 현재 학생의 Id로 Assets/UI/StudentPortraitCatalog.asset을 조회하고 StudentPlaceholder의 Image.sprite를 갱신한다. StudentPlaceholder의 Fill 자식은 제거했으며 흰색 tint와 Preserve Aspect를 적용한다. 빈 슬롯이나 이미지 누락 시 기존 초상화를 지우고 Image를 숨긴다. 분홍색 사각형인 Selected Image도 비활성화하며 학생 선택 동작과 드래그 영역은 유지한다. 학생 이동·교환·영입은 기존 Refresh/Bind 흐름을 사용한다.

Assets/sprite/characters/Girl00.png는 Girl0, Girl01.png는 Girl1로 연결한다. Girl05.png와 Girl11.png를 포함해 현재 Girl0~Girl35 전체 36장이 연결되어 있다. 이미지를 추가한 뒤 HongikIngan > Connect School student portraits를 실행하면 카탈로그와 공통 책상 프리팹 연결을 갱신한다. 각 PNG는 전체 그림 한 장이므로 Sprite Mode Single로 가져오며 원본 PNG는 수정하지 않는다. 카탈로그를 프리팹이 직접 참조하므로 플레이어 빌드에도 포함된다. 학생 이미지 적용은 실행 중이며 편집 상태에는 특정 학생을 고정하지 않는다.

### School 전투 시작 변신

SchoolScreen의 transformationDuration(기본 1.2초) 동안 원래 옷을 순백색 실루엣으로 페이드하고, 완전히 흰 상태를 유지하면서 같은 Girl ID의 magical_char 스프라이트로 바꾼 뒤 새 옷의 색상을 서서히 드러낸다. Inspector의 transformationWhiteHold(기본 0.25)는 전체 시간 중 순백색 유지 비율이며, transformedOutfitHoldDuration(기본 0.25초)은 새 옷을 보여 준 뒤 씬 전환을 시작하기까지의 대기 시간이다. SceneTransitionOverlay.Go의 beforeFade 동안 커튼은 투명하게 유지하고 입력은 IsBusy로 잠근다. 입력 잠금으로 Refresh가 발생해도 변신 진행 상태를 유지한다.

StudentPortraitCatalog.Entry.magicalSprite가 변신 이미지를 보관한다. Connect School student portraits 메뉴가 Girl00_Magical_Char와 Girl5_Magical 같은 파일명도 숫자 ID로 연결한다. 이미 Sprite Mode Multiple로 잘린 변신 이미지는 기존 슬라이싱을 보존하며 면적이 가장 큰 캐릭터 스프라이트를 연결한다. 이전 Single 모드의 fileID를 그대로 사용하면 이미지 참조가 끊기므로 재슬라이싱 후 이 메뉴로 카탈로그를 갱신한다. 변신 이미지가 없으면 원래 이미지를 유지한다. StudentTransformation 셰이더와 머티리얼은 Assets/UI에 있으며 기존 StudentPlaceholder Image 하나에 적용한다. 원본 PNG 변경이나 추가 사각형 생성 없이 투명도 외곽에만 은은한 발광을 표시한다.

School의 SceneTransitionOverlay/Curtain은 전체 화면 stretch, 위치 (0, 0), 크기 차이 (0, 0)으로 화면에 맞춘다. Band는 중앙 앵커와 위치 (0, 0)를 사용한다. 배치는 씬의 RectTransform에서 편집하며 런타임 코드가 좌표를 덮어쓰지 않는다. `SchoolPortraitBuilder.BuildTransitionValidation`으로 36명의 이미지 참조와 플레이어 빌드를 검증하고, 플레이어의 `--school-transition-smoke --smoke-output <폴더>`로 1920×1080 정렬·순백색 유지·새 옷 페이드·학교/전투 왕복을 확인할 수 있다.

## 최신 Battle 전체 화면과 전투 연출 (2026-11-06)

이 절이 앞의 이전 배치 안내보다 우선한다. BattleCanvas는 1600×900(16:9) 기준 좌표를 사용하며 1920×1080에서 1.2배로 표시된다. BattleCanvas/Background를 Canvas 양끝까지 Stretch하여 흰 여백을 없앴다. Layout은 1600×900이며 괴수·HP·원기옥·학생 호는 가운데, 공격·사운드 버튼은 오른쪽이다. 배치는 런타임에 다시 계산하지 않고 RectTransform으로 편집한다.

### 페이드

BattleCanvas/Layout/Presentation의 BattlePresentation에서 **Fade Combat Text**를 켜거나 끈다. 학생 계산식 패널·원기옥 로그·필살기 문구에 적용하며 HP와 합계 숫자는 항상 표시한다. **Fade In Duration**과 **Fade Out Duration**은 기본 0.12초 / 0.2초이고 Step Duration은 기존 1초다. 각 페이드는 항목 시간의 최대 40%까지만 사용해 읽을 시간을 확보한다. 빈 로그의 배경은 숨긴다.

계산식은 어두운 CalculationPanel에 흰색 굵은 글씨로 표시하고, 변경 전후 데미지 숫자를 효과 설명보다 먼저 보여 준다. Assets/UI/Prefabs/BattleStudentSlot.prefab의 패널 크기·색상과 텍스트 Auto Size(20–24)를 편집한다. 원기옥의 CombatLogPanel도 별도 오브젝트다.

### 학생 효과 동작

BattlePresentation의 **Animate Students**는 전체 토글이다. 각 BattleStudentSlot의 **Animate Effects**는 개별 토글이며 **Hop Height**(14), **Hop Duration**(0.5초), **Hop Curve**로 동작을 편집한다. 공통 프리팹에서 기본값을 바꾸거나 씬 슬롯에서 개별로 조절한다.

액션의 주체·명시적인 대상과 키워드 수치가 바뀐 아군이 반응한다. 전대 버프의 수신자도 포함한다. Portrait만 부드럽게 위아래로 움직이므로 이름·계산식·클릭 영역은 고정된다. 끝나거나 중단·비활성화·학생 교체가 일어나면 원래 위치를 복구하며 전투 수치와 트리거 순서는 바꾸지 않는다.

최초 적용 메뉴는 HongikIngan > Polish full-screen Battle presentation이며 FullBleedPresentationV1 표식이 있는 씬은 다시 덮어쓰지 않는다. 빌드 진입점은 HongikIngan.Editor.BattlePolishBuilder.Build다.

### 검증

열려 있던 에디터 작업을 보호하기 위해 별도 프로젝트 복사본에서 Unity 6000.3.7f1 Windows 빌드·씬/프리팹 참조·규칙 4,010개·초기 지급 2,000시드를 검사했다. 실제 1080p 플레이어 검사 222개는 배경 가장자리, 3·4·5·7·9명 배치, 페이드 시작/읽기/종료, 송신/수신/전대 버프 대상의 홉, 전체/개별 토글, 중단 후 위치 복구를 포함한다. 복구 오차 허용은 0.01 UI 단위 미만이다.

검증한 Battle 씬·학생 프리팹과 동일한 소스를 원본에 반영했고 기존 GUID를 유지했다. 캡처와 결과는 UnityProject/Builds/BattlePolishComplete/, 로그는 UnityProject/Logs/BattlePolishIsolatedBuild.log에 있다. 수동 마우스 인수 검사와 FMOD 청취는 미실시다. 이전 Battle을 열어 둔 에디터는 디스크의 씬을 다시 불러온다.

### Battle 뒷모습 이미지 (2026-11-06)

BattleFormationView.Artwork의 Girl0–Girl35 전체 36명을 Assets/sprite/magical_char_back의 같은 ID 이미지로 연결한다. 이 폴더가 전체 뒷모습 이미지 폴더이며, Assets/magical_char_back에는 별도의 3장만 있으므로 혼동하지 않는다. 기존 여러 Sprite 분할 중 면적이 가장 큰 캐릭터 본체를 참조하며 원본 PNG·GUID·가져오기 설정은 유지한다. 기존 홉·페이드·드래그와 비율 유지 설정도 그대로 사용한다. 36개 파일의 ID·GUID·Sprite 하위 식별자를 각각 확인했다.

### School 전투 시작 타일 전환

SchoolScreen.battleSweepPrefab은 Assets/UI/Prefabs/SchoolBattleSweep.prefab을 참조한다. 원본 Assets/sprite/UI/sides (1).png의 기존 세 스프라이트를 하단부터 Tile1(보라), Tile2(분홍), Tile3(주황) 순서로 사용하며 원본 이미지와 슬라이스 GUID를 유지한다. HongikIngan > Apply School tile sweep transition 메뉴로 프리팹 및 School 참조를 저장한다.

학생 변신 후 기존 텍스트 Band 대신 타일이 왼쪽에서 들어와 화면을 덮고, Battle 로드 후 오른쪽으로 빠져나간다. SceneTransitionOverlay의 sweepInterval은 0.1초, sweepDuration은 편도 0.45초이며 Inspector에서 조절한다. 전환 도형은 프리팹에서 편집하고, 런타임에는 화면 크기에 맞춰 이동 거리와 크기를 계산한다. 지속 오버레이 아래에 프리팹을 표시하므로 Main에서 시작해도 씬 로드 중 유지되고 완료 후 정리된다. 다른 씬 이동은 기존 페이드를 사용한다.

### 전투 시작 로고

SchoolBattleSweep/BattleStartLogo는 ui two.png의 battle_start 스프라이트이며 화면 중앙, 기본 영역 1296×720(기존 대비 1.8배), Preserve Aspect로 배치한다. 로고는 세 타일보다 뒤의 sibling으로 렌더링하며 스윕 타일 배열에서는 제외한다. 보라 타일이 중앙에 도달해 화면을 완전히 덮으면 크기 0 → 1 → 0.8 → 1 → 0, 회전 0 → -12 → +10 → -7 → 0도의 순서로 재생한다. Unity Z축의 음수는 시계 방향이다.

SceneTransitionOverlay의 battleLogoDuration(기본 0.85초), battleLogoScale, battleLogoRotation을 Inspector에서 조절한다. 로고는 두 번째·세 번째 타일 진입 중에도 재생되고, 로고 크기가 0이 된 후 타일 퇴장을 시작하므로 주황 타일이 화면에서 사라지기 전에 끝난다. 프리팹 갱신 메뉴는 기존 타일 편집 내용을 보존하며 로고를 추가한다.

### Battle 보스 피격·KO

BattlePresentation의 bossVisual은 BattleCanvas/BossVisual의 Image다. 합동 필살기 beam 단계 동안 이 이미지의 위치만 떨리며 단계 종료·중단 시 원래 위치로 복구한다. Inspector의 bossShakeAmount(기본 7), bossShakeFrequency(기본 24)로 조절한다.

표시 HP가 0이 되면 기존 StudentTransformation 머티리얼의 개별 런타임 복사본을 사용해 보스 전체를 순백색으로 표시하고 bossWhiteHold(기본 0.2초) 뒤 monsterKO.png의 가장 큰 기존 스프라이트로 교체한다. KO 이미지는 비율을 유지한다. 새 전투 시작은 원래 이미지·색·머티리얼로 초기화하며 연출 중 중단하면 흰색이 남지 않게 KO로 마무리한다. 연출 코루틴을 기다린 후 보상 화면을 열며 데미지·승패 계산은 바꾸지 않는다. 메뉴 HongikIngan > Connect Battle boss reactions가 씬의 세 참조를 저장한다.
