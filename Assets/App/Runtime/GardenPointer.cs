using UnityEngine;
using UnityEngine.EventSystems;

namespace HabitGarden
{
    public sealed class GardenPointer : MonoBehaviour, IPointerClickHandler
    {
        public GardenView garden;
        public void OnPointerClick(PointerEventData data)
        {
            var rect = (RectTransform)transform;
            if (garden == null || data.dragging || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,data.position,data.pressEventCamera,out var point)) return;
            var bounds = rect.rect;
            garden.Pick(new Vector2((point.x-bounds.xMin)/bounds.width,(point.y-bounds.yMin)/bounds.height));
        }
    }
    public sealed class GardenPlantTarget : MonoBehaviour { public string plantId; }
}
