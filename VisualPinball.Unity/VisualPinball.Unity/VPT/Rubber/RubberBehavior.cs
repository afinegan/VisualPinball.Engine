#region ReSharper
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
#endregion

using UnityEngine;
using VisualPinball.Engine.Math;
using VisualPinball.Engine.VPT.Rubber;
using VisualPinball.Unity.Extensions;

namespace VisualPinball.Unity.VPT.Rubber
{
	[AddComponentMenu("Visual Pinball/Rubber")]
	public class RubberBehavior : ItemBehavior<Engine.VPT.Rubber.Rubber, RubberData>, IDragPointsEditable
	{
		protected override string[] Children => null;

		protected override Engine.VPT.Rubber.Rubber GetItem()
		{
			return new Engine.VPT.Rubber.Rubber(data);
		}

		public override ItemDataTransformType EditorPositionType => ItemDataTransformType.ThreeD;
		public override Vector3 GetEditorPosition()
		{
			if (data == null || data.DragPoints.Length == 0) {
				return Vector3.zero;
			}
			return data.DragPoints[0].Vertex.ToUnityVector3(data.Height);
		}
		public override void SetEditorPosition(Vector3 pos)
		{
			if (data == null || data.DragPoints.Length == 0) {
				return;
			}

			data.Height = pos.z;
			pos.z = 0f;
			var diff = pos.ToVertex3D().Sub(data.DragPoints[0].Vertex);
			diff.Z = 0f;
			data.DragPoints[0].Vertex = pos.ToVertex3D();
			for (int i = 1; i < data.DragPoints.Length; i++) {
				var pt = data.DragPoints[i];
				pt.Vertex = pt.Vertex.Add(diff);
			}
		}

		public override ItemDataTransformType EditorRotationType => ItemDataTransformType.ThreeD;
		public override Vector3 GetEditorRotation() => new Vector3(data.RotX, data.RotY, data.RotZ);
		public override void SetEditorRotation(Vector3 rot)
		{
			data.RotX = rot.x;
			data.RotY = rot.y;
			data.RotZ = rot.z;
		}

		//IDragPointsEditable
		public bool DragPointEditEnabled { get; set; }
		public DragPointData[] GetDragPoints() { return data.DragPoints; }
		public void SetDragPoints(DragPointData[] dpoints) { data.DragPoints = dpoints; }
		public Vector3 GetEditableOffset() { return new Vector3(0.0f, 0.0f, data.HitHeight); }
		public bool PointsAreLooping() { return true; }
	}
}
