using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;


    

public class ParkingSystem : MonoBehaviour
{
    [System.Serializable]
    public class ParkingSpot
    {
        public Transform spotTransform;
        public bool isOccupied;
        public GameObject occupiedCar;
        public Renderer indicatorRenderer;
    }

    [Header("⚙️ Настройки системы")]
    [SerializeField] private int maxCars = 10;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private Vector3 spawnArea = new Vector3(3, 0, 2);
    
    [Header("🔗 Ссылки на объекты")]
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private Transform carSpawnPoint;
    [SerializeField] private TextMeshProUGUI statsText;
    
    [Header("🅿️ Парковочные места")]
    [SerializeField] private List<ParkingSpot> parkingSpots = new List<ParkingSpot>();

    private List<GameObject> activeCars = new List<GameObject>();
    private Queue<GameObject> carPool = new Queue<GameObject>();
    private int totalCarsSpawned = 0;
    private int successfullyParked = 0;

    private void Start()
    {
        InitializeParkingSpots();
        InitializeCarPool();
        StartCoroutine(CarSpawner());
        UpdateStatsUI();
    }

    private void InitializeParkingSpots()
    {
        // Автоматически находим все парковочные места
        GameObject[] spotObjects = GameObject.FindGameObjectsWithTag("ParkingSpot");
        
        foreach (GameObject spotObj in spotObjects)
        {
            ParkingSpot newSpot = new ParkingSpot
            {
                spotTransform = spotObj.transform,
                isOccupied = false,
                indicatorRenderer = spotObj.GetComponent<Renderer>()
            };

            // Устанавливаем начальный цвет - зеленый (свободно)
            if (newSpot.indicatorRenderer != null)
            {
                newSpot.indicatorRenderer.material.color = Color.green;
            }

            parkingSpots.Add(newSpot);
        }

        Debug.Log($"✅ Инициализировано {parkingSpots.Count} парковочных мест");
    }

    private void InitializeCarPool()
    {
        for (int i = 0; i < maxCars; i++)
        {
            GameObject car = Instantiate(carPrefab);
            car.SetActive(false);
            carPool.Enqueue(car);
        }
        Debug.Log($"✅ Создан пул из {maxCars} машин");
    }

    private IEnumerator CarSpawner()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            
            if (activeCars.Count < maxCars && carPool.Count > 0 && HasFreeSpots())
            {
                SpawnCar();
            }
        }
    }

    private void SpawnCar()
    {
        if (carPool.Count == 0) return;

        GameObject car = carPool.Dequeue();
        car.SetActive(true);
        
        // Случайная позиция в зоне спавна
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnArea.x, spawnArea.x),
            0,
            Random.Range(-spawnArea.z, spawnArea.z)
        );
        
        car.transform.position = carSpawnPoint.position + randomOffset;
        
        CarAI carAI = car.GetComponent<CarAI>();
        if (carAI != null)
        {
            carAI.Initialize(this);
            carAI.FindParkingSpot();
        }

        activeCars.Add(car);
        totalCarsSpawned++;
        
        UpdateStatsUI();
        Debug.Log($"🚗 Создана машина #{totalCarsSpawned}");
    }

    public ParkingSpot FindNearestFreeSpot(Vector3 position)
    {
        ParkingSpot nearestSpot = null;
        float nearestDistance = Mathf.Infinity;

        foreach (ParkingSpot spot in parkingSpots)
        {
            if (!spot.isOccupied)
            {
                float distance = Vector3.Distance(position, spot.spotTransform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSpot = spot;
                }
            }
        }

        return nearestSpot;
    }

    public void OccupySpot(ParkingSpot spot, GameObject car)
    {
        spot.isOccupied = true;
        spot.occupiedCar = car;
        
        if (spot.indicatorRenderer != null)
        {
            spot.indicatorRenderer.material.color = Color.red;
        }

        successfullyParked++;
        UpdateStatsUI();
        
        Debug.Log($"🅿️ Машина заняла место {spot.spotTransform.name}");
    }

    public void FreeSpot(ParkingSpot spot)
    {
        spot.isOccupied = false;
        spot.occupiedCar = null;
        
        if (spot.indicatorRenderer != null)
        {
            spot.indicatorRenderer.material.color = Color.green;
        }
        
        Debug.Log($"🅿️ Место {spot.spotTransform.name} освобождено");
    }

    public void ReturnCarToPool(GameObject car)
    {

        car.SetActive(false);
     activeCars.Remove(car);
     carPool.Enqueue(car);
    
     // ИСПРАВЛЕННАЯ СТРОКА 183:
     CarAI carAI = car.GetComponent<CarAI>();
     if (carAI != null)
     {
        carAI.ResetCar();
     }
    
    Debug.Log($"🚗 Машина возвращена в пул");
    }

    private bool HasFreeSpots()
    {
        foreach (ParkingSpot spot in parkingSpots)
        {
            if (!spot.isOccupied) return true;
        }
        return false;
    }

    private void UpdateStatsUI()
    {
        if (statsText != null)
        {
            int freeSpots = parkingSpots.Count - successfullyParked;
            float occupancyRate = (float)successfullyParked / parkingSpots.Count * 100;
            
            statsText.text = $"📊 СТАТИСТИКА ПАРКОВКИ\n" +
                           $"Всего машин: {totalCarsSpawned}\n" +
                           $"Припарковано: {successfullyParked}\n" +
                           $"Свободных мест: {freeSpots}\n" +
                           $"Загрузка: {occupancyRate:F1}%";
        }
    }

    private void OnDrawGizmos()
    {
        // Визуализация зоны спавна в редакторе
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(carSpawnPoint.position, spawnArea * 2);
    }
}

