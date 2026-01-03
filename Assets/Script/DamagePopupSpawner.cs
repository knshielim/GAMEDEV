using UnityEngine;

public class DamagePopupSpawner : MonoBehaviour
{
    public static DamagePopupSpawner Instance { get; private set; }

    [SerializeField] private DamagePopup popupPrefab;
    [SerializeField] private Camera mainCamera;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (!mainCamera) mainCamera = Camera.main;
    }

    public void Spawn(int damage, bool isCrit, Vector3 worldPos)
    {
        var popup = Instantiate(popupPrefab, worldPos, Quaternion.identity);

        // optional: hadap kamera (kalau kamu belum pakai billboard script)
        if (mainCamera)
            popup.transform.rotation = Quaternion.LookRotation(popup.transform.position - mainCamera.transform.position);

        popup.Setup(damage, isCrit);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            Spawn(123, false, Vector3.zero);
        if (Input.GetKeyDown(KeyCode.Y))
            Spawn(999, true, Vector3.zero);
    }

}
