using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UObject = UnityEngine.Object;

namespace UnityExtensions
{
    /// <summary>
    /// RuntimeUtilities
    /// </summary>
    public struct RuntimeUtilities
    {
        static GameObject _globalGameObject;
        static IEnumerable<Type> _allAssemblyTypes;

        public static event Action fixedUpdate;
        public static event Action waitForFixedUpdate;
        public static event Action update;
        public static event Action lateUpdate;
        public static event Action waitForEndOfFrame;

        public static event Action fixedUpdateOnce;
        public static event Action waitForFixedUpdateOnce;
        public static event Action updateOnce;
        public static event Action lateUpdateOnce;
        public static event Action waitForEndOfFrameOnce;

        /// <summary>
        /// An update callback can be used in edit-mode
        /// Note: works with unitedDeltaTime
        /// </summary>
        public static event Action unitedUpdate;

        /// <summary>
        /// An update callback (will be executed once) can be used in edit-mode
        /// </summary>
        public static event Action unitedUpdateOnce;

        /// <summary>
        /// Get deltaTime in unitedUpdate
        /// </summary>
        public static float unitedDeltaTime
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return Editor.EditorUtilities.unscaledDeltaTime * Time.timeScale;
                else
#endif
                    return Time.deltaTime;
            }
        }

        /// <summary>
        /// Get unscaled deltaTime in unitedUpdate
        /// </summary>
        public static float unitedUnscaledDeltaTime
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    return Editor.EditorUtilities.unscaledDeltaTime;
                else
#endif
                    return Time.unscaledDeltaTime;
            }
        }

        /// <summary>
        /// allAssemblyTypes
        /// </summary>
        public static IEnumerable<Type> allAssemblyTypes
        {
            get
            {
                if (_allAssemblyTypes == null)
                {
                    _allAssemblyTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(
                        t =>
                        {
                            try
                            {
                                return t.GetTypes();
                            }
                            catch
                            {
                                return new Type[0];
                            }
                        });
                }
                return _allAssemblyTypes;
            }
        }

        /// <summary>
        /// MaterialPropertyBlock pool
        /// </summary>
        public static readonly ObjectPool<MaterialPropertyBlock> materialPropertyBlockPool = new ObjectPool<MaterialPropertyBlock>();

        /// <summary>
        /// FixedUpdate times
        /// </summary>
        public static int fixedFrameCount { get; private set; }

        public static float GetUnitedDeltaTime(TimeMode mode)
        {
            switch (mode)
            {
                case TimeMode.Normal: return unitedDeltaTime;
                case TimeMode.Unscaled: return unitedUnscaledDeltaTime;
                case TimeMode.FixedInternal: return 1f / Application.targetFrameRate;
                default: return unitedDeltaTime;
            }
        }

        public static void AddUpdate(UpdateMode mode, Action action)
        {
            switch (mode)
            {
                case UpdateMode.FixedUpdate: fixedUpdate += action; return;
                case UpdateMode.WaitForFixedUpdate: waitForFixedUpdate += action; return;
                case UpdateMode.Update: update += action; return;
                case UpdateMode.LateUpdate: lateUpdate += action; return;
                case UpdateMode.WaitForEndOfFrame: waitForEndOfFrame += action; return;
            }
        }

        public static void RemoveUpdate(UpdateMode mode, Action action)
        {
            switch (mode)
            {
                case UpdateMode.FixedUpdate: fixedUpdate -= action; return;
                case UpdateMode.WaitForFixedUpdate: waitForFixedUpdate -= action; return;
                case UpdateMode.Update: update -= action; return;
                case UpdateMode.LateUpdate: lateUpdate -= action; return;
                case UpdateMode.WaitForEndOfFrame: waitForEndOfFrame -= action; return;
            }
        }

        public static void Swap<T>(ref T a, ref T b)
        {
            T c = a;
            a = b;
            b = c;
        }

        public static bool IsNullOrEmpty<T>(T collection) where T : ICollection
        {
            return collection == null || collection.Count == 0;
        }

        /// <summary>
        /// Set time scale and FixedUpdate frequency at sametime.
        /// </summary>
        public static void SetTimeScaleAndFixedUpdateFrequency(float timeScale, float fixedFrequency)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = timeScale / fixedFrequency;
        }

        /// <summary>
        /// Destroy Unity Object in a safe way
        /// </summary>
        public static void Destroy(UObject obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UObject.DestroyImmediate(obj);
                else
#endif
                    UObject.Destroy(obj);
            }
        }

        /// <summary>
        /// Find a loaded scene
        /// </summary>
        /// <returns> The loaded scene, return default(Scene) if no matched </returns>
        public static Scene FindScene(Predicate<Scene> match)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (match(scene)) return scene;
            }
            return default;
        }

        /// <summary>
        /// Find a loaded scene
        /// </summary>
        /// <returns> index of loaded scene (use SceneManager.GetSceneAt to get the scene), return -1 if no matched </returns>
        public static int FindSceneIndex(Predicate<Scene> match)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (match(scene)) return i;
            }
            return -1;
        }

        /// <summary>
        /// Support binary reading/writing for Vector2, Vector3, Vector4, byte[]
        /// </summary>
        public static void RegisterCommonIOMethods()
        {
            IOExtensions.Register((BinaryWriter writer, Vector2 value) =>
            {
                writer.Write(value.x);
                writer.Write(value.y);
            });

            IOExtensions.Register((BinaryReader reader) =>
            {
                Vector2 value;
                value.x = reader.ReadSingle();
                value.y = reader.ReadSingle();
                return value;
            });

            IOExtensions.Register((BinaryWriter writer, Vector3 value) =>
            {
                writer.Write(value.x);
                writer.Write(value.y);
                writer.Write(value.z);
            });

            IOExtensions.Register((BinaryReader reader) =>
            {
                Vector3 value;
                value.x = reader.ReadSingle();
                value.y = reader.ReadSingle();
                value.z = reader.ReadSingle();
                return value;
            });

            IOExtensions.Register((BinaryWriter writer, Vector4 value) =>
            {
                writer.Write(value.x);
                writer.Write(value.y);
                writer.Write(value.z);
                writer.Write(value.w);
            });

            IOExtensions.Register((BinaryReader reader) =>
            {
                Vector4 value;
                value.x = reader.ReadSingle();
                value.y = reader.ReadSingle();
                value.z = reader.ReadSingle();
                value.w = reader.ReadSingle();
                return value;
            });

            IOExtensions.Register((BinaryWriter writer, byte[] value) =>
            {
                writer.Write(value.Length);
                writer.Write(value);
            });

            IOExtensions.Register((BinaryReader reader) =>
            {
                int length = reader.ReadInt32();
                return reader.ReadBytes(length);
            });
        }

        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            _globalGameObject = new GameObject("GlobalGameObject");
            _globalGameObject.AddComponent<GlobalComponent>();
            _globalGameObject.hideFlags = HideFlags.HideInHierarchy;
            _globalGameObject.transform.ResetLocal();
            UObject.DontDestroyOnLoad(_globalGameObject);
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void InitializeEditor()
        {
            UnityEditor.EditorApplication.update += () =>
            {
                if (!Application.isPlaying)
                {
                    unitedUpdate?.Invoke();

                    if (unitedUpdateOnce != null)
                    {
                        var call = unitedUpdateOnce;
                        unitedUpdateOnce = null;
                        call();
                    }
                }
            };
        }
#endif

        public class GlobalComponent : ScriptableComponent
        {
            void Start()
            {
                StartCoroutine(WaitForFixedUpdate());
                StartCoroutine(WaitForEndOfFrame());

                IEnumerator WaitForFixedUpdate()
                {
                    var wait = new WaitForFixedUpdate();
                    while (true)
                    {
                        yield return wait;

                        fixedFrameCount += 1;

                        waitForFixedUpdate?.Invoke();

                        if (waitForFixedUpdateOnce != null)
                        {
                            var call = waitForFixedUpdateOnce;
                            waitForFixedUpdateOnce = null;
                            call();
                        }
                    }
                }

                IEnumerator WaitForEndOfFrame()
                {
                    var wait = new WaitForEndOfFrame();
                    while (true)
                    {
                        yield return wait;
                        waitForEndOfFrame?.Invoke();

                        if (waitForEndOfFrameOnce != null)
                        {
                            var call = waitForEndOfFrameOnce;
                            waitForEndOfFrameOnce = null;
                            call();
                        }
                    }
                }
            }

            void FixedUpdate()
            {
                fixedUpdate?.Invoke();

                if (fixedUpdateOnce != null)
                {
                    var call = fixedUpdateOnce;
                    fixedUpdateOnce = null;
                    call();
                }
            }

            void Update()
            {
                update?.Invoke();
                unitedUpdate?.Invoke();

                if (updateOnce != null)
                {
                    var call = updateOnce;
                    updateOnce = null;
                    call();
                }

                if (unitedUpdateOnce != null)
                {
                    var call = unitedUpdateOnce;
                    unitedUpdateOnce = null;
                    call();
                }
            }

            void LateUpdate()
            {
                lateUpdate?.Invoke();

                if (lateUpdateOnce != null)
                {
                    var call = lateUpdateOnce;
                    lateUpdateOnce = null;
                    call();
                }
            }

        } // class GlobalComponent

    } // struct RuntimeUtilities

} // namespace UnityExtensions