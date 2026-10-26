using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CICT.NetTwin
{
    public class CICTNetworkSimulator : MonoBehaviour
    {
        public static CICTNetworkSimulator Instance { get; private set; }

        [Header("Tham chiếu Thành phần")]
        public BuildingViewController buildingController;
        public NetworkUIManager uiManager;
        public Transform nodesContainer;
        public Transform linksContainer;

        [Header("Prefabs (Nếu để trống sẽ tự sinh khối 3D mẫu)")]
        public GameObject defaultNodePrefab;

        public List<NetworkNode3D> allNodes = new List<NetworkNode3D>();
        public List<NetworkLink3D> allLinks = new List<NetworkLink3D>();
        public NetworkNode3D selectedNode { get; private set; }

        private bool isTrafficRunning = true;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            BuildDefaultCICTTopology();
            if (buildingController != null)
            {
                buildingController.SwitchViewMode(ViewMode3D.VatLy, allNodes);
            }

            var coreSwitch = allNodes.FirstOrDefault(n => n.data.deviceName == "CICT_Core_Switch");
            if (coreSwitch != null) SelectNode(coreSwitch);

            StartCoroutine(BackgroundTrafficRoutine());
        }

        public void BuildDefaultCICTTopology()
        {
            var coreSwitch = CreateNode("CICT_Core_Switch", DeviceType.SwitchL3, "10.10.0.1", new List<int> { 10, 20, 30 }, 2, new Vector3(0, 0, 0));
            var routerGate = CreateNode("Router_L3_Gateway", DeviceType.RouterL3, "10.1.0.1", new List<int> { 10, 20, 30 }, 2, new Vector3(-4, 0, 1));
            var serverDmz  = CreateNode("Phòng Server DMZ", DeviceType.Server, "10.10.10.50", new List<int> { 10 }, 3, new Vector3(2, 0, 4));

            var swLab201   = CreateNode("Switch_L2_Lab201", DeviceType.SwitchL2, "10.10.20.2", new List<int> { 20 }, 3, new Vector3(-6, 0, 3));
            var pcLab201   = CreateNode("Phòng Lab 201", DeviceType.PC, "10.10.20.15", new List<int> { 20 }, 3, new Vector3(-9, 0, 4));

            var swLab301   = CreateNode("Switch_L2_Lab301", DeviceType.SwitchL2, "10.10.20.3", new List<int> { 20 }, 1, new Vector3(-5, 0, -2));
            var pcLab301   = CreateNode("Phòng Lab 301", DeviceType.PC, "10.10.20.25", new List<int> { 20 }, 1, new Vector3(-8, 0, -3));

            var wifiAp1    = CreateNode("CTU-Wifi AP (Tầng 3)", DeviceType.AccessPoint, "10.10.30.10", new List<int> { 30 }, 3, new Vector3(6, 0, 3));
            var wifiAp2    = CreateNode("CTU-Wifi AP (Tầng 1)", DeviceType.AccessPoint, "10.10.30.11", new List<int> { 30 }, 1, new Vector3(7, 0, -3));

            CreateLink(routerGate, coreSwitch, LinkType.Fiber10Gbps, 10);
            CreateLink(coreSwitch, serverDmz, LinkType.Fiber10Gbps, 10);
            CreateLink(coreSwitch, swLab201, LinkType.Fiber10Gbps, 20);
            CreateLink(swLab201, pcLab201, LinkType.Copper1Gbps, 20);
            CreateLink(coreSwitch, swLab301, LinkType.Fiber10Gbps, 20);
            CreateLink(swLab301, pcLab301, LinkType.Copper1Gbps, 20);
            CreateLink(coreSwitch, wifiAp1, LinkType.Copper1Gbps, 30);
            CreateLink(coreSwitch, wifiAp2, LinkType.Copper1Gbps, 30);
        }

        private NetworkNode3D CreateNode(string name, DeviceType type, string ip, List<int> vlans, int floor, Vector3 localXZ)
        {
            GameObject obj;
            if (defaultNodePrefab != null)
            {
                obj = Instantiate(defaultNodePrefab, nodesContainer);
            }
            else
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.transform.SetParent(nodesContainer);
                obj.transform.localScale = new Vector3(1.1f, 0.55f, 1.1f);
            }

            obj.name = name;
            var node = obj.GetComponent<NetworkNode3D>();
            if (node == null) node = obj.AddComponent<NetworkNode3D>();

            DeviceData d = new DeviceData
            {
                deviceId = System.Guid.NewGuid().ToString(),
                deviceName = name,
                deviceType = type,
                ipAddress = ip,
                vlans = vlans,
                floorIndex = floor,
                physicalLocalPos = localXZ
            };

            Vector3 initPos = new Vector3(localXZ.x, floor * 4.5f + 0.6f, localXZ.z);
            node.Initialize(d, initPos);
            allNodes.Add(node);
            return node;
        }

        private NetworkLink3D CreateLink(NetworkNode3D a, NetworkNode3D b, LinkType type, int vlan)
        {
            GameObject linkObj = new GameObject($"Link_{a.data.deviceName}_{b.data.deviceName}");
            linkObj.transform.SetParent(linksContainer);
            var link = linkObj.AddComponent<NetworkLink3D>();
            link.Initialize(a, b, type, vlan);
            allLinks.Add(link);
            return link;
        }

        public void SelectNode(NetworkNode3D node)
        {
            foreach (var n in allNodes) n.SetSelected(n == node);
            selectedNode = node;
            if (uiManager != null) uiManager.UpdateInspectorPanel(node.data);
        }

        public void ExecutePingOrTrace(string targetIp, bool isTraceroute)
        {
            NetworkNode3D sourceNode = selectedNode != null
                ? selectedNode
                : allNodes.FirstOrDefault(n => n.data.deviceName.Contains("Phòng Lab 201"));

            NetworkNode3D targetNode = allNodes.FirstOrDefault(n => n.data.ipAddress == targetIp);

            if (sourceNode == null || targetNode == null)
            {
                uiManager?.AppendCLILog($"<color=#FF5555>Lỗi: Không tìm thấy đích {targetIp} trong bảng định tuyến CICT.</color>");
                return;
            }

            List<NetworkNode3D> path = FindShortestPathBFS(sourceNode, targetNode);
            if (path == null || path.Count == 0)
            {
                uiManager?.AppendCLILog($"<color=#FF5555>Request timed out: Không có tuyến đường vật lý từ {sourceNode.data.deviceName} tới {targetIp}.</color>");
                return;
            }

            StartCoroutine(SimulatePacketAlongPath(path, isTraceroute));
        }

        private List<NetworkNode3D> FindShortestPathBFS(NetworkNode3D start, NetworkNode3D goal)
        {
            var queue = new Queue<NetworkNode3D>();
            var cameFrom = new Dictionary<NetworkNode3D, NetworkNode3D>();
            queue.Enqueue(start);
            cameFrom[start] = null;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == goal) break;

                foreach (var link in allLinks)
                {
                    NetworkNode3D neighbor = null;
                    if (link.nodeA == current) neighbor = link.nodeB;
                    else if (link.nodeB == current) neighbor = link.nodeA;

                    if (neighbor != null && neighbor.data.isOnline && !cameFrom.ContainsKey(neighbor))
                    {
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (!cameFrom.ContainsKey(goal)) return null;

            var path = new List<NetworkNode3D>();
            for (var at = goal; at != null; at = cameFrom[at])
                path.Add(at);
            path.Reverse();
            return path;
        }

        private IEnumerator SimulatePacketAlongPath(List<NetworkNode3D> path, bool isTraceroute)
        {
            NetworkNode3D src = path.First();
            NetworkNode3D dst = path.Last();

            if (!isTraceroute)
                uiManager?.AppendCLILog($"ping {dst.data.ipAddress}, 32 bytes từ {src.data.deviceName}...");
            else
                uiManager?.AppendCLILog($"traceroute tới {dst.data.ipAddress} ({dst.data.deviceName}):");

            for (int i = 0; i < path.Count - 1; i++)
            {
                var from = path[i];
                var to = path[i + 1];
                var link = allLinks.FirstOrDefault(l =>
                    (l.nodeA == from && l.nodeB == to) || (l.nodeB == from && l.nodeA == to));

                if (link != null)
                {
                    yield return StartCoroutine(link.AnimatePacketHop(from, to, Color.green, 0.4f));
                    if (isTraceroute)
                    {
                        uiManager?.AppendCLILog($"  {i + 1}   {to.data.ipAddress}   ({to.data.deviceName})   {(i + 1) * 4} ms");
                    }
                }
            }

            uiManager?.AppendCLILog($"<color=#44FF66>HÀNH TRÌNH GÓI TIN: {src.data.deviceName} -> {dst.data.deviceName} (Thành công)</color>");
        }

        private IEnumerator BackgroundTrafficRoutine()
        {
            while (true)
            {
                if (isTrafficRunning && allLinks.Count > 0)
                {
                    var randomLink = allLinks[Random.Range(0, allLinks.Count)];
                    bool forward = Random.value > 0.5f;
                    StartCoroutine(randomLink.AnimatePacketHop(
                        forward ? randomLink.nodeA : randomLink.nodeB,
                        forward ? randomLink.nodeB : randomLink.nodeA,
                        new Color(0.5f, 1f, 0.3f),
                        0.6f
                    ));
                }
                yield return new WaitForSeconds(0.35f);
            }
        }

        public void SetSimulationPlayState(bool play) => isTrafficRunning = play;
    }
}