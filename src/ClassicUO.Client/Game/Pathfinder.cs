#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Assets;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game
{
    public static class Pathfinder
    {
        private const int PATHFINDER_MAX_NODES = 150000;
        private static PathNode _goalNode = null;
        private static int _pathfindDistance;
        private static readonly PriorityQueue _openSet = new();
        private static readonly HashSet<(int x, int y, int z)> _closedSet = new();
        private static readonly List<PathNode> _path = new();
        private static int _pointIndex;
        private static bool _run;
        private static readonly int[] _offsetX =
        {
            0, 1, 1, 1, 0, -1, -1, -1, 0, 1
        };
        private static readonly int[] _offsetY =
        {
            -1, -1, 0, 1, 1, 1, 0, -1, -1, -1
        };
        private static readonly sbyte[] _dirOffset =
        {
            1, -1
        };
        private static Point _startPoint, _endPoint;

        private static int _endPointZ;
        private static readonly List<PathObject> _reusableList = new();

        public static Point StartPoint => _startPoint;
        public static Point EndPoint => _endPoint;
        public static int PathSize => _path.Count;

        public static bool AutoWalking { get; set; }

        public static bool PathFindingCanBeCancelled { get; set; }

        public static bool BlockMoving { get; set; }

        public static bool FastRotation { get; set; }


        private static bool CreateItemList(List<PathObject> list, int x, int y, int stepState)
        {
            GameObject tile = World.Map.GetTile(x, y, false);

            if (tile == null)
            {
                return false;
            }

            bool ignoreGameCharacters = ProfileManager.CurrentProfile.IgnoreStaminaCheck || stepState == (int)PATH_STEP_STATE.PSS_DEAD_OR_GM || World.Player.IgnoreCharacters || !(World.Player.Stamina < World.Player.StaminaMax && World.Map.Index == 0);

            bool isGM = World.Player.Graphic == 0x03DB;

            GameObject obj = tile;

            while (obj.TPrevious != null)
            {
                obj = obj.TPrevious;
            }

            for (; obj != null; obj = obj.TNext)
            {
                if (World.CustomHouseManager != null && obj.Z < World.Player.Z)
                {
                    continue;
                }

                ushort graphicHelper = obj.Graphic;

                switch (obj)
                {
                    case Land tile1:

                        if (graphicHelper < 0x01AE && graphicHelper != 2 || graphicHelper > 0x01B5 && graphicHelper != 0x01DB)
                        {
                            uint flags = (uint)PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE;

                            if (stepState == (int)PATH_STEP_STATE.PSS_ON_SEA_HORSE)
                            {
                                if (tile1.TileData.IsWet)
                                {
                                    flags = (uint)(PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE | PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE);
                                }
                            }
                            else
                            {
                                if (!tile1.TileData.IsImpassable)
                                {
                                    flags = (uint)(PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE | PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE);
                                }

                                if (stepState == (int)PATH_STEP_STATE.PSS_FLYING && tile1.TileData.IsNoDiagonal)
                                {
                                    flags |= (uint)PATH_OBJECT_FLAGS.POF_NO_DIAGONAL;
                                }
                            }

                            int landMinZ = tile1.MinZ;
                            int landAverageZ = tile1.AverageZ;
                            int landHeight = landAverageZ - landMinZ;

                            // TODO: Investigate reducing PathObject allocations here and below
                            list.Add
                            (
                                PathObject.Get
                                (
                                    flags,
                                    landMinZ,
                                    landAverageZ,
                                    landHeight,
                                    obj
                                )
                            );
                        }

                        break;

                    case GameEffect _: break;

                    default:
                        bool canBeAdd = true;
                        bool dropFlags = false;

                        switch (obj)
                        {
                            case Mobile mobile:
                                {
                                    if (!ignoreGameCharacters && !mobile.IsDead && !mobile.IgnoreCharacters)
                                    {
                                        list.Add
                                        (
                                            PathObject.Get
                                            (
                                                (uint)PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE,
                                                mobile.Z,
                                                mobile.Z + Constants.DEFAULT_CHARACTER_HEIGHT,
                                                Constants.DEFAULT_CHARACTER_HEIGHT,
                                                mobile
                                            )
                                        );
                                    }

                                    canBeAdd = false;

                                    break;
                                }

                            case Item item when item.IsMulti || item.ItemData.IsInternal:
                                {
                                    //canBeAdd = false;

                                    break;
                                }

                            case Item item2:
                                if (stepState == (int)PATH_STEP_STATE.PSS_DEAD_OR_GM && (item2.ItemData.IsDoor || item2.ItemData.Weight <= 0x5A || isGM && !item2.IsLocked))
                                {
                                    dropFlags = true;
                                }
                                else if (ProfileManager.CurrentProfile.SmoothDoors && item2.ItemData.IsDoor)
                                {
                                    dropFlags = true;
                                }
                                else
                                {
                                    dropFlags = graphicHelper >= 0x3946 && graphicHelper <= 0x3964 || graphicHelper == 0x0082;
                                }

                                break;

                            case Multi m:

                                if ((World.CustomHouseManager != null && m.IsCustom && (m.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_GENERIC_INTERNAL) == 0) || m.IsHousePreview)
                                {
                                    canBeAdd = false;
                                }

                                if ((m.State & CUSTOM_HOUSE_MULTI_OBJECT_FLAGS.CHMOF_IGNORE_IN_RENDER) != 0)
                                {
                                    dropFlags = true;
                                }

                                break;
                        }

                        if (canBeAdd)
                        {
                            uint flags = 0;

                            if (!(obj is Mobile))
                            {
                                var graphic = obj is Item it && it.IsMulti ? it.MultiGraphic : obj.Graphic;
                                ref StaticTiles itemdata = ref TileDataLoader.Instance.StaticData[graphic];

                                if (stepState == (int)PATH_STEP_STATE.PSS_ON_SEA_HORSE)
                                {
                                    if (itemdata.IsWet)
                                    {
                                        flags = (uint)(PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE);
                                    }
                                }
                                else
                                {
                                    if (itemdata.IsImpassable || itemdata.IsSurface)
                                    {
                                        flags = (uint)PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE;
                                    }

                                    if (!itemdata.IsImpassable)
                                    {
                                        if (itemdata.IsSurface)
                                        {
                                            flags |= (uint)PATH_OBJECT_FLAGS.POF_SURFACE;
                                        }

                                        if (itemdata.IsBridge)
                                        {
                                            flags |= (uint)PATH_OBJECT_FLAGS.POF_BRIDGE;
                                        }
                                    }

                                    if (stepState == (int)PATH_STEP_STATE.PSS_DEAD_OR_GM)
                                    {
                                        if (graphicHelper <= 0x0846)
                                        {
                                            if (!(graphicHelper != 0x0846 && graphicHelper != 0x0692 && (graphicHelper <= 0x06F4 || graphicHelper > 0x06F6)))
                                            {
                                                dropFlags = true;
                                            }
                                        }
                                        else if (graphicHelper == 0x0873)
                                        {
                                            dropFlags = true;
                                        }
                                    }

                                    if (dropFlags)
                                    {
                                        flags &= 0xFFFFFFFE;
                                    }

                                    if (stepState == (int)PATH_STEP_STATE.PSS_FLYING && itemdata.IsNoDiagonal)
                                    {
                                        flags |= (uint)PATH_OBJECT_FLAGS.POF_NO_DIAGONAL;
                                    }
                                }

                                if (flags != 0)
                                {
                                    int objZ = obj.Z;
                                    int staticHeight = itemdata.Height;
                                    int staticAverageZ = staticHeight;

                                    if (itemdata.IsBridge)
                                    {
                                        staticAverageZ /= 2;
                                        // revert fix from fwiffo because it causes unwalkable stairs [down --> up]
                                        //staticAverageZ += staticHeight % 2;
                                    }

                                    list.Add
                                    (
                                        PathObject.Get
                                        (
                                            flags,
                                            objZ,
                                            staticAverageZ + objZ,
                                            staticHeight,
                                            obj
                                        )
                                    );
                                }
                            }
                        }

                        break;
                }
            }

            return list.Count != 0;
        }

        private static int CalculateMinMaxZ
        (
            ref int minZ,
            ref int maxZ,
            int newX,
            int newY,
            int currentZ,
            int newDirection,
            int stepState
        )
        {
            minZ = -128;
            maxZ = currentZ;
            newDirection &= 7;
            int direction = newDirection ^ 4;
            newX += _offsetX[direction];
            newY += _offsetY[direction];

            foreach (PathObject o in _reusableList)
            {
                o.Return();
            }
            _reusableList.Clear();

            if (!CreateItemList(_reusableList, newX, newY, stepState) || _reusableList.Count == 0)
            {
                return 0;
            }

            foreach (PathObject obj in _reusableList)
            {
                GameObject o = obj.Object;
                int averageZ = obj.AverageZ;

                if (averageZ <= currentZ && o is Land tile && tile.IsStretched)
                {
                    int avgZ = tile.CalculateCurrentAverageZ(newDirection);

                    if (minZ < avgZ)
                    {
                        minZ = avgZ;
                    }

                    if (maxZ < avgZ)
                    {
                        maxZ = avgZ;
                    }
                }
                else
                {
                    if ((obj.Flags & (uint)PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE) != 0 && averageZ <= currentZ && minZ < averageZ)
                    {
                        minZ = averageZ;
                    }

                    if ((obj.Flags & (uint)PATH_OBJECT_FLAGS.POF_BRIDGE) != 0 && currentZ == averageZ)
                    {
                        int z = obj.Z;
                        int height = z + obj.Height;

                        if (maxZ < height)
                        {
                            maxZ = height;
                        }

                        if (minZ > z)
                        {
                            minZ = z;
                        }
                    }
                }
            }

            maxZ += 2;

            return maxZ;
        }

        public static bool CalculateNewZ(int x, int y, ref sbyte z, int direction)
        {
            int stepState = (int)PATH_STEP_STATE.PSS_NORMAL;

            if (World.Player.IsDead || World.Player.Graphic == 0x03DB)
            {
                stepState = (int)PATH_STEP_STATE.PSS_DEAD_OR_GM;
            }
            else
            {
                if (World.Player.IsGargoyle && World.Player.IsFlying)
                {
                    stepState = (int)PATH_STEP_STATE.PSS_FLYING;
                }
                else
                {
                    Item mount = World.Player.FindItemByLayer(Layer.Mount);

                    if (mount != null && mount.Graphic == 0x3EB3) // sea horse
                    {
                        stepState = (int)PATH_STEP_STATE.PSS_ON_SEA_HORSE;
                    }
                }
            }

            int minZ = -128;
            int maxZ = z;

            CalculateMinMaxZ
            (
                ref minZ,
                ref maxZ,
                x,
                y,
                z,
                direction,
                stepState
            );

            foreach (PathObject o in _reusableList)
            {
                o.Return();
            }
            _reusableList.Clear();

            if (World.CustomHouseManager != null)
            {
                Rectangle rect = new Rectangle(World.CustomHouseManager.StartPos.X, World.CustomHouseManager.StartPos.Y, World.CustomHouseManager.EndPos.X, World.CustomHouseManager.EndPos.Y);

                if (!rect.Contains(x, y))
                {
                    return false;
                }
            }

            if (!CreateItemList(_reusableList, x, y, stepState) || _reusableList.Count == 0)
            {
                return false;
            }

            _reusableList.Sort();

            _reusableList.Add
            (
                PathObject.Get
                (
                    (uint)PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE,
                    128,
                    128,
                    128,
                    null
                )
            );

            int resultZ = -128;

            if (z < minZ)
            {
                z = (sbyte)minZ;
            }

            int currentTempObjZ = 1000000;
            int currentZ = -128;

            for (int i = 0; i < _reusableList.Count; i++)
            {
                PathObject obj = _reusableList[i];

                if ((obj.Flags & (uint)PATH_OBJECT_FLAGS.POF_NO_DIAGONAL) != 0 && stepState == (int)PATH_STEP_STATE.PSS_FLYING)
                {
                    int objAverageZ = obj.AverageZ;
                    int delta = Math.Abs(objAverageZ - z);

                    if (delta <= 25)
                    {
                        resultZ = objAverageZ != -128 ? objAverageZ : currentZ;

                        break;
                    }
                }

                if ((obj.Flags & (uint)PATH_OBJECT_FLAGS.POF_IMPASSABLE_OR_SURFACE) != 0)
                {
                    int objZ = obj.Z;

                    if (objZ - minZ >= Constants.DEFAULT_BLOCK_HEIGHT)
                    {
                        for (int j = i - 1; j >= 0; j--)
                        {
                            PathObject tempObj = _reusableList[j];

                            if ((tempObj.Flags & (uint)(PATH_OBJECT_FLAGS.POF_SURFACE | PATH_OBJECT_FLAGS.POF_BRIDGE)) != 0)
                            {
                                int tempAverageZ = tempObj.AverageZ;

                                if (tempAverageZ >= currentZ && objZ - tempAverageZ >= Constants.DEFAULT_BLOCK_HEIGHT && (tempAverageZ <= maxZ && (tempObj.Flags & (uint)PATH_OBJECT_FLAGS.POF_SURFACE) != 0 || (tempObj.Flags & (uint)PATH_OBJECT_FLAGS.POF_BRIDGE) != 0 && tempObj.Z <= maxZ))
                                {
                                    int delta = Math.Abs(z - tempAverageZ);

                                    if (delta < currentTempObjZ)
                                    {
                                        currentTempObjZ = delta;
                                        resultZ = tempAverageZ;
                                    }
                                }
                            }
                        }
                    }

                    int averageZ = obj.AverageZ;

                    if (minZ < averageZ)
                    {
                        minZ = averageZ;
                    }

                    if (currentZ < averageZ)
                    {
                        currentZ = averageZ;
                    }
                }
            }

            z = (sbyte)resultZ;

            return resultZ != -128;
        }

        public static void GetNewXY(byte direction, ref int x, ref int y)
        {
            switch (direction & 7)
            {
                case 0:

                    {
                        y--;

                        break;
                    }

                case 1:

                    {
                        x++;
                        y--;

                        break;
                    }

                case 2:

                    {
                        x++;

                        break;
                    }

                case 3:

                    {
                        x++;
                        y++;

                        break;
                    }

                case 4:

                    {
                        y++;

                        break;
                    }

                case 5:

                    {
                        x--;
                        y++;

                        break;
                    }

                case 6:

                    {
                        x--;

                        break;
                    }

                case 7:

                    {
                        x--;
                        y--;

                        break;
                    }
            }
        }

        public static bool CanWalk(ref Direction direction, ref int x, ref int y, ref sbyte z)
        {
            int newX = x;
            int newY = y;
            sbyte newZ = z;
            byte newDirection = (byte)direction;
            GetNewXY((byte)direction, ref newX, ref newY);
            bool passed = CalculateNewZ(newX, newY, ref newZ, (byte)direction);

            if ((sbyte)direction % 2 != 0)
            {
                if (passed)
                {
                    for (int i = 0; i < 2 && passed; i++)
                    {
                        int testX = x;
                        int testY = y;
                        sbyte testZ = z;
                        byte testDir = (byte)(((byte)direction + _dirOffset[i]) % 8);
                        GetNewXY(testDir, ref testX, ref testY);
                        passed = CalculateNewZ(testX, testY, ref testZ, testDir);
                    }
                }

                if (!passed)
                {
                    for (int i = 0; i < 2 && !passed; i++)
                    {
                        newX = x;
                        newY = y;
                        newZ = z;
                        newDirection = (byte)(((byte)direction + _dirOffset[i]) % 8);
                        GetNewXY(newDirection, ref newX, ref newY);
                        passed = CalculateNewZ(newX, newY, ref newZ, newDirection);
                    }
                }
            }

            if (passed)
            {
                x = newX;
                y = newY;
                z = newZ;
                direction = (Direction)newDirection;
            }

            return passed;
        }

        public static bool CanWalkObstacules(ref Direction direction, ref int x, ref int y, ref sbyte z)
        {
            int newX = x;
            int newY = y;
            sbyte newZ = z;
            byte newDirection = (byte)direction;
            GetNewXY((byte)direction, ref newX, ref newY);
            bool passed = CalculateNewZ(newX, newY, ref newZ, (byte)direction);

            if ((sbyte)direction % 2 != 0)
            {
                if (passed)
                {
                    for (int i = 0; i < 2 && passed; i++)
                    {
                        int testX = x;
                        int testY = y;
                        sbyte testZ = z;
                        byte testDir = (byte)(((byte)direction + _dirOffset[i]) % 8);
                        GetNewXY(testDir, ref testX, ref testY);
                        passed = CalculateNewZ(testX, testY, ref testZ, testDir);
                    }
                }

                if (!passed)
                {
                    for (int i = 0; i < 2 && !passed; i++)
                    {
                        newX = x;
                        newY = y;
                        newZ = z;
                        newDirection = (byte)(((byte)direction + _dirOffset[i]) % 8);
                        GetNewXY(newDirection, ref newX, ref newY);
                        passed = CalculateNewZ(newX, newY, ref newZ, newDirection);
                    }
                }
            }

            if (passed)
            {
                x = newX;
                y = newY;
                z = newZ;
                direction = (Direction)newDirection;
            }

            return passed;
        }

        private static int GetGoalDistCost(Point point, int cost)
        {
            //return (Math.Abs(_endPoint.X - point.X) + Math.Abs(_endPoint.Y - point.Y)) * cost;
            return Math.Max(Math.Abs(_endPoint.X - point.X), Math.Abs(_endPoint.Y - point.Y));
        }

        private static bool AddNodeToList(int direction, int x, int y, int z, PathNode parent, int cost)
        {
            if (_closedSet.Contains((x, y, z)))
            {
                return false;
            }

            int newDistFromStart = parent.DistFromStartCost + cost + Math.Abs(z - parent.Z);

            var updatedNode = PathNode.Get();

            updatedNode.X = x;
            updatedNode.Y = y;
            updatedNode.Z = z;
            updatedNode.Direction = direction;
            updatedNode.Parent = parent;
            updatedNode.DistFromStartCost = newDistFromStart;
            updatedNode.DistFromGoalCost = GetGoalDistCost(new Point(x, y), cost); 
            updatedNode.Cost = updatedNode.DistFromStartCost + updatedNode.DistFromGoalCost;
            
            if (_openSet.Contains(x, y, z))
            {
                // Since tile is already in the open list, we enqueue the better option that
                // has a lower cost (existing one will be ignored later by PriorityQueue impl)
                updatedNode.X = x;
                updatedNode.Y = y;
                updatedNode.Z = z;
                updatedNode.Direction = direction;
                updatedNode.Parent = parent;
                updatedNode.DistFromStartCost = newDistFromStart;
                updatedNode.DistFromGoalCost = GetGoalDistCost(new Point(x, y), cost); 
                updatedNode.Cost = updatedNode.DistFromStartCost + updatedNode.DistFromGoalCost;
                
                _openSet.Enqueue(updatedNode, updatedNode.Cost);
                return false;
            }
            else
            {
                updatedNode.X = x;
                updatedNode.Y = y;
                updatedNode.Z = z;
                updatedNode.Direction = direction;
                updatedNode.Parent = parent;
                updatedNode.DistFromStartCost = newDistFromStart;
                updatedNode.DistFromGoalCost = GetGoalDistCost(new Point(x, y), cost); 
                updatedNode.Cost = updatedNode.DistFromStartCost + updatedNode.DistFromGoalCost;

                _openSet.Enqueue(updatedNode, updatedNode.Cost);

                if (MathHelper.GetDistance(_endPoint, new Point(x, y)) <= _pathfindDistance &&
                    Math.Abs(_endPointZ - z) < Constants.ALLOWED_Z_DIFFERENCE)
                {
                    _goalNode = updatedNode;
                }

                return true;
            }
        }

        private static bool OpenNodes(PathNode node)
        {
            bool found = false;

            for (int i = 0; i < 8; i++)
            {
                Direction direction = (Direction)i;
                int x = node.X;
                int y = node.Y;
                sbyte z = (sbyte)node.Z;
                Direction oldDirection = direction;

                if (CanWalk(ref direction, ref x, ref y, ref z))
                {
                    if (direction != oldDirection)
                    {
                        continue;
                    }

                    int diagonal = i % 2;

                    if (diagonal != 0)
                    {
                        Direction wantDirection = (Direction)i;
                        int wantX = node.X;
                        int wantY = node.Y;
                        GetNewXY((byte)wantDirection, ref wantX, ref wantY);

                        if (x != wantX || y != wantY)
                        {
                            diagonal = -1;
                        }
                    }

                    if (diagonal >= 0)
                    {
                        int cost = (diagonal == 0) ? 1 : 2;

                        if (AddNodeToList((int)direction, x, y, z, node, cost))
                        {
                            found = true;
                        }
                    }
                }
            }
            
            //node.Return();

            return found;
        }

        private static PathNode FindCheapestNode()
        {
            while (!_openSet.IsEmpty())
            {
                var node = _openSet.Dequeue();
                var key = (node.X, node.Y, node.Z);

                if (_closedSet.Contains(key))
                {
                    // Skip already processed nodes (e.g., old duplicates)
                    node?.Return();
                    continue;
                }

                _closedSet.Add(key);
                return node;
            }

            return null;
        }

        private static bool FindPath(int maxNodes)
        {
            _openSet.Clear();
            _closedSet.Clear();
            _goalNode = null;

            var startNode = PathNode.Get();

            startNode.X = _startPoint.X;
            startNode.Y = _startPoint.Y;
            startNode.Z = World.Player.Z;
            startNode.Parent = null;
            startNode.DistFromStartCost = 0;
            
            var startPoint = new Point(_startPoint.X, _startPoint.Y);
            startNode.DistFromGoalCost = GetGoalDistCost(startPoint, 0);
            startNode.Cost = startNode.DistFromGoalCost;

            _openSet.Enqueue(startNode, startNode.Cost);

            int closedNodesCount = 0;

            if (startNode.DistFromGoalCost > 14)
            {
                _run = true;
            }

            while (AutoWalking)
            {
                var currentNode = FindCheapestNode();

                if (currentNode == null)
                {
                    return false;
                }

                closedNodesCount++;

                if (closedNodesCount >= maxNodes)
                {
                    currentNode.Return();
                    return false;
                }

                if (_goalNode is not null)
                {
                    ReconstructPath(_goalNode);
                    return true;
                }

                OpenNodes(currentNode);
            }

            return false;
        }

        private static void ReconstructPath(PathNode goalNode)
        {
            var pathStack = new Stack<PathNode>();
            var current = goalNode;
            while (current is not null && current != current.Parent)
            {
                pathStack.Push(current);
                current = current.Parent;
            }
            
            _path.Clear();
            while (pathStack.Count > 0)
            {
                _path.Add(pathStack.Pop());
            }
        }

        public static bool WalkTo(int x, int y, int z, int distance)
        {
            if (World.Player == null /*|| World.Player.Stamina == 0*/ || World.Player.IsParalyzed)
            {
                return false;
            }

            EventSink.InvokeOnPathFinding(null, new Vector4(x, y, z, distance));

            _path.Clear();
            _pointIndex = 0;
            _goalNode = null;
            _run = false;
            _startPoint.X = World.Player.X;
            _startPoint.Y = World.Player.Y;
            _endPoint.X = x;
            _endPoint.Y = y;
            _endPointZ = z;
            _pathfindDistance = distance;
            AutoWalking = true;

            if (FindPath(PATHFINDER_MAX_NODES))
            {
                _pointIndex = 1;
                ProcessAutoWalk();
            }
            else
            {
                AutoWalking = false;
            }

            return _path.Count != 0;
        }

        public static void ProcessAutoWalk()
        {
            if (AutoWalking && World.InGame && World.Player.Walker.StepsCount < Constants.MAX_STEP_COUNT && World.Player.Walker.LastStepRequestTime <= Time.Ticks)
            {
                if (_pointIndex >= 0 && _pointIndex < _path.Count)
                {
                    PathNode p = _path[_pointIndex];

                    World.Player.GetEndPosition(out int x, out int y, out sbyte z, out Direction dir);

                    if (dir == (Direction)p.Direction)
                    {
                        _pointIndex++;
                    }

                    if (!World.Player.Walk((Direction)p.Direction, _run))
                    {
                        StopAutoWalk();
                    }
                }
                else
                {
                    StopAutoWalk();
                }
            }
        }

        public static void StopAutoWalk()
        {
            AutoWalking = false;
            _run = false;
            _path.Clear();
        }

        private enum PATH_STEP_STATE
        {
            PSS_NORMAL = 0,
            PSS_DEAD_OR_GM,
            PSS_ON_SEA_HORSE,
            PSS_FLYING
        }

        [Flags]
        private enum PATH_OBJECT_FLAGS : uint
        {
            POF_IMPASSABLE_OR_SURFACE = 0x00000001,
            POF_SURFACE = 0x00000002,
            POF_BRIDGE = 0x00000004,
            POF_NO_DIAGONAL = 0x00000008
        }

        private class PathObject : IComparable<PathObject>
        {
            private static ObjectPool<PathObject> _pool = new ObjectPool<PathObject>(
                ()=> new PathObject(0, 0, 0, 0, null), (po) =>
                {
                    po.Flags = 0;
                    po.Z = 0;
                    po.AverageZ = 0;
                    po.Height = 0;
                    po.Object = null;
                }, 
                15
                );
            private PathObject(uint flags, int z, int avgZ, int h, GameObject obj)
            {
                Flags = flags;
                Z = z;
                AverageZ = avgZ;
                Height = h;
                Object = obj;
            }

            public static PathObject Get(uint flags, int z, int avgZ, int h, GameObject obj)
            {
                var po = _pool.Get();
                po.Flags = flags;
                po.Z = z;
                po.AverageZ = avgZ;
                po.Height = h;
                po.Object = obj;
                return po;
            }

            public void Return()
            {
                _pool.Return(this);
            }
            
            public uint Flags { get; private set; }

            public int Z { get; private set; }

            public int AverageZ { get; private set; }

            public int Height { get; private set; }

            public GameObject Object { get; private set; }

            public int CompareTo(PathObject other)
            {
                int comparision = Z - other.Z;

                if (comparision == 0)
                {
                    comparision = Height - other.Height;
                }

                return comparision;
            }
        }

        private class PathNode
        {
            private static ObjectPool<PathNode> _pool = new(
                ()=>new PathNode(), 
                (pn) => {pn.Reset();},
                15
                );

            private PathNode()
            {
            }

            public static PathNode Get()
            {
                return _pool.Get();
            }

            public void Return()
            {
                _pool.Return(this);
            }

            public int X { get; set; }

            public int Y { get; set; }

            public int Z { get; set; }

            public int Direction { get; set; }

            public bool Used { get; set; }

            public int Cost { get; set; }

            public int DistFromStartCost { get; set; }

            public int DistFromGoalCost { get; set; }

            public PathNode Parent { get; set; }

            public void Reset()
            {
                Parent = null;
                Used = false;
                X = Y = Z = Direction = Cost = DistFromGoalCost = DistFromStartCost = 0;
            }
        }

        class PriorityQueue
        {
            class QueueNode
            {
                internal PathNode Node;
                internal int Priority;
                internal bool IsValid;

                internal QueueNode(PathNode node, int priority)
                {
                    Node = node;
                    Priority = priority;
                    IsValid = true;
                }
            }

            readonly List<QueueNode> _heap = new();
            readonly Dictionary<(int, int, int), QueueNode> _nodeLookup = new();

            internal bool Contains(int x, int y, int z) => _nodeLookup.ContainsKey((x, y, z));

            internal bool Contains(PathNode node)
            {
                if (_nodeLookup.TryGetValue(GetKey(node), out QueueNode queuenode))
                {
                    // The priority queue lazily remove duplicates, so we check
                    // whether the node is valid here.
                    return queuenode.IsValid;
                }

                return false;
            }

            internal void Clear()
            {
                foreach (QueueNode node in _heap)
                {
                    node.Node?.Return();
                }
                _heap.Clear();
                _nodeLookup.Clear();
            }

            internal bool IsEmpty()
            {
                while (_heap.Count > 0)
                {
                    // The priority queue lazily remove duplicates, so we check
                    // for them here. If should be removed lazily, remove it now
                    // and continue to next element.
                    if (_heap[0].IsValid)
                    {
                        return false;
                    }

                    RemoveAt(0);
                }

                return true;
            }

            internal void Enqueue(PathNode node, int priority)
            {
                var key = GetKey(node);
                if (_nodeLookup.TryGetValue(key, out QueueNode existing))
                {
                    if (existing.IsValid && existing.Priority <= priority)
                    {
                        // Existing priority is better or equal, so ignore
                        return;
                    }

                    // The priority queue lazily remove duplicates, so we mark existing to be deleted later.
                    existing.IsValid = false;
                }

                var qNode = new QueueNode(node, priority);
                _heap.Add(qNode);
                int index = _heap.Count - 1;
                _nodeLookup[key] = qNode;
                HeapifyUp(index);
            }

            internal PathNode Dequeue()
            {
                while (_heap.Count > 0)
                {
                    // The priority queue lazily remove duplicates, so we check
                    // for them here. If should be removed lazily, remove it now
                    // and continue to next element.
                    var top = _heap[0];
                    if (!top.IsValid)
                    {
                        RemoveAt(0);
                        continue;
                    }

                    RemoveAt(0);
                    _nodeLookup.Remove(GetKey(top.Node));
                    return top.Node;
                }

                return null;
            }

            void Swap(int i, int j)
            {
                (_heap[j], _heap[i]) = (_heap[i], _heap[j]);
            }

            void HeapifyUp(int index)
            {
                while (index > 0)
                {
                    int parent = (index - 1) / 2;
                    if (_heap[index].Priority < _heap[parent].Priority)
                    {
                        Swap(index, parent);
                        index = parent;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            void HeapifyDown(int index)
            {
                int lastIndex = _heap.Count - 1;
                while (true)
                {
                    int left = index * 2 + 1;
                    int right = index * 2 + 2;
                    int smallest = index;

                    if (left <= lastIndex && _heap[left].Priority < _heap[smallest].Priority)
                    {
                        smallest = left;
                    }

                    if (right <= lastIndex && _heap[right].Priority < _heap[smallest].Priority)
                    {
                        smallest = right;
                    }

                    if (smallest != index)
                    {
                        Swap(index, smallest);
                        index = smallest;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            (int, int, int) GetKey(PathNode node)
            {
                return (node.X, node.Y, node.Z);
            }

            void RemoveAt(int index)
            {
                int lastIndex = _heap.Count - 1;
                var keyToRemove = GetKey(_heap[index].Node);
                _nodeLookup.Remove(keyToRemove);
                if (index != lastIndex)
                {
                    Swap(index, lastIndex);
                }

                _heap.RemoveAt(lastIndex);

                if (index < _heap.Count)
                {
                    HeapifyDown(index);
                    HeapifyUp(index);
                }
            }
        }
    }
}