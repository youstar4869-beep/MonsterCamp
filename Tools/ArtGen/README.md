# 크리처 픽셀 그림 생성기

크리처 그림(32x32, Lv1~3)을 그리는 Python 스크립트예요. 새 크리처도 같은 그림체로 그릴 때 써요.

- `px.py` 그리기 엔진(도형 → 음영·외곽선 자동)
- `lib.py` 타입별 팔레트, 눈·입 모양
- `c_fire.py` 등 타입별 크리처 그림 함수(처음 15종), 맨 아래 `CREATURES`에 등록
- `n_fire.py` 등 1~5골드 등급으로 추가한 20종
- `f_low.py`·`f_mid.py`·`f_cross.py`·`f_top.py` 조합 크리처 17종 (`python build.py f_low f_mid f_cross f_top`)
- `items.py` 아이템 6종 아이콘 16x16 (`python items.py` → `Assets/_Project/Art/Items/`에 바로 저장, 미리보기 `out/items_preview.png`)

실행: `pip install pillow` 후 이 폴더에서 `mkdir out && python build.py` (새 20종까지: `python build.py c_fire c_water c_grass c_elec c_rock n_fire n_water n_grass n_elec n_rock`)
→ `out/{영문키}_Lv1~3.png`를 `Assets/_Project/Art/Creatures/`로 복사
