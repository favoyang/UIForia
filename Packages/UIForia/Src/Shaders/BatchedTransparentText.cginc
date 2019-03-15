      
#define gWeightNormal _globalFontData1.x
#define gWeightBold _globalFontData1.y
#define gFontTextureWidth _globalFontData1.z
#define gFontTextureHeight _globalFontData1.w
#define gGradientScale _globalFontData2.x
#define gScaleRatioA _globalFontData2.y
#define gScaleRatioB _globalFontData2.z
#define gScaleRatioC _globalFontData2.w
#define _ScaleX 1
#define _ScaleY 1
#define _FaceDilate 0
#define _GlowOuter 0
#define _GlowOffset 0
#define _UnderlayColor 0
#define _UnderlaySoftness 0
#define _UnderlayDilate 0
#define _UnderlayOffsetX 0
#define _UnderlayOffsetY 0
           
fixed4 GetColor(half d, fixed4 faceColor, fixed4 outlineColor, half outline, half softness) {
    half faceAlpha = 1 - saturate((d - outline * 0.5 + softness * 0.5) / (1.0 + softness));
    half outlineAlpha = saturate((d + outline * 0.5)) * sqrt(min(1.0, outline));

    faceColor.rgb *= faceColor.a;
    outlineColor.rgb *= outlineColor.a;

    faceColor = lerp(faceColor, outlineColor, outlineAlpha);

    faceColor *= faceAlpha;

    return faceColor;
}
        
inline float4 EncodeToFloat4(float v) {
    v = clamp(v, 0, 0.99999); // this conversion works only with [0, 1)
    float4 kEncodeMul = float4(1.0, 255.0, 65025.0, 16581375.0);
    float kEncodeBit = 1.0 / 255.0;
    float4 enc = kEncodeMul * v;
    enc = frac(enc);
    enc -= enc.yzww * kEncodeBit;
    return enc;
}
          
v2f TextVertex(appdata input) {
           
   // float outlineWidth = input.uv2.x;
   // float outlineSoftness = 0;//input.uv2.y;
   // 
   // float4 vPosition = UnityObjectToClipPos(input.vertex);
   // float2 pixelSize = vPosition.w;
   // 
   // pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
   // float scale = rsqrt(dot(pixelSize, pixelSize));
   // scale *= abs(input.uv.z) * gGradientScale * 1.5; 
   // 
   // int bold = 0;
   // 
   // float weight = lerp(gWeightNormal, gWeightBold, 0) / 4.0;
   // weight = (weight + _FaceDilate) * gScaleRatioA * 0.5;
   // 
   // float bias =(.5 - weight) + (.5 / scale);
   // float alphaClip = (1.0 - outlineWidth * gScaleRatioA - outlineSoftness * gScaleRatioA);
   // 
   // alphaClip = min(alphaClip, 1.0 - _GlowOffset * gScaleRatioB - _GlowOuter * gScaleRatioB);
   // alphaClip = alphaClip / 2.0 - ( .5 / scale) - weight;
   // 
   // float4 underlayColor = _UnderlayColor;
   // underlayColor.rgb *= underlayColor.a;
   // 
   // float bScale = scale;
   // bScale /= 1 + ((_UnderlaySoftness * gScaleRatioC) * bScale);
   // float bBias = (0.5 - weight) * bScale - 0.5 - ((_UnderlayDilate *  gScaleRatioC) * 0.5 * bScale);
   // 
   // float x = -(_UnderlayOffsetX *  gScaleRatioC) * gGradientScale / gFontTextureWidth;
   // float y = -(_UnderlayOffsetY *  gScaleRatioC) * gGradientScale / gFontTextureHeight;
   // float2 bOffset = float2(x, y);
   //     
   // float4 outlineColor = input.uv3;
    
    v2f o;
    o.vertex = UnityObjectToClipPos(input.vertex);
    o.color = input.color;
    o.uv = input.uv;
    o.flags = float4(RenderType_Text, 0, 0, 0);
    o.fragData1 = float4(input.vertex.xy, input.uv2.x, 0);
    o.fragData2 =  float4(0, 0, 0, 0); //alphaClip, scale, bias, weight);
    o.fragData3 = float4(0, 0, 0, 0);
    
    return o;
}

inline float contour(float d, float w) {
    return smoothstep(0.5 - w, 0.5 + w, d);
}

inline float samp(float2 uv, float w) {
    return contour(tex2D(_globalFontTexture, uv).a, w);
}

fixed4 TextFragment(v2f input) {
   float c = tex2D(_globalFontTexture, input.uv.xy).a;
   
   // this is the text mesh pro version of text. also requires a blend mode setting of 1 - (1 - srcAlpha)
   if(0) {
       // hard coded params for font size 15, change this to read from vertex output
       float4 param = float4(0.36486, 3.69998, 0.63514, 0);
       float	scale	= param.y;
       float	bias	= param.z;
       float	weight	= param.w;
       float	sd = (bias - c) * scale;
       float sca = 0.9;
       float outline = 0; //(_OutlineWidth * sca) * scale;
       float softness = 0; //(_OutlineSoftness * sca) * scale;
       half4 faceColor = half4(1, 1, 1, 1);//_FaceColor;
       half4 outlineColor = half4(1, 1, 1, 1); //_OutlineColor;
       faceColor.rgb *= input.color.rgb;
       
       faceColor = GetColor(sd, faceColor, outlineColor, outline, softness);
       return faceColor * input.color.a;
   }
   
   float smoothing = 1 / 16.0;   
   float alpha = smoothstep(0.5 - smoothing, 0.5 + smoothing, c);
   return fixed4(input.color.rgb, input.color.a * alpha);
}

#undef gWeightNormal
#undef gWeightBold
#undef gFontTextureWidth
#undef gFontTextureHeight
#undef gGradientScale
#undef gScaleRatioA
#undef gScaleRatioB
#undef gScaleRatioC
#undef _ScaleX 
#undef _ScaleY 
#undef _FaceDilate 
#undef _OutlineWidth 
#undef _OutlineSoftness 
#undef _GlowOuter 
#undef _GlowOffset 
#undef _UnderlayColor
#undef _UnderlaySoftness 
#undef _UnderlayDilate
#undef _UnderlayOffsetX 
#undef _UnderlayOffsetY 