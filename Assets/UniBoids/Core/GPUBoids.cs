using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UniBoids {

	public class GPUBoids : MonoBehaviour {

		[System.Serializable]
		public struct BoidData {
			public Vector3 velocity;    // 速度
			public Vector3 position;    // 位置
		}

		const int NUM_THREAD_IN_GROUP = 256;

		[Range(256, 32768)]
		public int maxObjectNum = 16384;                // 最大オブジェクト数

		public float cohesionNeighborRadius = 2f;       // 結合を適用する他の個体との半径
		public float alignmentNeighborRadius = 2f;      // 整列を適用する他の個体との半径
		public float separationNeighborRadius = 1f;     // 分離を適用する他の個体との半径

		public float maxSpeed = 5f;                     // 速度の最大値
		public float maxSteerForce = 0.5f;              // 操舵力の最大値

		public float separateWeight = 1f;               // 分離する力の重み
		public float alignmentWeight = 1f;              // 整列する力の重み
		public float cohesionWeight = 1f;               // 結合する力の重み

		public float avoidWallWeight = 10f;             // 壁を避ける力の重み

		public Vector3 wallCenter = Vector3.zero;               // 壁の中心座標
		public Vector3 wallSize = new Vector3(32f, 32f, 32f);   // 壁のサイズ

		public ComputeShader boidsCS;   // Boidsシミュレーションを行うComputeShader

		public ComputeBuffer boidForceBuffer { get; private set; } // Boidの操舵力を格納したバッファ
		public ComputeBuffer boidDataBuffer { get; private set; }  // Boidの基本データを格納したバッファ

		#region Builtin callbacks

		void Start() {
			InitBuffer();
		}

		void Update() {
			Simulation();
		}

		void OnDestroy() {
			ReleaseBuffer();
		}

		void OnDrawGizmos() {
			// デバッグとしてシミュレーション領域をワイヤーフレームで描画
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireCube(wallCenter, wallSize);
		}

		#endregion

		#region Simulation functions

		/// <summary>
		/// バッファを初期化
		/// </summary>
		void InitBuffer() {
			boidForceBuffer = new ComputeBuffer(maxObjectNum, Marshal.SizeOf(typeof(Vector3)));
			boidDataBuffer = new ComputeBuffer(maxObjectNum, Marshal.SizeOf(typeof(BoidData)));

			// Boidデータ、Forceバッファを初期化
			var forceArr = new Vector3[maxObjectNum];
			var boidDataArr = new BoidData[maxObjectNum];

			for(var i = 0; i < maxObjectNum; ++i) {
				forceArr[i] = Vector3.zero;
				boidDataArr[i].position = Random.insideUnitSphere * 1f;
				boidDataArr[i].velocity = Random.insideUnitSphere * 0.1f;
			}

			boidForceBuffer.SetData(forceArr);
			boidDataBuffer.SetData(boidDataArr);

			forceArr = null;
			boidDataArr = null;
		}

		/// <summary>
		/// バッファを解放
		/// </summary>
		void ReleaseBuffer() {
			if(boidForceBuffer != null) {
				boidForceBuffer.Release();
				boidForceBuffer = null;
			}
			if(boidDataBuffer != null) {
				boidDataBuffer.Release();
				boidDataBuffer = null;
			}
		}

		/// <summary>
		/// シミュレーション
		/// </summary>
		void Simulation() {
			ComputeShader cs = boidsCS;
			int id = -1;

			// スレッドグループ数を計算する
			int threadGroupSize = Mathf.CeilToInt(maxObjectNum / NUM_THREAD_IN_GROUP);

			// 操舵力を計算
			id = cs.FindKernel("ForceCS");   // カーネルIDを取得
			cs.SetInt("numMaxBoidObjects", maxObjectNum);

			cs.SetFloat("sepRadius", separationNeighborRadius);
			cs.SetFloat("aliRadius", alignmentNeighborRadius);
			cs.SetFloat("cohRadius", cohesionNeighborRadius);

			cs.SetFloat("sepWeight", separateWeight);
			cs.SetFloat("aliWeight", alignmentWeight);
			cs.SetFloat("cohWeight", cohesionWeight);

			cs.SetFloat("maxSpeed", maxSpeed);
			cs.SetFloat("maxSteerForce", maxSteerForce);

			cs.SetVector("wallCenter", wallCenter);

			cs.SetVector("wallSize", wallSize);

			cs.SetFloat("avoidWallWeight", avoidWallWeight);

			cs.SetBuffer(id, "rBoidDataBuffer", boidDataBuffer);
			cs.SetBuffer(id, "rwBoidForceBuffer", boidForceBuffer);

			cs.Dispatch(id, threadGroupSize, 1, 1); // ComputeShaderの実行
			
			id = cs.FindKernel("IntegrateCS");
			cs.SetFloat("deltaTime", Time.deltaTime);

			cs.SetBuffer(id, "rBoidForceBuffer", boidForceBuffer);
			cs.SetBuffer(id, "rwBoidDataBuffer", boidDataBuffer);

			cs.Dispatch(id, threadGroupSize, 1, 1); // ComputeShaderの実行
		}

		#endregion
	}
}