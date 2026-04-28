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
    }

    [Serializable]
    public class ParticleEffectConfig
    {
        public List<ParticleEffectData> effects;
    }
}
