using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniBoids {

	[RequireComponent(typeof(GPUBoids))]
	public class BoidsRenderer : MonoBehaviour {

		public Mesh instanceMesh;
		public Material renderMaterial;
		public Vector3 objectScale = new Vector3(0.1f, 0.2f, 0.5f);

		GPUBoids _gpuBoids;
		uint[] _args = new uint[]{ 0, 0, 0, 0, 0 };
		ComputeBuffer _argsBuffer;

		void Awake() {
			_gpuBoids = GetComponent<GPUBoids>();
			_argsBuffer = new ComputeBuffer(1, _args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
		}

		void OnDisable() {
			if(_argsBuffer != null) {
				_argsBuffer.Release();
			}
			_argsBuffer = null;
		}

		void Update() {
			RenderInstancedMesh();
		}

		void RenderInstancedMesh() {
			if(
				renderMaterial == null ||
				_gpuBoids == null ||
				!SystemInfo.supportsInstancing
			) {
				return;
			}

			uint numIndices = (instanceMesh != null) ? (uint)instanceMesh.GetIndexCount(0) : 0;
			_args[0] = numIndices;
			_args[1] = (uint)_gpuBoids.maxObjectNum;

			_argsBuffer.SetData(_args);
			renderMaterial.SetBuffer("_BoidDataBuffer", _gpuBoids.boidDataBuffer);

			renderMaterial.SetVector("_ObjectScale", objectScale);

			var bounds = new Bounds(_gpuBoids.wallCenter, _gpuBoids.wallSize);

			// インスタンシングして描画
			Graphics.DrawMeshInstancedIndirect(
				instanceMesh,
				0,
				renderMaterial,
				bounds,
				_argsBuffer
			);
		}
	}
}