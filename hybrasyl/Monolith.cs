﻿/*
 * This file is part of Project Hybrasyl.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the Affero General Public License as published by
 * the Free Software Foundation, version 3.
 *
 * This program is distributed in the hope that it will be useful, but
 * without ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE. See the Affero General Public License
 * for more details.
 *
 * You should have received a copy of the Affero General Public License along
 * with this program. If not, see <http://www.gnu.org/licenses/>.
 *
 * (C) 2016 Project Hybrasyl (info@hybrasyl.com)
 *
 * Authors:   Michael Norris <norrismiv@gmail.com>
 *
 */
 
 using Hybrasyl.Objects;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
 using System.Threading;

namespace Hybrasyl
{
    //This class is defined to control mob spawning.
    //MUCH MUCH MORE LOGIC IS NEEDED, this is designed to just get us set up.
    internal class Monolith
    {
        public static readonly ILog Logger =
                LogManager.GetLogger(
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public List<Map> ControlMaps { get; set; }
        public int MaxSpawns { get; set; }
        
        public void Spawn()
        {
            foreach(var map in ControlMaps)
            {
                var SpawnList = new List<Monster>();
                var spawnMap = Game.World.Maps[map.Id];
                var monsterList = spawnMap.Objects.OfType<Monster>().ToList();
                var monsterCount = monsterList.Count;

                //assuming 3 monsters per map, what type of breakdown do we want? tier 1 = 50%, tier 2 = 35%, tier 3 = 15%?
                                

                if (monsterCount < MaxSpawns)
                {
                    var tier1mob = map.MapMonsters.Where(x => x.Value == 1).FirstOrDefault().Key;
                    var tier2mob = map.MapMonsters.Where(x => x.Value == 2).FirstOrDefault().Key;
                    var tier3mob = map.MapMonsters.Where(x => x.Value == 3).FirstOrDefault().Key;

                    var tier1cur = monsterList.Where(x => x.Name == tier1mob.Name).ToList().Count;
                    var tier2cur = monsterList.Where(x => x.Name == tier2mob.Name).ToList().Count;
                    var tier3cur = monsterList.Where(x => x.Name == tier3mob.Name).ToList().Count;



                    while (((tier1cur < 49)))
                    {
                        SpawnList.Add((Monster)tier1mob.Clone());
                        tier1cur += 1;
                    }
                    while (((tier2cur < 34)))
                    {
                        SpawnList.Add((Monster)tier2mob.Clone());
                        tier2cur += 1;
                    }
                    while (((tier3cur < 14)))
                    {
                        SpawnList.Add((Monster)tier3mob.Clone());
                        tier3cur += 1;
                    }

                    foreach (var mob in SpawnList)
                    {
                        var rand = new Random();
                        var xcoord = 0;
                        var ycoord = 0;
                        do
                        {
                            xcoord = rand.Next(1, 100);
                            ycoord = rand.Next(1, 100);
                        }
                        while (map.IsWall[xcoord, ycoord]);
                        

                        mob.X = (byte)xcoord;
                        mob.Y = (byte)ycoord;
                        mob.Id = Convert.ToUInt32(rand.Next(0, int.MaxValue-1));
                        Thread.Sleep(30);
                        SpawnMonster(mob, map.Id);
                    }

                    
                }
            }
        }

        private void SpawnMonster(Monster monster, ushort mapId)
        {
            Game.World.Maps[mapId].InsertCreature(monster);
            Logger.DebugFormat("Spawning monster: {0} at {1}, {2}", monster.Name, (int)monster.X, (int)monster.Y);
        }
    }
}
