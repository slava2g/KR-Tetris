using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; 

public class Board : MonoBehaviour
{
    public Tilemap tilemap { get; private set; }
    public Piece activePiece { get; private set; }
    public TetrominoData[] tetrominoes;
    public Vector3Int spawnPosition;
    public Vector2Int boardSize = new Vector2Int(10, 20);
    public int score = 0;
    public TextMeshProUGUI scoreText;
    private bool isPaused = false;
    public GameObject notificationObject; 
    private Coroutine activeNotificationRoutine;

    public RectInt Bounds
    {
        get
        {
            Vector2Int position = new Vector2Int(-boardSize.x / 2, -boardSize.y / 2);
            return new RectInt(position, boardSize);
        }
    }

    private void Awake()
    {
        tilemap = GetComponentInChildren<Tilemap>();
        activePiece = GetComponentInChildren<Piece>();

        for (int i = 0; i < tetrominoes.Length; i++) {
            tetrominoes[i].Initialize();
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;
        UpdateScore(score);
        SpawnPiece();
    }

    public void SpawnPiece()
    {
        int random = Random.Range(0, tetrominoes.Length);
        TetrominoData data = tetrominoes[random];

        // УСКОРЕНИЕ: Считаем скорость на основе текущих очков. 
        // Каждые 100 очков ускоряют падение на 0.1 сек. Лимит разгона — 0.1 сек на шаг.
        if (activePiece != null) {
            float currentDelay = Mathf.Max(1.5f - (score / 100) * 0.05f, 0.05f);
            activePiece.stepDelay = currentDelay;
        }

        activePiece.Initialize(this, spawnPosition, data);

        if (IsValidPosition(activePiece, spawnPosition)) {
            Set(activePiece);
        } else {
            GameOver();
            if (notificationObject != null) {
                ShowNotification(2f);
            }
        }
    }

    public void GameOver()
    {
        score = 0;          
        UpdateScore(score);

        // ВОЗВРАТ СКОРОСТИ: Сбрасываем задержку новой фигуры обратно на 1 секунду
        if (activePiece != null) {
            activePiece.stepDelay = 1f;
        }

        tilemap.ClearAllTiles();
        Debug.Log("game over");
        
        if (EventSystem.current != null) {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void Set(Piece piece)
    {
        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePosition = piece.cells[i] + piece.position;
            tilemap.SetTile(tilePosition, piece.data.tile);
        }
    }

    public void Clear(Piece piece)
    {
        if (piece == null || piece.cells == null) return;

        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePosition = piece.cells[i] + piece.position;
            tilemap.SetTile(tilePosition, null);
        }
    }

    public bool IsValidPosition(Piece piece, Vector3Int position)
    {
        RectInt bounds = Bounds;

        for (int i = 0; i < piece.cells.Length; i++)
        {
            Vector3Int tilePosition = piece.cells[i] + position;

            if (!bounds.Contains((Vector2Int)tilePosition)) {
                return false;
            }

            if (tilemap.HasTile(tilePosition)) {
                return false;
            }
        }

        return true;
    }

    public void ClearLines()
    {
        RectInt bounds = Bounds;
        int row = bounds.yMin;

        while (row < bounds.yMax)
        {
            if (IsLineFull(row)) 
            {
                LineClear(row);
                score += 100;
                UpdateScore(score);

                // УСКОРЕНИЕ: Сразу же разгоняем текущую активную фигуру после сжигания линии
                if (activePiece != null) {
                    float newDelay = 1f - (score / 100) * 0.1f;
                    activePiece.stepDelay = Mathf.Max(newDelay, 0.1f);
                }
            } else {
                row++;
            }
        }
    }

    public bool IsLineFull(int row)
    {
        RectInt bounds = Bounds;

        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);

            if (!tilemap.HasTile(position)) {
                return false;
            }
        }

        return true;
    }

    public void LineClear(int row)
    {
        RectInt bounds = Bounds;

        for (int col = bounds.xMin; col < bounds.xMax; col++)
        {
            Vector3Int position = new Vector3Int(col, row, 0);
            tilemap.SetTile(position, null);
        }

        while (row < bounds.yMax)
        {
            for (int col = bounds.xMin; col < bounds.xMax; col++)
            {
                Vector3Int position = new Vector3Int(col, row + 1, 0);
                TileBase above = tilemap.GetTile(position);

                position = new Vector3Int(col, row, 0);
                tilemap.SetTile(position, above);
            }

            row++;
        }
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null) {
            scoreText.text = score.ToString();
        }
    }

    public void ToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("SampleScene");
        if (EventSystem.current != null) {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused) {
            Time.timeScale = 0f;
            Debug.Log("Игра на паузе");
        } else {
            Time.timeScale = 1f;
            Debug.Log("Игра возобновлена");
        }

        if (EventSystem.current != null) {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void ShowNotification(float duration)
    {
        if (activeNotificationRoutine != null) {
            StopCoroutine(activeNotificationRoutine);
        }

        activeNotificationRoutine = StartCoroutine(NotificationRoutine(duration));
    }

    private System.Collections.IEnumerator NotificationRoutine(float duration)
    {
        if (notificationObject != null) {
            notificationObject.SetActive(true);
        }

        // Заменили на Реальное время, чтобы не зависало при остановке игры
        yield return new WaitForSecondsRealtime(duration); 

        if (notificationObject != null) {
            notificationObject.SetActive(false);
        }
        activeNotificationRoutine = null;
    }
}