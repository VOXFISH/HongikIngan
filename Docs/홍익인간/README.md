# 프로젝트 문서

모든 팀원과 AI 에이전트는 저장소 루트의 [AGENTS.md](../../AGENTS.md)를 읽고, 이 폴더의 기획서와 관련 설계 문서를 확인한 뒤 UnityProject/로 진입합니다.

기획·설계·협업·사용 안내 Markdown 문서는 `Docs/홍익인간/` 또는 그 하위 폴더에서 관리합니다. 저장소 루트나 `Docs/` 바로 아래에 일반 Markdown 문서를 만들지 않습니다. 누락된 요구사항은 임의로 추정하지 않습니다.

## 문서 목록

- [게임 전체 흐름과 덱 예시 — 작업자용 요약](Game%20Overview%20and%20Deck%20Examples.md): 교실·전투의 흐름, 4가지 덱 예시와 영입·강화·배치에서 생기는 선택.
- [프로젝트 실행 안내](Project%20Setup.md): Unity 프로젝트 열기, 환경 확인 및 저장소 구조.
- [게임 시스템 플로우차트](Game%20System%20Flowchart.md): 제공된 원본 이미지와 사용자가 확인한 결과 분기.
- [게임 시스템 기획서](Game%20System%20Design.md): 턴 준비, 슬롯별 공격·트리거, 데미지·연출, 3턴 제한 및 미정 규칙.
- [마법소녀 속성 정의 초안](Magical%20Girl%20Attributes.md): 오브젝트 구분, 속성별 자료형 후보, 임시 값과 수정 체크리스트.
- [전투 설계 결정 기록](Decisions/README.md): 대화별 검토 기록, 주제별 결정 요구사항 및 권고 이력.

- [교실 시스템 기획서](Classroom%20System%20Design.md): 원본 도표, 학생 관리·리롤·모집·유물·펫·전투 진입.
- [교실 기능별 결정 사항](Decisions/Classroom/README.md): 7개 기능의 결정·미정·검증 기준.

- [프로토타입 실행 안내](Prototype%20Guide.md): 학교·전투 씬, 조작, 임시 콘텐츠, 검증 방법.
- [전투 UI 편집](Battle%20UI%20Editing.md): 독립 UI 오브젝트 배치, 컴포넌트 책임과 검증.
- [마법소녀 작성 서식](Girls/README.md): `Girl%d.jsonc` 복사·작성과 AI 일괄 구현 안내.
- [test 브랜치 전투 테스트](Test%20Branch.md): 무한 보스 HP와 3턴 시작 이후 교실 복귀.
- [FMOD 설정 안내](Audio%20Setup.md): 지속 SoundManager와 채널별 볼륨·음소거.

## 팀 공통 문서 관리 규칙

- 루트의 `AGENTS.md`와 이를 참조하는 `CLAUDE.md`는 도구가 공통 규칙을 찾기 위한 진입 파일이므로 루트에 유지합니다.
- AI 사용 기록은 [ai usage.csv](../ai%20usage.csv)와 [ai usage .md](../ai%20usage%20.md)에 계속 추가합니다. 기존 위치와 과거 기록을 보존합니다.
- 문서 이미지는 이 폴더의 `Images/`에 둡니다. 문서·이미지 링크는 상대 경로로 작성합니다.
- 문서를 추가하거나 이동하면 이 목차와 관련 링크를 갱신하고, 커밋·PR에서 문서 위치와 이미지 참조를 확인합니다.
- 외부 패키지·플러그인 내부 문서는 이동하지 않습니다.

## Unity 프로젝트 위치

Unity Hub에서는 저장소 루트가 아닌 UnityProject/ 폴더를 프로젝트로 선택합니다. 기존 Unity 버전은 UnityProject/ProjectSettings/ProjectVersion.txt를 따릅니다.

C# 스크립트는 UnityProject/Assets/Scripts/<기능명>/, UI 에셋은 UnityProject/Assets/UI/에 배치합니다. 새 Unity 에셋과 폴더의 .meta는 Unity Editor가 생성하도록 합니다.
