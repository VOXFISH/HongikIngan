# test 브랜치 전투 테스트

`test`는 `gamesystem`의 편집 가능한 교실·전투 UI 통합본에서 분기한 별도 테스트용 브랜치다. main과 gamesystem의 일반 보스 체력·3턴 승패 규칙은 바꾸지 않는다.

1. Unity 6000.3.7f1에서 `Assets/Scenes/School.unity` 또는 `Battle.unity`를 연다.
2. 전투에 들어가면 `TEST` 안내와 보스 `HP ∞ / ∞`가 표시된다. 데미지는 평소처럼 계산하고 보스 HP는 차감하지 않는다. 무한 데미지가 발생해도 `∞ - ∞`로 NaN이 되지 않는다.
3. 공격 버튼으로 1·2턴을 진행한다. 이때 학교 복귀 버튼은 잠겨 있다.
4. 세 번째 공격으로 **3턴이 시작되는 순간부터** `3턴부터 교실로 복귀` 버튼을 누를 수 있다. 계산·연출 도중에도 진행을 정리하고 교실로 이동한다. 사운드 창이 열렸다면 먼저 닫는다.
5. 3턴을 끝까지 보아도 패배하지 않고 복귀 버튼이 유지된다. 네 번째 공격은 시작하지 않는다.

복귀는 승리가 아니다. 스테이지·학생·강화·재화·사진·유물을 유지하고 승리 보상은 지급하지 않는다. 이미 실행한 재화 획득 효과는 유지된다. 학생의 전투 키워드는 정리하며 모집 제안은 다시 준비한다. 교실에서 재진입하면 0턴/무한 HP로 새 테스트 전투를 시작한다. 새 게임을 눌러도 테스트 모드는 유지된다.

## 구현과 검증

test 브랜치의 `PrototypeSession.ResetRun`이 `PracticeBattles`를 켠다. `BattleState.IsPracticeBattle`로 전투별 모드를 고정하며, 기본 `PrototypeRun`은 일반 규칙을 유지해 기존 규칙 검사를 계속 실행할 수 있다. `ReturnFromPracticeBattle`은 실행 중인 iterator를 UI에서 먼저 정리한 다음 복귀하도록 한다.

- 편집기 규칙 검사와 빌드: `HongikIngan.Editor.PracticeBattleChecks.BuildAndCheck`
- 실제 테스트 전투: 실행 파일에 `--practice-smoke --smoke-output <폴더>` 지정, 결과 `practice-result.txt`
- 일반 전투 회귀 검사: `--battle-ui-smoke --smoke-output <폴더>` 지정, 검사 안에서만 일반 모드로 전환

테스트 규칙을 일반 배포 브랜치로 병합하지 않는다. [전투 UI 편집](Battle%20UI%20Editing.md), [마법소녀 서식](Girls/README.md)을 함께 참고한다.

2026-11-05 검증 결과: Unity 6000.3.7f1 컴파일·씬 참조·Windows 빌드 성공. 일반 규칙 2,219개, 테스트 규칙 22개, 실제 플레이어 테스트 모드 23개, 일반 모드 UI 회귀 26개 검사 통과. 로그는 로컬 `UnityProject/Logs/PracticeBuildFinal.log`, `PracticePlayer.log`, `PracticeRegressionPlayer.log`이며 결과 파일은 `UnityProject/Builds/PracticeSmoke/practice-result.txt`와 `UnityProject/Builds/PracticeRegressionSmoke/battle-ui-result.txt`에 있다. 숨김 실행의 테스트 모드 캡처는 검게 출력되어 시각 검증에 사용하지 않았으며, 수동 플레이와 FMOD 실제 재생은 수행하지 않았다.

공격 버튼 바로 위의 `미라클 에너지 +99,999` 디버그 버튼은 누를 때마다 재화를 99,999 추가하고 화면 표시를 갱신한다(정수 저장 한도에서는 상한까지만 지급). 이 추가 변경은 사용자 요청에 따라 테스트하지 않았다.
