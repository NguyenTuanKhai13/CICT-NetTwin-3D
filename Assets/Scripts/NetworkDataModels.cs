using System;
using System.Collections.Generic;
using UnityEngine;

namespace CICT.NetTwin
{
    public enum DeviceType
    {
        RouterL3,
        SwitchL2,
        SwitchL3,
        Server,
        PC,
        AccessPoint,
        Rack
    }

    public enum ViewMode3D
    {
        VatLy,  // Xếp theo tầng tòa nhà thật
        Logic,  // Xếp theo 3 tầng: Core -> Distribution -> Access
        VLAN    // Tách mặt phẳng theo từng cụm VLAN 10, 20, 30
    }

    public enum LinkType
    {
        Fiber10Gbps,
        Copper1Gbps,
        Wireless
    }

    [Serializable]
    public class DeviceData
    {
        public string deviceId;
        public string deviceName;
        public DeviceType deviceType;
        public string ipAddress;
        public string subnetMask = "255.255.255.0";
        public List<int> vlans = new List<int>();
        public int floorIndex;
        public bool isOnline = true;
        public Vector3 physicalLocalPos;
    }
}