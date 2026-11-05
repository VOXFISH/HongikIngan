# 프로젝트 문서

AI 에이전트는 저장소 루트의 AGENTS.md를 읽고, 이 폴더의 기획서와 관련 설계 문서를 확인한 뒤 UnityProject/로 진입합니다.

기획서와 설계 문서를 이 폴더에서 관리합니다. 누락된 요구사항은 임의로 추정하지 않습니다.

- [게임 시스템 플로우차트](Game%20System%20Flowchart.md): 제공된 원본 이미지와 사용자가 확인한 결과 분기.
- [게임 시스템 기획서](Game%20System%20Design.md): 턴 준비, 슬롯별 공격·트리거, 데미지·연출, 3턴 제한 및 미정 규칙.

Unity Hub에서는 저장소 루트가 아닌 UnityProject/ 폴더를 프로젝트로 선택합니다. 기존 Unity 버전은 UnityProject/ProjectSettings/ProjectVersion.txt를 따릅니다.

C# 스크립트는 UnityProject/Assets/Scripts/<기능명>/, UI 에셋은 UnityProject/Assets/UI/에 배치합니다. 새 Unity 에셋과 폴더의 .meta는 Unity Editor가 생성하도록 합니다.
