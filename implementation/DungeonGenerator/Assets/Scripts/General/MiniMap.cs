using UnityEngine;

namespace General
{
    public class MiniMap : MonoBehaviour
    {
        public Transform player;

        // Use LateUpdate so it updates after the player moved
        private void LateUpdate()
        {
            // Set minimap position to player position
            Vector3 newMiniMapPosition = player.position;
            
            var currentTransform = transform;
            newMiniMapPosition.y = currentTransform.position.y;
            currentTransform.position = newMiniMapPosition;
            
            // Let camera rotate with player
            transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0);
        }
    }
}
