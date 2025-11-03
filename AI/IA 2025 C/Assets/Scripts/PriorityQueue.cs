using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//TPriority es un float o int que guarda la prioridad
//TElement es lo que guardas en realidad

public class PriorityQueue<TElement, TPriority>
{
    private SortedDictionary<TPriority, Queue<TElement>> _data = new SortedDictionary<TPriority, Queue<TElement>>();
    
    //INSERTAR
    public void Enqueue(TElement element, TPriority priority)
    {
        //ver si esta en el diccionario esa llave
        if(_data.TryGetValue(priority, out Queue<TElement> queue))
        {
            //SI SI ESTA LA KEY LA PONEMOS AL FINAL DE LA FILA
            queue.Enqueue(element);
            return;
        }
        
        //si no esta, hacemos un nuevo queue para esa llave
        Queue<TElement> newQueue = new Queue<TElement>();
        newQueue.Enqueue(element);
        _data.Add(priority, newQueue);
    }
    
    //public void Enqueue(TElement element, TPriority priorityCost, TPriority priorityHeuristic)
    //{
    //    dynamic cost = priorityCost;
    //    dynamic heuristic = priorityHeuristic;
    //     TPriority totalCost = cost + heuristic;
    //     
    //     //ver si esta en el diccionario esa llave
    //     if(_data.TryGetValue(totalCost, out Queue<TElement> queue))
    //     {
    //         queue.ToArray();
    //         //separar por heuristica
    //         
    //         
    //         //SI SI ESTA LA KEY LA PONEMOS AL FINAL DE LA FILA
    //         queue.Enqueue(element);
    //         return;
    //     }
    //     
    //     //si no esta, hacemos un nuevo queue para esa llave
    //     Queue<TElement> newQueue = new Queue<TElement>();
    //     newQueue.Enqueue(element);
    //     _data.Add(priority, newQueue);
    // }
    
    //BORRAR
    //sacar de la lista los que ya sirvierion
    public TElement Dequeue()
    {
        if (_data.Count == 0)
        {
            Debug.LogError("No elements enqueued");
            return default(TElement); //no hay un inválido por defecto
        }

        //si si hay algo en el queue, devolvemos el menor
        TPriority lowestKey = _data.Keys.Min();
        //obtenemos la queue de esa llave
        Queue<TElement> highestPriorityQueue = _data[lowestKey];
        TElement result = highestPriorityQueue.Dequeue();

        //si quedo vacia la queue, hay que quitar la key
        if (highestPriorityQueue.Count == 0)
        {
            _data.Remove(lowestKey);
        }

        return result;
    }

    public bool IsEmpty()
    {
        return _data.Count == 0;
    }
    
    //BUSCAR
    
    //ACCEDER
    
}

public class AStarPriorityQueue
{
    private SortedDictionary<float, List<Node>> _data = new SortedDictionary<float, List<Node>>();
    
    
    public void Enqueue(Node element, float priority)
    {
        //ver si esta en el diccionario esa llave
        if(_data.TryGetValue(priority, out List<Node> list))
        {
            Debug.Log($"Adding node X{element.X}, Y{element.Y} to list with priority: {priority}");
            list.Add(element);
            list.Sort((p1, p2)=> p1.HCost.CompareTo(p2.HCost));
            string order = "Order after sorting";
            foreach (var node in list)
            {
                order += $"X{node.X}, Y{node.Y}-> Hcost: {node.HCost}, Total Cost: {node.TotalCost};";
            }
            return;
        }
        
        //si no esta, hacemos un nuevo queue para esa llave
        List<Node> newList = new List<Node>();
        newList.Add(element);
        _data.Add(priority, newList);
    }
    
    
    public Node Dequeue()
    {
        if (_data.Count == 0)
        {
            Debug.LogError("No elements enqueued");
            return default(Node); //no hay un inválido por defecto
        }

        //si si hay algo en el queue, devolvemos el menor
        float lowestKey = _data.Keys.Min();
        //obtenemos la queue de esa llave
        List<Node> highestPriorityList = _data[lowestKey];
        Node result = highestPriorityList.First();
        highestPriorityList.RemoveAt(0);

        //si quedo vacia la queue, hay que quitar la key
        if (highestPriorityList.Count == 0)
        {
            _data.Remove(lowestKey);
        }
        
        Debug.LogWarning($"Dequeueing node with priority {lowestKey}, X{result.X}, Y{result.Y} -> HCost: {result.HCost}, Total Cost: {result.TotalCost};");

        return result;
    }
    //si no tiene elementos, esta vacia
    public bool IsEmpty()
    {
        return _data.Count == 0;
    }
    
}