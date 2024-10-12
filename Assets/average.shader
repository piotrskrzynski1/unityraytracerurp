Shader "Unlit/Average"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Weight ("Blend Weight", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            uniform int NumRenderedFrames; // The number of accumulated frames
            uniform sampler2D prevRender;  // Previous frame's render texture
            uniform sampler2D currRender;  // Current frame's render texture

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample previous and current frame render textures

                float4 oldRender = tex2D(prevRender, i.uv); 
                float4 newRender = tex2D(currRender, i.uv);

                // Calculate dynamic weight based on number of frames rendered
                float frameCount = 1.0/(NumRenderedFrames+1.0);

                // Blend the previous and current frames using the correct running average
                float4 blendedColor = oldRender * (1.0-frameCount) + newRender*frameCount;

             // return blendedColor;
             /* if(oldRender.x==newRender.x&&oldRender.y==newRender.y&&oldRender.z==newRender.z){
                   return float4(1,0,0,1);
                   }else{return float4(1,1,1,1);} */

                   return blendedColor;
                        
            }
            ENDCG
        }
    }
}
