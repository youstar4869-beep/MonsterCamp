# CLAUDE.md

## 프로젝트
- 이름: 몬스터 캠프 (가제)
- 장르: 크리처 수집 PvE 오토배틀러 (기획: GAME_DESIGN.md)
- 엔진: Unity 6 LTS, 2D (URP), 타겟: Android, 세로 1080x1920 기준

## 폴더 구조
Assets/_Project/
- Scripts/Core      게임 흐름, 런/라운드 관리, 저장
- Scripts/Creatures 크리처 데이터·인스턴스·합성
- Scripts/Battle    자동 전투
- Scripts/Shop      상점
- Scripts/UI        화면/버튼
- Scripts/Data      ScriptableObject 정의
- Data              ScriptableObject 에셋 (크리처, 트레이너, 적 팀)
- Prefabs, Scenes, Art, Audio

## 코딩 규칙
- 한 번에 한 기능, 구현 후 테스트, 최소 변경
- Update() 남용 금지, GetComponent/Find 결과는 캐싱
- 전투 중 반복 생성되는 오브젝트(투사체, 데미지 숫자)는 풀링
- 밸런스 수치는 코드가 아니라 ScriptableObject에 둔다
- 불필요한 추상화·디자인 패턴 금지

## 문서 유지
- 기능 완료 시 TODO.md 체크, 중요한 변경은 CHANGELOG.md 기록
- 기획 변경은 사용자 확인 후 GAME_DESIGN.md 수정
