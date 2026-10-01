# CHANGELOG.md

## [0.0.1] - 2026-10-01
- GAME_DESIGN.md 초안 v0.1 작성 (PvE, 10라운드, 목숨 3)
- 프로젝트 문서 초기화
- Assets/_Project 폴더 구조 생성, Unity용 .gitignore 보완, Git 저장소 초기화
- 기본 화면 전환 추가 (타이틀 → 트레이너 선택 → 상점·배치 → 전투 → 결과), 빈 화면에 제목·버튼만
  - `ScreenFlow`: 패널 하나만 켜는 방식, Main 씬 하나에서 전환
  - 메뉴 `MonsterCamp > Build Main Scene`으로 Main 씬 생성 (빌드 씬 목록을 Main 하나로 교체)
- 크리처 데이터 추가
  - `CreatureData` ScriptableObject: 이름, 타입, 티어, 체력, 공격력, 공격 속도, 스킬 설명, 레벨별 배율(Lv1~3), 그림
  - `CreatureType` 5종(불·물·풀·번개·바위), `TypeChart` 상성표 에셋 (유리 1.5배, 불리 0.75배)
  - 메뉴 `MonsterCamp > Create Initial Creature Data`로 상성표 + 초기 15종 + 임시 색깔 네모 그림 생성 (이미 있는 에셋은 건너뜀)
  - GAME_DESIGN.md 6-1에 티어 기준값·타입 보정·레벨 배율·15종 수치 초안 추가 (밸런스 조정 전)
- 상점 추가 (드래그 배치·합성·전투 제외)
  - `ShopScreen`: 상점 5칸 랜덤, 구매(가격 = 티어), 새로고침(1G), 대기석 6칸, 대기석 칸을 눌러 판매(+1G)
  - 시작 골드 10, 대기석이 꽉 차거나 골드가 부족하면 안내 문구
  - `ShopConfig` 에셋(시작 골드, 새로고침 비용, 판매 가격, 티어별 가격, 크리처 목록), `CreatureSlotView` UI 칸
  - `MainSceneBuilder`에 상점 화면 UI 추가 — Main 씬을 다시 만들어야 적용됨
  - 트레이너 선택의 "선택 완료"가 새 런 시작(골드·대기석 초기화)
