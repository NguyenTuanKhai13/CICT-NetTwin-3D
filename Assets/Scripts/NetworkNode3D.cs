using UnityEngine;
using TMPro;

namespace CICT.NetTwin
{
    [RequireComponent(typeof(BoxCollider))]
    public class NetworkNode3D : MonoBehaviour
    {
        [Header("Dữ liệu cấu hình thiết bị")]
        public DeviceData data = new DeviceData();

        [Header("Thành phần hiển thị 3D")]
        public Renderer meshRenderer;
        public TextMeshPro labelText;
        public GameObject wifiDomeSphere;
        public GameObject selectionHighlight;

        private Vector3 targetWorldPosition;
        private Camera mainCam;

        private void Start()
        {
            mainCam = Camera.main;
            targetWorldPosition = transform.position;
            UpdateVisuals();
        }

        private void Update()
        {
            transform.position = Vector3.Lerp(transform.position, targetWorldPosition, Time.deltaTime * 6f);

            if (labelText != null && mainCam != null)
            {
                labelText.transform.rotation = Quaternion.LookRotation(
                    labelText.transform.position - mainCam.transform.position
                );
            }
        }

        public void Initialize(DeviceData deviceData, Vector3 initialWorldPos)
        {
            data = deviceData;
            transform.position = initialWorldPos;
            targetWorldPosition = initialWorldPos;
            UpdateVisuals();
        }

        public void SetTargetPosition(Vector3 newPos)
        {
            targetWorldPosition = newPos;
        }

        public void UpdateVisuals()
        {
            if (labelText != null)
            {
                labelText.text = $"{data.deviceName}\n<size=70%><color=#88FFAA>{data.ipAddress}</color></size>";
            }

            if (wifiDomeSphere != null)
            {
                wifiDomeSphere.SetActive(data.deviceType == DeviceType.AccessPoint && data.isOnline);
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
                selectionHighlight.SetActive(selected);
        }

        private void OnMouseDown()
        {
            if (CICTNetworkSimulator.Instance != null)
            {
                CICTNetworkSimulator.Instance.SelectNode(this);
            }
        }
    }
}