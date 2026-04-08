using UnityEngine;
using IdleGame.Core;

namespace IdleGame.Gameplay
{
    public class TapController : MonoBehaviour
    {
        [SerializeField] private double _tapDamage = 10;

        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleClick();
            }
        }

        private void HandleClick()
        {
            Vector2 worldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log($"Click at: {worldPos}");

            RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log($"Hit: {hit.collider.gameObject.name}");
                Monster monster = hit.collider.GetComponent<Monster>();
                if (monster != null)
                {
                    Debug.Log("Monster hit! Dealing damage.");
                    monster.TakeDamage(_tapDamage);
                }
                else
                {
                    Debug.Log("Hit object has no Monster component");
                }
            }
            else
            {
                Debug.Log("No collider hit");
            }
        }
    }
}
