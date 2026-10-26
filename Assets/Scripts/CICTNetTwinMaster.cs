using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CICTNetTwinMaster : MonoBehaviour
{
    private Transform buildingRoot;
    private List<PacketFlow> packets = new List<PacketFlow>();
    private List<GameObject> vlanLabels = new List<GameObject>();
    private List<GameObject> physicalWalls = new List<GameObject>();
    private bool isSimRunning = true;
    private TMP_Text cliTextOutput;
    private TMP_Text[] inspectorFields = new TMP_Text[5];
    private RawImage bandwidthChartImg;
    private float chartTimer = 0f;

    private class PacketFlow
    {
        public Transform dot;
        public Vector3 startPos;
        public Vector3 endPos;
        public float speed;
        public float t;
    }

    void Start()
    {
        SetupCameraAndLighting();
        BuildCICT3DBuildingAndTopology();
        BuildCompleteDashboardUI();
    }

    void Update()
    {
        if (!isSimRunning) return;

        // 1. Di chuyển các hạt gói tin phát sáng dọc theo dây cáp
        for (int i = 0; i < packets.Count; i++)
        {
            var p = packets[i];
            if (p.dot == null) continue;
            p.t += Time.deltaTime * p.speed;
            if (p.t > 1f) p.t -= 1f;
            p.dot.position = Vector3.Lerp(p.startPos, p.endPos, p.t);
        }

        // 2. Cập nhật biểu đồ băng thông nhấp nhô theo thời gian thực
        chartTimer += Time.deltaTime;
        if (chartTimer > 0.4f && bandwidthChartImg != null)
        {
            chartTimer = 0f;
            bandwidthChartImg.texture = DrawBandwidthChart(Time.time);
        }
    }

    // =========================================================================
    // PHẦN 1: CẤU HÌNH CAMERA GÓC ISOMETRIC & ÁNH SÁNG CHUẨN ẢNH MẪU
    // =========================================================================
    void SetupCameraAndLighting()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera", typeof(Camera));
            camObj.tag = "MainCamera";
            cam = camObj.GetComponent<Camera>();
        }

        // Tắt các script camera cũ nếu có
        foreach (var comp in cam.GetComponents<MonoBehaviour>())
        {
            if (comp != this) comp.enabled = false;
        }

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.045f, 0.075f, 0.115f, 1f); // Nền xanh đen đậm chuẩn ảnh
        cam.orthographic = true;
        cam.orthographicSize = 10.6f;
        cam.transform.position = new Vector3(-16.5f, 14.8f, -16.5f);
        cam.transform.rotation = Quaternion.Euler(29f, 45f, 0f);
        cam.nearClipPlane = -50f;
        cam.farClipPlane = 200f;
    }

    Material CreateMat(Color col, bool transparent = false)
    {
        Shader sh = transparent ? Shader.Find("Sprites/Default") : Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material m = new Material(sh);
        m.color = col;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        return m;
    }

    // =========================================================================
    // PHẦN 2: DỰNG KIẾN TRÚC TÒA NHÀ 4 TẦNG, PHÒNG LAB & MẠNG 3D
    // =========================================================================
    void BuildCICT3DBuildingAndTopology()
    {
        buildingRoot = new GameObject("CICT_Building_Root").transform;
        buildingRoot.position = new Vector3(0.4f, -3.1f, 0.4f);
        buildingRoot.rotation = Quaternion.Euler(0f, -11f, 0f);

        Material matFloorGlass = CreateMat(new Color(0.14f, 0.42f, 0.78f, 0.30f), true);
        Material matWallGlass = CreateMat(new Color(0.22f, 0.62f, 0.92f, 0.18f), true);
        Material matGroundWall = CreateMat(new Color(0.24f, 0.39f, 0.54f, 0.65f), true);
        Material matWindowDark = CreateMat(new Color(0.08f, 0.15f, 0.24f, 0.85f), true);
        Material matEdgeCyan = CreateMat(new Color(0.35f, 0.88f, 1f, 0.75f), true);
        Material matEdgeSoft = CreateMat(new Color(0.25f, 0.70f, 0.95f, 0.40f), true);

        float[] floorHeights = { 0f, 2.25f, 4.5f, 6.75f };
        string[] floorTitles = { "Tầng trệt", "Tầng 1", "Tầng 2", "Tầng 3" };

        for (int f = 0; f < 4; f++)
        {
            float y = floorHeights[f];
            Transform fNode = new GameObject(floorTitles[f]).transform;
            fNode.SetParent(buildingRoot, false);

            // Sàn kính chính + Viền sáng Cyan
            GameObject slab = MakeCube(fNode, new Vector3(0, y, 0), new Vector3(13.2f, 0.12f, 8.6f), matFloorGlass);
            MakeWireframeBox(fNode, new Vector3(0, y, 0), new Vector3(13.2f, 0.12f, 8.6f), matEdgeCyan, 0.045f);

            // Tường bao mặt sau & mặt trái
            Material wallM = (f == 0) ? matGroundWall : matWallGlass;
            physicalWalls.Add(MakeCube(fNode, new Vector3(0, y + 0.95f, 4.25f), new Vector3(13.2f, 1.9f, 0.08f), wallM));
            physicalWalls.Add(MakeCube(fNode, new Vector3(-6.55f, y + 0.95f, 0), new Vector3(0.08f, 1.9f, 8.6f), wallM));

            // Vách kính chia các phòng Lab và phòng Server bên trong
            physicalWalls.Add(MakeCube(fNode, new Vector3(-1.9f, y + 0.95f, 1.3f), new Vector3(0.06f, 1.9f, 6.0f), matWallGlass));
            physicalWalls.Add(MakeCube(fNode, new Vector3(2.1f, y + 0.95f, 1.3f), new Vector3(0.06f, 1.9f, 6.0f), matWallGlass));
            physicalWalls.Add(MakeCube(fNode, new Vector3(0, y + 0.95f, -1.6f), new Vector3(13.2f, 1.9f, 0.06f), matWallGlass));
            MakeWireframeBox(fNode, new Vector3(-4.2f, y + 0.95f, 1.3f), new Vector3(4.6f, 1.9f, 5.9f), matEdgeSoft, 0.025f);
            MakeWireframeBox(fNode, new Vector3(0.1f, y + 0.95f, 1.3f), new Vector3(4.0f, 1.9f, 5.9f), matEdgeSoft, 0.025f);

            // Chữ tên Tầng + Đường kẻ ngang chỉ vào tầng ở bên trái (giống hệt ảnh mẫu)
            Make3DText(fNode, floorTitles[f], new Vector3(-8.0f, y + 0.55f, -2.2f), 3.6f, Color.white, false);
            MakeLine(fNode, new Vector3(-8.8f, y + 0.25f, -2.2f), new Vector3(-6.6f, y + 0.25f, -2.2f), new Color(0.7f, 0.85f, 1f, 0.7f), 0.03f);

            // Nội thất: Dãy bàn máy tính trong các Phòng Lab
            BuildComputerLab(fNode, new Vector3(-4.3f, y + 0.08f, 1.4f), 3, 3);
            if (f < 3) BuildComputerLab(fNode, new Vector3(-4.3f, y + 0.08f, -2.8f), 2, 3);
        }

        // Kiến trúc mặt tiền Tầng trệt: Tường trước, dãy cửa sổ và Sảnh chính có trụ cột
        physicalWalls.Add(MakeCube(buildingRoot, new Vector3(0, 0.95f, -4.25f), new Vector3(13.2f, 1.9f, 0.1f), matGroundWall));
        for (int w = -5; w <= 5; w += 2)
        {
            if (w == -1 || w == 1) continue;
            MakeCube(buildingRoot, new Vector3(w * 1.0f, 1.0f, -4.31f), new Vector3(0.9f, 0.85f, 0.05f), matWindowDark);
            MakeCube(buildingRoot, new Vector3(-6.61f, 1.0f, (w * 0.6f)), new Vector3(0.05f, 0.85f, 0.8f), matWindowDark);
        }

        // Sảnh đón (Portico) nhô ra ở mặt trước Tầng trệt
        Transform portico = new GameObject("Portico_Entrance").transform;
        portico.SetParent(buildingRoot, false);
        portico.localPosition = new Vector3(-0.4f, 0f, -5.2f);
        MakeCube(portico, new Vector3(0, 1.85f, 0), new Vector3(3.4f, 0.22f, 2.1f), matGroundWall);
        MakeWireframeBox(portico, new Vector3(0, 1.85f, 0), new Vector3(3.4f, 0.22f, 2.1f), matEdgeCyan, 0.035f);
        MakeCube(portico, new Vector3(-1.4f, 0.9f, -0.85f), new Vector3(0.25f, 1.8f, 0.25f), matGroundWall);
        MakeCube(portico, new Vector3(1.4f, 0.9f, -0.85f), new Vector3(0.25f, 1.8f, 0.25f), matGroundWall);
        MakeCube(portico, new Vector3(-0.5f, 0.9f, -0.85f), new Vector3(0.22f, 1.8f, 0.22f), matGroundWall);
        MakeCube(portico, new Vector3(0.5f, 0.9f, -0.85f), new Vector3(0.22f, 1.8f, 0.22f), matGroundWall);

        // Hệ trục tọa độ 3D (x, y, z) ở góc dưới bên trái tòa nhà
        BuildAxisGizmo(buildingRoot, new Vector3(-8.2f, -0.5f, -4.5f));

        // Nhãn tên các Phòng Lab & Phòng Server DMZ
        Make3DText(buildingRoot, "Phòng Lab 201", new Vector3(-4.2f, 8.15f, 2.0f), 3.1f, Color.white, true);
        Make3DText(buildingRoot, "Phòng Server\nDMZ", new Vector3(1.0f, 8.35f, 3.1f), 3.1f, Color.white, true);
        Make3DText(buildingRoot, "Phòng Server DMZ", new Vector3(5.0f, 5.4f, -0.4f), 2.9f, Color.white, true);
        Make3DText(buildingRoot, "Phòng Lab 301", new Vector3(-3.1f, 3.5f, -1.9f), 3.1f, Color.white, true);
        Make3DText(buildingRoot, "Phòng Lab 201", new Vector3(-3.3f, 1.05f, -3.5f), 3.1f, Color.white, true);

        // =====================================================================
        // ĐẶT CÁC THIẾT BỊ MẠNG 3D ĐÚNG TỌA ĐỘ TRONG ẢNH MẪU
        // =====================================================================
        Vector3 posRouterT3 = new Vector3(-1.6f, 6.95f, -0.4f);
        Vector3 posCoreSwT3 = new Vector3(0.4f, 6.95f, -0.2f);
        Vector3 posServerT3 = new Vector3(0.8f, 7.45f, 2.6f);
        Vector3 posSwRightT3 = new Vector3(3.6f, 6.95f, 1.1f);
        Vector3 posApTop1 = new Vector3(3.1f, 6.95f, 3.1f);
        Vector3 posApTop2 = new Vector3(5.5f, 6.95f, 1.7f);

        Vector3 posSwMidLeft = new Vector3(-0.8f, 4.7f, -1.1f);
        Vector3 posServerMid = new Vector3(4.6f, 5.2f, 1.5f);
        Vector3 posSwMidRight = new Vector3(4.3f, 4.7f, -1.7f);
        Vector3 posApMidRight = new Vector3(5.7f, 4.7f, -1.1f);
        Vector3 posApMidInner = new Vector3(3.2f, 4.7f, -0.2f);

        Vector3 posApLeftT1 = new Vector3(-5.6f, 2.45f, -2.4f);
        Vector3 posSwBotT1 = new Vector3(-0.3f, 2.45f, -1.5f);
        Vector3 posServerBot = new Vector3(-0.4f, 3.1f, -0.3f);

        Vector3 posRouterGround = new Vector3(4.5f, 0.35f, -4.1f);
        Vector3 posApGroundRight = new Vector3(5.6f, 1.25f, -3.2f);

        // Khởi tạo mô hình 3D chi tiết cho từng thiết bị
        CreateRouterL3(posRouterT3, "Router L3");
        CreateSwitch3D(posCoreSwT3, "Switch L2/L3");
        CreateServerRack3D(posServerT3);
        CreateSwitch3D(posSwRightT3, "Switch L2/L3");
        CreateWifiAPWithDome(posApTop1, "CTU-Wifi AP", 2.9f, false);
        CreateWifiAPWithDome(posApTop2, "CTU-Wifi AP", 2.9f, false);

        CreateSwitch3D(posSwMidLeft, "Switch L2");
        CreateServerRack3D(posServerMid);
        CreateSwitch3D(posSwMidRight, "");
        CreateWifiAPWithDome(posApMidRight, "", 2.8f, false);
        CreateWifiAPWithDome(posApMidInner, "", 2.2f, false);

        CreateWifiAPWithDome(posApLeftT1, "", 3.1f, false);
        CreateSwitch3D(posSwBotT1, "Switch L2");
        CreateServerRack3D(posServerBot);

        CreateRouterL3(posRouterGround, "Router L3");
        CreateWifiAPWithDome(posApGroundRight, "CTU-Wifi AP", 3.3f, true);

        // =====================================================================
        // KẾT NỐI DÂY CÁP QUANG PHÁT SÁNG & LUỒNG GÓI TIN CHẠY TRÊN DÂY
        // =====================================================================
        Color colGreen = new Color(0.25f, 1f, 0.45f, 0.95f);
        Color colGold = new Color(1f, 0.78f, 0.20f, 0.95f);

        // Cáp Tầng 3
        CreateNetworkCable(posRouterT3, posCoreSwT3, colGold, "", 5);
        CreateNetworkCable(posCoreSwT3, posSwRightT3, colGold, "VLAN 10", 6);
        CreateNetworkCable(posCoreSwT3, posServerT3, colGreen, "", 4);
        CreateNetworkCable(posSwRightT3, posApTop1, colGold, "", 3);
        CreateNetworkCable(posSwRightT3, posApTop2, colGold, "", 3);
        // Chùm cáp nối từ Router L3 vào dàn máy tính Phòng Lab 201
        CreateNetworkCable(posRouterT3, new Vector3(-3.8f, 6.95f, 2.2f), colGreen, "", 3);
        CreateNetworkCable(posRouterT3, new Vector3(-4.5f, 6.95f, 1.0f), colGreen, "", 3);
        CreateNetworkCable(posRouterT3, new Vector3(-3.5f, 6.95f, 0.2f), colGreen, "", 3);

        // Cáp liên tầng (Trunking & Fiber 10Gbps)
        CreateNetworkCable(posCoreSwT3, posSwMidLeft, colGreen, "", 4);
        CreateNetworkCable(posCoreSwT3, posSwMidRight, colGold, "VLAN 20 (Lab)", 6);
        CreateNetworkCable(posSwRightT3, posServerMid, colGreen, "", 3);
        CreateNetworkCable(posSwMidRight, posApMidRight, colGreen, "", 3);
        CreateNetworkCable(posSwMidLeft, posApLeftT1, colGreen, "", 4);
        CreateNetworkCable(posSwMidLeft, posSwBotT1, colGreen, "VLAN 20", 4);
        CreateNetworkCable(posSwBotT1, new Vector3(-3.8f, 2.45f, -2.5f), colGreen, "", 3);
        CreateNetworkCable(posSwBotT1, new Vector3(-4.5f, 0.25f, -3.0f), colGreen, "", 3);

        // 2 đường cáp trục chính Fiber 10Gbps nối xuống Router L3 tầng trệt
        CreateNetworkCable(posSwMidLeft, posRouterGround, colGreen, "Fiber 10Gbps", 6);
        CreateNetworkCable(posCoreSwT3, posRouterGround + new Vector3(0.3f, 0.1f, 0.2f), colGold, "Fiber 10Gbps", 6);
        CreateNetworkCable(posRouterGround, posApGroundRight, colGreen, "", 4);
    }

    void BuildComputerLab(Transform parent, Vector3 center, int rows, int cols)
    {
        Material matDesk = CreateMat(new Color(0.12f, 0.25f, 0.42f, 1f));
        Material matScreen = CreateMat(new Color(0.35f, 0.88f, 1f, 0.92f), true);
        Material matCase = CreateMat(new Color(0.08f, 0.14f, 0.22f, 1f));

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 p = center + new Vector3((c - 1) * 0.92f, 0, (r - 1) * 0.92f);
                MakeCube(parent, p + new Vector3(0, 0.18f, 0), new Vector3(0.62f, 0.32f, 0.38f), matDesk);
                MakeCube(parent, p + new Vector3(-0.08f, 0.48f, 0), new Vector3(0.38f, 0.26f, 0.05f), matScreen);
                MakeCube(parent, p + new Vector3(0.20f, 0.46f, 0), new Vector3(0.12f, 0.28f, 0.24f), matCase);
            }
        }
    }

    void CreateRouterL3(Vector3 pos, string title)
    {
        Transform r = new GameObject("Router_L3").transform;
        r.SetParent(buildingRoot, false);
        r.localPosition = pos;

        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.transform.SetParent(r, false);
        cyl.transform.localScale = new Vector3(0.95f, 0.22f, 0.95f);
        cyl.GetComponent<Renderer>().material = CreateMat(new Color(0.12f, 0.62f, 0.96f, 1f));

        // 4 mũi tên trắng chữ X trên mặt Router chuẩn Cisco
        Material wMat = CreateMat(Color.white, true);
        MakeCube(r, new Vector3(0, 0.23f, 0), new Vector3(0.68f, 0.02f, 0.11f), wMat).transform.localRotation = Quaternion.Euler(0, 45, 0);
        MakeCube(r, new Vector3(0, 0.23f, 0), new Vector3(0.68f, 0.02f, 0.11f), wMat).transform.localRotation = Quaternion.Euler(0, -45, 0);

        if (!string.IsNullOrEmpty(title))
            Make3DText(r, title, new Vector3(0, -0.42f, -0.55f), 2.8f, Color.white, true);
    }

    void CreateSwitch3D(Vector3 pos, string title)
    {
        Transform s = new GameObject("Switch_3D").transform;
        s.SetParent(buildingRoot, false);
        s.localPosition = pos;

        MakeCube(s, Vector3.zero, new Vector3(1.05f, 0.26f, 0.68f), CreateMat(new Color(0.18f, 0.38f, 0.68f, 1f)));
        MakeCube(s, new Vector3(0, 0, -0.35f), new Vector3(0.88f, 0.12f, 0.02f), CreateMat(new Color(0.3f, 1f, 0.65f, 1f), true));

        if (!string.IsNullOrEmpty(title))
            Make3DText(s, title, new Vector3(0, -0.42f, -0.55f), 2.8f, Color.white, true);
    }

    void CreateServerRack3D(Vector3 pos)
    {
        Transform sv = new GameObject("Server_DMZ").transform;
        sv.SetParent(buildingRoot, false);
        sv.localPosition = pos;

        MakeCube(sv, Vector3.zero, new Vector3(0.65f, 1.35f, 0.65f), CreateMat(new Color(0.10f, 0.15f, 0.22f, 1f)));
        MakeCube(sv, new Vector3(0, 0.35f, -0.34f), new Vector3(0.45f, 0.08f, 0.02f), CreateMat(new Color(1f, 0.25f, 0.25f, 1f), true));
        MakeCube(sv, new Vector3(0, 0.15f, -0.34f), new Vector3(0.45f, 0.08f, 0.02f), CreateMat(new Color(0.25f, 1f, 0.45f, 1f), true));
    }

    // Dựng AP + Biểu tượng sóng xanh + Vòm bán cầu (Hemisphere) màu cam có vòng phát sáng
    void CreateWifiAPWithDome(Vector3 pos, string title, float domeRadius, bool hasBlueSecondaryDome)
    {
        Transform ap = new GameObject("CTU_Wifi_AP").transform;
        ap.SetParent(buildingRoot, false);
        ap.localPosition = pos;

        // Hộp phát Wi-Fi màu trắng + 2 râu ăng-ten
        Material whiteM = CreateMat(new Color(0.92f, 0.95f, 1f, 1f));
        MakeCube(ap, Vector3.zero, new Vector3(0.68f, 0.20f, 0.56f), whiteM);
        MakeCube(ap, new Vector3(-0.22f, 0.32f, 0.16f), new Vector3(0.05f, 0.48f, 0.05f), whiteM);
        MakeCube(ap, new Vector3(0.22f, 0.32f, 0.16f), new Vector3(0.05f, 0.48f, 0.05f), whiteM);

        // Biểu tượng cột sóng Wi-Fi màu xanh cyan phát sáng ngay trên AP
        Color cyanGlow = new Color(0.2f, 0.9f, 1f, 0.95f);
        MakeRingArc(ap, new Vector3(0, 0.52f, 0), 0.20f, 40f, 140f, cyanGlow, 0.045f);
        MakeRingArc(ap, new Vector3(0, 0.58f, 0), 0.34f, 40f, 140f, cyanGlow, 0.045f);
        MakeRingArc(ap, new Vector3(0, 0.64f, 0), 0.48f, 40f, 140f, cyanGlow, 0.045f);

        // Vòm bán cầu (Hemisphere) màu cam trong suốt
        CreateHemisphereDome(ap, Vector3.zero, domeRadius, domeRadius * 0.72f,
            new Color(1f, 0.48f, 0.10f, 0.20f), new Color(1f, 0.62f, 0.20f, 0.85f));

        // Vòm sóng xanh dương phụ ở góc phải dưới (giống hệt ảnh mẫu)
        if (hasBlueSecondaryDome)
        {
            CreateHemisphereDome(ap, new Vector3(-0.35f, -0.45f, -0.2f), domeRadius * 1.18f, domeRadius * 0.85f,
                new Color(0.10f, 0.72f, 1f, 0.15f), new Color(0.25f, 0.85f, 1f, 0.75f));
        }

        if (!string.IsNullOrEmpty(title))
            Make3DText(ap, title, new Vector3(0, domeRadius * 0.78f + 0.35f, 0), 3.0f, Color.white, true);
    }

    void CreateHemisphereDome(Transform parent, Vector3 localOffset, float radiusXZ, float heightY, Color fillCol, Color ringCol)
    {
        GameObject domeObj = new GameObject("Wifi_Hemisphere_Dome", typeof(MeshFilter), typeof(MeshRenderer));
        domeObj.transform.SetParent(parent, false);
        domeObj.transform.localPosition = localOffset;

        // Tạo Mesh nửa hình cầu (chỉ lấy phần trên mặt phẳng sàn)
        Mesh mesh = new Mesh();
        int lon = 28, lat = 14;
        Vector3[] verts = new Vector3[(lon + 1) * (lat + 1)];
        int[] tris = new int[lon * lat * 6];

        int vIdx = 0;
        for (int y = 0; y <= lat; y++)
        {
            float phi = (float)y / lat * (Mathf.PI * 0.5f);
            for (int x = 0; x <= lon; x++)
            {
                float theta = (float)x / lon * Mathf.PI * 2f;
                verts[vIdx++] = new Vector3(
                    Mathf.Cos(phi) * Mathf.Cos(theta) * radiusXZ,
                    Mathf.Sin(phi) * heightY,
                    Mathf.Cos(phi) * Mathf.Sin(theta) * radiusXZ
                );
            }
        }

        int tIdx = 0;
        for (int y = 0; y < lat; y++)
        {
            for (int x = 0; x < lon; x++)
            {
                int cur = y * (lon + 1) + x;
                int next = cur + lon + 1;
                tris[tIdx++] = cur; tris[tIdx++] = next; tris[tIdx++] = cur + 1;
                tris[tIdx++] = cur + 1; tris[tIdx++] = next; tris[tIdx++] = next + 1;
            }
        }
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        domeObj.GetComponent<MeshFilter>().mesh = mesh;
        domeObj.GetComponent<MeshRenderer>().material = CreateMat(fillCol, true);

        // Vẽ các vòng tròn phát sáng bao quanh chân và thân vòm sóng
        MakeCircleRing(domeObj.transform, Vector3.zero, radiusXZ, ringCol, 0.055f);
        MakeCircleRing(domeObj.transform, new Vector3(0, heightY * 0.45f, 0), radiusXZ * 0.88f, new Color(ringCol.r, ringCol.g, ringCol.b, 0.35f), 0.03f);
    }

    void CreateNetworkCable(Vector3 a, Vector3 b, Color col, string label, int numPackets)
    {
        // Lớp hào quang ngoài + Lõi dây phát sáng bên trong
        MakeLine(buildingRoot, a, b, new Color(col.r, col.g, col.b, 0.28f), 0.22f);
        MakeLine(buildingRoot, a, b, col, 0.075f);

        Material dotMat = CreateMat(new Color(1f, 1f, 0.45f, 1f), true);
        for (int i = 0; i < numPackets; i++)
        {
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(dot.GetComponent<Collider>());
            dot.transform.SetParent(buildingRoot, false);
            dot.transform.localScale = Vector3.one * 0.16f;
            dot.GetComponent<Renderer>().material = dotMat;

            packets.Add(new PacketFlow
            {
                dot = dot.transform,
                startPos = buildingRoot.TransformPoint(a),
                endPos = buildingRoot.TransformPoint(b),
                speed = Random.Range(0.32f, 0.58f),
                t = (float)i / numPackets
            });
        }

        if (!string.IsNullOrEmpty(label))
        {
            Vector3 mid = (a + b) * 0.5f + new Vector3(0, 0.35f, 0);
            GameObject tagObj = Make3DText(buildingRoot, label, mid, 2.5f, new Color(0.9f, 1f, 0.9f), true);
            vlanLabels.Add(tagObj);
        }
    }

    void BuildAxisGizmo(Transform parent, Vector3 pos)
    {
        Transform g = new GameObject("Axis_Gizmo").transform;
        g.SetParent(parent, false);
        g.localPosition = pos;
        MakeLine(g, Vector3.zero, new Vector3(0.9f, 0, 0), Color.red, 0.045f);
        MakeLine(g, Vector3.zero, new Vector3(0, 0.9f, 0), Color.green, 0.045f);
        MakeLine(g, Vector3.zero, new Vector3(0, 0, 0.9f), Color.cyan, 0.045f);
        Make3DText(g, "x", new Vector3(1.1f, 0, 0), 2.2f, Color.red, false);
        Make3DText(g, "y", new Vector3(0, 1.1f, 0), 2.2f, Color.green, false);
        Make3DText(g, "z", new Vector3(0, 0, 1.1f), 2.2f, Color.cyan, false);
    }

    GameObject MakeCube(Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(c.GetComponent<Collider>());
        c.transform.SetParent(parent, false);
        c.transform.localPosition = pos;
        c.transform.localScale = scale;
        c.GetComponent<Renderer>().material = mat;
        return c;
    }

    void MakeWireframeBox(Transform parent, Vector3 center, Vector3 size, Material mat, float width)
    {
        Vector3 h = size * 0.5f;
        Vector3[] top = {
            center + new Vector3(-h.x, h.y, -h.z), center + new Vector3(h.x, h.y, -h.z),
            center + new Vector3(h.x, h.y, h.z), center + new Vector3(-h.x, h.y, h.z),
            center + new Vector3(-h.x, h.y, -h.z)
        };
        GameObject go = new GameObject("WireEdge");
        go.transform.SetParent(parent, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = top.Length;
        lr.SetPositions(top);
        lr.startWidth = width; lr.endWidth = width;
        lr.material = mat;
    }

    void MakeLine(Transform parent, Vector3 a, Vector3 b, Color col, float width)
    {
        GameObject go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, a); lr.SetPosition(1, b);
        lr.startWidth = width; lr.endWidth = width;
        lr.material = CreateMat(col, true);
    }

    void MakeCircleRing(Transform parent, Vector3 center, float radius, Color col, float width)
    {
        int segs = 40;
        GameObject go = new GameObject("CircleRing");
        go.transform.SetParent(parent, false);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = segs + 1;
        for (int i = 0; i <= segs; i++)
        {
            float a = (float)i / segs * Mathf.PI * 2f;
            lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius));
        }
        lr.startWidth = width; lr.endWidth = width;
        lr.material = CreateMat(col, true);
    }

    void MakeRingArc(Transform parent, Vector3 center, float radius, float startDeg, float endDeg, Color col, float width)
    {
        int segs = 16;
        GameObject go = new GameObject("WaveArc");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.rotation = Quaternion.Euler(29f, 45f, 0f);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = segs + 1;
        for (int i = 0; i <= segs; i++)
        {
            float rad = Mathf.Lerp(startDeg, endDeg, (float)i / segs) * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0));
        }
        lr.startWidth = width; lr.endWidth = width;
        lr.material = CreateMat(col, true);
    }

    GameObject Make3DText(Transform parent, string txt, Vector3 pos, float fontSize, Color col, bool withDarkBadge)
    {
        GameObject go = new GameObject("Label_" + txt);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.rotation = Quaternion.Euler(29f, 45f, 0f);

        if (withDarkBadge)
        {
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(bg.GetComponent<Collider>());
            bg.transform.SetParent(go.transform, false);
            bg.transform.localPosition = new Vector3(0, 0, 0.05f);
            float w = Mathf.Max(1.6f, txt.Length * 0.18f);
            bg.transform.localScale = new Vector3(w, txt.Contains("\n") ? 0.9f : 0.48f, 1f);
            bg.GetComponent<Renderer>().material = CreateMat(new Color(0.04f, 0.08f, 0.14f, 0.78f), true);
        }

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = txt;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = col;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = new Vector2(10f, 3f);
        return go;
    }

    // =========================================================================
    // PHẦN 3: DỰNG TOÀN BỘ 6 KHUNG GIAO DIỆN DASHBOARD Y HỆT ẢNH THIẾT KẾ
    // =========================================================================
    void BuildCompleteDashboardUI()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject canvasObj = new GameObject("Canvas_UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Vùng tương tác xoay/zoom 3D ở giữa màn hình (An toàn 100% với mọi hệ thống Input của Unity 6)
        RectTransform viewportDrag = CreateUIPanel(canvas.transform, "Viewport_Interactive", Vector2.zero, Vector2.one, new Vector2(315, 200), new Vector2(-315, -70), Color.clear);
        ViewportDragController dragCtrl = viewportDrag.gameObject.AddComponent<ViewportDragController>();
        dragCtrl.targetRoot = buildingRoot;

        Color headerBg = new Color(0.06f, 0.09f, 0.14f, 0.98f);
        Color panelBg = new Color(0.08f, 0.12f, 0.18f, 0.93f);
        Color headerStrip = new Color(0.13f, 0.19f, 0.27f, 0.95f);
        Color cardBg = new Color(0.12f, 0.17f, 0.24f, 0.96f);

        // ---------------------------------------------------------------------
        // 1. THANH HEADER TRÊN CÙNG (LOGO CICT + TIÊU ĐỀ + CHẾ ĐỘ XEM 3D + NÚT ĐIỀU KHIỂN)
        // ---------------------------------------------------------------------
        RectTransform topBar = CreateUIPanel(canvas.transform, "Top_Bar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -68), Vector2.zero, headerBg);

        // Logo CICT hình thoi ở góc trái trên
        RectTransform logoBox = CreateUIPanel(topBar, "Logo_Diamond", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, -22), new Vector2(62, 22), new Color(0.12f, 0.68f, 0.98f));
        logoBox.localRotation = Quaternion.Euler(0, 0, 45f);
        RectTransform innerLogo = CreateUIPanel(logoBox, "Inner", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-14, -14), new Vector2(14, 14), new Color(0.05f, 0.18f, 0.35f));

        CreateUIText(topBar, "CICT-NetTwin 3D v1.0 - GIÁM SÁT & MÔ PHỎNG MẠNG TRƯỜNG CNTT&TT - ĐẠI HỌC CẦN THƠ", 21, FontStyles.Bold, Color.white, new Vector2(82, -8), new Vector2(1250, 32));
        CreateUIText(topBar, "Trường Công nghệ Thông tin và Truyền thông (CICT) - Đại học Cần Thơ", 14, FontStyles.Normal, new Color(0.75f, 0.85f, 0.95f), new Vector2(82, -38), new Vector2(850, 24));

        // Hộp CHẾ ĐỘ XEM 3D: [Vật lý] [Logic] [VLAN]
        RectTransform viewModeBox = CreateUIPanel(topBar, "ViewMode_Box", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-535, -27), new Vector2(-225, 27), cardBg);
        CreateUIText(viewModeBox, "CHẾ ĐỘ XEM 3D", 12, FontStyles.Bold, Color.white, new Vector2(98, -3), new Vector2(150, 18));
        CreateUIButton(viewModeBox, "Vật lý", new Vector2(12, -23), new Vector2(88, 25), new Color(0.20f, 0.46f, 0.72f), () => SetViewMode(0));
        CreateUIButton(viewModeBox, "Logic", new Vector2(110, -23), new Vector2(88, 25), new Color(0.17f, 0.23f, 0.31f), () => SetViewMode(1));
        CreateUIButton(viewModeBox, "VLAN", new Vector2(208, -23), new Vector2(88, 25), new Color(0.17f, 0.23f, 0.31f), () => SetViewMode(2));

        // 3 Nút Play (▶), Pause (||), Reset (↻)
        CreateUIButton(topBar, "▶", new Vector2(1715, -14), new Vector2(54, 40), new Color(0.22f, 0.62f, 0.32f), () => isSimRunning = true);
        CreateUIButton(topBar, "||", new Vector2(1780, -14), new Vector2(54, 40), new Color(0.55f, 0.43f, 0.18f), () => isSimRunning = false);
        CreateUIButton(topBar, "↻", new Vector2(1845, -14), new Vector2(54, 40), new Color(0.68f, 0.24f, 0.20f), () => { buildingRoot.rotation = Quaternion.Euler(0f, -11f, 0f); isSimRunning = true; });

        // ---------------------------------------------------------------------
        // 2. THANH CÔNG CỤ DỌC BÊN TRÁI CÙNG (LEFT TOOL STRIP)
        // ---------------------------------------------------------------------
        RectTransform leftStrip = CreateUIPanel(canvas.transform, "Left_Tool_Strip", new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(42, -68), headerBg);
        string[] toolIcons = { "↖", "⊞", "☍", "❐", "⌲", "☁" };
        for (int i = 0; i < toolIcons.Length; i++)
        {
            Color bg = (i == 1) ? new Color(0.18f, 0.45f, 0.75f) : Color.clear;
            CreateUIButton(leftStrip, toolIcons[i], new Vector2(4, -14 - i * 48), new Vector2(34, 36), bg, null);
        }
        CreateUIButton(leftStrip, "⚙", new Vector2(4, -965), new Vector2(34, 36), Color.clear, null);

        // ---------------------------------------------------------------------
        // 3. BẢNG THƯ VIỆN THIẾT BỊ 3D (6 Ô KÈM HÌNH VẼ MINH HỌA THIẾT BỊ)
        // ---------------------------------------------------------------------
        RectTransform libPanel = CreateUIPanel(canvas.transform, "Panel_Library", new Vector2(0, 0), new Vector2(0, 1), new Vector2(48, 205), new Vector2(315, -75), panelBg);
        CreateUIPanel(libPanel, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -36), Vector2.zero, headerStrip);
        CreateUIText(libPanel, "THƯ VIỆN THIẾT BỊ 3D            ✕", 15, FontStyles.Bold, Color.white, new Vector2(14, -8), new Vector2(255, 24));

        string[] devTitles = { "Router L3", "Switch", "Server", "PC", "AP", "Tủ Rack" };
        for (int i = 0; i < 6; i++)
        {
            int r = i / 2, c = i % 2;
            RectTransform card = CreateUIPanel(libPanel, "Card_" + devTitles[i], new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(14 + c * 124, -172 - r * 136), new Vector2(128 + c * 124, -48 - r * 136), cardBg);

            // Vẽ hình minh họa thiết bị chuẩn xác cho từng ô
            RawImage iconImg = new GameObject("Icon", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            iconImg.transform.SetParent(card, false);
            iconImg.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            iconImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            iconImg.rectTransform.sizeDelta = new Vector2(76, 64);
            iconImg.rectTransform.anchoredPosition = new Vector2(0, 12);
            iconImg.texture = DrawDeviceIconTexture(i);

            TMP_Text lbl = CreateUIText(card, devTitles[i], 14, FontStyles.Normal, Color.white, new Vector2(0, -92), new Vector2(114, 24));
            lbl.alignment = TextAlignmentOptions.Center;
        }

        // ---------------------------------------------------------------------
        // 4. BẢNG CHI TIẾT THIẾT BỊ & BIỂU ĐỒ BĂNG THÔNG (GÓC PHẢI TRÊN)
        // ---------------------------------------------------------------------
        RectTransform inspPanel = CreateUIPanel(canvas.transform, "Panel_Inspector", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-315, -495), new Vector2(-8, -75), panelBg);
        CreateUIPanel(inspPanel, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -36), Vector2.zero, headerStrip);
        CreateUIText(inspPanel, "CHI TIẾT THIẾT BỊ                           ✕", 15, FontStyles.Bold, Color.white, new Vector2(14, -8), new Vector2(295, 24));

        // Icon Switch nhỏ cạnh tên thiết bị
        RawImage swMini = new GameObject("SwMini", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        swMini.transform.SetParent(inspPanel, false);
        swMini.rectTransform.anchorMin = new Vector2(0, 1); swMini.rectTransform.anchorMax = new Vector2(0, 1);
        swMini.rectTransform.anchoredPosition = new Vector2(36, -64);
        swMini.rectTransform.sizeDelta = new Vector2(44, 28);
        swMini.texture = DrawDeviceIconTexture(1);

        inspectorFields[0] = CreateUIText(inspPanel, "CICT_Core_Switch", 16, FontStyles.Bold, Color.white, new Vector2(68, -52), new Vector2(230, 26));
        inspectorFields[1] = CreateUIText(inspPanel, "Tên:          CICT_Core_Switch", 15, FontStyles.Normal, Color.white, new Vector2(16, -94), new Vector2(280, 24));
        inspectorFields[2] = CreateUIText(inspPanel, "IP:             10.10.0.1", 15, FontStyles.Normal, Color.white, new Vector2(16, -125), new Vector2(280, 24));
        inspectorFields[3] = CreateUIText(inspPanel, "VLANs:     10, 20, 30", 15, FontStyles.Normal, Color.white, new Vector2(16, -156), new Vector2(280, 24));
        inspectorFields[4] = CreateUIText(inspPanel, "Trạng thái: <color=#55FF55>Đang hoạt động</color>", 15, FontStyles.Normal, Color.white, new Vector2(16, -187), new Vector2(280, 24));

        CreateUIText(inspPanel, "Băng thông                                          nhấp", 13, FontStyles.Normal, new Color(0.75f, 0.82f, 0.90f), new Vector2(16, -225), new Vector2(285, 22));
        CreateUIText(inspPanel, "200k\n150k\n100k\n  50k\n      0", 11, FontStyles.Normal, new Color(0.7f, 0.76f, 0.82f), new Vector2(10, -250), new Vector2(40, 110));

        RectTransform chartFrame = CreateUIPanel(inspPanel, "Chart_Box", new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -356), new Vector2(294, -250), new Color(0.05f, 0.08f, 0.12f, 0.95f));
        bandwidthChartImg = new GameObject("Chart_Raw", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        bandwidthChartImg.transform.SetParent(chartFrame, false);
        bandwidthChartImg.rectTransform.anchorMin = Vector2.zero; bandwidthChartImg.rectTransform.anchorMax = Vector2.one;
        bandwidthChartImg.rectTransform.offsetMin = Vector2.zero; bandwidthChartImg.rectTransform.offsetMax = Vector2.zero;
        bandwidthChartImg.texture = DrawBandwidthChart(0f);

        CreateUIText(inspPanel, "00:00                                                     16:00", 11, FontStyles.Normal, new Color(0.7f, 0.76f, 0.82f), new Vector2(48, -360), new Vector2(250, 20));

        // ---------------------------------------------------------------------
        // 5. BẢNG MÔ PHỎNG GÓI TIN (GÓC DƯỚI TRÁI)
        // ---------------------------------------------------------------------
        RectTransform simPanel = CreateUIPanel(canvas.transform, "Panel_PacketSim", new Vector2(0, 0), new Vector2(0, 0), new Vector2(360, 8), new Vector2(1150, 196), panelBg);
        CreateUIPanel(simPanel, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -34), Vector2.zero, headerStrip);
        CreateUIText(simPanel, "MÔ PHỎNG GÓI TIN                                                                                                                 ✕", 15, FontStyles.Bold, Color.white, new Vector2(14, -7), new Vector2(775, 24));

        CreateUIText(simPanel, "Tên:", 15, FontStyles.Normal, Color.white, new Vector2(16, -55), new Vector2(50, 26));
        CreateUIButton(simPanel, "CICT", new Vector2(65, -48), new Vector2(170, 34), cardBg, null);
        CreateUIText(simPanel, "IP:", 15, FontStyles.Normal, Color.white, new Vector2(255, -55), new Vector2(40, 26));
        CreateUIButton(simPanel, "10.10.0.1", new Vector2(288, -48), new Vector2(190, 34), cardBg, null);

        CreateUIText(simPanel, "VLAN:", 15, FontStyles.Normal, Color.white, new Vector2(16, -105), new Vector2(60, 26));
        CreateUIButton(simPanel, "10, 20                      ▼", new Vector2(65, -98), new Vector2(170, 34), cardBg, null);
        CreateUIText(simPanel, "→", 16, FontStyles.Bold, Color.white, new Vector2(252, -103), new Vector2(30, 26));
        CreateUIButton(simPanel, "Đang hoạt động     ▼", new Vector2(288, -98), new Vector2(190, 34), cardBg, null);
        CreateUIButton(simPanel, "Traffic", new Vector2(495, -98), new Vector2(92, 34), new Color(0.18f, 0.38f, 0.58f), TriggerTrafficBurst);

        // ---------------------------------------------------------------------
        // 6. CỬA SỔ DÒNG LỆNH CLI (GÓC DƯỚI PHẢI - CÓ Ô NHẬP LỆNH THỰC TẾ)
        // ---------------------------------------------------------------------
        RectTransform cliPanel = CreateUIPanel(canvas.transform, "Panel_CLI", new Vector2(0, 0), new Vector2(1, 0), new Vector2(1160, 8), new Vector2(-8, 196), new Color(0.04f, 0.07f, 0.10f, 0.96f));
        CreateUIPanel(cliPanel, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -34), Vector2.zero, headerStrip);
        CreateUIText(cliPanel, "CLI                                                                                                                           —   ▢   ✕", 15, FontStyles.Bold, Color.white, new Vector2(14, -7), new Vector2(735, 24));

        string cliInit = "<color=#55FF55>clis ~$</color> ping 10.10.10.50\n" +
                         "<color=#66CC66>ping 10.10.10.50, 12 bytes from 395 ms</color>\n\n" +
                         "<color=#66CC66>cli: pin></color> traceroute\n" +
                         "1  10.10.10.50  3  ↑traceroute\n" +
                         "2  ...\n" +
                         "HÀNH TRÌNH GÓI TIN: Phòng Lab 201 -> Server DMZ (<color=#55FF55>Thành công</color>)";
        cliTextOutput = CreateUIText(cliPanel, cliInit, 13, FontStyles.Normal, Color.white, new Vector2(14, -38), new Vector2(715, 118));

        // Ô nhập lệnh CLI tương tác ở dòng cuối
        GameObject inputGO = new GameObject("CLI_Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputGO.transform.SetParent(cliPanel, false);
        RectTransform inRT = inputGO.GetComponent<RectTransform>();
        inRT.anchorMin = new Vector2(0, 0); inRT.anchorMax = new Vector2(1, 0);
        inRT.offsetMin = new Vector2(14, 6); inRT.offsetMax = new Vector2(-14, 30);
        inputGO.GetComponent<Image>().color = new Color(0.08f, 0.13f, 0.19f, 0.9f);

        TMP_Text inTxt = CreateUIText(inRT, "", 13, FontStyles.Normal, new Color(0.4f, 1f, 0.5f), new Vector2(8, -3), new Vector2(680, 20));
        TMP_Text placeTxt = CreateUIText(inRT, "Nhập lệnh (vd: ping 10.10.10.50, traceroute, clear) rồi nhấn Enter...", 12, FontStyles.Italic, new Color(0.5f, 0.6f, 0.7f, 0.6f), new Vector2(8, -3), new Vector2(680, 20));
        TMP_InputField inputField = inputGO.GetComponent<TMP_InputField>();
        inputField.textComponent = inTxt;
        inputField.placeholder = placeTxt;
        inputField.onSubmit.AddListener( cmd => {
            if (string.IsNullOrWhiteSpace(cmd)) return;
            ExecuteCLICommand(cmd.Trim());
            inputField.text = "";
            inputField.ActivateInputField();
        });
    }

    void SetViewMode(int mode)
    {
        // 0: Vật lý, 1: Logic, 2: VLAN
        foreach (var w in physicalWalls) if (w != null) w.SetActive(mode == 0);
        foreach (var l in vlanLabels) if (l != null) l.SetActive(mode != 1);
    }

    void TriggerTrafficBurst()
    {
        foreach (var p in packets) p.speed = Random.Range(0.75f, 1.25f);
    }

    void ExecuteCLICommand(string cmd)
    {
        string lower = cmd.ToLower();
        if (lower == "clear" || lower == "cls")
        {
            cliTextOutput.text = "<color=#55FF55>clis ~$</color> ";
        }
        else if (lower.StartsWith("ping"))
        {
            cliTextOutput.text = "<color=#55FF55>clis ~$</color> " + cmd + "\n" +
                                 "<color=#66CC66>Reply from 10.10.10.50: bytes=32 time=1.2ms TTL=64</color>\n" +
                                 "HÀNH TRÌNH GÓI TIN: Phòng Lab 201 -> CICT_Core_Switch -> Server DMZ (<color=#55FF55>Thành công</color>)";
            TriggerTrafficBurst();
        }
        else
        {
            cliTextOutput.text = "<color=#55FF55>clis ~$</color> " + cmd + "\n" +
                                 "1  10.10.0.1 (CICT_Core_Switch)  0.8 ms\n" +
                                 "2  10.10.10.50 (Server_DMZ)      1.4 ms\n" +
                                 "HÀNH TRÌNH GÓI TIN: Phòng Lab 201 -> Server DMZ (<color=#55FF55>Thành công</color>)";
        }
    }

    // Vẽ biểu đồ Băng thông màu Xanh lá & Cam nhấp nhô như ảnh mẫu
    Texture2D DrawBandwidthChart(float timeOffset)
    {
        int w = 240, h = 100;
        Texture2D tex = new Texture2D(w, h);
        Color bg = new Color(0.05f, 0.08f, 0.12f, 1f);
        Color orange = new Color(0.92f, 0.50f, 0.16f, 0.92f);
        Color green = new Color(0.25f, 0.78f, 0.36f, 0.88f);

        for (int x = 0; x < w; x++)
        {
            float nx = (float)x / w;
            float val = 0.06f;
            if (nx > 0.17f && nx < 0.23f) val = Mathf.Sin((nx - 0.17f) * 52f) * 0.36f;
            else if (nx > 0.41f && nx < 0.46f) val = 0.28f;
            else if (nx > 0.50f && nx < 0.93f)
                val = 0.24f + Mathf.Abs(Mathf.Sin((x + timeOffset * 3f) * 0.24f) * Mathf.Cos(x * 0.12f)) * 0.66f;

            int peak = Mathf.Clamp(Mathf.RoundToInt(val * h), 2, h - 2);
            bool isGreenZone = (nx > 0.14f && nx < 0.48f);

            for (int y = 0; y < h; y++)
            {
                if (y <= peak) tex.SetPixel(x, y, isGreenZone ? green : orange);
                else if (y % 24 == 0) tex.SetPixel(x, y, new Color(1, 1, 1, 0.07f));
                else tex.SetPixel(x, y, bg);
            }
        }
        tex.Apply();
        return tex;
    }

    // Vẽ hình minh họa 3D cho 6 thiết bị trong bảng THƯ VIỆN THIẾT BỊ 3D
    Texture2D DrawDeviceIconTexture(int type)
    {
        int w = 64, h = 64;
        Texture2D tex = new Texture2D(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, Color.clear);

        for (int y = 8; y < 56; y++)
        {
            for (int x = 8; x < 56; x++)
            {
                float dx = (x - 32f) / 22f, dy = (y - 32f) / 14f;
                if (type == 0) // Router L3 (Hình trụ tròn xanh + chữ X trắng)
                {
                    if (dx * dx + dy * dy <= 1f)
                    {
                        bool cross = Mathf.Abs(Mathf.Abs(x - 32) - Mathf.Abs(y - 34)) < 2.2f && (dx * dx + dy * dy < 0.55f);
                        tex.SetPixel(x, y, cross ? Color.white : new Color(0.15f, 0.62f, 0.96f));
                    }
                }
                else if (type == 1) // Switch (Hộp chữ nhật xanh xám + đèn cổng mạng)
                {
                    if (Mathf.Abs(x - 32) < 22 && Mathf.Abs(y - 30) < 10)
                    {
                        bool port = (y >= 24 && y <= 27 && x % 4 == 0 && Mathf.Abs(x - 32) < 18);
                        tex.SetPixel(x, y, port ? new Color(0.3f, 1f, 0.5f) : new Color(0.28f, 0.44f, 0.72f));
                    }
                }
                else if (type == 2 || type == 5) // Server / Tủ Rack (Tháp đứng màu đen xám + đèn LED)
                {
                    if (Mathf.Abs(x - 32) < 12 && Mathf.Abs(y - 32) < 22)
                    {
                        bool led = (y % 6 == 0 && Mathf.Abs(x - 32) < 8);
                        tex.SetPixel(x, y, led ? (type == 5 ? new Color(1f, 0.5f, 0.1f) : new Color(0.2f, 0.9f, 1f)) : new Color(0.22f, 0.27f, 0.34f));
                    }
                }
                else if (type == 3) // PC (Màn hình xanh + Thùng máy)
                {
                    if (x > 12 && x < 38 && y > 24 && y < 46) tex.SetPixel(x, y, new Color(0.25f, 0.75f, 0.98f));
                    if (x >= 40 && x < 52 && y > 18 && y < 46) tex.SetPixel(x, y, new Color(0.25f, 0.32f, 0.42f));
                }
                else if (type == 4) // AP (Hộp trắng + 2 râu ăng ten)
                {
                    if (Mathf.Abs(x - 32) < 16 && y > 14 && y < 28) tex.SetPixel(x, y, new Color(0.92f, 0.95f, 1f));
                    if ((Mathf.Abs(x - 22) < 2 || Mathf.Abs(x - 42) < 2) && y >= 28 && y < 50) tex.SetPixel(x, y, Color.white);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    RectTransform CreateUIPanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Color bg)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = oMin; rt.offsetMax = oMax;
        go.GetComponent<Image>().color = bg;
        return rt;
    }

    RectTransform CreateUIButton(Transform parent, string text, Vector2 pos, Vector2 size, Color bg, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Btn_" + text, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.GetComponent<Image>().color = bg;
        if (onClick != null) go.GetComponent<Button>().onClick.AddListener(onClick);

        TMP_Text t = CreateUIText(rt, text, 14, FontStyles.Bold, Color.white, Vector2.zero, size);
        t.alignment = TextAlignmentOptions.Center;
        return rt;
    }

    TMP_Text CreateUIText(Transform parent, string content, int size, FontStyles style, Color col, Vector2 pos, Vector2 box)
    {
        GameObject go = new GameObject("TMP", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = col;
        tmp.raycastTarget = false;
        return tmp;
    }
}

// Bộ điều khiển xoay & thu phóng 3D bằng chuột (Hoạt động mượt trên cả Input System mới & cũ của Unity 6)
public class ViewportDragController : MonoBehaviour, IDragHandler, IScrollHandler
{
    public Transform targetRoot;

    public void OnDrag(PointerEventData eventData)
    {
        if (targetRoot != null)
        {
            targetRoot.Rotate(Vector3.up, -eventData.delta.x * 0.35f, Space.World);
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (Camera.main != null && Camera.main.orthographic)
        {
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - eventData.scrollDelta.y * 0.6f, 5.5f, 18f);
        }
    }
}