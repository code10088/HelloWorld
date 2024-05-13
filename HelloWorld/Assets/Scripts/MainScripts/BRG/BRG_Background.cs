using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Collections.Generic;

public unsafe class BRG_Background : MonoBehaviour
{
    public static BRG_Background gBackgroundManager;

    public Mesh m_mesh;
    public Material m_material;
    public bool m_castShadows;
    public float m_motionAmplitude = 2.0f;
    public float m_phaseSpeed1 = 6.0f;

    public int m_backgroundW = 60;
    public int m_backgroundH = 150;
    private const int kGpuItemSize = (3 * 2 + 1) * 16;  //  GPU item size ( 2 * 4x3 matrices plus 1 color per item )

    private BRG_Container m_brgContainer;
    private JobHandle m_updateJobFence;

    private int m_itemCount;
    private uint m_slicePos;

    public struct BackgroundItem
    {
        public float z;
        public float hInitial;
        public float h;     // scale
        public float phase;
        public Vector4 color;
    };

    public NativeArray<BackgroundItem> m_backgroundItems;

    public void Awake()
    {
        gBackgroundManager = this;
    }

    [BurstCompile]
    private void InjectNewSlice()
    {

        m_slicePos++;

        int sPos0 = (int)((m_slicePos + m_backgroundH - 1) % m_backgroundH);
        sPos0 *= m_backgroundW;
        for (int x = 0; x < m_backgroundW; x++)
        {
            float fx = (float)x * -1.0f;
            float scaleY = 40.0f;
            if (x > 0)
            {
                float rnd = UnityEngine.Random.Range(0.0f, 1.0f);
                float amp = 20.0f * (rnd + 1.0f);
                scaleY = 1.0f + rnd;
                scaleY += amp / (float)(x + 1);
            }

            float sat = 0.6f + 0.2f + 0.2f * Mathf.Sin((m_slicePos + x) / 12.0f);
            float value = 0.3f;

            if ((x > 0) && (x < m_backgroundW - 1))
            {
                if (UnityEngine.Random.Range(0.0f, 1.0f) > 0.8f)
                {
                    value = 1.0f;
                    sat = 1.0f;
                }
            }

            Color col = Color.HSVToRGB(((float)(m_slicePos + x) / 400.0f) % 1.0f, sat, value);

            // write colors right after the 4x3 matrices
            BackgroundItem item = new BackgroundItem();
            item.z = fx + 60;
            item.hInitial = scaleY;
            item.phase = (float)x / 2.0f;
            item.color = new Vector4(col.r, col.g, col.b, 1.0f);
            m_backgroundItems[sPos0] = item;

            sPos0++;
        }
    }


    // Start is called before the first frame update
    void Start()
    {

        m_itemCount = m_backgroundW * m_backgroundH;

        m_brgContainer = new BRG_Container();
        m_brgContainer.Init(m_mesh, m_material, m_itemCount, kGpuItemSize, m_castShadows);

        // setup positions & scale of each background elements
        m_backgroundItems = new NativeArray<BackgroundItem>(m_itemCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        // fill a complete background buffer
        for (int i = 0; i < m_backgroundH; i++)
            InjectNewSlice();

        m_brgContainer.UploadGpuData(m_itemCount);
    }

    [BurstCompile]
    private struct UpdatePositionsJob : IJobFor
    {
        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float4> _sysmemBuffer;

        [NativeDisableParallelForRestriction]
        public NativeArray<BackgroundItem> backgroundItems;

        public int slicePos;
        public int backgroundW;
        public int backgroundH;
        public float _dt;
        public float _phaseSpeed;
        public int _maxInstancePerWindow;
        public int _windowSizeInFloat4;

        public void Execute(int sliceIndex)
        {
            int slice = (int)((sliceIndex + slicePos) % backgroundH);
            float px = (float)sliceIndex - backgroundH / 2;
            float phaseSpeed = _phaseSpeed * _dt;


            for (int x = 0; x < backgroundW; x++)
            {
                int itemId = slice * backgroundW + x;
                BackgroundItem item = backgroundItems[itemId];

                float4 color = backgroundItems[itemId].color;

                float4 bpos;
                float waveY = item.hInitial + 0.5f + math.sin(item.phase) * 0.5f;
                float scaleY = waveY;

                bpos.z = item.z;
                bpos.y = scaleY * 0.5f - 20;

                item.h = scaleY;

                float phaseInc = phaseSpeed;
                item.phase += phaseInc;

                int i;
                int windowId = System.Math.DivRem(slice * backgroundW + x, _maxInstancePerWindow, out i);
                int windowOffsetInFloat4 = windowId * _windowSizeInFloat4;

                // compute the new current frame matrix
                _sysmemBuffer[(windowOffsetInFloat4 + i * 3 + 0)] = new float4(1, 0, 0, 0);
                _sysmemBuffer[(windowOffsetInFloat4 + i * 3 + 1)] = new float4(scaleY, 0, 0, 0);
                _sysmemBuffer[(windowOffsetInFloat4 + i * 3 + 2)] = new float4(1, px, bpos.y, bpos.z);

                // compute the new inverse matrix (note: shortcut use identity because aligned cubes normals aren't affected by any non uniform scale
                _sysmemBuffer[(windowOffsetInFloat4 + _maxInstancePerWindow * 3 * 1 + i * 3 + 0)] = new float4(1, 0, 0, 0);
                _sysmemBuffer[(windowOffsetInFloat4 + _maxInstancePerWindow * 3 * 1 + i * 3 + 1)] = new float4(1, 0, 0, 0);
                _sysmemBuffer[(windowOffsetInFloat4 + _maxInstancePerWindow * 3 * 1 + i * 3 + 2)] = new float4(1, 0, 0, 0);

                // update colors
                _sysmemBuffer[windowOffsetInFloat4 + _maxInstancePerWindow * 3 * 2 + i] = color;

                backgroundItems[itemId] = item;

            }
        }
    }


    [BurstCompile]
    JobHandle UpdatePositions(float dt, JobHandle jobFence)
    {
        int totalGpuBufferSize;
        int alignedWindowSize;
        NativeArray<float4> sysmemBuffer = m_brgContainer.GetSysmemBuffer(out totalGpuBufferSize, out alignedWindowSize);

        UpdatePositionsJob myJob = new UpdatePositionsJob()
        {
            _sysmemBuffer = sysmemBuffer,
            backgroundItems = m_backgroundItems,
            slicePos = (int)m_slicePos,
            backgroundW = m_backgroundW,
            backgroundH = m_backgroundH,
            _dt = dt,
            _phaseSpeed = m_phaseSpeed1,
            _maxInstancePerWindow = alignedWindowSize / kGpuItemSize,
            _windowSizeInFloat4 = alignedWindowSize / 16,
        };
        jobFence = myJob.ScheduleParallel(m_backgroundH, 4, jobFence);      // 4 slices per job
        return jobFence;
    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;

        JobHandle jobFence = new JobHandle();

        m_updateJobFence = UpdatePositions(dt, jobFence);
    }

    private void LateUpdate()
    {
        m_updateJobFence.Complete();
        m_brgContainer.UploadGpuData(m_itemCount);
    }

    private void OnDestroy()
    {
        if (m_brgContainer != null)
            m_brgContainer.Shutdown();
        m_backgroundItems.Dispose();
    }
}