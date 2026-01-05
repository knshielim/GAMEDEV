using UnityEngine;
using UnityEngine.SceneManagement;

public class DamagePopupSpawner : MonoBehaviour
{
    public static DamagePopupSpawner Instance { get; private set; }

    [SerializeField] private DamagePopup popupPrefab;
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        // Singleton + anti duplikat
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!mainCamera)
            mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Update camera setiap ganti scene
        if (!mainCamera)
            mainCamera = Camera.main;
    }

    public void Spawn(float damage, bool isCrit, Vector3 worldPos)
    {
        if (popupPrefab == null)
        {
            Debug.LogError("[DamagePopupSpawner] Popup Prefab belum di-assign!");
            return;
        }

        worldPos.z = 0f; // aman untuk 2D

        var popup = Instantiate(popupPrefab, worldPos, Quaternion.identity);

        // Untuk 2D, jangan pakai LookRotation
        popup.transform.rotation = Quaternion.identity;

        popup.Setup(damage, isCrit);
    }
}
