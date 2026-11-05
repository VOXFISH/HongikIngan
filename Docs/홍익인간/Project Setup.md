# HongikIngan

Unity Editor **6000.3.7f1** 프로젝트입니다.

## 처음 열기

1. Unity 프로젝트가 포함된 브랜치를 내려받습니다. 이 구조를 main에 반영하기 전에는 `chore/unity-project-layout` 브랜치를 사용합니다.
2. Unity Hub에서 **Add > Add project from disk**를 선택합니다.
3. 복제한 저장소의 **UnityProject/** 폴더를 선택합니다. 저장소 루트 HongikIngan/을 선택하지 않습니다.
4. Unity **6000.3.7f1**로 엽니다. 처음 열 때 패키지 복원과 에셋 임포트에 시간이 걸릴 수 있습니다.

올바른 폴더에는 Assets/, Packages/, ProjectSettings/가 함께 있고, ProjectSettings/ProjectVersion.txt에 버전이 기록되어 있습니다.

## “Your project was saved with an unknown Unity version” 경고

이 경고만으로 에디터 버전 불일치를 단정할 수 없습니다. 먼저 **Quit**를 선택하고 다음을 확인합니다.

- 현재 브랜치에 UnityProject/ProjectSettings/ProjectVersion.txt가 있는지 확인합니다.
- 파일의 m_EditorVersion 값이 6000.3.7f1인지 확인합니다.
- Unity Hub에 등록한 경로가 저장소 루트가 아닌 UnityProject/인지 확인합니다.
- 잘못 등록한 경로는 Hub 목록에서만 제거하고 UnityProject/를 다시 추가합니다. 디스크의 프로젝트 파일을 삭제하지 않습니다.
- 이미 다른 폴더에서 작업한 내용이 있다면 보존한 뒤 올바른 프로젝트와 비교합니다.

위 조건이 모두 맞는데도 경고가 지속되면 열린 프로젝트의 전체 경로와 Unity Editor.log를 확인해야 합니다. 경고를 없애기 위해 ProjectVersion.txt를 임의로 변경하지 않습니다.

## 저장소 구조

- [AGENTS.md](../../AGENTS.md): 개발자와 AI 에이전트의 공통 작업 규칙
- [CLAUDE.md](../../CLAUDE.md): 루트 AGENTS.md 참조
- [Docs/홍익인간/](README.md): 기획·설계·협업·사용 안내 문서와 목차
- `Docs/ai usage.csv`, `Docs/ai usage .md`: AI 사용 기록 (기존 위치 유지)
- UnityProject/: Unity 프로젝트

위 경로는 저장소 루트를 기준으로 합니다. 모든 팀원과 에이전트는 루트 `AGENTS.md`와 `Docs/홍익인간/README.md`를 먼저 읽습니다. 일반 Markdown 문서는 `Docs/홍익인간/` 아래에 작성하고, 생성 캐시인 Library/ 등은 Git에 포함하지 않습니다.
