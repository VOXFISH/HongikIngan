# 교실·전투 UI 목업 → Unity 구현 v4

> 2026-11-06 후속 변경: 전투 구도는 사용자의 새 스케치에 맞춘 가운데 호 배치로 교체했다. 슬롯 채우기·호버·원기옥과 빔·1초 계산 표시는 [최신 전투 UI 편집](../Battle%20UI%20Editing.md)의 마지막 절을 우선한다. 아래 v4 좌표는 이전 구현 이력이다.

- 날짜: 2026-11-06
- 기준: enkephalin-needed의 [v3 기획서](UI%20Mockup%20Implementation%20v3.md), 사용자가 지정한 로컬 [HTML 원본](Reference/classroom-lobby.html), gamesystem `83200b3`의 Girl0–Girl17·경제·전투 규칙.
- HTML 원본 이름: `교실 로비 — 인터랙티브 목업 (학생증 추가).html`. 카카오톡 받은 파일 폴더에서 찾았으며 수정 없이 참고 폴더에 보존했다. HTML의 게임 로직은 Unity 구현에 사용하지 않는다.
- v3는 당시 기획 이력으로 유지한다. 현재 UI와 규칙은 이 문서를 따른다.

## 현재 규칙과 목업의 차이

| 항목 | 이번 적용 |
| --- | --- |
| 시작 | Main → 학교, 에너지 100과 중복 없는 1·2학년 3명. 학년 가중치 4:2 |
| 영입 | 일반 학생 모집 상점 없음. 펫 부르기 → 펫 결제 → 후보 한 명 영입. 소진된 펫은 같은 칸에 즉시 보충 |
| 유물 | 사물함의 한 제안만 리롤하여 선택. 전투 준비마다 한 번 구매, 구매 후 리롤·추가 구매 차단 |
| 사진 | 영입하지 않은 펫 후보만 사진 획득. 사진부 효과는 현재 구현 유지 |
| 전대 | 일반 최대 7명. 전투 중 복제 분신을 포함할 때만 총 9명 |
| 턴 | 한 전투 씬에서 최대 3턴. 매 턴 준비 전에 별도 `Turn x` 표시. 턴 사이 학교 복귀 없음 |
| 결과 | 승리 후 현재 게임을 유지해 학교 복귀. 3턴 실패는 게임 오버 후 새 게임 |
| test 예외 | 무한 보스, 3턴 도중 강제 복귀, 99999 지급 버튼을 공통 구현에서 제거 |
| 유지 | 18명 스킬·강화·역할·시너지, 이동 연쇄 제한, 정밀화·분신·키워드 규칙 |

`battlesystem`은 사용자 확인에 따라 `gamesystem`을 뜻한다. 새로운 동명 브랜치를 만들지 않는다. 동기화 대상은 test·gamesystem·main이다.

## Inspector에서 편집할 오브젝트

모든 배치와 크기는 씬에 직렬화한 `RectTransform`이 소유한다. 플레이 중 위치를 다시 계산하는 LayoutGroup은 사용하지 않는다. `SchoolScreen.Refresh()`는 책상 위치·배율을 변경하지 않는다. 아래 오브젝트를 옮기고 씬을 저장하면 다음 실행에도 유지된다. 드래그 중 학생 표시만 일시적으로 포인터를 따라가며, 드래그 취소 시 원래 위치로 돌아온다.

| 씬 / Hierarchy | 편집 대상 |
| --- | --- |
| School / SchoolCanvas / Layout / ClassroomArchitecture | 문·창문·시계·달력·사진 게시판·책장 |
| School / SchoolCanvas / Layout / RelicLockers | 유물 제안 한 칸·리롤·닫힌 사물함 문 |
| School / SchoolCanvas / Layout / OwnedRelics | 사물함 위 보유 유물 목록 |
| School / SchoolCanvas / Layout / StudentDesks / DeskSlot0…8 | 두 줄 책상 위치·스케일. 일반 7번째는 앞줄 왼쪽, 8·9번째 학교 책상은 숨김 |
| School / SchoolCanvas / Layout / StudentIdCard | 학생증 정보·학습·계약 해제·닫기 버튼 |
| School / SchoolCanvas / Layout / PetCaller | 펫 제안을 여는 영입 버튼 |
| School / SchoolCanvas / Layout / PetOfferPopup | 펫 제안 3개와 닫기 |
| School / SchoolCanvas / Layout / PetCandidatePopup | 후보 최대 5개·교체 판매·영입 포기·ReplacementPanel |
| School / SchoolCanvas / Layout / RestartConfirmPopup, SoundModal | 새 게임 확인과 실제 사운드 슬라이더·음소거 |
| Battle / BattleCanvas / Layout / Formation / Slot01…09 | 왼쪽에 떠 있는 전투 슬롯. 배열 순서는 공격 순서, 시각 위치는 독립 |
| Battle / BattleCanvas / Layout / CityBack, BossPanel | 도시 배경·보스 표시·HP |
| Battle / BattleCanvas / Layout / Presentation | 원기옥 수치·빔·영창·공격자·데미지 표시 |
| Battle / BattleCanvas / Layout / BonusPanel, CurrentStep | 실제 순차 계산 로그와 현재 행동 |
| Battle / BattleCanvas / Layout / TurnTransition / Band / TurnTitle | 턴 전환 배경·띠·`Turn x` 텍스트 |
| School 또는 Battle / SceneTransitionOverlay / Curtain / Band | 출동·학교 복귀·새 게임 전환 배경과 제목 |

학교 Canvas의 기준은 1440×810, 전투는 1440×900이며 Expand로 화면 안에 맞춘다. 아트가 없는 학생·펫·보스는 편집 가능한 도형과 정보 자리로 표시한다. HTML의 캐릭터 일러스트와 변신·유리 파편·펫 행렬 애니메이션 전체를 복제한 것은 아니다.

## 책임과 입력 잠금

- `SchoolScreen`: 현재 진행 상태와 교실 표시·거래 명령 연결.
- `SchoolDialogs`: 씬에 저장된 uGUI 팝업만 표시한다. 결제·판매·선택 처리는 `PrototypeRun`이 담당한다. 교체 판매와 새 영입은 별도 입력이다.
- `SceneTransitionOverlay`: 씬을 덮은 뒤 전환하고 다시 연다. 씬을 넘어 하나만 유지되며 위치는 변경하지 않고 투명도만 연출한다.
- `TurnTransitionView`: `Turn x`와 표시 시간을 관리한다. `BattleScreen.turnTransition`으로 연결하며 `duration`은 Inspector에서 조절한다.
- `BattleScreen`: 전환 표시 중 공격·배치 변경 차단, 같은 Battle에서 다음 턴 준비. 승리 또는 패배가 확정된 뒤에만 결과 동작을 허용한다.
- `BattleStep.Actor`·`Target`: 기존 연출용 계약을 분리된 최신 행동 처리기에 연결했다. `CanStartBattle`과 보상 미리보기도 현재 규칙과 함께 보존했다. `VictoryReward`는 처치 보상에 획득 시너지를 반영한 금액이며 전투 종료 시너지 자체의 보상은 별도다.

`HongikIngan/Apply HTML mockup layout once`는 기존 씬을 한 번 변환하는 Editor 전용 도구다. 적용 표식이 있는 씬의 수동 배치를 다시 덮어쓰지 않는다. 런타임에서는 오브젝트를 생성해 위치를 강제하지 않는다. 새 스크립트의 `.meta`와 씬 참조는 Unity가 생성·저장했다.

## 검증

`HongikIngan.Editor.MockupLayoutBuilder.Build`로 명시적 씬 마이그레이션과 Windows 빌드를 실행할 수 있다. 이미 변환한 씬은 `HongikIngan.Editor.PrototypeBuilder.Build`로 빌드한다.

플레이어 인수 `--mockup-ui-smoke --smoke-output <폴더>`는 실제 버튼을 사용해 영입·교체·오디오·새 게임 확인, Inspector 위치 보존, 출동 오버레이, Turn 1/2/3, 전투 중 복귀 차단, 승리/패배 경계를 검사한다. 결과는 `mockup-ui-result.txt`, 캡처는 같은 출력 폴더에 기록한다. 검사용 보스 체력 변경은 이 실행에만 적용한다.

최종 검증: Unity 6000.3.7f1 컴파일·씬 참조·Windows 빌드, 규칙 검사 3,498개와 시작 추첨 2,000개 시드를 통과했다. 실제 플레이어 검사 총 232개(목업 UI 48, 기존 전투 UI 26, 18명 전투 109, 메인 진입 14, 학교 거래 35)를 통과했고 각 최종 로그에 런타임 예외가 없음을 확인했다. 학교·학생증·후보 5개·전투·Turn 1/2/3 캡처를 확인했다. 문서 상대 링크 401개와 원본 HTML의 SHA-256 일치를 확인했다. 수동 마우스 플레이와 FMOD 실제 재생은 수행하지 않았다.

로컬 결과: `UnityProject/Logs/MockupVerifiedBuild.log`, `MockupFinalUi.log`, `MockupBattleRegression.log`, `MockupGirlRegression.log`, `MockupMainRegression.log`, `MockupEconomyRegression.log`. 결과 파일과 캡처는 `UnityProject/Builds/`의 같은 이름 폴더에 있다. 빌드·로그는 버전 관리에서 제외한다.

[문서 목차](../README.md) · [프로토타입 실행](../Prototype%20Guide.md)
