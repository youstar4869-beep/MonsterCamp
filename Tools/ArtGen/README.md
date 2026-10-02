# 크리처 픽셀 그림 생성기

크리처 그림(32x32, Lv1~3)을 그리는 Python 스크립트예요. 새 크리처도 같은 그림체로 그릴 때 써요.

- `px.py` 그리기 엔진(도형 → 음영·외곽선 자동)
- `lib.py` 타입별 팔레트, 눈·입 모양
- `c_fire.py` 등 타입별 크리처 그림 함수, 맨 아래 `CREATURES`에 등록

실행: `pip install pillow` 후 이 폴더에서 `mkdir out && python build.py`
→ `out/{영문키}_Lv1~3.png`를 `Assets/_Project/Art/Creatures/`로 복사
