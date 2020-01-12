﻿using System.Linq;
using JeremyAnsel.Media.WavefrontObj;
using VisualPinball.Engine.Test.Test;
using Xunit;

namespace VisualPinball.Engine.Test.VPT.Surface
{
	public class SurfaceMeshTests : MeshTests
	{
		private readonly Engine.VPT.Table.Table _table;
		private readonly ObjFile _obj;

		public SurfaceMeshTests()
		{
			_table = Engine.VPT.Table.Table.Load(@"..\..\Fixtures\SurfaceData.vpx");
			_obj = LoadObjFixture(@"..\..\Fixtures\SurfaceData.obj");
		}

		[Fact]
		public void ShouldGenerateTopAndSides()
		{
			var surface = _table.Surfaces["Wall"];
			var surfaceMeshes = surface.GetRenderObjects(_table).Select(ro => ro.Mesh).ToArray();
			AssertObjMesh(_obj, surface.Name, surfaceMeshes);
		}
	}
}