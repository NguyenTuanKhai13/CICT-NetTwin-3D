using UnityEngine;

namespace CICT.NetTwin
{
    public class OrbitCameraController : MonoBehaviour
    {
        public Transform target;
        public Vector3 targetOffset = new Vector3(0, 6f, 0);
        public float distance = 28.0f;
        public float xSpeed = 120.0f;
        public float ySpeed = 80.0f;
        public float zoomSpeed = 12.0f;

        private float x = -35.0f;
        private float y = 30.0f;

        private void Start()
        {
            UpdateCameraTransform();
        }

        private void LateUpdate()
        {
            if (Input.GetMouseButton(1))
            {
                x += Input.GetAxis("Mouse X") * xSpeed * Time.deltaTime;
                y -= Input.GetAxis("Mouse Y") * ySpeed * Time.deltaTime;
                y = Mathf.Clamp(y, 5f, 85f);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, 8f, 60f);

            UpdateCameraTransform();
        }

        private void UpdateCameraTransform()
        {
            Quaternion rotation = Quaternion.Euler(y, x, 0);
            Vector3 pivot = (target != null ? target.position : Vector3.zero) + targetOffset;
            Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + pivot;

            transform.rotation = rotation;
            transform.position = position;
        }
    }
}