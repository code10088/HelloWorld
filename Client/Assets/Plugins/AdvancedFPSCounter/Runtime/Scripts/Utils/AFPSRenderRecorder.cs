#region copyright
//-------------------------------------------------------
// Copyright (C) Dmitriy Yukhanov [https://codestage.net]
//-------------------------------------------------------
#endregion

namespace CodeStage.AdvancedFPSCounter.Utils
{
	using CountersData;
	using UnityEngine;
	using UnityEngine.Rendering;

	/// <summary>
	/// This is a helper class for the %AFPSCounter Render Time feature.
	/// </summary>
	/// It should be attached to the Camera to measure the approximate render time and report it to the current %AFPSCounter instance.
	/// <br/>You may use \link CodeStage.AdvancedFPSCounter.CountersData.FPSCounterData::RenderAutoAdd FPSCounterData.RenderAutoAdd \endlink 
	/// property to let %AFPSCounter add it automatically to the Camera with Main Camera tag.
	/// <br/>You also may add it by hand to all cameras you wish to measure.
	/// <br/><strong>\htmlonly<font color="7030A0">NOTE:</font>\endhtmlonly It doesn't take into account Image Effects and IMGUI!</strong>
	/// \sa \link CodeStage.AdvancedFPSCounter.CountersData.FPSCounterData::Render FPSCounterData.Render \endlink
	/// \sa \link CodeStage.AdvancedFPSCounter.CountersData.FPSCounterData::RenderAutoAdd FPSCounterData.RenderAutoAdd \endlink
	[DisallowMultipleComponent]
	public class AFPSRenderRecorder : MonoBehaviour
	{
		private static FPSCounterData currentListener;
		private static bool recording;
		private static float renderTime;
		
		public static void Add(FPSCounterData counter)
		{
			currentListener = counter;
			
			if (GraphicsSettings.defaultRenderPipeline == null) // built-in
			{
				var mainCamera = Camera.main;
				if (mainCamera == null) return;

				if (!mainCamera.TryGetComponent<AFPSRenderRecorder>(out _))
					mainCamera.gameObject.AddComponent<AFPSRenderRecorder>();
			}
			else // URP, HDRP etc.
			{
				Remove();
				RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
				RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
			}
		}

		public static void Remove()
		{
			if (GraphicsSettings.defaultRenderPipeline == null) // built-in
			{
				var mainCamera = Camera.main;
				if (mainCamera == null) return;

				if (mainCamera.TryGetComponent<AFPSRenderRecorder>(out var recorder))
					Destroy(recorder);
			}
			else // URP, HDRP etc.
			{
				RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
				RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
			}
		}
		
		private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			if (camera.cameraType != CameraType.Game)
				return;
			BeginRecording();
		}
		
		private static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			if (camera.cameraType != CameraType.Game)
				return;
			EndRecording();
		}
		
		private static void BeginRecording()
		{
			if (recording || currentListener == null) return;

#if UNITY_EDITOR
			if (!editorWarningFired && !currentListener.Render)
			{
				Debug.LogWarning(AFPSCounter.LogPrefix + "You have this AFPSRenderRecorder instance running, " +
				                 "but Render Time is disabled at the AFPSCounter.\n" +
				                 "It's a waste of resources. " +
				                 "Consider removing this instance or enable Render Time at the AFPSCounter.");
				editorWarningFired = true;
			}
#endif
			recording = true;
			renderTime = Time.realtimeSinceStartup;
		}
		
		private static void EndRecording()
		{
			if (!recording || currentListener == null) return;

			recording = false;
			renderTime = Time.realtimeSinceStartup - renderTime;
			currentListener.AddRenderTime(renderTime * 1000f);
		}

#if UNITY_EDITOR
		private static bool editorWarningFired = false;

		private void OnValidate()
		{
			var cam = GetComponent<Camera>();
			if (cam == null)
			{
				Debug.LogError(AFPSCounter.LogPrefix + "Look like this AFPSRenderRecorder instance is added to the Game Object without Camera! It will not work.", this);
			}
		}
#endif

		private void OnPreCull()
		{
			BeginRecording();
		}

		private void OnPostRender()
		{
			EndRecording();
		}
	}
}