using System.Collections.Generic;
using UnityEngine;

namespace CICT.NetTwin
{
    public class BuildingViewController : MonoBehaviour
    {
        [Header("Danh sách mặt sàn 3D của tòa nhà CICT (Tầng trệt -> Tầng 3)")]
        public Transform[] buildingFloors;
        public float physicalFloorSpacing = 4.5f;

        private ViewMode3D currentMode = ViewMode3D.VatLy;

        public void SwitchViewMode(ViewMode3D mode, List<NetworkNode3D> allNodes)
        {
            currentMode = mode;

            for (int i = 0; i < buildingFloors.Length; i++)
            {
                if (buildingFloors[i] != null)
                {
                    buildingFloors[i].gameObject.SetActive(mode == ViewMode3D.VatLy);
                    buildingFloors[i].localPosition = new Vector3(0, i * physicalFloorSpacing, 0);
                }
            }

            foreach (var node in allNodes)
            {
                Vector3 basePos = node.data.physicalLocalPos;

                switch (mode)
                {
                    case ViewMode3D.VatLy:
                        float floorY = node.data.floorIndex * physicalFloorSpacing + 0.6f;
                        node.SetTargetPosition(new Vector3(basePos.x, floorY, basePos.z));
                        break;

                    case ViewMode3D.Logic:
                        float logicY = GetLogicalTierHeight(node.data.deviceType);
                        node.SetTargetPosition(new Vector3(basePos.x * 1.2f, logicY, basePos.z * 1.2f));
                        break;

                    case ViewMode3D.VLAN:
                        int primaryVlan = node.data.vlans.Count > 0 ? node.data.vlans[0] : 10;
                        float vlanY = (primaryVlan / 10) * 5.0f;
                        node.SetTargetPosition(new Vector3(basePos.x, vlanY, basePos.z));
                        break;
                }
            }
        }

        private float GetLogicalTierHeight(DeviceType type)
        {
            switch (type)
            {
                case DeviceType.RouterL3:
                case DeviceType.SwitchL3:
                    return 14.0f;
                case DeviceType.Server:
                case DeviceType.SwitchL2:
                    return 7.5f;
                default:
                    return 1.5f;
            }
        }
    }
}