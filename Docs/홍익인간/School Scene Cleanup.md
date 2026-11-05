# School 씬 삭제 오브젝트 목록

- 날짜: 2026-11-06
- 대상: UnityProject/Assets/Scenes/School.unity
- 삭제: 185개 GameObject(하위 오브젝트 포함). 에셋 파일과 .meta는 삭제하지 않음.
- 기준: 새 이미지 또는 현재 UI로 대체된 숨겨진 장식이며, 유지되는 컴포넌트에서 삭제 대상에 대한 직렬화 참조가 없는 항목만 선택.
- 변경 전 삭제 대상 참조를 조사했다. Unity 실행·플레이 테스트 및 커밋은 수행하지 않았다.

## 유지한 오브젝트

| 오브젝트 | 유지 이유 |
| --- | --- |
| DragLayer | 자식이 없는 RectTransform이지만 학생 드래그 중 임시 부모로 사용한다. |
| MockupLayoutV4 / MockupLayoutV4_1 / ReadabilityV4_2 | 자식이 없는 적용 표식이지만 Editor 생성 도구가 이를 조회하여 중복 배치와 위치 초기화를 방지한다. |
| 비활성 팝업·학생증·DeskSlot6~8 | 플레이 중 조건에 따라 활성화하며 스크립트 참조가 있다. |
| Layout / ClassroomArchitecture / StudentDesks 등 부모 | 하위 UI의 배치와 계층을 관리한다. |
| 현재 표시 중인 이미지·버튼·텍스트·책 도형 | 장식 또는 조작·정보 표시 목적이 있다. |

## 개별 삭제 내역

경로는 School 씬 Hierarchy 기준이다. 하위 항목의 삭제 이유는 해당 비활성 장식 전체가 교체되었기 때문이다. 단순히 비활성이거나 사용자 스크립트가 없다는 이유로 삭제하지 않았다.

| 삭제 오브젝트 경로 | 삭제 이유 |
| --- | --- |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book0_0 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book0_1 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book0_2 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book0_3 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book0_4 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book0_5 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book0_6 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book0_7 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book0_8 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book0_9 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book1_0 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book1_1 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book1_2 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book1_3 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book1_4 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book1_5 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book1_6 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book1_7 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book1_8 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book1_9 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book2_0 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book2_1 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book2_2 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book2_3 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book2_4 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book2_5 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book2_6 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book2_7 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book2_8 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Book2_9 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Fill | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Shelf0 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Shelf1 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Bookshelf/Shelf2 | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Calendar | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Calendar/Caption | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Calendar/Fill | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/ClassDoor/DoorGlass | 문 스프라이트에 포함되어 중복되는 비활성 유리·채움·손잡이. |
| SchoolCanvas/Layout/ClassroomArchitecture/ClassDoor/DoorGlass/Fill | 문 스프라이트에 포함되어 중복되는 비활성 유리·채움·손잡이. |
| SchoolCanvas/Layout/ClassroomArchitecture/ClassDoor/Fill | 문 스프라이트에 포함되어 중복되는 비활성 유리·채움·손잡이. |
| SchoolCanvas/Layout/ClassroomArchitecture/ClassDoor/Handle | 문 스프라이트에 포함되어 중복되는 비활성 유리·채움·손잡이. |
| SchoolCanvas/Layout/ClassroomArchitecture/PolaroidBoard/Fill | 배경의 게시판 그림을 사용하므로 불필요한 비활성 채움 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/WallClock | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/WallClock/Fill | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/WallClock/HourHand | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/WallClock/MinuteHand | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/WallClock/Numbers | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Window | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Window/Fill | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Window/HorizontalFrame | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/ClassroomArchitecture/Window/VerticalFrame | 배경 이미지에 이미 포함되어 표시하지 않는 장식과 하위 도형. |
| SchoolCanvas/Layout/Floor | 교실 배경 스프라이트로 대체된 비활성 벽·바닥 도형. |
| SchoolCanvas/Layout/FormationCaption | 펫 팝업·게시판·현재 책상 배치로 대체된 비활성 이전 UI 잔여물. |
| SchoolCanvas/Layout/PetOfferPopup/Pet0/PetPlaceholder | 현재 제안 UI에서 숨긴 이전 비활성 장식; 버튼과 정보 라벨은 별도 유지. |
| SchoolCanvas/Layout/PetOfferPopup/Pet0/PetPlaceholder/Fill | 현재 제안 UI에서 숨긴 이전 비활성 장식; 버튼과 정보 라벨은 별도 유지. |
| SchoolCanvas/Layout/PetOfferPopup/Pet1/PetPlaceholder | 현재 제안 UI에서 숨긴 이전 비활성 장식; 버튼과 정보 라벨은 별도 유지. |
| SchoolCanvas/Layout/PetOfferPopup/Pet1/PetPlaceholder/Fill | 현재 제안 UI에서 숨긴 이전 비활성 장식; 버튼과 정보 라벨은 별도 유지. |
| SchoolCanvas/Layout/PetOfferPopup/Pet2/PetPlaceholder | 현재 제안 UI에서 숨긴 이전 비활성 장식; 버튼과 정보 라벨은 별도 유지. |
| SchoolCanvas/Layout/PetOfferPopup/Pet2/PetPlaceholder/Fill | 현재 제안 UI에서 숨긴 이전 비활성 장식; 버튼과 정보 라벨은 별도 유지. |
| SchoolCanvas/Layout/Pets | 펫 팝업·게시판·현재 책상 배치로 대체된 비활성 이전 UI 잔여물. |
| SchoolCanvas/Layout/Pets/Caption | 펫 팝업·게시판·현재 책상 배치로 대체된 비활성 이전 UI 잔여물. |
| SchoolCanvas/Layout/Pets/Fill | 펫 팝업·게시판·현재 책상 배치로 대체된 비활성 이전 UI 잔여물. |
| SchoolCanvas/Layout/Photos | 펫 팝업·게시판·현재 책상 배치로 대체된 비활성 이전 UI 잔여물. |
| SchoolCanvas/Layout/Photos/Fill | 펫 팝업·게시판·현재 책상 배치로 대체된 비활성 이전 UI 잔여물. |
| SchoolCanvas/Layout/RelicLockers/Caption | 칠판 UI에서 남은 비활성 채움·제목으로 현재 사물함 UI에서 사용하지 않음. |
| SchoolCanvas/Layout/RelicLockers/Fill | 칠판 UI에서 남은 비활성 채움·제목으로 현재 사물함 UI에서 사용하지 않음. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_4/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_4/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_4/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_4/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_4/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_5/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_5/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_5/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_5/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_5/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_6/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_6/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_6/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_6/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor0_6/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_0/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_0/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_0/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_0/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_0/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_1/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_1/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_1/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_1/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_1/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_2/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_2/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_2/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_2/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_2/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_3/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_3/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_3/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_3/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_3/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_4/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_4/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_4/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_4/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_4/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_5/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_5/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_5/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_5/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_5/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_6/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_6/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_6/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_6/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor1_6/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_0/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_0/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_0/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_0/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_0/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_1/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_1/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_1/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_1/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_1/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_2/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_2/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_2/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_2/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_2/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_3/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_3/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_3/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_3/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_3/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_4/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_4/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_4/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_4/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_4/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_5/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_5/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_5/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_5/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_5/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_6/Fill | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_6/Handle | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_6/Vent0 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_6/Vent1 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/LockerDoor2_6/Vent2 | 사물함 문 스프라이트에 포함된 비활성 채움·손잡이·통풍구. |
| SchoolCanvas/Layout/RelicLockers/RelicOffer0/RelicPlaceholder | 현재 제안 UI에서 숨긴 이전 비활성 장식; 버튼과 정보 라벨은 별도 유지. |
| SchoolCanvas/Layout/RelicLockers/RelicOffer0/RelicPlaceholder/Fill | 현재 제안 UI에서 숨긴 이전 비활성 장식; 버튼과 정보 라벨은 별도 유지. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot0/DeskLegRight | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot0/Desktop/Fill | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot1/DeskLegRight | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot1/Desktop/Fill | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot2/DeskLegRight | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot2/Desktop/Fill | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot3/DeskLegRight | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot3/Desktop/Fill | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot4/DeskLegRight | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot4/Desktop/Fill | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot5/DeskLegRight | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot5/Desktop/Fill | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot6/DeskLegRight | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot6/Desktop/Fill | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot7/DeskLegRight | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot7/Desktop/Fill | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot8/DeskLegRight | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentDesks/DeskSlot8/Desktop/Fill | 책상 스프라이트로 대체된 비활성 다리·상판 채움 도형. |
| SchoolCanvas/Layout/StudentIdCard/Binding | 학생증으로 개편한 뒤 사용하지 않는 비활성 상세 책 UI 장식. |
| SchoolCanvas/Layout/StudentIdCard/LeftPage | 학생증으로 개편한 뒤 사용하지 않는 비활성 상세 책 UI 장식. |
| SchoolCanvas/Layout/StudentIdCard/PortraitPlaceholder | 학생증으로 개편한 뒤 사용하지 않는 비활성 상세 책 UI 장식. |
| SchoolCanvas/Layout/StudentIdCard/PortraitPlaceholder/Fill | 학생증으로 개편한 뒤 사용하지 않는 비활성 상세 책 UI 장식. |
| SchoolCanvas/Layout/StudentIdCard/RightPage | 학생증으로 개편한 뒤 사용하지 않는 비활성 상세 책 UI 장식. |
| SchoolCanvas/Layout/WallPanel10 | 교실 배경 스프라이트로 대체된 비활성 벽·바닥 도형. |
| SchoolCanvas/Layout/WallPanel11 | 교실 배경 스프라이트로 대체된 비활성 벽·바닥 도형. |
| SchoolCanvas/Layout/WallPanel6 | 교실 배경 스프라이트로 대체된 비활성 벽·바닥 도형. |
| SchoolCanvas/Layout/WallPanel7 | 교실 배경 스프라이트로 대체된 비활성 벽·바닥 도형. |
| SchoolCanvas/Layout/WallPanel8 | 교실 배경 스프라이트로 대체된 비활성 벽·바닥 도형. |
| SchoolCanvas/Layout/WallPanel9 | 교실 배경 스프라이트로 대체된 비활성 벽·바닥 도형. |
| SchoolCanvas/Layout/WallRail | 교실 배경 스프라이트로 대체된 비활성 벽·바닥 도형. |
| SchoolCanvas/Layout/WallWainscot | 교실 배경 스프라이트로 대체된 비활성 벽·바닥 도형. |

[문서 목차](README.md)
