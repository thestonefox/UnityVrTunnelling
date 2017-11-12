using UnityEngine;
using System.Collections;

namespace Sigtrap.ImageEffects
{
    public class Tunnelling : MonoBehaviour
    {
        #region Public Fields
        [Header("Angular Velocity")]
        /// <summary>
        /// Angular velocity calculated for this Transform. DO NOT USE HMD!
        /// </summary>
        [Tooltip("Angular velocity calculated for this Transform.\nDO NOT USE HMD!")]
        public Transform refTransform;

        /// <summary>
        /// Below this angular velocity, effect will not kick in. Degrees per second
        /// </summary>
        [Tooltip("Below this angular velocity, effect will not kick in.\nDegrees per second")]
        public float minAngVel = 0f;

        /// <summary>
        /// At/above this angular velocity, effect will be maxed out. Degrees per second
        /// </summary>
        [Tooltip("At/above this angular velocity, effect will be maxed out.\nDegrees per second")]
        public float maxAngVel = 180f;

        /// <summary>
        /// Below this speed, effect will not kick in.
        /// </summary>
        [Tooltip("Below this speed, effect will not kick in.")]
        public float minSpeed = 0f;

        /// <summary>
        /// At/above this speed, effect will be maxed out.
        /// </summary>
        [Tooltip("At/above this speed, effect will be maxed out.\nSet negative for no effect.")]
        public float maxSpeed = -1f;

        [Header("Effect Settings")]
        /// <summary>
        /// Screen coverage at max angular velocity.
        /// </summary>
        [Range(0f, 1f)]
        [Tooltip("Screen coverage at max angular velocity.\n(1-this) is radius of visible area at max effect (screen space).")]
        public float maxEffect = 0.75f;

        /// <summary>
        /// Feather around cut-off as fraction of screen.
        /// </summary>
        [Range(0f, 0.5f)]
        [Tooltip("Feather around cut-off as fraction of screen.")]
        public float feather = 0.1f;

        /// <summary>
        /// Smooth out radius over time. 0 for no smoothing.
        /// </summary>
        [Tooltip("Smooth out radius over time. 0 for no smoothing.")]
        public float smoothTime = 0.15f;

        /// <summary>
        /// An optional CubeMap texture for the cage skybox.
        /// </summary>
        [Tooltip("An optional CubeMap texture for the cage skybox.")]
        public Texture cageSkybox;
        #endregion

        #region Smoothing
        private float _avSlew;
        private float _av;
        #endregion

        #region Shader property IDs
        private int _propAV;
        private int _propFeather;
        private int _propCageCubeMap;
        #endregion

        #region Misc Fields
        private Vector3 _lastFwd;
        private Vector3 _lastPos;
        private Material _m;
        private Camera _camera;
        #endregion

        #region Messages
        void Awake()
        {
            _m = new Material(Shader.Find("Hidden/Tunnelling"));
            _camera = Camera.main;
            if (refTransform == null)
            {
                refTransform = transform;
            }

            _propAV = Shader.PropertyToID("_AV");
            _propFeather = Shader.PropertyToID("_Feather");
            _propCageCubeMap = Shader.PropertyToID("_CageCubeMap");
            SetCageSkyboxTexture();
        }

        void SetCageSkyboxTexture()
        {
            //If not cage skybox texture is provided, create a temporary white texture for blending
            if (cageSkybox == null)
            {
                Cubemap tempTexture = new Cubemap(1, TextureFormat.ARGB32, false);
                tempTexture.SetPixel(CubemapFace.NegativeX, 0, 0, Color.white);
                tempTexture.SetPixel(CubemapFace.NegativeY, 0, 0, Color.white);
                tempTexture.SetPixel(CubemapFace.NegativeZ, 0, 0, Color.white);
                tempTexture.SetPixel(CubemapFace.PositiveX, 0, 0, Color.white);
                tempTexture.SetPixel(CubemapFace.PositiveY, 0, 0, Color.white);
                tempTexture.SetPixel(CubemapFace.PositiveZ, 0, 0, Color.white);
                cageSkybox = tempTexture;
            }
            _m.SetTexture(_propCageCubeMap, cageSkybox);
        }

        void Update()
        {
            Vector3 fwd = refTransform.forward;
            float av = Vector3.Angle(_lastFwd, fwd) / Time.deltaTime;
            av = (av - minAngVel) / (maxAngVel - minAngVel);

            Vector3 pos = refTransform.position;

            if (maxSpeed > 0)
            {
                float speed = (pos - _lastPos).magnitude / Time.deltaTime;
                speed = (speed - minSpeed) / (maxSpeed - minSpeed);

                if (speed > av)
                {
                    av = speed;
                }
            }

            av = Mathf.Clamp01(av) * maxEffect;

            _av = Mathf.SmoothDamp(_av, av, ref _avSlew, smoothTime);

            _m.SetFloat(_propAV, _av);
            _m.SetFloat(_propFeather, feather);
            SetCageView();

            _lastFwd = fwd;
            _lastPos = pos;
        }

        void SetCageView()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                return;
            }

            if (cageSkybox != null)
            {
                _m.SetMatrixArray("_EyeToWorld", new Matrix4x4[2]{
                    _camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left).inverse,
                    _camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right).inverse
                });

                Matrix4x4[] eyeProjection = new Matrix4x4[2];
                eyeProjection[0] = _camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                eyeProjection[1] = _camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
                eyeProjection[0] = GL.GetGPUProjectionMatrix(eyeProjection[0], true).inverse;
                eyeProjection[1] = GL.GetGPUProjectionMatrix(eyeProjection[1], true).inverse;
                eyeProjection[0][1, 1] *= -1f;
                eyeProjection[1][1, 1] *= -1f;
                _m.SetMatrixArray("_EyeProjection", eyeProjection);
            }
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            Graphics.Blit(src, dest, _m);
        }

        void OnDestroy()
        {
            Destroy(_m);
        }
        #endregion
    }
}
