using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace SG{

    public class PlayerSelector : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera; // The camera used for raycasting
        [SerializeField] private float maxRaycastDistance = 5f; // Max distance for raycasting
        [SerializeField] private TextMeshProUGUI interactableNameUI; // UI element to display the name of the interactable
        private GameDevCW inputActions; // Input action class reference
        private PlayerAttributesManager playerAttributesManager; // The player attributes managerplayerAttributesManager; // The player attributes managerplayerAttributesManager; // The player attributes manager

        private void Awake()
        {
            inputActions = new GameDevCW();
            playerAttributesManager = GetComponent<PlayerAttributesManager>();
            inputActions.Player.Interact.performed += ctx => InteractWithChest();
        }

        private void OnEnable()
        {
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }

        private void Update()
        {
            CheckForInteractable();
        }

        private void CheckForInteractable()
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * maxRaycastDistance, Color.green); // Debug ray visualization

            if (Physics.Raycast(ray, out hit, maxRaycastDistance))
            {
                InteractiveChest chest = hit.collider.GetComponent<InteractiveChest>();
                if (chest != null)
                {
                    // Display the name of the interactable chest on the UI
                    interactableNameUI.text = chest.chestName;
                }
                else
                {
                    interactableNameUI.text = "";
                }

                Collectible collectible = hit.collider.GetComponent<Collectible>();
                if (collectible != null)
                {
                    // Display the name of the interactable chest on the UI
                    interactableNameUI.text = collectible.displayName;
                }
                else
                {
                    interactableNameUI.text = "";
                }
            }
            else
            {
                // Clear the UI if no chest is hit
                interactableNameUI.text = "";
            }
        }

        private void InteractWithChest()
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxRaycastDistance))
            {
                InteractiveChest chest = hit.collider.GetComponent<InteractiveChest>();
                
                if (chest != null)
                {
                    chest.OnSelect();
                }
                
                Collectible collectible = hit.collider.GetComponent<Collectible>();
                if (collectible != null)
                {
                    playerAttributesManager.InventoryManager.Add(collectible.GetCollectible(), 1);
                    Destroy(collectible);
                }
            }
        }
    }
}
