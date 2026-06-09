using UnityEngine;

public class Piece : MonoBehaviour
{
    public Board board { get; private set; }
    public TetrominoData data { get; private set; }
    public Vector3Int[] cells { get; private set; }
    public Vector3Int position { get; private set; }
    public int rotationIndex { get; private set; }

    public float stepDelay = 1f;
    public float lockDelay = 0.5f;

    private float stepTime;
    private float lockTime;

    public void Initialize(Board board, Vector3Int position, TetrominoData data)
    {
        this.data = data;
        this.board = board;
        this.position = position;
        this.rotationIndex = 0;

        stepTime = Time.time + stepDelay;
        lockTime = 0f;

       if (this.cells == null) 
       {
            this.cells = new Vector3Int[data.cells.Length];
       }
          
        for (int i = 0; i < data.cells.Length; i++) 
        {
            this.cells[i] = (Vector3Int)data.cells[i];
        }
    }

    private void Update()
    {
    

        // БЛОКИРОВКА НА ПАУЗЕ: если игра остановлена, клавиши не работают
        if (Time.timeScale == 0f) return; 

        if (cells == null || board == null) return;

        board.Clear(this);

        // Таймер блокировки растет только у земли
        if (!board.IsValidPosition(this, position + Vector3Int.down)) {
            lockTime += Time.deltaTime;
        } else {
            lockTime = 0f;
        }

        if (Input.GetKeyDown(KeyCode.Q)) {
            Rotate(-1);
        } else if (Input.GetKeyDown(KeyCode.E)) {
            Rotate(1);
        }

        if (Input.GetKeyDown(KeyCode.A)) {
            Move(Vector2Int.left);
        } else if (Input.GetKeyDown(KeyCode.D)) {
            Move(Vector2Int.right);
        }

        if (Input.GetKeyDown(KeyCode.S)) {
            Move(Vector2Int.down);
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
           HardDrop();
        }

        if (Time.time > stepTime) {
            Step();
        }

        board.Set(this);
    }

    private void Step()
    {
        stepTime = Time.time + stepDelay;

        bool movedDown = Move(Vector2Int.down);

        // Фиксируем фигуру, только если она не может идти вниз и вышло время задержки
        if (!movedDown && lockTime >= lockDelay) {
            Lock();
        }
    }

    private void Lock()
    {
        board.Set(this);
        board.ClearLines();
        board.SpawnPiece();
    }

    private void HardDrop()
    {
        while (Move(Vector2Int.down)) {
           continue;
        }

        Lock();
    }

    private bool Move(Vector2Int translation)
    {
        Vector3Int newPosition = position;
        newPosition.x += translation.x;
        newPosition.y += translation.y;

        bool valid = board.IsValidPosition(this, newPosition);

        if (valid)
        {
            position = newPosition;
            lockTime = 0f; // Сбрасываем таймер фиксации при успешном движении
        }

        return valid;
    }

    private void Rotate(int direction)
    {
        this.rotationIndex = Wrap(rotationIndex + direction, 0, 4);
        int originalRotation = rotationIndex;
        ApplyRotationMatrix(direction);

        if (!TestWallKicks(rotationIndex, direction))
        {
            rotationIndex = originalRotation;
            ApplyRotationMatrix(-direction);
        }
    }

    private void ApplyRotationMatrix(int direction)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            Vector3 cell = cells[i];
            int x, y;

            switch (data.tetromino)
            {
                case Tetromino.I:
                case Tetromino.O:
                    cell.x -= 0.5f;
                    cell.y -= 0.5f;
                    x = Mathf.CeilToInt((cell.x * Data.RotationMatrix[0] * direction) + (cell.y * Data.RotationMatrix[1] * direction));
                    y = Mathf.CeilToInt((cell.x * Data.RotationMatrix[2] * direction) + (cell.y * Data.RotationMatrix[3] * direction));
                    break;

                default:
                    x = Mathf.RoundToInt((cell.x * Data.RotationMatrix[0] * direction) + (cell.y * Data.RotationMatrix[1] * direction));
                    y = Mathf.RoundToInt((cell.x * Data.RotationMatrix[2] * direction) + (cell.y * Data.RotationMatrix[3] * direction));
                    break;
            }

            cells[i] = new Vector3Int(x, y, 0);
        }
    }

    private bool TestWallKicks(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = GetWallKickIndex(rotationIndex, rotationDirection);

        for (int i = 0; i < data.wallKicks.GetLength(1); i++)
        {
            Vector2Int translation = data.wallKicks[wallKickIndex, i];

            if (Move(translation)) {
                return true;
            }
        }

        return false;
    }

    private int GetWallKickIndex(int rotationIndex, int rotationDirection)
    {
        int wallKickIndex = rotationIndex * 2;

        if (rotationDirection < 0) {
            wallKickIndex--;
        }

        return Wrap(wallKickIndex, 0, data.wallKicks.GetLength(0));
    }

    private int Wrap(int input, int min, int max)
    {
        if (input < min) {
            return max - (min - input) % (max - min);
        } else {
            return min + (input - min) % (max - min);
        }
    }
}