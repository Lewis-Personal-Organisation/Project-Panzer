using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TessClick : MonoBehaviour
{
    public float tessValue = 3f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Clicked Mouse");
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Tessellator tess = hit.collider.GetComponent<Tessellator>();

                if (tess != null)
                {
                    // tess.SetTessAtPoint(hit.point, tessValue);
                    return;
                }
                
                Debug.Log("Could not find Tessellator comp");
                return;
            }
            else
            {
                Debug.Log("No hit with Raycast");
            }
        }
    }
}
