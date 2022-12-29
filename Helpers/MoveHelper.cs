﻿using robotManager.Helpful;
using System.Collections.Generic;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeDungeonCrawler.Helpers
{
    internal class MoveHelper
    {
        public static List<(Vector3 a, Vector3 b)> GetLinesOnPath(List<Vector3> path, int maxDistance = 50)
        {
            List<(Vector3 a, Vector3 b)> result = new List<(Vector3, Vector3)>();
            Vector3 nextNode = MovementManager.CurrentMoveTo;
            Vector3 myPos = ObjectManager.Me.Position;
            bool nextNodeFound = false;
            float lineToCHeckDistance = 0;

            for (int i = 0; i < path.Count; i++)
            {
                // break on last node unless it's the only node
                if (i >= path.Count - 1 && result.Count > 0)
                {
                    break;
                }

                // skip nodes behind me
                if (!nextNodeFound)
                {
                    if (path[i] != nextNode)
                    {
                        continue;
                    }
                    nextNodeFound = true;
                }

                // Ignore if too far
                if (result.Count > 2 && lineToCHeckDistance > maxDistance)
                {
                    break;
                }

                // Path ahead of me
                if (result.Count <= 0)
                {
                    result.Add((myPos, path[i]));
                    lineToCHeckDistance += myPos.DistanceTo(path[i]);
                    if (path.Count > i + 1)
                    {
                        result.Add((path[i], path[i + 1]));
                        lineToCHeckDistance += path[i].DistanceTo(path[i + 1]);
                    }
                }
                else
                {
                    result.Add((path[i], path[i + 1]));
                    lineToCHeckDistance += path[i].DistanceTo(path[i + 1]);
                }
            }

            return result;
        }

        // Gets X neighboring nodes on a path
        public static List<Vector3> GetNodesAround(List<Vector3> path, Vector3 node, int nodeAmount = 5)
        {
            List<Vector3> result = new List<Vector3>();
            int nodeIndex = path.IndexOf(node);
            // before node
            for (int i = -nodeAmount; i < 0; i++)
            {
                if (nodeIndex + i > 0)
                {
                    result.Add(path[nodeIndex + i]);
                }
            }
            // node itself
            result.Add(node);
            // after node
            for (int i = 1; i <= nodeAmount; i++)
            {
                if (nodeIndex + i < path.Count)
                {
                    result.Add(path[nodeIndex + i]);
                }
            }
            return result;
        }
    }
}
