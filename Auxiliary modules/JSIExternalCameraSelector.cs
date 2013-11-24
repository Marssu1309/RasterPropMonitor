using UnityEngine;

namespace JSI
{
	public class JSIExternalCameraSelector: PartModule
	{
		// Actual configuration parameters.
		[KSPField]
		public string cameraContainer;
		[KSPField]
		public string cameraIDPrefix = "ExtCam";
		[KSPField]
		public int maximum = 8;
		// Internal data storage.
		[KSPField(isPersistant = true)]
		public int current = 1;
		// Fields to handle right-click GUI.
		[KSPField(guiActive = true, guiName = "Camera ID: ")]
		public string visibleCameraName;
		private GameObject lightCone;
		private LineRenderer lightConeRenderer;
		private static readonly Material lightConeMaterial = new Material(Shader.Find("Particles/Additive"));
		private Transform actualCamera;
		private const float endSpan = 15f;
		private const float fovAngle = 60f;

		[KSPEvent(guiActive = true, guiName = "ID+")]
		public void IdPlus()
		{
			current++;
			if (current > maximum)
				current = 1;
			UpdateName();
		}

		[KSPEvent(guiActive = true, guiName = "ID-")]
		public void IdMinus()
		{
			current--;
			if (current <= 0)
				current = maximum;
			UpdateName();
		}

		private void UpdateName()
		{
			Transform containingTransform = part.FindModelTransform(cameraContainer);
			foreach (Transform thatTransform in containingTransform.gameObject.GetComponentsInChildren<Transform>()) {
				if (containingTransform != thatTransform) {
					actualCamera = thatTransform;
					break;
				}
			}
			// I'm amused to find that this does appear to work.
			if (actualCamera == null) {
				actualCamera = new GameObject().transform;
				actualCamera.position = containingTransform.position;
				actualCamera.rotation = containingTransform.rotation;
				actualCamera.parent = containingTransform;
			}
			visibleCameraName = actualCamera.name = cameraIDPrefix + current;
			if (HighLogic.LoadedSceneIsEditor)
				ScreenMessages.PostScreenMessage("Camera ID: " + visibleCameraName, 3, ScreenMessageStyle.UPPER_RIGHT);
		}

		public override void OnStart(PartModule.StartState state)
		{
			UpdateName();
			if (state == StartState.Editor) {
				CreateLightCone();
				part.OnEditorAttach += new Callback(DestroyLightCone);
				part.OnEditorDetach += new Callback(PickupCamera);
			} else {
				DestroyLightCone();
			}
		}

		private void PickupCamera()
		{
			UpdateName();
			CreateLightCone();
		}

		private void CreateLightCone()
		{
			RenderingManager.AddToPostDrawQueue(0, DrawLightCone);
			if (lightConeRenderer == null) {
				lightCone = new GameObject();
				lightConeRenderer = lightCone.AddComponent<LineRenderer>();
				lightConeRenderer.useWorldSpace = true;
				lightConeRenderer.material = lightConeMaterial;
				lightConeRenderer.SetColors(new Color(0f, 0f, 1f, 1f), new Color(0f, 0f, 1f, 0f));
				lightConeRenderer.SetWidth(0.054f, endSpan);
				lightConeRenderer.SetVertexCount(2);
				lightConeRenderer.castShadows = false;
				lightConeRenderer.receiveShadows = false;
				lightConeRenderer.SetPosition(0, Vector3.zero);
				lightConeRenderer.SetPosition(1, Vector3.zero);
			}
		}

		private void DestroyLightCone()
		{
			RenderingManager.RemoveFromPostDrawQueue(0, DrawLightCone);
			if (lightConeRenderer != null) {
				Destroy(lightConeRenderer);
				lightConeRenderer = null;
				Destroy(lightCone);
				lightCone = null;
			}
		}

		public void Update()
		{
			if (!HighLogic.LoadedSceneIsEditor)
				return;

			if (Input.GetKeyDown(KeyCode.Space)) {
				RaycastHit whereAmI;
				Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out whereAmI);
				if (Part.FromGO(whereAmI.transform.gameObject) == part) {
					IdPlus();
				}
			}
		}

		public void DrawLightCone()
		{
			if (!HighLogic.LoadedSceneIsEditor) {
				DestroyLightCone();
			}
			if (lightConeRenderer != null) {
				Vector3 origin = actualCamera.transform.TransformPoint(Vector3.zero);
				Vector3 direction = actualCamera.transform.TransformDirection(Vector3.forward);
				lightConeRenderer.SetPosition(0, origin);
				lightConeRenderer.SetPosition(1, origin + direction * (endSpan / 2 / Mathf.Tan(Mathf.Deg2Rad * fovAngle / 2)));
			}
		}
	}
}

