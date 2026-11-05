# C02 — 모듈형 기반 구조와 슬롯 설계 — 2026-11-05 16:40:26+09:00

- 기록 시각: 2026-11-05 16:40:26+09:00 (Asia/Seoul).
- 출처: 이 전투 설계 대화의 02번째 핵심 요청·응답.
- 형식: 발언 주체를 구분한 요약이며, 대화 원문 전체가 아니다. 원래 메시지 시각은 추정하지 않는다.

## 사용자 코멘트

사용자는 모듈형 핵심 구조, 실행 순서와 그룹, 변수·함수 정의, 파일별 Unity 구현 계획을 요청했다. “아직 게임 제작 작업을 시작하지 말 것”이라고 명시했으며, UI 에셋은 에디터에서 만들고 캐릭터 콘텐츠는 추후 다루도록 했다.

## AI 검토 요약

전투 흐름·상태·콘텐츠·연출의 분리, 고정된 슬롯 ID와 배정 정보 저장, 명시적인 처리 순서, 파일별 계약·컨트롤러·배치·스케줄러·UI의 책임을 제안했다. 캐릭터 카드는 키워드·액션·폴라로이드 정보를 연결하도록 했다.

## 현재 해석과 수정 사항

구조와 클래스명은 모두 제안이다. 같은 캐릭터의 중복 보유를 가정한 이전 예시는 C03에서 수정되었다. 행동 순서 고정과 입력 잠금은 확정된 게임 규칙이 아니라 권고였다. 슬롯 요구사항은 D02에 보존했다.

## 관련 결정 항목

- [D01 — 모듈형 전투 구조](D01-Modular-Battle-Structure-2026-11-05_16-40-26_KST.md)
- [D02 — 배치 상태의 관리 주체와 위치 저장](D02-Formation-Ownership-and-Saved-Locations-2026-11-05_16-40-26_KST.md)
- [D03 — 행동 순서와 턴별 행동 상태 관리](D03-Action-Scheduling-and-Turn-Ownership-2026-11-05_16-40-26_KST.md)
- [D04 — 트리거 처리 순서와 연쇄 반응](D04-Trigger-Ordering-and-Reaction-Chains-2026-11-05_16-40-26_KST.md)
- [D05 — 대상 선택과 조건 평가 시점](D05-Target-Selection-and-Evaluation-Timing-2026-11-05_16-40-26_KST.md)
- [D06 — float 데미지 계산과 최종값 확정](D06-Float-Damage-and-Finalization-2026-11-05_16-40-26_KST.md)
- [D07 — 키워드 상태와 턴 사이의 상태 유지](D07-Keyword-State-and-Turn-Persistence-2026-11-05_16-40-26_KST.md)
- [D08 — 캐릭터 식별과 강화 시스템 연결](D08-Character-Identity-and-Upgrade-Integration-2026-11-05_16-40-26_KST.md)
- [D09 — 캐릭터 카드 연결과 폴라로이드 보유 정보](D09-Character-Card-and-Polaroid-Ownership-2026-11-05_16-40-26_KST.md)
- [D10 — 입력·연출과 Unity 생명주기](D10-Input-Presentation-and-Unity-Lifecycle-2026-11-05_16-40-26_KST.md)
- [D11 — 전투 단계와 결과 판정 경계](D11-Battle-Phases-and-Result-Boundaries-2026-11-05_16-40-26_KST.md)
- [D12 — 구현 단계와 검증 계획](D12-Implementation-Stages-and-Validation-2026-11-05_16-40-26_KST.md)

[결정 요구사항 목차](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md) · [권고 이력](Recommendation-Log-2026-11-05_16-40-26_KST.md)
