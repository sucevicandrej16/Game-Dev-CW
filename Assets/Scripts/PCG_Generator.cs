using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Room
{
    public int X; // Top-left corner X
    public int Z; // Top-left corner Z
    public int Width;
    public int Depth;

    public Room(int x, int z, int width, int depth)
    {
        X = x;
        Z = z;
        Width = width;
        Depth = depth;
    }
}



public class PCG_Generator : MonoBehaviour
{
    [SerializeField]
    private int _roomCount = 3; // How many rooms to generate

    [SerializeField]
    private int _minRoomSize = 2; // Minimum room size

    [SerializeField]
    private int _maxRoomSize = 4; // Maximum room size

    private List<Room> _rooms = new List<Room>(); // Store all the rooms

    [SerializeField]
    private MazeCell _mazeCellPrefab;

    [SerializeField]
    private int _mazeWidth;

    [SerializeField]
    private int _mazeDepth;

    private MazeCell[,] _mazeGrid;

    void Start()
    {
    _mazeGrid = new MazeCell[_mazeWidth, _mazeDepth];

    // Get the plane's size
    Renderer planeRenderer = GetComponent<Renderer>();
    float planeWidth = planeRenderer.bounds.size.x;
    float planeDepth = planeRenderer.bounds.size.z;

    // Calculate the size of each maze cell based on the plane and grid dimensions
    float cellSizeX = planeWidth / _mazeWidth;
    float cellSizeZ = planeDepth / _mazeDepth;

    for (int x = 0; x < _mazeWidth; x++)
    {
        for (int z = 0; z < _mazeDepth; z++)
        {
            float planeY = transform.position.y;

            Vector3 cellPosition = new Vector3(
                transform.position.x - planeWidth / 2 + (x + 0.5f) * cellSizeX,
                planeY,
                transform.position.z - planeDepth / 2 + (z + 0.5f) * cellSizeZ
            );

            MazeCell cell = Instantiate(_mazeCellPrefab, cellPosition, Quaternion.identity);

            // Set grid indices for this cell
            cell.GridX = x;
            cell.GridZ = z;

            // Scale the cell
            cell.transform.localScale = new Vector3(cellSizeX, cell.transform.localScale.y, cellSizeZ);

            _mazeGrid[x, z] = cell;
        }
    }

    GenerateRooms();

    // Connect rooms to the maze
    ConnectRooms();

    // Start generating the maze
    GenerateMaze(null, _mazeGrid[0, 0]);
    }



    private void ConnectRooms()
    {
        foreach (Room room in _rooms)
        {
            // Store valid edge cells of the room
            List<Vector2Int> edgeCells = new List<Vector2Int>();

            // Collect room edge cells that have a neighboring maze cell
            for (int dx = 0; dx < room.Width; dx++)
            {
                // Top edge
                edgeCells.Add(new Vector2Int(room.X + dx, room.Z - 1));
                // Bottom edge
                edgeCells.Add(new Vector2Int(room.X + dx, room.Z + room.Depth));
            }
            for (int dz = 0; dz < room.Depth; dz++)
            {
                // Left edge
                edgeCells.Add(new Vector2Int(room.X - 1, room.Z + dz));
                // Right edge
                edgeCells.Add(new Vector2Int(room.X + room.Width, room.Z + dz));
            }

            // Filter edge cells that are within the maze bounds
            edgeCells = edgeCells.Where(pos => 
                pos.x >= 0 && pos.x < _mazeWidth && pos.y >= 0 && pos.y < _mazeDepth
            ).ToList();

            // Randomly pick one edge cell
            Vector2Int entrancePos = edgeCells[Random.Range(0, edgeCells.Count)];

            // Connect the room to the maze
            MazeCell roomEdgeCell = _mazeGrid[entrancePos.x, entrancePos.y];
            MazeCell roomCell = _mazeGrid[Mathf.Clamp(entrancePos.x, room.X, room.X + room.Width - 1),
                                        Mathf.Clamp(entrancePos.y, room.Z, room.Z + room.Depth - 1)];

            ClearWalls(roomEdgeCell, roomCell);
        }
    }


    private void GenerateRooms()
    {
        int attempts = 0; // Track attempts to avoid infinite loops
        int maxAttempts = _roomCount * 10; // A safe limit for retries

        while (_rooms.Count < _roomCount && attempts < maxAttempts)
        {
            // Randomly determine room size
            int roomWidth = Random.Range(_minRoomSize, _maxRoomSize + 1);
            int roomDepth = Random.Range(_minRoomSize, _maxRoomSize + 1);

            // Randomly determine room position
            int x = Random.Range(1, _mazeWidth - roomWidth - 1);
            int z = Random.Range(1, _mazeDepth - roomDepth - 1);

            Room newRoom = new Room(x, z, roomWidth, roomDepth);

            // Check for overlaps
            if (!DoesRoomOverlap(newRoom))
            {
                _rooms.Add(newRoom);

                // Mark cells as rooms and clear their walls
                for (int dx = 0; dx < roomWidth; dx++)
                {
                    for (int dz = 0; dz < roomDepth; dz++)
                    {
                        MazeCell cell = _mazeGrid[x + dx, z + dz];
                        cell.MarkAsRoom();
                        cell.Visit(); // Prevent maze generation here
                    }
                }
            }

            attempts++;
        }

        if (_rooms.Count < _roomCount)
        {
            Debug.LogWarning($"Only {_rooms.Count} rooms were placed out of {_roomCount} due to space constraints.");
        }
    }



    private bool DoesRoomOverlap(Room room)
    {
        foreach (Room existingRoom in _rooms)
        {
            if (room.X < existingRoom.X + existingRoom.Width &&
                room.X + room.Width > existingRoom.X &&
                room.Z < existingRoom.Z + existingRoom.Depth &&
                room.Z + room.Depth > existingRoom.Z)
            {
                return true; // Overlap detected
            }
        }
        return false; // No overlap
    }



    private void GenerateMaze(MazeCell previousCell, MazeCell currentCell)
    {
        if (!(currentCell.IsVisited && currentCell.IsRoom)){
            currentCell.Visit();
            ClearWalls(previousCell, currentCell);

            

            MazeCell nextCell;

            do
            {
                nextCell = GetNextUnvisitedCell(currentCell);

                if (nextCell != null)
                {
                    GenerateMaze(currentCell, nextCell);
                }
            } while (nextCell != null);
        }
    }


    private MazeCell GetNextUnvisitedCell(MazeCell currentCell)
    {
        var unvisitedCells = GetUnvisitedCells(currentCell).ToList();
        

        if (unvisitedCells.Count == 0)
            return null;

        // Shuffle the list of unvisited cells for random selection
        int randomIndex = Random.Range(0, unvisitedCells.Count);
        return unvisitedCells[randomIndex];
    }

    private IEnumerable<MazeCell> GetUnvisitedCells(MazeCell currentCell)
    {
        int x = currentCell.GridX; // Use the stored grid X index
        int z = currentCell.GridZ; // Use the stored grid Z index

        if (x + 1 < _mazeWidth)
        {
            var cellToRight = _mazeGrid[x + 1, z];
            
            if (cellToRight.IsVisited == false)
            {
                yield return cellToRight;
            }
        }

        if (x - 1 >= 0)
        {
            var cellToLeft = _mazeGrid[x - 1, z];

            if (cellToLeft.IsVisited == false)
            {
                yield return cellToLeft;
            }
        }

        if (z + 1 < _mazeDepth)
        {
            var cellToFront = _mazeGrid[x, z + 1];

            if (cellToFront.IsVisited == false)
            {
                yield return cellToFront;
            }
        }

        if (z - 1 >= 0)
        {
            var cellToBack = _mazeGrid[x, z - 1];

            if (cellToBack.IsVisited == false)
            {
                yield return cellToBack;
            }
        }
    }

    private void ClearWalls(MazeCell previousCell, MazeCell currentCell)
    {
        if (previousCell == null)
        {
            return;
        }

        if (previousCell.transform.position.x < currentCell.transform.position.x)
        {
            previousCell.ClearRightWall();
            currentCell.ClearLeftWall();
            return;
        }

        if (previousCell.transform.position.x > currentCell.transform.position.x)
        {
            previousCell.ClearLeftWall();
            currentCell.ClearRightWall();
            return;
        }

        if (previousCell.transform.position.z < currentCell.transform.position.z)
        {
            previousCell.ClearFrontWall();
            currentCell.ClearBackWall();
            return;
        }

        if (previousCell.transform.position.z > currentCell.transform.position.z)
        {
            previousCell.ClearBackWall();
            currentCell.ClearFrontWall();
            return;
        }
    }

}
