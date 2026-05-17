using System;
using System.Collections.Generic;
using UnityEngine;

namespace IdleGame.Data
{
    [Serializable]
    public class ParticleColorData
    {
        public float r, g, b, a;
        public Color ToColor() => new Color(r, g, b, a);
    }

    [Serializable]
    public class ParticleEffectData
    {
        public string id;
        public string comment;

        // 방출 기본값
        public int burstCount;
        public float startLifetime;
        public float startSpeedMin;
        public float startSpeedMax;
        public float startSizeMin;
        public float startSizeMax;
        public float gravityModifier;

        // 색상
        public ParticleColorData color;
        public ParticleColorData colorEnd;

        // 형태 (sphere / edge / cone)
        public string shapeType;
        public float shapeRadius;
        public float shapeAngle;     // cone 전용

        // 렌더 (billboard / stretched)
        public string renderMode;
        public float velocityScale;  // stretched 전용
        public float lengthScale;    // stretched 전용

        // 라이프타임 커브
        public bool sizeOverLifetime;
        public bool colorFadeOut;

        // 이펙트 타입 (burst / magic_circle / slash_line / stab_line)
        public string effectType;
        public float spawnInterval;   // 순차 스폰 간격(초)
        public float fallSpeed;       // 튕김/발사 속도

        // magic_circle 전용
        public float circleRadius;

        // slash_line 전용
        public float lineLength;      // 슬래시 선 길이
        public float lineAngle;       // 슬래시 각도 (도, 기본 45)

        // stab_line 전용
        public float stabLength;      // 찌르기 선 길이

        // 텍스처/스프라이트 시트
        public string materialPath;      // Resources 하위 경로 (확장자 제외), 비어있으면 기본 머티리얼
        public int sheetColumns;         // 스프라이트 시트 열 수 (0이면 시트 미사용)
        public int sheetRows;            // 스프라이트 시트 행 수
        public bool sheetRandomFrame;    // true: 파티클마다 랜덤 프레임 고정 / false: 순차 재생
        public float sheetFrameOverTime; // 순차 재생 시 속도 배율 (sheetRandomFrame=false일 때만 사용)
    }

    [Serializable]
    public class ParticleEffectConfig
    {
        public List<ParticleEffectData> effects;
    }
}
