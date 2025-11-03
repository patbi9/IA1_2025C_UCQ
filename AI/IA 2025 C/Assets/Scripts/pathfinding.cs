using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;


// las structs son POD (Plain Old Data), se manejan como si fueran int, float, etc. y no como punteros internamente.
// En contraste, las class siempre se manejan como punteros.

public enum EDirections : int
{
    Up = -1,
    Right = 1,
    Left = -1,
    Down = 1
}

public enum ETileType
{
    Normal,
    Fire,
    Forest,
    Sand,
    COUNT,
}

public class Node
{
    public Node()
    {
        Parent = null;
    }
        
    public Node(int x, int y, ETileType tileType, float terrainCost, bool isWalkable = true)
    {
        X = x;
        Y = y;

        TerrainCost = terrainCost;
        GCost = 0;
        TotalCost = 0;
        HCost = float.PositiveInfinity;
        
        TileType = tileType;
        
        // Hay que tener la certeza de que inicializa en null, para hacer el pathfinding bien.
        Parent = null;
        Walkable = isWalkable;
    }
    
    // Tienen información de algo relevante en un grafo... ¿pero qué?
    public int X { get; }

    public int Y { get; }

    public bool Walkable = true;

    //costo propio del nodo
    public float TerrainCost;
    //costo del parent
    public float GCost;
    //costo de heuristica
    public float HCost;
    public float TotalCost; //costo propio mas el del parent. tambien llamado fcoast
    //public float h_cost;
    
    public ETileType TileType =  ETileType.Normal;

    // aristas que nos dicen a qué nodos puede visitar este nodo.
    // private List<Edge> edges = new List<Edge>();
    /*
     * public edge UpEdge = this, upNode;
     * public edge RightEdge = this, RightNode;
     * public edge DownEdge = this, downNode;
     * public edge LeftEdge = this, leftNode;
     * 

     * public Node UpNode;
     * public Node RightNode;
     * public Node DownNode;
     * public Node LeftNode;
     *
     * Ni siquiera necesitamos esos Node de arriba, abajo, derecha, izquierda, porque los índices en la cuadrícula
     * ya nos dicen cuáles serían dichos vecinos arriba (-1 en Y), abajo (+1 en Y), derecha (+1 en X), izquierda (-1 en X)
     */
    
    // Tiene una referencia al nodo Parent, es el que lo metió al árbol. (El que lo metió a la lista abierta).
    public Node Parent;
}

// No la vamos a usar en este caso, porque están implícitas las aristas en la estructura del grid.
// Si tuviéramos otro tipo de representación, como un grafo no-grid, ahí sí necesitaríamos tener aristas explícitas.
// public class Edge
// {
//     public Node A;
//     public Node B;
//     // podríamos poner que tiene un costo moverse entre ellos o algo.
//     // Pero ahorita lo voy a dejar así.
// }


public class pathfinding : MonoBehaviour
{
    [SerializeField] private int height = 5;
    [SerializeField] private int width = 5;
    
    [SerializeField] private int originX = 1;
    [SerializeField] private int originY = 1;

    [SerializeField] private int goalX = 3;
    [SerializeField] private int goalY = 3;

    [SerializeField] private float cycleSpeed = 0.5f;
    
    [Range(0.0f, 1.0f)]
    [SerializeField] private float walkableProbability = 0.5f;

    [SerializeField] private int normalTileCost = 1;
    [SerializeField] private int fireTileCost = 5;
    [SerializeField] private int forestTileCost = 2;
    [SerializeField] private int sandTileCost = 3;
    
    [Range(0.0f, 1.0f)]
    [SerializeField] private float gizmosSphereSize = 0.2f;
    
    
    
    // Alguien que contenga todos los Nodos.
    // Esos nodos van a estar en forma de Grid/Cuadrícula, entonces podemos usar un array bidimensional.
    private Node[][] _grid;
    
    // lista cerrada. Los nodos que ya están en la lista cerrada ya nunca se necesitan tocar, visitar, etc en ninguno de los
    // algoritmos de pathfinding.
    // Hash (casi) te garantiza tiempos de búsqueda, inserción y borrado de O(1), tiempo constante en el caso promedio.
    // Lo malo en performance de los hashes es iterarlos, porque no guardan realmente un orden.
    private HashSet<Node> _closedList = new HashSet<Node>();
    
    private List<Node> _pathToGoal = new List<Node>();
    public List<Node> PathToGoal => _pathToGoal;
    
    
    // Algoritmo para inicializar el grid de width*height
    void InitializeGrid()
    {
        _grid = new Node[height][];
        // Primero eje vertical, 
        for (int i = 0; i < height; i++)
        {
            _grid[i] = new Node[width];
            // después el eje horizontal
            for (int j = 0; j < width; j++)
            {
                bool isWalkable = Random.value < walkableProbability;
                
                ETileType tileType = (ETileType)Random.Range(0, (int)ETileType.COUNT);
                float fCost = 1;
                switch (tileType)
                {
                    case ETileType.Normal:
                        fCost = normalTileCost;
                        break;
                    case ETileType.Fire:
                        fCost = fireTileCost;
                        break;
                    case ETileType.Forest:
                        fCost = forestTileCost;
                        break;
                    case ETileType.Sand:
                        fCost = sandTileCost;
                        break;
                    case ETileType.COUNT:
                
                    default:
                        throw new ArgumentOutOfRangeException(nameof(tileType), tileType, null);
                }
                
                // NOTA: Entre más anidado (interno, profundo) esté el for, más a la derecha va en el corchete su índice.
                _grid[i][j] = new Node(j, i, tileType, fCost, isWalkable);
            }
        }
    }

    /// <summary>
    /// Algoritmo que nos dice si hay un camino desde un Nodo origen hacia un Nodo objetivo.
    /// </summary>
    /// <returns></returns>
    private bool DepthFirstSearchRecursive(Node origin, Node goal)
    {
        // Los algoritmos de pathfinding construyen un árbol con la información del grafo.
        // Lo primero que tenemos que establecer es ¿Cuál es la raíz del árbol?
        // La raíz del árbol siempre va a ser el punto de origen del algoritmo.
        // Node root = origin; // su padre tiene que ser null
        
        // si esto es recursivo, ¿Qué valor es el que vamos "avanzando"?
        // la meta no va a cambiar, entonces lo único que podemos avanzar es el origen
        
        // ¿Cuándo se detiene esta recursión? Hay dos:
        // A) cuando llegamos a la meta.
        if (origin == goal)
            return true; // sí hubo camino
        
        // B) cuando no hay camino.

        int x = origin.X;
        int y = origin.Y;
        
        // Mandamos a llamar la misma función, pero un paso más adelante en el tiempo.
        // Primero checamos arriba:
        if(CheckNode(origin, 0, EDirections.Up, goal))
            return true;
        
        // checamos derecha:
        if(CheckNode(origin, EDirections.Right, 0, goal))
            return true;

        // nodo de la izquierda
        if(CheckNode(origin, EDirections.Left, 0, goal))
            return true;
        
        // cambia la comparación de si se puede ir a esa dirección o no; hacia donde se obtiene el nodo vecino, 
        // Nodo de abajo
        if(CheckNode(origin, 0, EDirections.Down, goal))
            return true;
        
        // si se acabó la recursión y nunca llegaste a la meta, es que no hay camino.
        return false; 
    }

    private bool CheckNode(Node current, EDirections xOffset, EDirections yOffset, Node goal)
    {
        return CheckNode(current, (int)xOffset, (int)yOffset, goal);
    }

    private bool CheckNode(Node current, int xOffset, int yOffset, Node goal)
    {
        if (current.Y + yOffset >= height
            || current.Y + yOffset < 0
            || current.X + xOffset >= width
            || current.X + xOffset < 0) 
            return false; // si se sale del grid, entonces no es nodo válido.
        
        Node neighborNode = _grid[current.Y + yOffset][current.X + xOffset];
        if (neighborNode.Parent == null && neighborNode.Walkable) // tiene que ser un nodo no-visitado Y que sea caminable.
        {
            neighborNode.Parent = current;
            // Si eso fue true, regresamos true
            if (DepthFirstSearchRecursive(neighborNode, goal))
            {
                Debug.Log($"el nodo {current.X},{current.Y} fue parte del camino.");
                return true;
            }
        }

        return false;
    }

    private Node CheckValidNode(Node current, int xOffset, int yOffset)
    {
        if (current.Y + yOffset >= height
           || current.Y + yOffset < 0
           || current.X + xOffset >= width
           || current.X + xOffset < 0) 
        return null; // si se sale del grid, entonces no es nodo válido.
        
        Node neighborNode = _grid[current.Y + yOffset][current.X + xOffset];
        
        if (!neighborNode.Walkable)
            return null;
        
        return neighborNode;
    }

    
    private Node CheckNode(Node current, int xOffset, int yOffset)
    {
        Node neighborNode = CheckValidNode(current, xOffset, yOffset);
        if(neighborNode == null)
            return null;
        
        if (neighborNode.Parent == null && neighborNode.Walkable) // tiene que ser un nodo no-visitado Y que sea caminable.
        {
            neighborNode.Parent = current;
            // Si eso fue true, regresamos true
            return neighborNode;
        }

        return null;
    }
    
    private Node CheckNodeDjikstra(Node current, int xOffset, int yOffset)
    {
        Node neighborNode = CheckValidNode(current, xOffset, yOffset);
        if(neighborNode == null)
            return null;
        
        if (neighborNode.Parent == null) // tiene que ser un nodo no-visitado Y que sea caminable.
        {
            neighborNode.Parent = current;
            neighborNode.TotalCost = neighborNode.TerrainCost +  neighborNode.Parent.TotalCost;
            // Si eso fue true, regresamos true
            return neighborNode;
        }
        //si si tiene parent, tenemos que checar si el current node seria mejor padre
        if (current.TotalCost < neighborNode.Parent.TotalCost)
        {
            Debug.Log($"Se cambió el padre del nodo X{neighborNode.X},Y{neighborNode.Y} " +
                      $"que era X{neighborNode.Parent.X},Y{neighborNode.Parent.Y} con costo {neighborNode.Parent.TotalCost} " +
                      $"y ahora es X{current.X},Y{current.Y} con costo {current.TotalCost}");
            
            //nosotros nos volvemos el nuebo padre
            neighborNode.Parent = current;
            neighborNode.TotalCost = neighborNode.TerrainCost +  neighborNode.Parent.TotalCost;
            return neighborNode;
        }

        return null;
    }

    private bool DepthFirstSearchIterative(Node origin, Node goal)
    {
        // Ponerle al nodo raíz que él es su propio padre:
        origin.Parent = origin; // esto ya lo hace SetupGrid(); Lo dejo por pura claridad.
        
        // Nodo actual, que representa al parámetro Origin en la versión recursiva, 
        // es el que representa el "avance" en el algoritmo.
        Node current = origin;

        // Necesitamos un registro de cuáles nodos ya se han "conocido" o "abierto" en el algoritmo.
        // Aquí necesitamos memoria, algo que almacene cuáles nodos ya conocimos, para diferenciarlos de cuáles no.
        Stack<Node> openList = new Stack<Node>();
        // lista abierta son los nodos que se sabe que existen, pero todavía visitan.
        
        // los nodos "visitados" o explorados, o expandidos, se les conoce como "Cerrados"
        // Si cerraste todos los nodos, y no llegaste a la meta, quiere decir que no hay camino.
        // List<Node> closedList = new List<Node>();
        
        // La lista abierta empieza con el origen dentro
        openList.Push(origin);
        
        // mientras nuestro nodo actual no sea nuestro nodo meta, Y mientras todavía haya nodos por explorar,
        // entonces le seguimos.
        while (openList.Count > 0)
        {
            // revisar el del tope de la pila, SIN SACARLO, porque esto es como una pila de llamadas, no se termina
            // de procesar ese nodo hasta que se terminen de procesar todos los que irían encima de él en la pila de llamadas.
            current = openList.Peek();

            // lo checamos aquí para no hacer el paso extra de que Goal visita a alguno de sus vecinos.
            if (current == goal)
                return true; // si sí se llegó a la meta, sí hubo un camino.
            
            // metemos elementos en la pila de abiertos.
            // Metemos nodos, que sean vecinos de current (arriba, abajo, izquierda, derecha),
            // y que su parent sea null, y él sea caminable.

            int x = current.X;
            int y = current.Y;
            // checamos nodo de arriba
            Node upNode = CheckNode(current, 0, (int)EDirections.Up);
            if (upNode != null)
            {
                openList.Push(upNode);
                continue;
            }

            Node rightNode = CheckNode(current, (int)EDirections.Right, 0);
            if (rightNode != null)
            {
                openList.Push(rightNode);
                continue;
            }
            
            Node downNode = CheckNode(current, 0, (int)EDirections.Down);
            if (downNode != null)
            {
                openList.Push(downNode);
                continue;
            }
            
            Node leftNode = CheckNode(current, (int)EDirections.Left, 0);
            if (leftNode != null)
            {
                openList.Push(leftNode);
                continue;
            }
            
            // esto de aquí hace que su antecesor continue justo donde se había quedado.
            openList.Pop();
            _closedList.Add(current);
        }
        
        
        // si no, pos no.
        return false;
    }

    // ESTA ES LA QUE TIENEN QUE HACER DE TAREA.
    // Usen la Queue https://learn.microsoft.com/es-es/dotnet/api/system.collections.generic.queue-1?view=net-8.0
    private bool BreadthFirstSearch(Node origin, Node goal)
    {
        // Nodo origen es su propio padre.
        origin.Parent = origin;
        // current inicia siendo origin.
        
        // lista abierta.
        Queue<Node> openQueue = new Queue<Node>();
        
        openQueue.Enqueue(origin);
        
        // lista cerrada. Los nodos que ya están en la lista cerrada ya nunca se necesitan tocar, visitar, etc en ninguno de los
        // algoritmos de pathfinding.
        // Hash (casi) te garantiza tiempos de búsqueda, inserción y borrado de O(1), tiempo constante en el caso promedio.
        // Lo malo en performance de los hashes es iterarlos, porque no guardan realmente un orden.
        HashSet<Node> closedList = new HashSet<Node>();
        
        // Ciclo while, básicamente igual al de Best first search pero sin las prioridades.
        while (openQueue.Count > 0)
        {
            //sacar el de enfrente
            Node current = openQueue.Dequeue();
            _closedList.Add(current);
            // checar si ya llegaron a la meta
            if (current == goal)
            {
                Debug.Log($"Camino encontrado desde {origin.X}, {origin.Y} hasta {goal.X}, {goal.Y}");

                Node pathNode = goal;
                while (pathNode.Parent != pathNode)
                {
                    _pathToGoal.Add(pathNode);
                    pathNode = pathNode.Parent;
                }
                _pathToGoal.Add(origin);
                _pathToGoal.Reverse();
                return true;
            }
            // si no, intentar meter a todos los vecinos.
            Node upNode = CheckNode(current, 0, (int)EDirections.Up);
            if (upNode != null)
                openQueue.Enqueue(upNode);

            Node rightNode = CheckNode(current, (int)EDirections.Right, 0);
            if (rightNode != null)
                openQueue.Enqueue(rightNode);

            Node downNode = CheckNode(current, 0, (int)EDirections.Down);
            if (downNode != null)
                openQueue.Enqueue(downNode);

            Node leftNode = CheckNode(current, (int)EDirections.Left, 0);
            if (leftNode != null)
                openQueue.Enqueue(leftNode);
        }
        Debug.Log($"No se encontró camino");
        return false; // si no se encontró camino.
    }
    
    
    private bool BestFirstSearch(Node origin, Node goal)
    {
        // Nodo origen es su propio padre.
        origin.Parent = origin;
        
        // lista abierta. Que es una fila de prioridad (PriorityQueue)
        PriorityQueue<Node, float> openPriorityQueue = new PriorityQueue<Node, float>();
        
        // Hay que meter al origin a la lista abierta, para que current inicie siendo origin.
        openPriorityQueue.Enqueue(origin, 0.0f);
        


        Node current = null;
        while (!openPriorityQueue.IsEmpty()) // mientras todavía haya elementos en la lista abierta.
        {
            // tomamos el del frente y ese se vuelve current
            current = openPriorityQueue.Dequeue();

            // lo metemos a la lista cerrada
            _closedList.Add(current);

            // checamos si ya llegamos a la meta.
            if (current == goal)
            {
                Debug.Log($"Camino encontrado desde {origin.X}, {origin.Y} hasta {goal.X}, {goal.Y}" );
                // si ya llegamos entonces retornamos true.
                return true;
            }

            // si no hemos llegado, intentamos meter a cada uno de los vecinos de este nodo a la lista abierta.
            // Lo mismo, excepto que vamos a tomar en cuenta la heurística para meterlos (ordenarlos) en la lista abierta.
            int x = current.X;
            int y = current.Y;
            // checamos nodo de arriba (Nos dice que está dentro de la cuadrícula, que no tiene parent y que sí
            // es caminable).
            Node upNode = CheckNode(current, 0, (int)EDirections.Up);
            if (upNode != null)
            {
                // Necesitamos calcular la prioridad antes de meterlo a la lista abierta.
                // En este caso es la distancia euclidiana entre dicho nodo y la meta.
                float distance = math.sqrt(math.square(upNode.X - goal.X) + math.square(upNode.Y - goal.Y));
                openPriorityQueue.Enqueue(upNode, distance);
            }
            
            Node rightNode = CheckNode(current, (int)EDirections.Right, 0);
            if (rightNode != null)
            {
                float distance = math.sqrt(math.square(rightNode.X - goal.X) + math.square(rightNode.Y - goal.Y));
                openPriorityQueue.Enqueue(rightNode, distance);
            }
            
            Node downNode = CheckNode(current, 0, (int)EDirections.Down);
            if (downNode != null)
            {
                float distance = math.sqrt(math.square(downNode.X - goal.X) + math.square(downNode.Y - goal.Y));
                openPriorityQueue.Enqueue(downNode, distance);
            }
            
            Node leftNode = CheckNode(current, (int)EDirections.Left, 0);
            if (leftNode != null)
            {
                float distance = math.sqrt(math.square(leftNode.X - goal.X) + math.square(leftNode.Y - goal.Y));
                openPriorityQueue.Enqueue(leftNode, distance);
            }
        }
        
        return false; // si no se encontró camino.
    }
    
    
    private bool DjikstraSearch(Node origin, Node goal)
    {
        // Nodo origen es su propio padre.
        origin.Parent = origin;
        
        // lista abierta. Que es una fila de prioridad (PriorityQueue)
        PriorityQueue<Node, float> openPriorityQueue = new PriorityQueue<Node, float>();
        
        // Hay que meter al origin a la lista abierta, para que current inicie siendo origin.
        openPriorityQueue.Enqueue(origin, 0.0f);
        


        Node current = null;
        while (!openPriorityQueue.IsEmpty()) // mientras todavía haya elementos en la lista abierta.
        {
            // tomamos el del frente y ese se vuelve current
            current = openPriorityQueue.Dequeue();

            // lo metemos a la lista cerrada
            _closedList.Add(current);

            // checamos si ya llegamos a la meta.
            if (current == goal)
            {
                Debug.Log($"Camino encontrado desde {origin.X}, {origin.Y} hasta {goal.X}, {goal.Y}" );
                // si ya llegamos entonces retornamos true.
                return true;
            }

            // si no hemos llegado, intentamos meter a cada uno de los vecinos de este nodo a la lista abierta.
            // Lo mismo, excepto que vamos a tomar en cuenta la heurística para meterlos (ordenarlos) en la lista abierta.
            int x = current.X;
            int y = current.Y;
            // checamos nodo de arriba (Nos dice que está dentro de la cuadrícula, que no tiene parent y que sí
            // es caminable).
            Node upNode = CheckNodeDjikstra(current, 0, (int)EDirections.Up);
            if (upNode != null)
            {
                // costo del nodo mas el del parent
                float totalCost = upNode.TerrainCost + upNode.Parent.TotalCost;
                openPriorityQueue.Enqueue(upNode, totalCost);
            }
            
            Node rightNode = CheckNodeDjikstra(current, (int)EDirections.Right, 0);
            if (rightNode != null)
            {
                float totalCost = rightNode.TerrainCost + rightNode.Parent.TotalCost;
                openPriorityQueue.Enqueue(rightNode, totalCost);
            }
            
            Node downNode = CheckNodeDjikstra(current, 0, (int)EDirections.Down);
            if (downNode != null)
            {
                float totalCost = downNode.TerrainCost + downNode.Parent.TotalCost;
                openPriorityQueue.Enqueue(downNode, totalCost);
            }
            
            Node leftNode = CheckNodeDjikstra(current, (int)EDirections.Left, 0);
            if (leftNode != null)
            {
                float totalCost = leftNode.TerrainCost + leftNode.Parent.TotalCost;
                openPriorityQueue.Enqueue(leftNode, totalCost);
            }
        }
        
        return false; // si no se encontró camino.
    }
    
    
    private bool AStarSearch(Node origin, Node goal)
    {
        // Nodo origen es su propio padre.
        origin.Parent = origin;
        
        // lista abierta. Que es una fila de prioridad (PriorityQueue)
        AStarPriorityQueue openPriorityQueue = new AStarPriorityQueue();
        
        // Hay que meter al origin a la lista abierta, para que current inicie siendo origin.
        openPriorityQueue.Enqueue(origin, 0.0f);
        


        Node current = null;
        while (!openPriorityQueue.IsEmpty()) // mientras todavía haya elementos en la lista abierta.
        {
            // tomamos el del frente y ese se vuelve current
            current = openPriorityQueue.Dequeue();

            // lo metemos a la lista cerrada
            _closedList.Add(current);

            // checamos si ya llegamos a la meta.
            if (current == goal)
            {
                Debug.Log($"Camino encontrado desde {origin.X}, {origin.Y} hasta {goal.X}, {goal.Y}" );
                // si ya llegamos entonces retornamos true.
                return true;
            }

            // si no hemos llegado, intentamos meter a cada uno de los vecinos de este nodo a la lista abierta.
            // Lo mismo, excepto que vamos a tomar en cuenta la heurística para meterlos (ordenarlos) en la lista abierta.
            int x = current.X;
            int y = current.Y;
            // checamos nodo de arriba (Nos dice que está dentro de la cuadrícula, que no tiene parent y que sí
            // es caminable).
            Node upNode = CheckNodeDjikstra(current, 0, (int)EDirections.Up);
            if (upNode != null)
            {
                // costo del nodo mas el del parent
                float totalCost = upNode.TerrainCost + upNode.Parent.TotalCost;
                //parte heuristica
                float distance = math.sqrt(math.square(upNode.X -goal.X)+math.square(upNode.Y -goal.Y));
                
                //al ponerlo en la fila abierta si es con todo y distancia
                //pero el totalcost no guarda esa parte heuristica
                openPriorityQueue.Enqueue(upNode, totalCost+distance);
            }
            
            Node rightNode = CheckNodeDjikstra(current, (int)EDirections.Right, 0);
            if (rightNode != null)
            {
                float totalCost = rightNode.TerrainCost + rightNode.Parent.TotalCost;
                float distance = math.sqrt(math.square(rightNode.X -goal.X)+math.square(rightNode.Y -goal.Y));
                openPriorityQueue.Enqueue(rightNode, totalCost+distance);
            }
            
            Node downNode = CheckNodeDjikstra(current, 0, (int)EDirections.Down);
            if (downNode != null)
            {
                float totalCost = downNode.TerrainCost + downNode.Parent.TotalCost;
                float distance = math.sqrt(math.square(downNode.X -goal.X)+math.square(downNode.Y -goal.Y));
                openPriorityQueue.Enqueue(downNode, totalCost+distance);
            }
            
            Node leftNode = CheckNodeDjikstra(current, (int)EDirections.Left, 0);
            if (leftNode != null)
            {
                float totalCost = leftNode.TerrainCost + leftNode.Parent.TotalCost;
                float distance = math.sqrt(math.square(leftNode.X -goal.X)+math.square(leftNode.Y -goal.Y));
                openPriorityQueue.Enqueue(leftNode, totalCost+distance);
            }
        }
        
        return false; // si no se encontró camino.
    }
    
    
    // recursivo VS iterativo
    // Recursivo es que se manda a llamar a sí mismo dentro del cuerpo de la función.
    public int RestaHastaCero(int value)
    {
        // stopping condition. La condición que hace que la recursión se detenga.
        if (value < 0)
            return value; // aquí ya no va a volver a mandar a llamarse, y entonces se acaba la recursión.
        
        Debug.Log($"Conteo regresivo: {value}");
        return RestaHastaCero(value - 1);
    }


    void SetupGrid()
    {
        // asegurarse de que tanto el origen como la meta son caminables.
        _grid[originY][originX].Walkable = true;
        _grid[goalY][goalX].Walkable = true;

        // le ponemos que es su propio padre.
        _grid[originY][originX].Parent = _grid[originY][originX];

    }

    public bool FindPath()
    {
        // Inicializamos nuestra cuadrícula antes de mandar a llamar cualquier algoritmo de pathfinding.
        InitializeGrid();
        SetupGrid();
        
        // él es su propio parent, de lo contrario los otros nodos lo toman como que no ha sido visitado y lo usan 
        // para el pathfinding.
        _grid[originY][originX].Parent = _grid[originY][originX];
        
        if (AStarSearch(_grid[originY][originX],  _grid[goalY][goalX]))
        { 
            Debug.Log($"Sí se encontró camino desde {originX},{originY}, hasta {goalX},{goalY}");
            //iterar desde la meta usando parent hasta regresar a la raiz
            Node current = _grid[goalY][goalX];
            while (current.Parent != current)
            {
                Debug.Log($"El nodo: {current.X}, {current.Y} fue parte de la ruta");
                _pathToGoal.Add(current);
                current = current.Parent; //lo movemos para regresar a la raiz
            }
            Debug.Log($"El nodo: {current.X}, {current.Y} fue parte de la ruta");
            return true;
        }
        else
        { 
            Debug.Log($"No se encontró camino desde {originX},{originY}, hasta {goalX},{goalY}");
            return false;
        }
    }
    
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RestaHastaCero(10);
        // Iterativo:
        for (int i = 10; i < 0; i--)
        {
            Debug.Log($"Conteo regresivo: {i}");
        }
        
        // Todo lo que se puede hacer iterativo se puede hacer recursivo y viceversa.

        // Inicializamos nuestra cuadrícula antes de mandar a llamar cualquier algoritmo de pathfinding.
        InitializeGrid();
        SetupGrid();
        
        // él es su propio parent, de lo contrario los otros nodos lo toman como que no ha sido visitado y lo usan 
        // para el pathfinding.
        _grid[originY][originX].Parent = _grid[originY][originX];
        //if (DepthFirstSearchRecursive(_grid[originY][originX], _grid[goalY][goalX]))
        //if (DepthFirstSearchIterative(_grid[originY][originX], _grid[goalY][goalX]))
        //if (BestFirstSearch(_grid[originY][originX], _grid[goalY][goalX]))
        //if (DjikstraSearch(_grid[originY][originX], _grid[goalY][goalX]))
        //if (AStarSearch(_grid[originY][originX],  _grid[goalY][goalX]))
        // if (BreadthFirstSearch(_grid[originY][originX],  _grid[goalY][goalX]))
        // { 
        //     Debug.Log($"Sí se encontró camino desde {originX},{originY}, hasta {goalX},{goalY}");
        //     //iterar desde la meta usando parent hasta regresar a la raiz
        //     Node current = _grid[goalY][goalX];
        //     while (current.Parent != current)
        //     {
        //         Debug.Log($"El nodo: {current.X}, {current.Y} fue parte de la ruta");
        //         _pathToGoal.Add(current);
        //         current = current.Parent; //lo movemos para regresar a la raiz
        //     }
        //     Debug.Log($"El nodo: {current.X}, {current.Y} fue parte de la ruta");
        // }
        // else
        // { 
        //     Debug.Log($"No se encontró camino desde {originX},{originY}, hasta {goalX},{goalY}");
        // }
        
        // VERSIONES CORRUTINA
        //StartCoroutine(DepthFirstSearchIterativeCoroutine(_grid[originY][originX], _grid[goalY][goalX]));

        //StartCoroutine(BestFirstSearchCoroutine(_grid[originY][originX], _grid[goalY][goalX]));
        //StartCoroutine(AStarSearchCoroutine(_grid[originY][originX], _grid[goalY][goalX]));
        //StartCoroutine(AStarSearchDiagonalCoroutine(_grid[originY][originX], _grid[goalY][goalX]));
        StartCoroutine(BreadthFirstSearchCoroutine(_grid[originY][originX], _grid[goalY][goalX]));
        
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;

        if (_grid == null)
            return;

        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // para cada cuadro, vamos a dibujar un cubo con cierto color. 
                // Blanco para los caminables, y magenta para los no caminables.
                if (_grid[y][x].Walkable)
                {
                    switch (_grid[y][x].TileType)
                    {
                        case ETileType.Normal:
                            Gizmos.color = Color.white;
                            break;
                        case ETileType.Fire:
                            Gizmos.color = Color.orangeRed;
                            break;
                        case ETileType.Forest:
                            Gizmos.color = Color.darkGreen;
                            break;
                        case ETileType.Sand:
                            Gizmos.color = Color.sandyBrown;
                            break;
                        case ETileType.COUNT:
                
                        default:
                            throw new ArgumentOutOfRangeException(nameof(ETileType), _grid[y][x].TileType, null);
                    }
                    
                }
                else
                {
                    Gizmos.color = Color.black;
                }

                Gizmos.DrawCube(new Vector3(x, -y, 0.0f), Vector3.one * 0.8f);

                if (_grid[y][x].Parent != null)
                {
                    if (_pathToGoal.Contains(_grid[y][x]))
                    {
                        Gizmos.color = Color.magenta;
                    } else if (_closedList.Contains(_grid[y][x]))
                    {
                        Gizmos.color = Color.darkRed;
                    }
                    else
                    {
                        Gizmos.color = Color.navyBlue;
                    }
                    // entonces que nos dibuje una línea desde el parent hasta este nodo.
                    Gizmos.DrawLine(new Vector3(_grid[y][x].Parent.X, -_grid[y][x].Parent.Y, 1.0f), new Vector3(x, -y, 1.0f));
                    // le ponemos una esfera chiquita al nodo hijo, para diferenciar quién es padre y quien es hijo.
                    Gizmos.DrawSphere(new Vector3(x, -y, 1.0f), gizmosSphereSize);
                }
            }
        }
        
        Gizmos.color = Color.green;
        Gizmos.DrawCube(new Vector3(originX, -originY, 0.0f), Vector3.one * 0.9f);

        Gizmos.color = Color.red;
        Gizmos.DrawCube(new Vector3(goalX, -goalY, 0.0f), Vector3.one * 0.9f);
        
    }
    
    
    private IEnumerator DepthFirstSearchIterativeCoroutine(Node origin, Node goal)
    {
        // Ponerle al nodo raíz que él es su propio padre:
        origin.Parent = origin; // esto ya lo hace SetupGrid(); Lo dejo por pura claridad.
        
        // Nodo actual, que representa al parámetro Origin en la versión recursiva, 
        // es el que representa el "avance" en el algoritmo.
        Node current = origin;

        // Necesitamos un registro de cuáles nodos ya se han "conocido" o "abierto" en el algoritmo.
        // Aquí necesitamos memoria, algo que almacene cuáles nodos ya conocimos, para diferenciarlos de cuáles no.
        Stack<Node> openList = new Stack<Node>();
        // lista abierta son los nodos que se sabe que existen, pero todavía visitan.
        
        // los nodos "visitados" o explorados, o expandidos, se les conoce como "Cerrados"
        // Si cerraste todos los nodos, y no llegaste a la meta, quiere decir que no hay camino.
        // List<Node> closedList = new List<Node>();
        
        // La lista abierta empieza con el origen dentro
        openList.Push(origin);
        
        // mientras nuestro nodo actual no sea nuestro nodo meta, Y mientras todavía haya nodos por explorar,
        // entonces le seguimos.
        while (openList.Count > 0)
        {
            yield return new WaitForSeconds(cycleSpeed);
            
            // revisar el del tope de la pila, SIN SACARLO, porque esto es como una pila de llamadas, no se termina
            // de procesar ese nodo hasta que se terminen de procesar todos los que irían encima de él en la pila de llamadas.
            current = openList.Peek();

            // lo checamos aquí para no hacer el paso extra de que Goal visita a alguno de sus vecinos.
            if (current == goal)
                yield break; // si sí se llegó a la meta, sí hubo un camino.
            
            // metemos elementos en la pila de abiertos.
            // Metemos nodos, que sean vecinos de current (arriba, abajo, izquierda, derecha),
            // y que su parent sea null, y él sea caminable.

            int x = current.X;
            int y = current.Y;
            // checamos nodo de arriba
            Node upNode = CheckNode(current, 0, (int)EDirections.Up);
            if (upNode != null)
            {
                openList.Push(upNode);
                continue;
            }

            Node rightNode = CheckNode(current, (int)EDirections.Right, 0);
            if (rightNode != null)
            {
                openList.Push(rightNode);
                continue;
            }
            
            Node downNode = CheckNode(current, 0, (int)EDirections.Down);
            if (downNode != null)
            {
                openList.Push(downNode);
                continue;
            }
            
            Node leftNode = CheckNode(current, (int)EDirections.Left, 0);
            if (leftNode != null)
            {
                openList.Push(leftNode);
                continue;
            }
            
            // esto de aquí hace que su antecesor continue justo donde se había quedado.
            openList.Pop();
            _closedList.Add(current);
        }
        
        
        // si no, pos no.
        yield break;
    }

    private IEnumerator BreadthFirstSearchCoroutine(Node origin, Node goal)
    {
        // Nodo origen es su propio padre.
        origin.Parent = origin;
        // current inicia siendo origin.
        
        // lista abierta.
        Queue<Node> openQueue = new Queue<Node>();
        
        openQueue.Enqueue(origin);
        
        // lista cerrada. Los nodos que ya están en la lista cerrada ya nunca se necesitan tocar, visitar, etc en ninguno de los
        // algoritmos de pathfinding.
        // Hash (casi) te garantiza tiempos de búsqueda, inserción y borrado de O(1), tiempo constante en el caso promedio.
        // Lo malo en performance de los hashes es iterarlos, porque no guardan realmente un orden.
        HashSet<Node> closedList = new HashSet<Node>();
        
        // Ciclo while, básicamente igual al de Best first search pero sin las prioridades.
        while (openQueue.Count > 0)
        {
            yield return new WaitForSeconds(cycleSpeed);
            //sacar el de enfrente
            Node current = openQueue.Dequeue();
            _closedList.Add(current);
            // checar si ya llegaron a la meta
            if (current == goal)
            {
                Debug.Log($"Camino encontrado desde {origin.X}, {origin.Y} hasta {goal.X}, {goal.Y}");

                // Node pathNode = goal;
                // while (pathNode.Parent != pathNode)
                // {
                //     _pathToGoal.Add(pathNode);
                //     pathNode = pathNode.Parent;
                // }
                // // _pathToGoal.Add(origin);
                // _pathToGoal.Reverse();
                LogPathEnOrden(goal);
                yield break;
            }
            // si no, intentar meter a todos los vecinos.
            Node upNode = CheckNode(current, 0, (int)EDirections.Up);
            if (upNode != null)
                openQueue.Enqueue(upNode);

            Node rightNode = CheckNode(current, (int)EDirections.Right, 0);
            if (rightNode != null)
                openQueue.Enqueue(rightNode);

            Node downNode = CheckNode(current, 0, (int)EDirections.Down);
            if (downNode != null)
                openQueue.Enqueue(downNode);

            Node leftNode = CheckNode(current, (int)EDirections.Left, 0);
            if (leftNode != null)
                openQueue.Enqueue(leftNode);
        }
        Debug.Log($"No se encontró camino");
        
        yield break; // si no se encontró camino.
    }
    
    
    private IEnumerator BestFirstSearchCoroutine(Node origin, Node goal)
    {
        // Nodo origen es su propio padre.
        origin.Parent = origin;
        
        // lista abierta. Que es una fila de prioridad (PriorityQueue)
        PriorityQueue<Node, float> openPriorityQueue = new PriorityQueue<Node, float>();
        
        // Hay que meter al origin a la lista abierta, para que current inicie siendo origin.
        openPriorityQueue.Enqueue(origin, 0.0f);
        


        Node current = null;
        while (!openPriorityQueue.IsEmpty()) // mientras todavía haya elementos en la lista abierta.
        {
            yield return new WaitForSeconds(cycleSpeed);
            
            // tomamos el del frente y ese se vuelve current
            current = openPriorityQueue.Dequeue();

            // lo metemos a la lista cerrada
            _closedList.Add(current);

            // checamos si ya llegamos a la meta.
            if (current == goal)
            {
                Debug.Log($"Camino encontrado desde {origin.X}, {origin.Y} hasta {goal.X}, {goal.Y}" );
                // si ya llegamos entonces retornamos true.
                yield break;
            }

            // si no hemos llegado, intentamos meter a cada uno de los vecinos de este nodo a la lista abierta.
            // Lo mismo, excepto que vamos a tomar en cuenta la heurística para meterlos (ordenarlos) en la lista abierta.
            int x = current.X;
            int y = current.Y;
            // checamos nodo de arriba (Nos dice que está dentro de la cuadrícula, que no tiene parent y que sí
            // es caminable).
            Node upNode = CheckNode(current, 0, (int)EDirections.Up);
            if (upNode != null)
            {
                // Necesitamos calcular la prioridad antes de meterlo a la lista abierta.
                // En este caso es la distancia euclidiana entre dicho nodo y la meta.
                float distance = math.sqrt(math.square(upNode.X - goal.X) + math.square(upNode.Y - goal.Y));
                openPriorityQueue.Enqueue(upNode, distance);
            }
            
            Node rightNode = CheckNode(current, (int)EDirections.Right, 0);
            if (rightNode != null)
            {
                float distance = math.sqrt(math.square(rightNode.X - goal.X) + math.square(rightNode.Y - goal.Y));
                openPriorityQueue.Enqueue(rightNode, distance);
            }
            
            Node downNode = CheckNode(current, 0, (int)EDirections.Down);
            if (downNode != null)
            {
                float distance = math.sqrt(math.square(downNode.X - goal.X) + math.square(downNode.Y - goal.Y));
                openPriorityQueue.Enqueue(downNode, distance);
            }
            
            Node leftNode = CheckNode(current, (int)EDirections.Left, 0);
            if (leftNode != null)
            {
                float distance = math.sqrt(math.square(leftNode.X - goal.X) + math.square(leftNode.Y - goal.Y));
                openPriorityQueue.Enqueue(leftNode, distance);
            }
        }
        
        yield break; // si no se encontró camino.
    }
    
    private IEnumerator AStarSearchCoroutine(Node origin, Node goal)
    {
        // Nodo origen es su propio padre.
        origin.Parent = origin;
        
        // lista abierta. Que es una fila de prioridad (PriorityQueue)
        AStarPriorityQueue openPriorityQueue = new AStarPriorityQueue();
        
        // Hay que meter al origin a la lista abierta, para que current inicie siendo origin.
        openPriorityQueue.Enqueue(origin, 0.0f);
        


        Node current = null;
        while (!openPriorityQueue.IsEmpty()) // mientras todavía haya elementos en la lista abierta.
        {
            yield return new WaitForSeconds(cycleSpeed);
            // tomamos el del frente y ese se vuelve current
            current = openPriorityQueue.Dequeue();

            // lo metemos a la lista cerrada
            _closedList.Add(current);

            // checamos si ya llegamos a la meta.
            if (current == goal)
            {
                Debug.Log($"Camino encontrado desde {origin.X}, {origin.Y} hasta {goal.X}, {goal.Y}" );
                
                Node currentToGoal = _grid[goalY][goalX];
                while (currentToGoal.Parent != currentToGoal)
                {
                    Debug.Log($"El nodo: {currentToGoal.X}, {currentToGoal.Y} fue parte de la ruta");
                    _pathToGoal.Add(currentToGoal);
                    currentToGoal = currentToGoal.Parent; //lo movemos para regresar a la raiz
                }
                // si ya llegamos entonces retornamos true.
                yield break;
            }

            // si no hemos llegado, intentamos meter a cada uno de los vecinos de este nodo a la lista abierta.
            // Lo mismo, excepto que vamos a tomar en cuenta la heurística para meterlos (ordenarlos) en la lista abierta.
            int x = current.X;
            int y = current.Y;
            // checamos nodo de arriba (Nos dice que está dentro de la cuadrícula, que no tiene parent y que sí
            // es caminable).
            Node upNode = CheckNodeDjikstra(current, 0, (int)EDirections.Up);
            if (upNode != null)
            {
                // costo del nodo mas el del parent
                float totalCost = upNode.TerrainCost + upNode.Parent.TotalCost;
                //parte heuristica
                upNode.HCost = math.sqrt(math.square(upNode.X -goal.X)+math.square(upNode.Y -goal.Y));
                
                //al ponerlo en la fila abierta si es con todo y distancia
                //pero el totalcost no guarda esa parte heuristica
                openPriorityQueue.Enqueue(upNode, totalCost+upNode.HCost);
            }
            
            Node rightNode = CheckNodeDjikstra(current, (int)EDirections.Right, 0);
            if (rightNode != null)
            {
                float totalCost = rightNode.TerrainCost + rightNode.Parent.TotalCost;
                rightNode.HCost = math.sqrt(math.square(rightNode.X -goal.X)+math.square(rightNode.Y -goal.Y));
                openPriorityQueue.Enqueue(rightNode, totalCost+rightNode.HCost);
            }
            
            Node downNode = CheckNodeDjikstra(current, 0, (int)EDirections.Down);
            if (downNode != null)
            {
                float totalCost = downNode.TerrainCost + downNode.Parent.TotalCost;
                downNode.HCost = math.sqrt(math.square(downNode.X -goal.X)+math.square(downNode.Y -goal.Y));
                openPriorityQueue.Enqueue(downNode, totalCost+downNode.HCost);
            }
            
            Node leftNode = CheckNodeDjikstra(current, (int)EDirections.Left, 0);
            if (leftNode != null)
            {
                float totalCost = leftNode.TerrainCost + leftNode.Parent.TotalCost;
                leftNode.HCost = math.sqrt(math.square(leftNode.X -goal.X)+math.square(leftNode.Y -goal.Y));
                openPriorityQueue.Enqueue(leftNode, totalCost+leftNode.HCost);
            }
        }
        
        yield break; // si no se encontró camino.
    }
    
    private IEnumerator AStarSearchDiagonalCoroutine(Node origin, Node goal)
    {
        // Nodo origen es su propio padre.
        origin.Parent = origin;
        
        // lista abierta. Que es una fila de prioridad (PriorityQueue)
        AStarPriorityQueue openPriorityQueue = new AStarPriorityQueue();
        
        // Hay que meter al origin a la lista abierta, para que current inicie siendo origin.
        openPriorityQueue.Enqueue(origin, 0.0f);
        


        Node current = null;
        while (!openPriorityQueue.IsEmpty()) // mientras todavía haya elementos en la lista abierta.
        {
            yield return new WaitForSeconds(cycleSpeed);
            // tomamos el del frente y ese se vuelve current
            current = openPriorityQueue.Dequeue();

            // lo metemos a la lista cerrada
            _closedList.Add(current);

            // checamos si ya llegamos a la meta.
            if (current == goal)
            {
                Debug.Log($"Camino encontrado desde {origin.X}, {origin.Y} hasta {goal.X}, {goal.Y}" );
                
                Node currentToGoal = _grid[goalY][goalX];
                while (currentToGoal.Parent != currentToGoal)
                {
                    Debug.Log($"El nodo: {currentToGoal.X}, {currentToGoal.Y} fue parte de la ruta");
                    _pathToGoal.Add(currentToGoal);
                    currentToGoal = currentToGoal.Parent; //lo movemos para regresar a la raiz
                }
                // si ya llegamos entonces retornamos true.
                yield break;
            }

            // si no hemos llegado, intentamos meter a cada uno de los vecinos de este nodo a la lista abierta.
            // Lo mismo, excepto que vamos a tomar en cuenta la heurística para meterlos (ordenarlos) en la lista abierta.
            int x = current.X;
            int y = current.Y;
            // checamos nodo de arriba (Nos dice que está dentro de la cuadrícula, que no tiene parent y que sí
            // es caminable).

            for (int yOffset = -1; yOffset < 1; yOffset++)
            {
                for (int xOffset = -1; xOffset < 1; xOffset++)
                {
                    if (x == 0 && y == 0)
                        continue;
                    Node node = CheckNodeDjikstra(current, xOffset, yOffset);
                    if (node != null)
                    {
                        // costo del nodo mas el del parent
                        float totalCost = node.TerrainCost + node.Parent.TotalCost;
                        //parte heuristica
                        node.HCost = math.sqrt(math.square(node.X -goal.X)+math.square(node.Y -goal.Y));
                
                        //al ponerlo en la fila abierta si es con todo y distancia
                        //pero el totalcost no guarda esa parte heuristica
                        openPriorityQueue.Enqueue(node, totalCost+node.HCost);
                    }
                }
            }
            
            
            // Node upNode = CheckNodeDjikstra(current, 0, (int)EDirections.Up);
            // if (upNode != null)
            // {
            //     // costo del nodo mas el del parent
            //     float totalCost = upNode.TerrainCost + upNode.Parent.TotalCost;
            //     //parte heuristica
            //     upNode.HCost = math.sqrt(math.square(upNode.X -goal.X)+math.square(upNode.Y -goal.Y));
            //     
            //     //al ponerlo en la fila abierta si es con todo y distancia
            //     //pero el totalcost no guarda esa parte heuristica
            //     openPriorityQueue.Enqueue(upNode, totalCost+upNode.HCost);
            // }
            //
            // Node rightNode = CheckNodeDjikstra(current, (int)EDirections.Right, 0);
            // if (rightNode != null)
            // {
            //     float totalCost = rightNode.TerrainCost + rightNode.Parent.TotalCost;
            //     rightNode.HCost = math.sqrt(math.square(rightNode.X -goal.X)+math.square(rightNode.Y -goal.Y));
            //     openPriorityQueue.Enqueue(rightNode, totalCost+rightNode.HCost);
            // }
            //
            // Node downNode = CheckNodeDjikstra(current, 0, (int)EDirections.Down);
            // if (downNode != null)
            // {
            //     float totalCost = downNode.TerrainCost + downNode.Parent.TotalCost;
            //     downNode.HCost = math.sqrt(math.square(downNode.X -goal.X)+math.square(downNode.Y -goal.Y));
            //     openPriorityQueue.Enqueue(downNode, totalCost+downNode.HCost);
            // }
            //
            // Node leftNode = CheckNodeDjikstra(current, (int)EDirections.Left, 0);
            // if (leftNode != null)
            // {
            //     float totalCost = leftNode.TerrainCost + leftNode.Parent.TotalCost;
            //     leftNode.HCost = math.sqrt(math.square(leftNode.X -goal.X)+math.square(leftNode.Y -goal.Y));
            //     openPriorityQueue.Enqueue(leftNode, totalCost+leftNode.HCost);
            // }
            //
            // Node upLeftNode = CheckNodeDjikstra(current, (int)EDirections.Left, (int)EDirections.Up);
            // if (upLeftNode != null)
            // {
            //     float totalCost = upLeftNode.TerrainCost + upLeftNode.Parent.TotalCost;
            //     upLeftNode.HCost = math.sqrt(math.square(upLeftNode.X -goal.X)+math.square(upLeftNode.Y -goal.Y));
            //     openPriorityQueue.Enqueue(upLeftNode, totalCost+upLeftNode.HCost);
            // }
            // Node upRightNode = CheckNodeDjikstra(current, (int)EDirections.Left, (int)EDirections.Up);
            // if (upRightNode != null)
            // {
            //     float totalCost = upRightNode.TerrainCost + upRightNode.Parent.TotalCost;
            //     upRightNode.HCost = math.sqrt(math.square(upRightNode.X -goal.X)+math.square(upRightNode.Y -goal.Y));
            //     openPriorityQueue.Enqueue(upRightNode, totalCost+upRightNode.HCost);
            // }
            // Node downLeftNode = CheckNodeDjikstra(current, (int)EDirections.Left, (int)EDirections.Down);
            // if (downLeftNode != null)
            // {
            //     float totalCost = downLeftNode.TerrainCost + downLeftNode.Parent.TotalCost;
            //     downLeftNode.HCost = math.sqrt(math.square(downLeftNode.X -goal.X)+math.square(downLeftNode.Y -goal.Y));
            //     openPriorityQueue.Enqueue(downLeftNode, totalCost+downLeftNode.HCost);
            // }
            // Node downRightNode = CheckNodeDjikstra(current, (int)EDirections.Left, (int)EDirections.Down);
            // if (downRightNode != null)
            // {
            //     float totalCost = downRightNode.TerrainCost + downRightNode.Parent.TotalCost;
            //     downRightNode.HCost = math.sqrt(math.square(downRightNode.X -goal.X)+math.square(downRightNode.Y -goal.Y));
            //     openPriorityQueue.Enqueue(downRightNode, totalCost+downRightNode.HCost);
            // }
            
        }
        
        yield break; // si no se encontró camino.
    }

    void LogPathEnOrden(Node goal)
    {
        Stack<Node> stack = new Stack<Node>();
        Node cur = goal;
        while (cur.Parent != cur)
        {
            stack.Push(cur);
            cur = cur.Parent;
        }
        stack.Push(cur);

        while (stack.Count > 0)
        {
            var n= stack.Pop();
            Debug.Log($"El nodo: {n.X}, {n.Y} fue parte");
            _pathToGoal.Add(n);
        }
    }
    
    
}