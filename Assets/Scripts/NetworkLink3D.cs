using System.Collections;
using UnityEngine;

namespace CICT.NetTwin
{
    [RequireComponent(typeof(LineRenderer))]
    public class NetworkLink3D : MonoBehaviour
    {
        public NetworkNode3D nodeA;
        public NetworkNode3D nodeB;
        public LinkType linkType = LinkType.Fiber10Gbps;
        public int activeVlan = 10;

        private LineRenderer lineRenderer;

        public void Initialize(NetworkNode3D a, NetworkNode3D b, LinkType type, int vlan)
        {
            nodeA = a;
            nodeB = b;
            linkType = type;
            activeVlan = vlan;

            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = (type == LinkType.Fiber10Gbps) ? 0.12f : 0.06f;
            lineRenderer.endWidth = lineRenderer.startWidth;

            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

            Color linkColor = GetLinkColor(type, vlan);
            lineRenderer.startColor = linkColor;
            lineRenderer.endColor = linkColor;
        }

        private void Update()
        {
            if (nodeA != null && nodeB != null && lineRenderer != null)
            {
                lineRenderer.SetPosition(0, nodeA.transform.position);
                lineRenderer.SetPosition(1, nodeB.transform.position);
            }
        }

        private Color GetLinkColor(LinkType type, int vlan)
        {
            if (type == LinkType.Fiber10Gbps)
                return new Color(1f, 0.75f, 0.1f, 0.9f);
            if (vlan == 20)
                return new Color(0.1f, 1f, 0.3f, 0.9f);
            return new Color(0.1f, 0.8f, 1f, 0.9f);
        }

        public IEnumerator AnimatePacketHop(NetworkNode3D fromNode, NetworkNode3D toNode, Color packetColor, float duration = 0.45f)
        {
            GameObject packetObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(packetObj.GetComponent<Collider>());
            packetObj.transform.localScale = Vector3.one * 0.35f;

            Renderer rend = packetObj.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = packetColor;
            rend.material = mat;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (fromNode == null || toNode == null) break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                packetObj.transform.position = Vector3.Lerp(fromNode.transform.position, toNode.transform.position, t);
                yield return null;
            }

            Destroy(packetObj);
        }
    }
}