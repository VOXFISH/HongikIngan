# D01 — 모듈형 전투 구조 — 2026-11-05 16:40:26+09:00

- 기록 시각: 2026-11-05 16:40:26+09:00 (Asia/Seoul). 이 문서를 기록한 시각이며, 메시지 작성 시각을 추정한 값이 아니다.
- 상태: 제안 · 결정 대기. 아래 확정 제약은 기존의 효력을 유지한다.
- 출처 코멘트: [C01](C01-Initial-Battle-Assessment-2026-11-05_16-40-26_KST.md), [C02](C02-Modular-Framework-and-Slot-Planning-2026-11-05_16-40-26_KST.md), [C03](C03-Updated-Markdown-Review-2026-11-05_16-40-26_KST.md).
- 참고 문서: [게임 시스템 기획서](../Game%20System%20Design.md), [마법소녀 속성 정의](../Magical%20Girl%20Attributes.md).

## 확정 제약

캐릭터와 콘텐츠는 추후 추가하며, 게임 제작은 보류 중이다.

## 권고안

전투 흐름, 런타임 상태, 콘텐츠 어댑터, 화면 표시·연출의 책임을 분리한다. BattleController에는 특정 캐릭터에만 해당하는 분기 처리를 넣지 않는다.

## 결정할 사항

어댑터에 필요한 최소 계약과 정의 데이터의 ScriptableObject 사용 여부를 정한다. 클래스명은 아직 제안이다.

## 예상 영향 범위

BattleController, BattleSetup, BattleContracts, BattleSceneInstaller. 위 항목은 제안된 책임이며, 이 기록을 작성하면서 스크립트를 생성하지는 않았다.

## 추후 검증 기준

실제 게임 콘텐츠 없이도 테스트용 동작을 등록할 수 있고, 이를 위해 컨트롤러를 수정할 필요가 없는지 확인한다.

## 결정 기록

- 담당자: 미지정.
- 결정: 대기 중. 격려나 문서 작성 요청을 승인으로 해석하지 않는다.
- 결정 시각: 미기록.
- 구현: 시작하지 않음.

[결정 요구사항 목차](Battle-Decision-Requirements-2026-11-05_16-40-26_KST.md) · [권고 이력](Recommendation-Log-2026-11-05_16-40-26_KST.md)
