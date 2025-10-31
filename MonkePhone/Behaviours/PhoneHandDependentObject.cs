using UnityEngine;

namespace MonkePhone.Behaviours
{
    public class PhoneHandDependentObject : MonoBehaviour
    {
        private Vector3 _position;
        private Vector3 _scale;

        public void Awake()
        {
            _position = transform.localPosition;
            _scale    = transform.localScale;
        }

        public void SetFlip(bool useFlipped)
        {
            transform.localPosition = new Vector3(useFlipped ? -_position.x : _position.x, _position.y, _position.z);
            transform.localScale    = new Vector3(_scale.x, _scale.y, useFlipped ? -_scale.z : _scale.z);
        }

#if PLUGIN == false
        public bool testFlip = true;

        public void Update()
        {
            SetFlip(testFlip);
        }
#endif
    }
}