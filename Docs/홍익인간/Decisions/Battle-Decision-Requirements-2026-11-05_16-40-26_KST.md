# 전투 설계 결정 요구사항 — 2026-11-05 16:40:26+09:00

Asia/Seoul 기준 시각으로 gamesystem 브랜치에서 작성했다. 이 문서 묶음은 현재 대화를 실질적인 코멘트 기록(C01~C04)과 주제별 결정 문서(D01~D12)로 나눈 것이다. 대화 원문 전체가 아닌 결정 검토 요약이며, 과거 의견은 수정 사항을 명시하여 보존했다.

## 범위와 판단 근거

- 게임 제작은 계속 보류한다. 허용된 작업은 문서화뿐이다.
- 확정 요구사항의 근거는 사용자의 명시적 지시와 현재 [게임 시스템 기획서](../Game%20System%20Design.md), [마법소녀 속성 정의](../Magical%20Girl%20Attributes.md)다.
- 구조의 명칭, 계약, 처리 순서 정책, 상태 관리 주체는 결정되기 전까지 제안으로 남는다. 가격과 강화 예시도 임시 값이다.
- 제목과 파일명에는 이 문서 묶음의 실제 기록 시각을 사용한다. 이전 대화 시각은 임의로 추정하지 않는다.
- 각 기록은 확정 제약, 권고안, 결정할 사항, 영향 범위, 추후 검증 기준을 구분한다.

## 대화별 코멘트

- [C01 — 초기 전투 시스템 검토](C01-Initial-Battle-Assessment-2026-11-05_16-40-26_KST.md)
- [C02 — 모듈형 기반 구조와 슬롯 설계](C02-Modular-Framework-and-Slot-Planning-2026-11-05_16-40-26_KST.md)
- [C03 — 갱신된 Markdown 문서 검토](C03-Updated-Markdown-Review-2026-11-05_16-40-26_KST.md)
- [C04 — 결정 사항 문서화 요청](C04-Decision-Documentation-Request-2026-11-05_16-40-26_KST.md)

## 주제별 결정 요구사항

| ID | 주제 | 현재 상태 |
| --- | --- | --- |
| D01 | [모듈형 전투 구조](D01-Modular-Battle-Structure-2026-11-05_16-40-26_KST.md) | 권고 기록 완료 · 미정 사항 결정 대기 |
| D02 | [배치 상태의 관리 주체와 위치 저장](D02-Formation-Ownership-and-Saved-Locations-2026-11-05_16-40-26_KST.md) | 권고 기록 완료 · 미정 사항 결정 대기 |
| D03 | [행동 순서와 턴별 행동 상태 관리](D03-Action-Scheduling-and-Turn-Ownership-2026-11-05_16-40-26_KST.md) | 권고 기록 완료 · 미정 사항 결정 대기 |
| D04 | [트리거 처리 순서와 연쇄 반응](D04-Trigger-Ordering-and-Reaction-Chains-2026-11-05_16-40-26_KST.md) | 권고 기록 완료 · 미정 사항 결정 대기 |
| D05 | [대상 선택과 조건 평가 시점](D05-Target-Selection-and-Evaluation-Timing-2026-11-05_16-40-26_KST.md) | 권고 기록 완료 · 미정 사항 결정 대기 |
| D06 | [float 데미지 계산과 최종값 확정](D06-Float-Damage-and-Finalization-2026-11-05_16-40-26_KST.md) | 권고 기록 완료 · 미정 사항 결정 대기 |
| D07 | [키워드 상태와 턴 사이의 상태 유지](D07-Keyword-State-and-Turn-Persistence-2026-11-05_16-40-26_KST.md) | 권고 기록 완료 · 미정 사항 결정 대기 |
| D08 | [캐릭터 식별과 강화 시스템 연결](D08-Character-Identity-and-Upgrade-Integration-2026-11-05_16-40-26_KST.md) | 권고 기록 완료 · 미정 사항 결정 대기 |
| D09 | [캐릭터 카드 연결과 폴라로이드 보유 정보](D09-Character-Card-and-Polaroid-Ownership-2026-11-05_16-40-26_KST.md) | 권고 기록 완료 · 미정 사항 결정 대기 |
| D10 | [입력·연출과 Unity 생명주기](D10-Input-Presentation-and-Unity-Lifecycle-2026-11-05_16-40-26_KST.md) | 권고 기록 완료 · 미정 사항 결정 대기 |
| D11 | [전투 단계와 결과 판정 경계](D11-Battle-Phases-and-Result-Boundaries-2026-11-05_16-40-26_KST.md) | 권고 기록 완료 · 미정 사항 결정 대기 |
| D12 | [구현 단계와 검증 계획](D12-Implementation-Stages-and-Validation-2026-11-05_16-40-26_KST.md) | 권고 기록 완료 · 미정 사항 결정 대기 |

## 권장 결정 순서

1. D01~D03: 기반 구조의 책임 범위, 배치 상태 관리 주체, 행동 순서.
2. D04~D07: 이벤트, 대상 선택, 수치 연산의 의미, 키워드·초기화 정책.
3. D08~D09: 추후 콘텐츠 연결과 폴라로이드 보유 정보 관리.
4. D10~D12: 입력·연출, 전투 경계, 구현 완료 기준.

이 순서는 계획 수립을 위한 안내이며 구현 시작을 허용하는 지시가 아니다. 앞선 항목에 의존하지 않는 정책은 독립적으로 결정할 수 있다.

[권고 이력](Recommendation-Log-2026-11-05_16-40-26_KST.md) · [프로젝트 문서 목차](../README.md)
