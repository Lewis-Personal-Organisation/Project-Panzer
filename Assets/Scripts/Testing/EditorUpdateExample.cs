using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

[ExecuteAlways]
public class EditorUpdateExample : MonoBehaviour
{
    public float totalTime;
    
    [FormerlySerializedAs("testText")]
    public Text infotext;
    public Camera gameCamera;
    public Transform bouncingCube;
    
    [SerializeField] private bool hasTarget = false;
    public float cubeSpeed;
    private Vector2 viewportPos;
    private Vector2 viewportDir;
    
    [FormerlySerializedAs("depth")]
    [Range(0.3F, 1F)]
    public float cubeDepth = 0.9F;
    
    
    private void OnEnable()
    {
        if (!infotext || !bouncingCube)
            return;

        totalTime = 0;
        
        EditorApplication.update += OnEditorUpdate;
        
        if (!hasTarget)
        {
            hasTarget = true;
            
            viewportPos = new Vector2(0.5f, 0.5f);                                                          // Start at Center of viewport
            float randomAngle = Random.Range(0f, Mathf.PI * 2f);                                            // Random start angle
            viewportDir = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)).normalized;           // Assign direction
            
            // Assign initial position to cube
            bouncingCube.position = gameCamera.ViewportToWorldPoint(new Vector3(viewportPos.x, viewportPos.y, Mathf.Lerp(gameCamera.nearClipPlane, gameCamera.farClipPlane, cubeDepth)));
        }
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    /// <summary>
    /// Example method for updating times and cube position in non-play mode
    /// </summary>
    private void OnEditorUpdate()
    {
        if (!infotext || !bouncingCube)
            return;
        
        totalTime += EditorApplicationUpdater.DeltaTime;
        infotext.text = $"Editor Delta Time\n{EditorApplicationUpdater.DeltaTime} \n\nAccumulated Time\n{totalTime}";
        
        MoveCube();
    }
    
    /// <summary>
    /// Moves the Bouncing cube using EditorApplicationUpdater.DeltaTime
    /// </summary>
    private void MoveCube()
    {
        // Move Viewport vector toward direction
        viewportPos += viewportDir * (cubeSpeed * EditorApplicationUpdater.DeltaTime);
        
        // Check for edge collisions of the Viewport. Reflect if we collide
        if (viewportPos.x <= 0f)
        {
            viewportPos.x = 0f;                                        // Clamp to boundary
            viewportDir = Vector2.Reflect(viewportDir, Vector2.right); // Reflect across X-axis
        }
        else if (viewportPos.x >= 1f)
        {
            viewportPos.x = 1f;
            viewportDir = Vector2.Reflect(viewportDir, Vector2.left);
        }
        
        if (viewportPos.y <= 0f)
        {
            viewportPos.y = 0f;
            viewportDir = Vector2.Reflect(viewportDir, Vector2.up); // Reflect across Y-axis
        }
        else if (viewportPos.y >= 1f)
        {
            viewportPos.y = 1f;
            viewportDir = Vector2.Reflect(viewportDir, Vector2.down);
        }
        
        bouncingCube.position = gameCamera.ViewportToWorldPoint(new Vector3(viewportPos.x, viewportPos.y, Mathf.Lerp(gameCamera.nearClipPlane, gameCamera.farClipPlane, cubeDepth)));
        bouncingCube.Rotate(viewportDir);
    }
}
