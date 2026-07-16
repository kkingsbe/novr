using UnityEngine;

namespace NOVR.VrUi.SpecialBehavior;

public class NoVrHudBehavior : UIRenderedCanvasBehavior
{
    private void Update()
    {
        transform.position = new Vector3(0f, 0f, 1050f);
        transform.rotation = Quaternion.identity;
    }
}
