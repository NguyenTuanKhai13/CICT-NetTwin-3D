using UnityEngine;
using TMPro;

namespace CICT.NetTwin
{
    public class NetworkUIManager : MonoBehaviour
    {
        [Header("Bảng Chi Tiết Thiết Bị (Góc phải)")]
        public TMP_Text txtDeviceName;
        public TMP_Text txtIPAddress;
        public TMP_Text txtVLANs;
        public TMP_Text txtStatus;

        [Header("Cửa sổ Dòng lệnh CLI (Góc dưới phải)")]
        public TMP_InputField cliInputField;
        public TMP_Text cliOutputText;

        private void Start()
        {
            if (cliInputField != null)
            {
                cliInputField.onSubmit.AddListener(OnCLICommandSubmitted);
            }

            AppendCLILog("<color=#88CCFF>CICT-NetTwin 3D v1.0 CLI - Trường CNTT&TT (ĐH Cần Thơ)</color>");
            AppendCLILog("Gõ 'ping 10.10.10.50' hoặc 'traceroute 10.10.10.50' để kiểm tra luồng gói tin.");
        }

        public void UpdateInspectorPanel(DeviceData data)
        {
            if (txtDeviceName != null) txtDeviceName.text = data.deviceName;
            if (txtIPAddress != null) txtIPAddress.text = data.ipAddress;
            if (txtVLANs != null) txtVLANs.text = string.Join(", ", data.vlans);
            if (txtStatus != null)
            {
                txtStatus.text = data.isOnline ? "<color=#44FF66>Đang hoạt động</color>" : "<color=#FF4444>Ngắt kết nối</color>";
            }
        }

        public void OnCLICommandSubmitted(string rawCommand)
        {
            if (string.IsNullOrWhiteSpace(rawCommand)) return;

            string cmd = rawCommand.Trim();
            AppendCLILog($"<color=#AAFF00>cli:~$ {cmd}</color>");
            cliInputField.text = "";
            cliInputField.ActivateInputField();

            string[] parts = cmd.Split(' ');
            string action = parts[0].ToLowerInvariant();
            string targetIp = parts.Length > 1 ? parts[1] : "10.10.10.50";

            if (action == "ping")
            {
                CICTNetworkSimulator.Instance.ExecutePingOrTrace(targetIp, false);
            }
            else if (action == "traceroute" || action == "tracert")
            {
                CICTNetworkSimulator.Instance.ExecutePingOrTrace(targetIp, true);
            }
            else if (action == "clear" || action == "cls")
            {
                if (cliOutputText != null) cliOutputText.text = "";
            }
            else
            {
                AppendCLILog("Lệnh hỗ trợ: ping <IP>, traceroute <IP>, clear");
            }
        }

        public void AppendCLILog(string message)
        {
            if (cliOutputText != null)
            {
                cliOutputText.text += "\n" + message;
            }
        }

        public void OnClickViewVatLy() =>
            CICTNetworkSimulator.Instance.buildingController.SwitchViewMode(ViewMode3D.VatLy, CICTNetworkSimulator.Instance.allNodes);

        public void OnClickViewLogic() =>
            CICTNetworkSimulator.Instance.buildingController.SwitchViewMode(ViewMode3D.Logic, CICTNetworkSimulator.Instance.allNodes);

        public void OnClickViewVLAN() =>
            CICTNetworkSimulator.Instance.buildingController.SwitchViewMode(ViewMode3D.VLAN, CICTNetworkSimulator.Instance.allNodes);
    }
}