# PixelLight Multi-Slot + View×Scope Settings

## Per view × scope settings

Helicopter (and airplane/airport catalogs) store **independent** `PixelLightViewScopeSettings` keyed by:

`view (Top/Front/Back/Left/Right/Bottom) | scope (Airframe/Magneto) | magnetoIndex`

Changing View or Scope loads that bag’s properties (grid, pattern, colors, brush, paint frame). Edits do **not** overwrite other view/scope bags. Switching views keeps each bag’s fields; use **Pull mount → this view+scope bag** only when you intentionally want the live mount to seed the current bag. Prefer a **distinct pattern asset per bag** if paint frames must differ (shared `PixelLightPatternAsset` references share paint data).

Host asset: `PixelLightMultiSlotCatalog` (`Create → Locomotion/Civil/Pixel Light Multi Slot Catalog`).

Assigned on:

- `HelicopterVehicleRagdoll.pixelLightCatalog`
- `MagnetoHelicopterConfigurationAsset.pixelLightCatalog`
- `AirplaneVehicleRagdoll.pixelLightCatalog`
- Airport Pixel Light Designer catalog field

## Multi grid slots

`PixelLightMultiSlotCatalog.gridSlots` lists `PixelLightGridSlotEntry` rows (heli `HelicoptorGridSlotGameObject` and/or `PixelLightGridMountGameObject`).

**Placement** tab (heli) and **Airport Pixel Light Designer** show a **scrollable accordion** of slots (`PixelLightGridSlotAccordionDrawer`).

## Feature Budget

| Id | Display | Notes |
|----|---------|--------|
| `pixel_light` | PixelLight / Grid Slots | Perf scopes: `PixelLight`, `PixelLightRig`, `PixelLightOptic`, `PixelLightGridMount` |

`maxRecommendedSlots` on the catalog warns when slot count exceeds the soft cap (default 16). Auto granularity can reduce aesthetic PixelLight work via Feature Budget like other civil features.

See [`FeatureBudget.md`](../../SystemDrawer/docs/FeatureBudget.md).

## Radial / N×N minigrid brush

`PixelLightGridMountGameObject` and `PixelLightViewScopeSettings` share a radial stamp:

- **Centroid cell** pick + 9-way side dropdown (Center / Upper Left / Up / Upper Right / Right / Lower Right / Bottom / Left Bottom / Left)
- Optional `CustomRadialSideAsset` and **Preview configuration** (solved joints that match `startPostAnchor`)
- **N×N minigrid** — cells of the stamp sit on a ring around the centroid / side origin
- **Recursive block** — one nested minigrid around each outer cell
- CenterPost / Create Anchor Objects / `customAngle` / `customAngleObject` via `RadialBuildHost`

Heli PixelLight tab, Airplane PixelLight tab, and Airport accordion (`PixelLightGridSlotAccordionDrawer`) all draw `PixelLightRadialBrushDrawer`. No new Feature Budget id — still `pixel_light`.

**Garage door / chain:** `GarageChainDesignerWindow` and `GarageDoorDesignerWindow` reuse the same brush for link faces, axle placement, sprocket teeth, and door-piece mounts. See [GarageDoor.md](GarageDoor.md).

See [RadialBuild.md](../../BedogaGenerator/RadialBuild.md).

Releasing render texture that is set as Camera.targetTexture!

[FeatureBudget] BudgetMode: rolling CPU 944.59ms / target 16.67ms
UnityEngine.Debug:LogWarning (object)
FeatureBudgetGovernor:MaybeLogWarn (single,single) (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetGovernor.cs:95)
FeatureBudgetGovernor:Tick (single) (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetGovernor.cs:43)
FeatureBudgetRuntime:LateUpdate () (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetRuntime.cs:87)

[FeatureBudget] BudgetMode: rolling CPU 26.51ms / target 16.67ms
UnityEngine.Debug:LogWarning (object)
FeatureBudgetGovernor:MaybeLogWarn (single,single) (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetGovernor.cs:95)
FeatureBudgetGovernor:Tick (single) (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetGovernor.cs:43)
FeatureBudgetRuntime:LateUpdate () (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetRuntime.cs:87)

[FeatureBudget] BudgetMode: rolling CPU 22.26ms / target 16.67ms
UnityEngine.Debug:LogWarning (object)
FeatureBudgetGovernor:MaybeLogWarn (single,single) (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetGovernor.cs:95)
FeatureBudgetGovernor:Tick (single) (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetGovernor.cs:43)
FeatureBudgetRuntime:LateUpdate () (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetRuntime.cs:87)

[FeatureBudget] BudgetMode: rolling CPU 21.90ms / target 16.67ms
UnityEngine.Debug:LogWarning (object)
FeatureBudgetGovernor:MaybeLogWarn (single,single) (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetGovernor.cs:95)
FeatureBudgetGovernor:Tick (single) (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetGovernor.cs:43)
FeatureBudgetRuntime:LateUpdate () (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetRuntime.cs:87)

Account API did not become accessible within 30 seconds. This may be due to network issues or editor focus.
UnityEngine.Debug:LogWarning (object)
Unity.AI.Toolkit.Accounts.Services.States.ApiAccessibleState/<WaitForCloudProjectSettings>d__3:MoveNext () (at ./Library/PackageCache/com.unity.ai.toolkit@b9677ce01ef8/Modules/Accounts/Services/States/ApiAccessibleState.cs:33)
System.Threading.Tasks.TaskCompletionSource`1<bool>:TrySetResult (bool)
Unity.AI.Toolkit.EditorTask/<>c__DisplayClass16_0:<WaitForCondition>b__0 () (at ./Library/PackageCache/com.unity.ai.toolkit@b9677ce01ef8/Modules/Async/EditorTask.cs:358)
UnityEditor.EditorApplication:Internal_CallUpdateFunctions ()

[FeatureBudget] BudgetMode: rolling CPU 17.92ms / target 16.67ms
UnityEngine.Debug:LogWarning (object)
FeatureBudgetGovernor:MaybeLogWarn (single,single) (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetGovernor.cs:95)
FeatureBudgetGovernor:Tick (single) (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetGovernor.cs:43)
FeatureBudgetRuntime:LateUpdate () (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetRuntime.cs:87)

[FeatureBudget] BudgetMode: rolling CPU 18.80ms / target 16.67ms
UnityEngine.Debug:LogWarning (object)
FeatureBudgetGovernor:MaybeLogWarn (single,single) (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetGovernor.cs:95)
FeatureBudgetGovernor:Tick (single) (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetGovernor.cs:43)
FeatureBudgetRuntime:LateUpdate () (at Assets/SystemDrawer/FeatureBudget/FeatureBudgetRuntime.cs:87)

Releasing render texture that is set as Camera.targetTexture!

Destroy may not be called from edit mode! Use DestroyImmediate instead.
Destroying an object in edit mode destroys it permanently.
UnityEngine.Object:Destroy (UnityEngine.Object)
PixelLightRig:EnsureLuminanceTexture () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:118)
PixelLightRig:PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:131)
MagnetoHelicopterDesignerWindow:ApplyFrameScrubToMount (PixelLightViewScopeSettings) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow:DrawFrameScrubber (PixelLightViewScopeSettings,PixelLightLayer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow:DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow:OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPathWithCompatibilityEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.EventBase compatibilityEvt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToCapturingElementOrElementUnderPointer (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.PointerUpEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPath (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.WheelEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.DoIMGUIRepaint () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderChainCommand.ExecuteNonDrawMesh (UnityEngine.UIElements.UIR.DrawParams drawParams, System.Single pixelsPerPoint, System.Exception& immediateException) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
Rethrow as ImmediateModeException
UnityEngine.UIElements.UIR.RenderTreeManager.RenderSingleTree (UnityEngine.UIElements.UIR.RenderTree renderTree, UnityEngine.RenderTexture nestedTreeRT, UnityEngine.RectInt nestedTreeViewport, UnityEngine.Rect bounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIR.RenderTreeManager.RenderRootTree () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIRRepaintUpdater.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.Panel.Render () (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPathWithCompatibilityEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.EventBase compatibilityEvt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToElementUnderPointerOrPanelRoot (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToCapturingElementOrElementUnderPointer (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.PointerDownEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, UnityEngine.Matrix4x4 worldTransform, UnityEngine.Rect clippingRect, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleIMGUIEvent (UnityEngine.Event e, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUIRaw (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.SendEventToIMGUI (UnityEngine.UIElements.EventBase evt, System.Boolean canAffectFocus, System.Boolean verifyBounds) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.IMGUIContainer.HandleEventBubbleUp (UnityEngine.UIElements.EventBase evt) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.HandleEventAcrossPropagationPathWithCompatibilityEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.EventBase compatibilityEvt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.VisualElement target, System.Boolean isCapturingTarget) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatchUtilities.DispatchToCapturingElementOrElementUnderPointer (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, System.Int32 pointerId, UnityEngine.Vector2 position) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.PointerMoveEvent.Dispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.ProcessEvent (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.EventDispatcher.Dispatch (UnityEngine.UIElements.EventBase evt, UnityEngine.UIElements.BaseVisualElementPanel panel, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.BaseVisualElementPanel.SendEvent (UnityEngine.UIElements.EventBase e, UnityEngine.UIElements.DispatchMode dispatchMode) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.DoDispatch (UnityEngine.UIElements.BaseVisualElementPanel panel) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIElementsUtility.UnityEngine.UIElements.IUIElementsUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& eventHandled) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.UIElements.UIEventRegistration+<>c.<.cctor>b__1_2 (System.Int32 i, System.IntPtr ptr) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility.ProcessEvent (System.Int32 instanceID, System.IntPtr nativeEventPtr, System.Boolean& result) (at <822ef144c83a49dfbe8ab1337f169b36>:0)

GUI Error: Invalid GUILayout state in MagnetoHelicopterDesignerWindow view. Verify that all layout Begin/End calls match
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)

IndexOutOfRangeException: Index was outside the bounds of the array.
PixelLightRig.PushFrame () (at Assets/locomotion/pathing/civil/lights/PixelLightRig.cs:144)
MagnetoHelicopterDesignerWindow.ApplyFrameScrubToMount (PixelLightViewScopeSettings vs) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:734)
MagnetoHelicopterDesignerWindow.DrawFrameScrubber (PixelLightViewScopeSettings vs, PixelLightLayer layer) (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:701)
MagnetoHelicopterDesignerWindow.DrawPixelLight () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:619)
MagnetoHelicopterDesignerWindow.OnGUI () (at Assets/locomotion/Editor/MagnetoHelicopterDesignerWindow.cs:75)
UnityEditor.HostView.InvokeOnGUI (UnityEngine.Rect onGUIPosition) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.DrawView (UnityEngine.Rect dockAreaRect) (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEditor.DockArea.OldOnGUI () (at <d9024bdc2a414b3c81b047ea3e343594>:0)
UnityEngine.UIElements.IMGUIContainer.DoOnGUI (UnityEngine.Event evt, UnityEngine.Matrix4x4 parentTransform, UnityEngine.Rect clippingRect, System.Boolean isComputingLayout, UnityEngine.Rect layoutSize, System.Action onGUIHandler, System.Boolean canAffectFocus) (at <5c2380b11c444983ac3ffcbc73d04ba8>:0)
UnityEngine.GUIUtility:ProcessEvent(Int32, IntPtr, Boolean&)

GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)


## Designer flow

1. Create/assign `PixelLightMultiSlotCatalog`.
2. Placement → add/sync slots; accordion edit cells/contents.
3. PixelLight tab → pick View + Scope → edit bag → **Apply this view+scope to selected mount**.
4. Quick brush row: **On / Delete / Grid Slot / Fill / Chase / Clear**. **Grid Slot** click places a `HelicoptorGridSlot` at that cell (overlay **G**). **Delete** on a **G** cell prompts *Do you want to delete this grid slot?* (Yes/No) and removes the scene slot + catalog entry.
5. **Frame scrubber** — IntSlider + Time (ms) slider + tick bar to hand-scrub pattern frames; optional live preview on the selected mount (pauses rig playback).
6. Save all on Overview persists catalog + craft config.
