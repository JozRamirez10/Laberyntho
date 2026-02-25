using UnityEngine;
using System.Collections.Generic;

public class GridOccupantWallLarge : GridOccupant
{
    // Implementamos la lógica específica para este tipo de muro
    // donde el Pivote está en el borde trasero central.
    public override List<Vector3> GetOccupiedWorldCenters()
    {
        var board = GetBoard();
        if (board == null) return new List<Vector3>();

        List<Vector3> bluePoints = new List<Vector3>();
        float tileSize = board.tileSize;

        // Dimensiones totales
        float totalWidth = baseSize.x * tileSize;
        float totalDepth = baseSize.y * tileSize;

        Vector3 right = transform.right;
        Vector3 forward = transform.forward;
        
        // --- PASO 1: ENCONTRAR CENTRO MURO (PUNTO ROJO) ---
        // Lógica específica 2x1: Pivote atrás -> Restamos mitad profundidad
        Vector3 centroMuro = transform.position - (forward * (totalDepth / 2f));
        centroMuro.y = transform.position.y;

        // --- PASO 2: DISTRIBUIR PUNTOS AZULES ---
        float startOffsetFromCenter = -(totalWidth / 2f) + (tileSize / 2f);

        for (int x = 0; x < baseSize.x; x++)
        {
            for (int y = 0; y < baseSize.y; y++)
            {
                float currentXOffset = startOffsetFromCenter + (x * tileSize);
                
                // Para profundidad > 1
                float startZOffset = (totalDepth / 2f) - (tileSize / 2f);
                float currentZOffset = startZOffset - (y * tileSize);

                // Cálculo final
                Vector3 bluePoint = centroMuro + (right * currentXOffset) - (forward * (startZOffset - currentZOffset));

                // Simplificación para Y=1 (común en muros)
                if (baseSize.y == 1) 
                {
                    bluePoint = centroMuro + (right * currentXOffset);
                }

                bluePoints.Add(bluePoint);
            }
        }

        return bluePoints;
    }

    // Opcional: Si quieres ver el "Punto Rojo" específico de este cálculo en los gizmos
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected(); // Dibuja lo básico

        // Dibujo extra específico para depurar la lógica 2x1
        var board = GetBoard();
        if (board != null)
        {
            float totalDepth = baseSize.y * board.tileSize;
            Vector3 centroMuro = transform.position - (transform.forward * (totalDepth / 2f));
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(centroMuro, 0.2f); // El famoso punto rojo
            Gizmos.DrawLine(transform.position, centroMuro);
        }
    }
}