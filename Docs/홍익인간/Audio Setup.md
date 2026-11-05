# FMOD 오디오 설정

## 현재 구현과 설치 상태

`Assets/Scripts/Audio/SoundManager.cs`는 학교·전투 씬 전환에도 유지되는 단일 관리자다. BGM/SFX/Voice의 볼륨과 음소거를 독립 관리하고 PlayerPrefs에 저장한다. 음소거를 해제하면 기존 볼륨 값으로 돌아간다.

FMOD 패키지와 프로젝트용 bank는 현재 저장소에 없다. `HONGIK_FMOD` 심볼이 없을 때는 설정 UI와 저장만 작동하고 실제 소리는 재생하지 않는다. `BackendAvailable`과 `Status`로 이 상태를 확인할 수 있다. 이 경로는 Unity AudioSource로 대체 재생하지 않으며 FMOD가 설치된 것처럼 표시하지 않는다.

2026-11-05 확인: FMOD 공식 2.04 안내는 UPM 이름 `com.fmod.fmod-for-unity`를 제공하지만, 현재 Unity registry 조회에서는 해당 패키지를 찾지 못했다. FMOD 다운로드 페이지는 로그인 요구 상태였으며 로컬 다운로드·Unity 캐시에서 FMOD 패키지를 찾지 못했다. 사용자가 설치·로그인은 지금 생략하도록 요청하여 진행하지 않았다. 패키지 manifest를 변경하지 않았으며 실제 플러그인 설치와 음원 재생 검증은 아직 완료하지 못했다.

## 연결 절차

1. [공식 FMOD 다운로드](https://www.fmod.com/download) 또는 [공식 Unity Asset Store 패키지](https://assetstore.unity.com/packages/tools/audio/fmod-for-unity-2-02-161631)에서 팀이 사용하는 버전의 통합 패키지를 설치한다. FMOD 계정 로그인 및 필요한 사용 조건 확인은 사용자 계정에서 수행한다.
2. FMOD Studio 프로젝트에서 학교 BGM·전투 BGM·UI SFX·Voice 이벤트를 만들고 bank를 빌드한다. 이 저장소에는 실제 음악·목소리 콘텐츠가 포함되지 않는다.
3. Unity FMOD 설정에 bank 위치를 연결하고 Master bank와 strings bank를 포함해 필요한 bank를 로드한다. 문자열 이벤트 경로 조회에는 strings bank가 필요하다.
4. 대상 플랫폼의 Player Settings → Scripting Define Symbols에 `HONGIK_FMOD`를 추가한다. 플러그인이 없는 상태에서 이 심볼을 켜면 컴파일할 수 없다.
5. 학교와 전투에서 아래 API로 실제 존재하는 이벤트 경로를 전달한다. 경로 예시는 콘텐츠 제작 시 맞춰야 하며 현재 존재하는 이벤트를 뜻하지 않는다.

```csharp
using HongikIngan.Audio;

var sound = SoundManager.EnsureExists();
sound.SetVolume(AudioCategory.BGM, 0.5f);
sound.SetMuted(AudioCategory.Voice, true);
sound.PlayBgm("event:/BGM/School");
sound.PlayOneShot("event:/SFX/Click", AudioCategory.SFX);
sound.PlayOneShot("event:/Voice/Attack", AudioCategory.Voice);
sound.SaveSettings();
```

## 동작 규칙

- `EnsureExists()`는 기존 관리자를 재사용한다. 중복 컴포넌트는 제거하고 루트 관리자는 `DontDestroyOnLoad`로 유지한다.
- `PlayBgm`은 새 FMOD EventInstance의 시작에 성공한 후 이전 BGM을 정지·해제한다. 동일 경로를 다시 요청하면 중복 생성하지 않는다.
- `PlayOneShot`은 재생 인스턴스와 카테고리를 추적한다. 재생 중인 SFX·Voice에도 볼륨·음소거 변경이 즉시 반영된다. 종료한 인스턴스는 release한다.
- 카테고리 볼륨은 FMOD 이벤트 인스턴스에 적용한다. 특정 bus 이름이나 믹서 구조를 강제하지 않는다. 이 관리자를 거치지 않고 재생한 FMOD 이벤트에는 설정이 적용되지 않는다.
- Voice/SFX에는 끝나는 이벤트를 전달한다. 루프 음악은 `PlayBgm`과 `StopBgm`으로 관리한다.
- 0~1 이외 볼륨은 범위에 맞추고 NaN/Infinity 입력은 무시한다.
- 설정은 변경 시 PlayerPrefs에 반영하고 설정 창 닫기·앱 일시정지·종료 시 디스크에 저장한다. 설정 UI는 닫힐 때 `SaveSettings()`를 호출한다.
- FMOD event 또는 bank가 없으면 재생은 false를 반환하고 원인을 Status에 남긴다.

## 검증 항목

설치 전에는 씬을 오가며 동일 SoundManager와 카테고리 설정 유지, 음소거 해제 시 볼륨 복원, 앱 재실행 후 설정 복원을 확인한다. 실제 FMOD 플러그인과 bank 연결 후에는 세 카테고리별 소리를 동시에 재생해 각각의 볼륨·음소거가 다른 카테고리를 바꾸지 않는지 확인하고, 학교→전투→학교에서 BGM 중복 재생과 EventInstance 누수가 없는지 확인한다.

자료: [FMOD Unity 설치·bank 연결 안내](https://www.fmod.com/docs/unity/user-guide.html), [문자열 이벤트 조회 문제와 strings bank](https://www.fmod.com/docs/unity/troubleshooting.html), [공식 소스 저장소의 native binaries 제외 안내](https://github.com/fmod/fmod-for-unity).
